/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BoogieAST;
using WasmToBoogie.Parser.Ast;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private readonly string contractName;
        private int labelCounter = 0;

        // Flag: assertion de pile à la sortie de fonction
        public bool EnableFooterStackAssert { get; set; } = false;

        // Module Boogie en construction
        private BoogieProgram? program;

        // Générateur unique pour popArgsN
        private readonly HashSet<int> popArgsMade = new();

        // NEW: generator unique pour popDiscardN
        private readonly HashSet<int> popDiscardMade = new();

        // État par fonction
        private List<BoogieIdentifierExpr>? currentLocalMap; // arg1..argN, loc1..locM
        private WasmFunction? currentFunction;
        private HashSet<string>? neededLoopStartLabels;
        private HashSet<string>? neededBlockEndLabels;
        private readonly Stack<LabelContext> labelStack = new();
        private string? functionExitLabel;

        // NEW: globals created lazily + modifies tracking
        private readonly Dictionary<string, string> globalNameMap = new(StringComparer.Ordinal);
        private readonly HashSet<string> declaredBoogieGlobals = new(StringComparer.Ordinal);
        private HashSet<string>? currentModifiedGlobals;

        private class LabelContext
        {
            public string? WatLabel;
            public string? StartLabel; // loop "continue"
            public string EndLabel = ""; // block/loop end ("break")
            public bool IsLoop;
            public bool MarkEndUsed;
        }

        public WasmAstToBoogie(string contractName) => this.contractName = contractName;

        // ======== Helpers ========

        private static bool AllDigits(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            for (int i = 0; i < s.Length; i++)
                if (s[i] < '0' || s[i] > '9')
                    return false;
            return true;
        }

        private static string NormalizeLabel(string raw) =>
            string.IsNullOrEmpty(raw) ? raw : (raw[0] == '$' ? raw[1..] : raw);

        private static string MapCalleeName(string target)
        {
            if (string.IsNullOrEmpty(target))
                return target;
            string name = target[0] == '$' ? target[1..] : target;
            return AllDigits(name) ? "func_" + name : name;
        }

        private static string SanitizeFunctionName(string? watName, string contractName)
        {
            if (!string.IsNullOrEmpty(watName))
            {
                var n = watName![0] == '$' ? watName.Substring(1) : watName;
                if (int.TryParse(n, out _))
                    return $"func_{n}";
                n = Regex.Replace(n, @"[^A-Za-z0-9_]", "_");
                if (!char.IsLetter(n[0]) && n[0] != '_')
                    n = "_" + n;
                return n;
            }
            return $"func_{contractName}";
        }

        private string GenerateLabel(string baseName) => $"{baseName}_{++labelCounter}";

        // NEW: sanitize global name for Boogie
        private static string SanitizeGlobalName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "g";
            var n = raw[0] == '$' ? raw.Substring(1) : raw;
            n = Regex.Replace(n, @"[^A-Za-z0-9_]", "_");
            if (!char.IsLetter(n[0]) && n[0] != '_')
                n = "_" + n;
            // prefix to avoid clash
            return "g_" + n;
        }

        private string ResolveGlobalKey(int? index, string? name)
        {
            if (!string.IsNullOrEmpty(name))
                return name!;
            if (index.HasValue)
                return index.Value.ToString();
            throw new NotSupportedException("Unknown global index/name");
        }

        // Ensure Boogie global exists; returns Boogie identifier name
        private string EnsureGlobalVar(string watNameOrIndex)
        {
            // normalize key to always start with '$' so "$g" and "g" don't diverge
            string key = watNameOrIndex.StartsWith("$", StringComparison.Ordinal)
                ? watNameOrIndex
                : "$" + watNameOrIndex;

            if (!globalNameMap.TryGetValue(key, out var boogieName))
            {
                // keep raw key to generate stable sanitized name (e.g., "$g" -> "g_g", "$0" -> "g_0")
                boogieName = SanitizeGlobalName(key);
                globalNameMap[key] = boogieName;
            }

            // declare once lazily
            if (program != null && !declaredBoogieGlobals.Contains(boogieName))
            {
                program.Declarations.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent(boogieName, BoogieType.Real))
                );
                declaredBoogieGlobals.Add(boogieName);
            }

            return boogieName;
        }

        // Résolution d'une cible de branchement (label texte ou profondeur numérique)
        private string ResolveBranchTarget(string labOrDepth)
        {
            if (AllDigits(labOrDepth))
            {
                int depth = int.Parse(labOrDepth);
                if (depth < 0 || depth >= labelStack.Count)
                    return functionExitLabel ?? (functionExitLabel = GenerateLabel("func_exit"));

                var arr = labelStack.ToArray(); // top -> bottom
                var ctx = arr[depth];

                ctx.MarkEndUsed = true;

                return ctx.IsLoop ? (ctx.StartLabel ?? ctx.EndLabel) : ctx.EndLabel;
            }

            var norm = NormalizeLabel(labOrDepth);
            var hit = labelStack.FirstOrDefault(c => c.WatLabel == norm);
            if (hit != null)
                return hit.IsLoop ? (hit.StartLabel ?? hit.EndLabel) : hit.EndLabel;

            return functionExitLabel ?? (functionExitLabel = GenerateLabel("func_exit"));
        }

        // ======== Entrée publique ========

        public BoogieProgram Convert(WasmModule wasmModule)
        {
            var program = new BoogieProgram();
            this.program = program;

            AddPrelude(program);

            foreach (var func in wasmModule.Functions)
            {
                var (proc, impl) = TranslateFunction(func);
                program.Declarations.Add(proc);
                program.Declarations.Add(impl);
            }

            return program;
        }

        private void AddMemoryWriteProcedures(BoogieProgram program)
        {
            var memModSet = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(
                    new BoogieTypedIdent("$mem", new BoogieMapType(BoogieType.Int, BoogieType.Int))
                ),
            };
            // Helper local: create byte extractor function byte<i>_<w>(x) with range axiom
            void AddByteExtractor(string name)
            {
                var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int));
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Int));
                program.Declarations.Add(new BoogieFunction(name, new() { x }, new() { r }));

                var xx = new BoogieIdentifierExpr("x");
                var fx = new BoogieFunctionCall(name, new() { xx });
                var ge0 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieLiteralExpr(0),
                    fx
                );
                var lt256 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LT,
                    fx,
                    new BoogieLiteralExpr(256)
                );
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new() { xx },
                            new() { BoogieType.Int },
                            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, ge0, lt256)
                        )
                    )
                );
            }

            // Declare extractors (abstract) for 16/32/64
            AddByteExtractor("byte0_16");
            AddByteExtractor("byte1_16");

            AddByteExtractor("byte0_32");
            AddByteExtractor("byte1_32");
            AddByteExtractor("byte2_32");
            AddByteExtractor("byte3_32");

            AddByteExtractor("byte0_64");
            AddByteExtractor("byte1_64");
            AddByteExtractor("byte2_64");
            AddByteExtractor("byte3_64");
            AddByteExtractor("byte4_64");
            AddByteExtractor("byte5_64");
            AddByteExtractor("byte6_64");
            AddByteExtractor("byte7_64");

            // procedure {:inline true} mem_write_u8(a:int, v:int)
            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("a", BoogieType.Int)),
                    new BoogieFormalParam(new BoogieTypedIdent("v", BoogieType.Int)),
                };

                var proc = new BoogieProcedure(
                    "mem_write_u8",
                    ins,
                    new(), // no outputs
                    new() { new BoogieAttribute("inline", true) },
                    memModSet, // ✅ modifies $mem
                    new(), // ✅ no requires
                    new()
                );
                program.Declarations.Add(proc);

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieMapSelect(
                            new BoogieIdentifierExpr("$mem"),
                            new BoogieIdentifierExpr("a")
                        ),
                        new BoogieFunctionCall("to_u8", new() { new BoogieIdentifierExpr("v") })
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation("mem_write_u8", ins, new(), new(), body)
                );
            }

            // mem_write_u16(a, v): little endian
            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("a", BoogieType.Int)),
                    new BoogieFormalParam(new BoogieTypedIdent("v", BoogieType.Int)),
                };

                var proc = new BoogieProcedure(
                    "mem_write_u16",
                    ins,
                    new(),
                    new() { new BoogieAttribute("inline", true) },
                    memModSet, // ✅
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var body = new BoogieStmtList();

                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_write_u8",
                        new()
                        {
                            new BoogieIdentifierExpr("a"),
                            new BoogieFunctionCall(
                                "byte0_16",
                                new() { new BoogieIdentifierExpr("v") }
                            ),
                        },
                        new()
                    )
                );

                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_write_u8",
                        new()
                        {
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.ADD,
                                new BoogieIdentifierExpr("a"),
                                new BoogieLiteralExpr(1)
                            ),
                            new BoogieFunctionCall(
                                "byte1_16",
                                new() { new BoogieIdentifierExpr("v") }
                            ),
                        },
                        new()
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation("mem_write_u16", ins, new(), new(), body)
                );
            }

            // mem_write_u32
            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("a", BoogieType.Int)),
                    new BoogieFormalParam(new BoogieTypedIdent("v", BoogieType.Int)),
                };

                var proc = new BoogieProcedure(
                    "mem_write_u32",
                    ins,
                    new(),
                    new() { new BoogieAttribute("inline", true) },
                    memModSet, // ✅ modifies $mem
                    new(), // ✅ requires vide
                    new()
                );
                program.Declarations.Add(proc);

                var body = new BoogieStmtList();

                BoogieExpr a0 = new BoogieIdentifierExpr("a");
                BoogieExpr a1 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.ADD,
                    a0,
                    new BoogieLiteralExpr(1)
                );
                BoogieExpr a2 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.ADD,
                    a0,
                    new BoogieLiteralExpr(2)
                );
                BoogieExpr a3 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.ADD,
                    a0,
                    new BoogieLiteralExpr(3)
                );

                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_write_u8",
                        new()
                        {
                            a0,
                            new BoogieFunctionCall(
                                "byte0_32",
                                new() { new BoogieIdentifierExpr("v") }
                            ),
                        },
                        new()
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_write_u8",
                        new()
                        {
                            a1,
                            new BoogieFunctionCall(
                                "byte1_32",
                                new() { new BoogieIdentifierExpr("v") }
                            ),
                        },
                        new()
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_write_u8",
                        new()
                        {
                            a2,
                            new BoogieFunctionCall(
                                "byte2_32",
                                new() { new BoogieIdentifierExpr("v") }
                            ),
                        },
                        new()
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_write_u8",
                        new()
                        {
                            a3,
                            new BoogieFunctionCall(
                                "byte3_32",
                                new() { new BoogieIdentifierExpr("v") }
                            ),
                        },
                        new()
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation("mem_write_u32", ins, new(), new(), body)
                );
            }

            // mem_write_u64
            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("a", BoogieType.Int)),
                    new BoogieFormalParam(new BoogieTypedIdent("v", BoogieType.Int)),
                };

                var proc = new BoogieProcedure(
                    "mem_write_u64",
                    ins,
                    new(),
                    new() { new BoogieAttribute("inline", true) },
                    memModSet, // ✅ modifies $mem
                    new(), // ✅ requires vide
                    new()
                );
                program.Declarations.Add(proc);

                var body = new BoogieStmtList();
                BoogieExpr a0 = new BoogieIdentifierExpr("a");

                for (int i = 0; i < 8; i++)
                {
                    var ai =
                        (i == 0)
                            ? a0
                            : new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.ADD,
                                a0,
                                new BoogieLiteralExpr(i)
                            );

                    body.AddStatement(
                        new BoogieCallCmd(
                            "mem_write_u8",
                            new()
                            {
                                ai,
                                new BoogieFunctionCall(
                                    $"byte{i}_64",
                                    new() { new BoogieIdentifierExpr("v") }
                                ),
                            },
                            new()
                        )
                    );
                }

                program.Declarations.Add(
                    new BoogieImplementation("mem_write_u64", ins, new(), new(), body)
                );
            }
        }

        private void AddMemoryReadProcedures(BoogieProgram program)
        {
            // Helper: make common ins/outs
            static List<BoogieVariable> InsA() =>
                new() { new BoogieFormalParam(new BoogieTypedIdent("a", BoogieType.Int)) };

            static List<BoogieVariable> OutsResultInt() =>
                new() { new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Int)) };

            // ------------------------------------------------------------
            // mem_read_u8(a) : int   (0..255)
            // ------------------------------------------------------------
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_u8",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(), // modifies list (empty)
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var body = new BoogieStmtList();

                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieMapSelect(
                            new BoogieIdentifierExpr("$mem"),
                            new BoogieIdentifierExpr("a")
                        )
                    )
                );

                // assume 0 <= result < 256
                var ge0 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieLiteralExpr(0),
                    new BoogieIdentifierExpr("result")
                );
                var lt256 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LT,
                    new BoogieIdentifierExpr("result"),
                    new BoogieLiteralExpr(256)
                );
                body.AddStatement(
                    new BoogieAssumeCmd(
                        new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, ge0, lt256)
                    )
                );

                var impl = new BoogieImplementation("mem_read_u8", ins, outs, new(), body);
                program.Declarations.Add(impl);
            }

            // ------------------------------------------------------------
            // mem_read_s8(a) : int   (-128..127)
            // ------------------------------------------------------------
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_s8",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(), // no modifies
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var locals = new List<BoogieVariable>
                {
                    new BoogieLocalVariable(new BoogieTypedIdent("u", BoogieType.Int)),
                };

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { new BoogieIdentifierExpr("a") },
                        new() { new BoogieIdentifierExpr("u") }
                    )
                );

                var cond = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.GE,
                    new BoogieIdentifierExpr("u"),
                    new BoogieLiteralExpr(128)
                );

                var thenBlk = new BoogieStmtList();
                thenBlk.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.SUB,
                            new BoogieIdentifierExpr("u"),
                            new BoogieLiteralExpr(256)
                        )
                    )
                );

                var elseBlk = new BoogieStmtList();
                elseBlk.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieIdentifierExpr("u")
                    )
                );

                body.AddStatement(new BoogieIfCmd(cond, thenBlk, elseBlk));

                var impl = new BoogieImplementation("mem_read_s8", ins, outs, locals, body);
                program.Declarations.Add(impl);
            }

            // ------------------------------------------------------------
            // mem_read_u16(a) : int  (0..65535) little-endian
            // result = u8(a) + 256*u8(a+1)
            // ------------------------------------------------------------
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_u16",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(),
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var locals = new List<BoogieVariable>
                {
                    new BoogieLocalVariable(new BoogieTypedIdent("b0", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b1", BoogieType.Int)),
                };

                var body = new BoogieStmtList();

                // b0 := u8(a)
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { new BoogieIdentifierExpr("a") },
                        new() { new BoogieIdentifierExpr("b0") }
                    )
                );

                // b1 := u8(a+1)
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new()
                        {
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.ADD,
                                new BoogieIdentifierExpr("a"),
                                new BoogieLiteralExpr(1)
                            ),
                        },
                        new() { new BoogieIdentifierExpr("b1") }
                    )
                );

                // result := b0 + 256*b1
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            new BoogieIdentifierExpr("b0"),
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.MUL,
                                new BoogieLiteralExpr(256),
                                new BoogieIdentifierExpr("b1")
                            )
                        )
                    )
                );

                // assume 0 <= result < 65536
                var ge0 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieLiteralExpr(0),
                    new BoogieIdentifierExpr("result")
                );
                var lt = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LT,
                    new BoogieIdentifierExpr("result"),
                    new BoogieLiteralExpr(65536)
                );
                body.AddStatement(
                    new BoogieAssumeCmd(
                        new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, ge0, lt)
                    )
                );

                var impl = new BoogieImplementation("mem_read_u16", ins, outs, locals, body);
                program.Declarations.Add(impl);
            }

            // ------------------------------------------------------------
            // mem_read_s16(a) : int (-32768..32767)
            // ------------------------------------------------------------
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_s16",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(),
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var locals = new List<BoogieVariable>
                {
                    new BoogieLocalVariable(new BoogieTypedIdent("u", BoogieType.Int)),
                };

                var body = new BoogieStmtList();

                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u16",
                        new() { new BoogieIdentifierExpr("a") },
                        new() { new BoogieIdentifierExpr("u") }
                    )
                );

                var cond = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.GE,
                    new BoogieIdentifierExpr("u"),
                    new BoogieLiteralExpr(32768)
                );

                var thenBlk = new BoogieStmtList();
                thenBlk.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.SUB,
                            new BoogieIdentifierExpr("u"),
                            new BoogieLiteralExpr(65536)
                        )
                    )
                );

                var elseBlk = new BoogieStmtList();
                elseBlk.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieIdentifierExpr("u")
                    )
                );

                body.AddStatement(new BoogieIfCmd(cond, thenBlk, elseBlk));

                var impl = new BoogieImplementation("mem_read_s16", ins, outs, locals, body);
                program.Declarations.Add(impl);
            }

            // ------------------------------------------------------------
            // mem_read_u32(a) : int (0..2^32-1) little-endian
            // result = sum_{i=0..3} 256^i * u8(a+i)
            // ------------------------------------------------------------
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_u32",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(),
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var locals = new List<BoogieVariable>
                {
                    new BoogieLocalVariable(new BoogieTypedIdent("b0", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b1", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b2", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b3", BoogieType.Int)),
                };

                BoogieExpr APlus(int k) =>
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.ADD,
                        new BoogieIdentifierExpr("a"),
                        new BoogieLiteralExpr(k)
                    );

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { new BoogieIdentifierExpr("a") },
                        new() { new BoogieIdentifierExpr("b0") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(1) },
                        new() { new BoogieIdentifierExpr("b1") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(2) },
                        new() { new BoogieIdentifierExpr("b2") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(3) },
                        new() { new BoogieIdentifierExpr("b3") }
                    )
                );

                // result := b0 + 256*b1 + 65536*b2 + 16777216*b3
                var expr = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.ADD,
                    new BoogieIdentifierExpr("b0"),
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.ADD,
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.MUL,
                            new BoogieLiteralExpr(256),
                            new BoogieIdentifierExpr("b1")
                        ),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.MUL,
                                new BoogieLiteralExpr(65536),
                                new BoogieIdentifierExpr("b2")
                            ),
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.MUL,
                                new BoogieLiteralExpr(16777216),
                                new BoogieIdentifierExpr("b3")
                            )
                        )
                    )
                );

                body.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr("result"), expr));

                // assume 0 <= result < 2^32  (4294967296)
                var ge0 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieLiteralExpr(0),
                    new BoogieIdentifierExpr("result")
                );
                var lt = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LT,
                    new BoogieIdentifierExpr("result"),
                    new BoogieLiteralExpr(4294967296L)
                );
                body.AddStatement(
                    new BoogieAssumeCmd(
                        new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, ge0, lt)
                    )
                );

                var impl = new BoogieImplementation("mem_read_u32", ins, outs, locals, body);
                program.Declarations.Add(impl);
            }

            // ------------------------------------------------------------
            // mem_read_s32(a) : int (signed 32)
            // ------------------------------------------------------------
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_s32",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(),
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var locals = new List<BoogieVariable>
                {
                    new BoogieLocalVariable(new BoogieTypedIdent("u", BoogieType.Int)),
                };

                var body = new BoogieStmtList();

                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u32",
                        new() { new BoogieIdentifierExpr("a") },
                        new() { new BoogieIdentifierExpr("u") }
                    )
                );

                var cond = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.GE,
                    new BoogieIdentifierExpr("u"),
                    new BoogieLiteralExpr(2147483648L) // 2^31
                );

                var thenBlk = new BoogieStmtList();
                thenBlk.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.SUB,
                            new BoogieIdentifierExpr("u"),
                            new BoogieLiteralExpr(4294967296L) // 2^32
                        )
                    )
                );

                var elseBlk = new BoogieStmtList();
                elseBlk.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieIdentifierExpr("u")
                    )
                );

                body.AddStatement(new BoogieIfCmd(cond, thenBlk, elseBlk));

                var impl = new BoogieImplementation("mem_read_s32", ins, outs, locals, body);
                program.Declarations.Add(impl);
            }

            // ------------------------------------------------------------
            // mem_read_u64(a) : int  little-endian (abstractly as int)
            // result = sum_{i=0..7} 256^i * u8(a+i)
            // ------------------------------------------------------------
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_u64",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(),
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var locals = new List<BoogieVariable>
                {
                    new BoogieLocalVariable(new BoogieTypedIdent("b0", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b1", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b2", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b3", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b4", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b5", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b6", BoogieType.Int)),
                    new BoogieLocalVariable(new BoogieTypedIdent("b7", BoogieType.Int)),
                };

                BoogieExpr APlus(int k) =>
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.ADD,
                        new BoogieIdentifierExpr("a"),
                        new BoogieLiteralExpr(k)
                    );

                var body = new BoogieStmtList();

                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { new BoogieIdentifierExpr("a") },
                        new() { new BoogieIdentifierExpr("b0") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(1) },
                        new() { new BoogieIdentifierExpr("b1") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(2) },
                        new() { new BoogieIdentifierExpr("b2") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(3) },
                        new() { new BoogieIdentifierExpr("b3") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(4) },
                        new() { new BoogieIdentifierExpr("b4") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(5) },
                        new() { new BoogieIdentifierExpr("b5") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(6) },
                        new() { new BoogieIdentifierExpr("b6") }
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u8",
                        new() { APlus(7) },
                        new() { new BoogieIdentifierExpr("b7") }
                    )
                );

                // constants 256^k
                var t1 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.MUL,
                    new BoogieLiteralExpr(256L),
                    new BoogieIdentifierExpr("b1")
                );
                var t2 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.MUL,
                    new BoogieLiteralExpr(65536L),
                    new BoogieIdentifierExpr("b2")
                );
                var t3 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.MUL,
                    new BoogieLiteralExpr(16777216L),
                    new BoogieIdentifierExpr("b3")
                );
                var t4 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.MUL,
                    new BoogieLiteralExpr(4294967296L),
                    new BoogieIdentifierExpr("b4")
                );
                var t5 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.MUL,
                    new BoogieLiteralExpr(1099511627776L),
                    new BoogieIdentifierExpr("b5")
                );
                var t6 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.MUL,
                    new BoogieLiteralExpr(281474976710656L),
                    new BoogieIdentifierExpr("b6")
                );
                var t7 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.MUL,
                    new BoogieLiteralExpr(72057594037927936L),
                    new BoogieIdentifierExpr("b7")
                );

                var expr = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.ADD,
                    new BoogieIdentifierExpr("b0"),
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.ADD,
                        t1,
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            t2,
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.ADD,
                                t3,
                                new BoogieBinaryOperation(
                                    BoogieBinaryOperation.Opcode.ADD,
                                    t4,
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.ADD,
                                        t5,
                                        new BoogieBinaryOperation(
                                            BoogieBinaryOperation.Opcode.ADD,
                                            t6,
                                            t7
                                        )
                                    )
                                )
                            )
                        )
                    )
                );

                body.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr("result"), expr));

                // NOTE: on ne met pas de borne < 2^64 ici (trop grand/chiant en int), on laisse libre.

                var impl = new BoogieImplementation("mem_read_u64", ins, outs, locals, body);
                program.Declarations.Add(impl);
            }

            // ------------------------------------------------------------
            // mem_read_s64(a) : int  (on l'abstrait)
            // ------------------------------------------------------------
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_s64",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(),
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                // abstraction: result := mem_read_u64(a)  OR any int
                // Ici on choisit "any int" via une assignation non déterministe si tu as nd_int().
                // Comme tu n'as pas nd_int dans ton prelude, on laisse "assume true" et pas d'impl intelligente.
                // -> plus simple: impl = result := mem_read_u64(a); (ça donne un modèle non signé, mais OK pour démarrer)

                var locals = new List<BoogieVariable>
                {
                    new BoogieLocalVariable(new BoogieTypedIdent("u", BoogieType.Int)),
                };

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieCallCmd(
                        "mem_read_u64",
                        new() { new BoogieIdentifierExpr("a") },
                        new() { new BoogieIdentifierExpr("u") }
                    )
                );
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieIdentifierExpr("u")
                    )
                );

                var impl = new BoogieImplementation("mem_read_s64", ins, outs, locals, body);
                program.Declarations.Add(impl);
            }
        }

        // ======== Préambule Boogie (pile + helpers) ========

        private void AddPrelude(BoogieProgram program)
        {
            // Globals
            program.Declarations.Add(
                new BoogieGlobalVariable(
                    new BoogieTypedIdent(
                        "$stack",
                        new BoogieMapType(BoogieType.Int, BoogieType.Real)
                    )
                )
            );
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int))
            );
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", BoogieType.Real))
            );
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", BoogieType.Real))
            );
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", BoogieType.Real))
            );

            // ===== Memory model (memory 0) =====
            program.Declarations.Add(
                new BoogieGlobalVariable(
                    new BoogieTypedIdent(
                        "$mem",
                        new BoogieMapType(BoogieType.Int, BoogieType.Int) // byte-addressed: 0..255
                    )
                )
            );
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$mem_pages", BoogieType.Int)) // pages of 64KiB
            );
            AddMemoryReadProcedures(program);
            AddMemoryWriteProcedures(program);

            // nd_real (uninterpreted)
            {
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(new BoogieFunction("nd_real", new(), new() { res }));
            }

            // bool_to_real
            {
                var b = new BoogieFormalParam(new BoogieTypedIdent("b", BoogieType.Bool));
                var body = new BoogieITE(
                    new BoogieIdentifierExpr("b"),
                    new BoogieLiteralExpr(new Pfloat(1)),
                    new BoogieLiteralExpr(new Pfloat(0))
                );
                program.Declarations.Add(
                    new BoogieFunctionDef("bool_to_real", new() { b }, BoogieType.Real, body)
                );
            }

            // real_to_bool
            {
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                var body = new BoogieITE(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        new BoogieIdentifierExpr("r"),
                        new BoogieLiteralExpr(new Pfloat(0))
                    ),
                    new BoogieLiteralExpr(false),
                    new BoogieLiteralExpr(true)
                );
                program.Declarations.Add(
                    new BoogieFunctionDef("real_to_bool", new() { r }, BoogieType.Bool, body)
                );
            }

            // real_to_int : uninterpreted cast
            {
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Int));
                program.Declarations.Add(
                    new BoogieFunction("real_to_int", new() { r }, new() { res })
                );
            }

            // int_to_real : uninterpreted cast
            {
                var i = new BoogieFormalParam(new BoogieTypedIdent("i", BoogieType.Int));
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("int_to_real", new() { i }, new() { res })
                );
            }

            // Bitcast helpers for floats (abstract)
            {
                var i = new BoogieFormalParam(new BoogieTypedIdent("i", BoogieType.Int));
                var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("bits32_to_real", new() { i }, new() { r })
                );
                program.Declarations.Add(
                    new BoogieFunction("bits64_to_real", new() { i }, new() { r })
                );
            }

            // to_u8(x): int -> int in [0,256)
            {
                var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int));
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Int));
                program.Declarations.Add(new BoogieFunction("to_u8", new() { x }, new() { r }));

                // axiom forall x :: 0 <= to_u8(x) < 256
                var xx = new BoogieIdentifierExpr("x");
                var f = new BoogieFunctionCall("to_u8", new() { xx });
                var ge0 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieLiteralExpr(0),
                    f
                );
                var lt256 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LT,
                    f,
                    new BoogieLiteralExpr(256)
                );
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new() { xx },
                            new() { BoogieType.Int },
                            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, ge0, lt256)
                        )
                    )
                );
            }

            // min_real
            {
                var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
                var y = new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Real));

                var body = new BoogieITE(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LE,
                        new BoogieIdentifierExpr("x"),
                        new BoogieIdentifierExpr("y")
                    ),
                    new BoogieIdentifierExpr("x"),
                    new BoogieIdentifierExpr("y")
                );

                program.Declarations.Add(
                    new BoogieFunctionDef("min_real", new() { x, y }, BoogieType.Real, body)
                );
            }

            // max_real
            {
                var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
                var y = new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Real));

                var body = new BoogieITE(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GE,
                        new BoogieIdentifierExpr("x"),
                        new BoogieIdentifierExpr("y")
                    ),
                    new BoogieIdentifierExpr("x"),
                    new BoogieIdentifierExpr("y")
                );

                // sqrt_real : uninterpreted + axioms
                {
                    // Declare uninterpreted function sqrt_real(r: real) returns (result: real);
                    var rParam = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                    var resParam = new BoogieFormalParam(
                        new BoogieTypedIdent("result", BoogieType.Real)
                    );
                    program.Declarations.Add(
                        new BoogieFunction("sqrt_real", new() { rParam }, new() { resParam })
                    );

                    // Build bound variable r: real
                    var r = new BoogieIdentifierExpr("r");
                    var rType = BoogieType.Real;

                    // 0.0
                    var zero = new BoogieLiteralExpr(new Pfloat(0));

                    // Guard: r >= 0.0
                    var guard = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, r, zero);

                    // sqrt_real(r)
                    var sqrt_r = new BoogieFunctionCall("sqrt_real", new List<BoogieExpr> { r });

                    // Axiom 1: forall r: real :: r >= 0.0 ==> sqrt_real(r) >= 0.0
                    {
                        var rhs = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.GE,
                            sqrt_r,
                            zero
                        );
                        var imp = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.IMP,
                            guard,
                            rhs
                        );

                        var forall = new BoogieQuantifiedExpr(
                            isForall: true,
                            qvars: new List<BoogieIdentifierExpr> { r },
                            qvarTypes: new List<BoogieType> { rType },
                            bodyExpr: imp,
                            trigger: new List<BoogieExpr> { sqrt_r } // optional trigger (helps SMT sometimes)
                        );

                        program.Declarations.Add(new BoogieAxiom(forall));
                    }

                    // Axiom 2: forall r: real :: r >= 0.0 ==> sqrt_real(r) * sqrt_real(r) == r
                    {
                        var mul = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.MUL,
                            sqrt_r,
                            sqrt_r
                        );
                        var eq = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, mul, r);
                        var imp = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.IMP,
                            guard,
                            eq
                        );

                        var forall = new BoogieQuantifiedExpr(
                            isForall: true,
                            qvars: new List<BoogieIdentifierExpr> { r },
                            qvarTypes: new List<BoogieType> { rType },
                            bodyExpr: imp,
                            trigger: new List<BoogieExpr> { sqrt_r } // optional trigger
                        );

                        program.Declarations.Add(new BoogieAxiom(forall));
                    }
                }
            }

            // nearest_real : uninterpreted rounding-to-nearest abstraction
            {
                var rIn = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                var rOut = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("nearest_real", new() { rIn }, new() { rOut })
                );

                // Build: forall r: real :: (nearest_real(r) - 0.5 <= r) && (r <= nearest_real(r) + 0.5)
                var r = new BoogieIdentifierExpr("r");
                BoogieExpr nr = new BoogieFunctionCall("nearest_real", new() { r });
                var half = new BoogieLiteralExpr(new Pfloat(0.5f));

                var leftBound = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.SUB, nr, half),
                    r
                );

                var rightBound = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    r,
                    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.ADD, nr, half)
                );

                var boundAxiomBody = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.AND,
                    leftBound,
                    rightBound
                );

                var boundAxiom = new BoogieAxiom(
                    new BoogieQuantifiedExpr(
                        isForall: true,
                        qvars: new() { new BoogieIdentifierExpr("r") },
                        qvarTypes: new() { BoogieType.Real },
                        bodyExpr: boundAxiomBody,
                        trigger: new List<BoogieExpr> { nr } // trigger {nearest_real(r)}
                    )
                );
                program.Declarations.Add(boundAxiom);

                // Build: forall r: real :: nearest_real(nearest_real(r)) == nearest_real(r)
                var nnr = new BoogieFunctionCall("nearest_real", new() { (BoogieExpr)nr });

                var idemBody = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, nnr, nr);

                var idemAxiom = new BoogieAxiom(
                    new BoogieQuantifiedExpr(
                        isForall: true,
                        qvars: new() { new BoogieIdentifierExpr("r") },
                        qvarTypes: new() { BoogieType.Real },
                        bodyExpr: idemBody,
                        trigger: new List<BoogieExpr> { nr }
                    )
                );
                program.Declarations.Add(idemAxiom);

                // Optional helper: forall r: real :: (r == nearest_real(r)) ==> (nearest_real(r) == r)
                // (logically redundant, but sometimes helps)
                var fixedPointCond = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    r,
                    nr
                );

                var fixedPointBody = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.IMP,
                    fixedPointCond,
                    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, nr, r)
                );

                var fixedPointAxiom = new BoogieAxiom(
                    new BoogieQuantifiedExpr(
                        isForall: true,
                        qvars: new() { new BoogieIdentifierExpr("r") },
                        qvarTypes: new() { BoogieType.Real },
                        bodyExpr: fixedPointBody,
                        trigger: new List<BoogieExpr> { nr }
                    )
                );
                program.Declarations.Add(fixedPointAxiom);
            }

            // floor_real : real -> real
            {
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("floor_real", new() { r }, new() { res })
                );
            }
            // axiom forall r: real :: floor_real(r) <= r
            {
                var rId = new BoogieIdentifierExpr("r");
                var qvars = new List<BoogieIdentifierExpr> { rId };
                var qtypes = new List<BoogieType> { BoogieType.Real };

                var body = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieFunctionCall("floor_real", new() { rId }),
                    rId
                );

                program.Declarations.Add(
                    new BoogieAxiom(new BoogieQuantifiedExpr(true, qvars, qtypes, body))
                );
            }

            // axiom forall r: real :: r < floor_real(r) + 1
            {
                var rId = new BoogieIdentifierExpr("r");
                var qvars = new List<BoogieIdentifierExpr> { rId };
                var qtypes = new List<BoogieType> { BoogieType.Real };

                var rhs = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.ADD,
                    new BoogieFunctionCall("floor_real", new() { rId }),
                    new BoogieLiteralExpr(new Pfloat(1))
                );

                var body = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LT, rId, rhs);

                program.Declarations.Add(
                    new BoogieAxiom(new BoogieQuantifiedExpr(true, qvars, qtypes, body))
                );
            }

            // push(val)
            {
                var proc = new BoogieProcedure(
                    "push",
                    new() { new BoogieFormalParam(new BoogieTypedIdent("val", BoogieType.Real)) },
                    new(),
                    new() { new BoogieAttribute("inline", true) },
                    new()
                    {
                        new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                        new BoogieGlobalVariable(
                            new BoogieTypedIdent(
                                "$stack",
                                new BoogieMapType(BoogieType.Int, BoogieType.Real)
                            )
                        ),
                    },
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieMapSelect(
                            new BoogieIdentifierExpr("$stack"),
                            new BoogieIdentifierExpr("$sp")
                        ),
                        new BoogieIdentifierExpr("val")
                    )
                );
                var oldSp = new BoogieIdentifierExpr("$sp");
                body.AddStatement(
                    new BoogieAssignCmd(
                        oldSp,
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            oldSp,
                            new BoogieLiteralExpr(1)
                        )
                    )
                );
                var impl = new BoogieImplementation(
                    "push",
                    new() { new BoogieFormalParam(new BoogieTypedIdent("val", BoogieType.Real)) },
                    new(),
                    new(),
                    body
                );
                program.Declarations.Add(impl);
            }

            // popToTmp1/2/3
            AddPopToTmp("popToTmp1", "$tmp1");
            AddPopToTmp("popToTmp2", "$tmp2");
            AddPopToTmp("popToTmp3", "$tmp3");

            // pop() (drop 1)
            {
                var proc = new BoogieProcedure(
                    "pop",
                    new(),
                    new(),
                    new(),
                    new() { new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)) },
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieAssumeCmd(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.GT,
                            new BoogieIdentifierExpr("$sp"),
                            new BoogieLiteralExpr(0)
                        )
                    )
                );
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("$sp"),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.SUB,
                            new BoogieIdentifierExpr("$sp"),
                            new BoogieLiteralExpr(1)
                        )
                    )
                );
                var impl = new BoogieImplementation("pop", new(), new(), new(), body);
                program.Declarations.Add(impl);
            }

            // local helper
            void AddPopToTmp(string name, string tmp)
            {
                var proc = new BoogieProcedure(
                    name,
                    new(),
                    new(),
                    new(),
                    new()
                    {
                        new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                        new BoogieGlobalVariable(
                            new BoogieTypedIdent(
                                "$stack",
                                new BoogieMapType(BoogieType.Int, BoogieType.Real)
                            )
                        ),
                        new BoogieGlobalVariable(new BoogieTypedIdent(tmp, BoogieType.Real)),
                    },
                    new(),
                    new()
                );
                program.Declarations.Add(proc);

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieAssumeCmd(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.GT,
                            new BoogieIdentifierExpr("$sp"),
                            new BoogieLiteralExpr(0)
                        )
                    )
                );
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("$sp"),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.SUB,
                            new BoogieIdentifierExpr("$sp"),
                            new BoogieLiteralExpr(1)
                        )
                    )
                );
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr(tmp),
                        new BoogieMapSelect(
                            new BoogieIdentifierExpr("$stack"),
                            new BoogieIdentifierExpr("$sp")
                        )
                    )
                );

                var impl = new BoogieImplementation(name, new(), new(), new(), body);
                program.Declarations.Add(impl);
            }
        }

        // popArgsN (inline, retourne a1..aN) — callee pops args
        private void EnsurePopArgsProc(int n)
        {
            if (n <= 0 || program == null || popArgsMade.Contains(n))
                return;
            popArgsMade.Add(n);

            var outs = new List<BoogieVariable>();
            for (int i = 1; i <= n; i++)
                outs.Add(new BoogieFormalParam(new BoogieTypedIdent($"a{i}", BoogieType.Real)));

            var proc = new BoogieProcedure(
                $"popArgs{n}",
                new(),
                outs,
                new() { new BoogieAttribute("inline", true) },
                new()
                {
                    new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                    new BoogieGlobalVariable(
                        new BoogieTypedIdent(
                            "$stack",
                            new BoogieMapType(BoogieType.Int, BoogieType.Real)
                        )
                    ),
                },
                new(),
                new()
            );
            program!.Declarations.Add(proc);

            var body = new BoogieStmtList();
            body.AddStatement(
                new BoogieAssumeCmd(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GE,
                        new BoogieIdentifierExpr("$sp"),
                        new BoogieLiteralExpr(n)
                    )
                )
            );
            for (int i = n; i >= 1; i--)
            {
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("$sp"),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.SUB,
                            new BoogieIdentifierExpr("$sp"),
                            new BoogieLiteralExpr(1)
                        )
                    )
                );
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr($"a{i}"),
                        new BoogieMapSelect(
                            new BoogieIdentifierExpr("$stack"),
                            new BoogieIdentifierExpr("$sp")
                        )
                    )
                );
            }
            var impl = new BoogieImplementation($"popArgs{n}", new(), outs, new(), body);
            program!.Declarations.Add(impl);
        }

        // NEW: popDiscardN (inline, just decreases $sp)
        private void EnsurePopDiscardProc(int n)
        {
            if (n <= 0 || program == null || popDiscardMade.Contains(n))
                return;
            popDiscardMade.Add(n);

            var proc = new BoogieProcedure(
                $"popDiscard{n}",
                new(),
                new(),
                new() { new BoogieAttribute("inline", true) },
                new() { new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)) },
                new(),
                new()
            );
            program!.Declarations.Add(proc);

            var body = new BoogieStmtList();
            body.AddStatement(
                new BoogieAssumeCmd(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GE,
                        new BoogieIdentifierExpr("$sp"),
                        new BoogieLiteralExpr(n)
                    )
                )
            );
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("$sp"),
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.SUB,
                        new BoogieIdentifierExpr("$sp"),
                        new BoogieLiteralExpr(n)
                    )
                )
            );

            var impl = new BoogieImplementation($"popDiscard{n}", new(), new(), new(), body);
            program!.Declarations.Add(impl);
        }

        // ======== Traduction de fonction ========

        private (BoogieProcedure, BoogieImplementation) TranslateFunction(WasmFunction func)
        {
            var inParams = new List<BoogieVariable>(); // (pile)
            var outParams = new List<BoogieVariable>(); // (pile)
            var locals = new List<BoogieVariable>();
            var body = new BoogieStmtList();

            // état par fonction
            currentFunction = func;
            functionExitLabel = null;
            PrecomputeLabelNeeds(func);

            // NEW: track globals modified by this function
            currentModifiedGlobals = new HashSet<string>(StringComparer.Ordinal);

            // Construire la table arg/loc
            int n = func.ParamCount;
            int m = func.LocalCount;
            int r = Math.Max(0, func.ResultCount);
            var indexToId = new List<BoogieIdentifierExpr>(n + m);

            for (int i = 1; i <= n; i++)
            {
                var name = $"arg{i}";
                locals.Add(new BoogieLocalVariable(new BoogieTypedIdent(name, BoogieType.Real)));
                indexToId.Add(new BoogieIdentifierExpr(name));
            }
            for (int i = 1; i <= m; i++)
            {
                var name = $"loc{i}";
                locals.Add(new BoogieLocalVariable(new BoogieTypedIdent(name, BoogieType.Real)));
                indexToId.Add(new BoogieIdentifierExpr(name));
            }
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("idx", BoogieType.Int)));
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("entry_sp", BoogieType.Int)));
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("load_i", BoogieType.Int)));
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("store_i", BoogieType.Int)));

            currentLocalMap = indexToId;

            // Prologue
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("entry_sp"),
                    new BoogieIdentifierExpr("$sp")
                )
            );
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("$tmp1"),
                    new BoogieLiteralExpr(new Pfloat(0))
                )
            );
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("$tmp2"),
                    new BoogieLiteralExpr(new Pfloat(0))
                )
            );
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("$tmp3"),
                    new BoogieLiteralExpr(new Pfloat(0))
                )
            );

            // Args : callee pop ses args
            if (n > 0)
            {
                EnsurePopArgsProc(n);
                body.AddStatement(
                    new BoogieAssumeCmd(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.GE,
                            new BoogieIdentifierExpr("$sp"),
                            new BoogieLiteralExpr(n)
                        )
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd($"popArgs{n}", new(), indexToId.Take(n).ToList())
                );
            }

            // Locals init 0
            for (int i = n; i < n + m; i++)
                body.AddStatement(
                    new BoogieAssignCmd(indexToId[i], new BoogieLiteralExpr(new Pfloat(0)))
                );

            // Corps
            foreach (var node in func.Body)
                TranslateNode(node, body);

            // Matérialiser label de sortie si utilisé
            if (!string.IsNullOrEmpty(functionExitLabel))
            {
                body.AddStatement(new BoogieSkipCmd(functionExitLabel + ":"));
                functionExitLabel = null;
            }

            // Épilogue : assertion de discipline de pile (optionnelle)
            var expected = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.ADD,
                new BoogieIdentifierExpr("entry_sp"),
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.SUB,
                    new BoogieLiteralExpr(r),
                    new BoogieLiteralExpr(n)
                )
            );

            if (EnableFooterStackAssert)
            {
                body.AddStatement(
                    new BoogieAssertCmd(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.EQ,
                            new BoogieIdentifierExpr("$sp"),
                            expected
                        )
                    )
                );
            }
            else
            {
                body.AddStatement(new BoogieCommentCmd("// footer stack assert disabled"));
            }

            string funcName = SanitizeFunctionName(func.Name, contractName);

            // Build modifies list dynamically (includes globals written by global.set)
            var mods = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", BoogieType.Real)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", BoogieType.Real)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", BoogieType.Real)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                new BoogieGlobalVariable(
                    new BoogieTypedIdent(
                        "$stack",
                        new BoogieMapType(BoogieType.Int, BoogieType.Real)
                    )
                ),
                new BoogieGlobalVariable(
                    new BoogieTypedIdent("$mem", new BoogieMapType(BoogieType.Int, BoogieType.Int))
                ),
            };

            if (currentModifiedGlobals != null)
            {
                foreach (var g in currentModifiedGlobals)
                    mods.Add(new BoogieGlobalVariable(new BoogieTypedIdent(g, BoogieType.Real)));
            }

            var proc = new BoogieProcedure(
                funcName,
                inParams,
                outParams,
                new(),
                mods,
                new(),
                new()
            );

            var impl = new BoogieImplementation(proc.Name, inParams, outParams, locals, body);

            // reset état
            currentLocalMap = null;
            currentFunction = null;
            neededLoopStartLabels = null;
            neededBlockEndLabels = null;
            labelStack.Clear();
            currentModifiedGlobals = null;

            return (proc, impl);
        }

        // ======== Pré-scan des labels ========

        private void PrecomputeLabelNeeds(WasmFunction func)
        {
            neededLoopStartLabels = new HashSet<string>(StringComparer.Ordinal);
            neededBlockEndLabels = new HashSet<string>(StringComparer.Ordinal);
            var scope = new Stack<(string label, bool isLoop)>();

            void Walk(WasmNode n)
            {
                switch (n)
                {
                    case BlockNode blk:
                    {
                        bool hasUser =
                            !string.IsNullOrEmpty(blk.Label)
                            && blk.Label!.StartsWith("$", StringComparison.Ordinal);
                        if (hasUser)
                            scope.Push((blk.Label!.Substring(1), false));
                        foreach (var m in blk.Body)
                            Walk(m);
                        if (hasUser)
                            scope.Pop();
                        break;
                    }
                    case LoopNode lp:
                    {
                        bool hasUser =
                            !string.IsNullOrEmpty(lp.Label)
                            && lp.Label!.StartsWith("$", StringComparison.Ordinal);
                        if (hasUser)
                            scope.Push((lp.Label!.Substring(1), true));
                        foreach (var m in lp.Body)
                            Walk(m);
                        if (hasUser)
                            scope.Pop();
                        break;
                    }
                    case IfNode iff:
                    {
                        Walk(iff.Condition);
                        foreach (var m in iff.ThenBody)
                            Walk(m);
                        if (iff.ElseBody != null)
                            foreach (var m in iff.ElseBody)
                                Walk(m);
                        break;
                    }
                    case BinaryOpNode b:
                        Walk(b.Left);
                        Walk(b.Right);
                        break;
                    case UnaryOpNode u:
                        if (u.Operand != null)
                            Walk(u.Operand);
                        break;
                    case BrNode br:
                    {
                        var target = NormalizeLabel(br.Label);
                        foreach (var (lab, isLoop) in scope)
                        {
                            if (lab == target)
                            {
                                if (isLoop)
                                    neededLoopStartLabels!.Add(lab);
                                else
                                    neededBlockEndLabels!.Add(lab);
                                break;
                            }
                        }
                        break;
                    }
                    case BrIfNode bri:
                    {
                        Walk(bri.Condition);
                        var target = NormalizeLabel(bri.Label);
                        foreach (var (lab, isLoop) in scope)
                        {
                            if (lab == target)
                            {
                                if (isLoop)
                                    neededLoopStartLabels!.Add(lab);
                                else
                                    neededBlockEndLabels!.Add(lab);
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            foreach (var n in func.Body)
                Walk(n);
        }

        // ======== Traduction des nœuds ========

        private int ResolveLocalIndex(int? index, string? name)
        {
            if (index.HasValue)
                return index.Value;

            if (!string.IsNullOrEmpty(name))
            {
                if (
                    currentFunction != null
                    && currentFunction.LocalIndexByName.TryGetValue(name, out var idx)
                )
                    return idx;

                if (name[0] == '$' && int.TryParse(name.AsSpan(1), out var autoIdx))
                    return autoIdx;
            }

            throw new NotSupportedException($"Unknown local index/name: {name ?? "<null>"}");
        }

        private void TranslateNode(WasmNode node, BoogieStmtList body)
        {
            switch (node)
            {
                case ConstNode cn:
                {
                    if (
                        double.TryParse(
                            cn.Value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var dv
                        )
                    )
                    {
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieLiteralExpr(new Pfloat((float)dv)) },
                                new()
                            )
                        );
                    }
                    else
                    {
                        body.AddStatement(
                            new BoogieCommentCmd($"// unsupported const value: {cn.Value}")
                        );
                    }
                    break;
                }

                case LocalGetNode lg:
                {
                    int idx = ResolveLocalIndex(lg.Index, lg.Name);
                    var id = currentLocalMap![idx];
                    body.AddStatement(new BoogieCallCmd("push", new() { id }, new()));
                    break;
                }

                case LocalSetNode ls:
                {
                    int idx = ResolveLocalIndex(ls.Index, ls.Name);
                    var id = currentLocalMap![idx];
                    if (ls.Value != null)
                        TranslateNode(ls.Value, body);
                    EnsurePopArgsProc(1);
                    body.AddStatement(new BoogieCallCmd("popArgs1", new(), new() { id }));
                    break;
                }

                case LocalTeeNode lt:
                {
                    int idx = ResolveLocalIndex(lt.Index, lt.Name);
                    var id = currentLocalMap![idx];
                    EnsurePopArgsProc(1);
                    body.AddStatement(new BoogieCallCmd("popArgs1", new(), new() { id }));
                    body.AddStatement(new BoogieCallCmd("push", new() { id }, new()));
                    break;
                }

                // ===== NEW: global.get / global.set =====
                case GlobalGetNode gg:
                {
                    string gkey = ResolveGlobalKey(gg.Index, gg.Name);
                    string bname = EnsureGlobalVar(gkey);
                    body.AddStatement(
                        new BoogieCallCmd("push", new() { new BoogieIdentifierExpr(bname) }, new())
                    );
                    break;
                }

                case GlobalSetNode gs:
                {
                    string gkey = ResolveGlobalKey(gs.Index, gs.Name);
                    string bname = EnsureGlobalVar(gkey);

                    // folded form: (global.set $g <expr>)
                    if (gs.Value != null)
                        TranslateNode(gs.Value, body);

                    EnsurePopArgsProc(1);
                    body.AddStatement(
                        new BoogieCallCmd(
                            "popArgs1",
                            new(),
                            new() { new BoogieIdentifierExpr(bname) }
                        )
                    );

                    // IMPORTANT: Boogie framing (modifies)
                    currentModifiedGlobals?.Add(bname);
                    break;
                }

                case CallNode call:
                {
                    if (call.Args != null)
                        foreach (var a in call.Args)
                            TranslateNode(a, body);
                    string target = MapCalleeName(call.Target);
                    body.AddStatement(new BoogieCallCmd(target, new(), new()));
                    break;
                }

                // ===== call_indirect (conservative model) =====
                case CallIndirectNode ci:
                {
                    foreach (var a in ci.Args)
                        TranslateNode(a, body);
                    TranslateNode(ci.CalleeIndex, body);

                    int nArgs = ci.Args.Count;
                    EnsurePopDiscardProc(nArgs + 1); // args + idx
                    body.AddStatement(new BoogieCallCmd($"popDiscard{nArgs + 1}", new(), new()));

                    int r = ResolveIndirectResultCount(ci.TypeUse);
                    for (int i = 0; i < r; i++)
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieFunctionCall("nd_real", new()) },
                                new()
                            )
                        );

                    break;
                }

                // ===== return_call (tail-call) =====
                case ReturnCallNode rc:
                {
                    if (rc.Args != null)
                        foreach (var a in rc.Args)
                            TranslateNode(a, body);

                    string target = MapCalleeName(rc.Target);
                    body.AddStatement(new BoogieCallCmd(target, new(), new()));

                    if (functionExitLabel == null)
                        functionExitLabel = GenerateLabel("func_exit");
                    body.AddStatement(new BoogieGotoCmd(functionExitLabel));
                    break;
                }

                // ===== return_call_indirect (tail-call indirect) =====
                case ReturnCallIndirectNode rci:
                {
                    foreach (var a in rci.Args)
                        TranslateNode(a, body);
                    TranslateNode(rci.CalleeIndex, body);

                    int nArgs = rci.Args.Count;
                    EnsurePopDiscardProc(nArgs + 1);
                    body.AddStatement(new BoogieCallCmd($"popDiscard{nArgs + 1}", new(), new()));

                    int r = ResolveIndirectResultCount(rci.TypeUse);
                    for (int i = 0; i < r; i++)
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieFunctionCall("nd_real", new()) },
                                new()
                            )
                        );

                    if (functionExitLabel == null)
                        functionExitLabel = GenerateLabel("func_exit");
                    body.AddStatement(new BoogieGotoCmd(functionExitLabel));
                    break;
                }

                case MemoryOpNode mem:
                {
                    // Helpers communs
                    BoogieExpr Tmp1() => new BoogieIdentifierExpr("$tmp1");
                    BoogieExpr Tmp2() => new BoogieIdentifierExpr("$tmp2");

                    // --- helper: compute idx from an address already on the stack (top) ---
                    void PopAddrComputeIdx()
                    {
                        // pop address into $tmp1
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        // idx := real_to_int($tmp1) + offset
                        var addrInt = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            new BoogieFunctionCall("real_to_int", new() { (BoogieExpr)Tmp1() }),
                            new BoogieLiteralExpr(mem.Offset)
                        );

                        body.AddStatement(
                            new BoogieAssignCmd(new BoogieIdentifierExpr("idx"), addrInt)
                        );
                    }

                    // --- helper: compute idx from folded address node (mem.Address) ---
                    void EvalAddrComputeIdx()
                    {
                        if (mem.Address != null)
                            TranslateNode(mem.Address, body);
                        // address is now on stack
                        PopAddrComputeIdx();
                    }

                    // --- STORE path ---
                    bool isStore =
                        mem.Op
                        is "i32.store"
                            or "i64.store"
                            or "f32.store"
                            or "f64.store"
                            or "i32.store8"
                            or "i32.store16"
                            or "i64.store8"
                            or "i64.store16"
                            or "i64.store32";

                    if (isStore)
                    {
                        // WebAssembly store expects stack order: ... addr, value
                        // If you have folded nodes, keep the same order:
                        //   evaluate address first, then value, so top-of-stack is value, below is addr.
                        if (mem.Address != null)
                            TranslateNode(mem.Address, body);
                        if (mem.Value != null)
                            TranslateNode(mem.Value, body);

                        // pop value -> $tmp2
                        body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

                        // pop addr -> $tmp1 and compute idx
                        PopAddrComputeIdx();

                        // store_i := real_to_int($tmp2)
                        body.AddStatement(
                            new BoogieAssignCmd(
                                new BoogieIdentifierExpr("store_i"),
                                new BoogieFunctionCall("real_to_int", new() { (BoogieExpr)Tmp2() })
                            )
                        );

                        // dispatch write
                        switch (mem.Op)
                        {
                            case "i32.store":
                            case "f32.store":
                            case "i64.store32":
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "mem_write_u32",
                                        new()
                                        {
                                            new BoogieIdentifierExpr("idx"),
                                            new BoogieIdentifierExpr("store_i"),
                                        },
                                        new()
                                    )
                                );
                                break;

                            case "i64.store":
                            case "f64.store":
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "mem_write_u64",
                                        new()
                                        {
                                            new BoogieIdentifierExpr("idx"),
                                            new BoogieIdentifierExpr("store_i"),
                                        },
                                        new()
                                    )
                                );
                                break;

                            case "i32.store8":
                            case "i64.store8":
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "mem_write_u8",
                                        new()
                                        {
                                            new BoogieIdentifierExpr("idx"),
                                            new BoogieIdentifierExpr("store_i"),
                                        },
                                        new()
                                    )
                                );
                                break;

                            case "i32.store16":
                            case "i64.store16":
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "mem_write_u16",
                                        new()
                                        {
                                            new BoogieIdentifierExpr("idx"),
                                            new BoogieIdentifierExpr("store_i"),
                                        },
                                        new()
                                    )
                                );
                                break;

                            default:
                                body.AddStatement(
                                    new BoogieCommentCmd($"// unsupported store op: {mem.Op}")
                                );
                                break;
                        }

                        break; // done
                    }

                    // --- LOAD path ---
                    // evaluate address (folded) then pop it and compute idx
                    EvalAddrComputeIdx();

                    BoogieExpr idxExpr = new BoogieIdentifierExpr("idx");
                    var loadVar = new BoogieIdentifierExpr("load_i");

                    void CallRead(string procName)
                    {
                        body.AddStatement(
                            new BoogieCallCmd(procName, new() { idxExpr }, new() { loadVar })
                        );
                    }

                    void PushLoadedIntAsReal()
                    {
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieFunctionCall("int_to_real", new() { loadVar }) },
                                new()
                            )
                        );
                    }

                    void PushLoadedBits32AsReal()
                    {
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new()
                                {
                                    new BoogieFunctionCall("bits32_to_real", new() { loadVar }),
                                },
                                new()
                            )
                        );
                    }

                    void PushLoadedBits64AsReal()
                    {
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new()
                                {
                                    new BoogieFunctionCall("bits64_to_real", new() { loadVar }),
                                },
                                new()
                            )
                        );
                    }

                    switch (mem.Op)
                    {
                        case "i32.load":
                            CallRead("mem_read_s32");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load":
                            CallRead("mem_read_s64");
                            PushLoadedIntAsReal();
                            break;

                        case "f32.load":
                            CallRead("mem_read_u32");
                            PushLoadedBits32AsReal();
                            break;

                        case "f64.load":
                            CallRead("mem_read_u64");
                            PushLoadedBits64AsReal();
                            break;

                        case "i32.load8_s":
                            CallRead("mem_read_s8");
                            PushLoadedIntAsReal();
                            break;

                        case "i32.load8_u":
                            CallRead("mem_read_u8");
                            PushLoadedIntAsReal();
                            break;

                        case "i32.load16_s":
                            CallRead("mem_read_s16");
                            PushLoadedIntAsReal();
                            break;

                        case "i32.load16_u":
                            CallRead("mem_read_u16");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load8_s":
                            CallRead("mem_read_s8");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load8_u":
                            CallRead("mem_read_u8");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load16_s":
                            CallRead("mem_read_s16");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load16_u":
                            CallRead("mem_read_u16");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load32_s":
                            CallRead("mem_read_s32");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load32_u":
                            CallRead("mem_read_u32");
                            PushLoadedIntAsReal();
                            break;

                        default:
                            body.AddStatement(
                                new BoogieCommentCmd($"// unsupported memory op: {mem.Op}")
                            );
                            break;
                    }

                    break;
                }

                case UnaryOpNode un:
                {
                    if (un.Operand != null)
                        TranslateNode(un.Operand, body);

                    if (un.Op == "drop")
                    {
                        body.AddStatement(new BoogieCallCmd("pop", new(), new()));
                    }
                    else if (un.Op == "i32.eqz" || un.Op == "i64.eqz")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        var eqzExpr = new BoogieFunctionCall(
                            "bool_to_real",
                            new()
                            {
                                new BoogieBinaryOperation(
                                    BoogieBinaryOperation.Opcode.EQ,
                                    new BoogieIdentifierExpr("$tmp1"),
                                    new BoogieLiteralExpr(new Pfloat(0))
                                ),
                            }
                        );
                        body.AddStatement(new BoogieCallCmd("push", new() { eqzExpr }, new()));
                    }
                    else if (un.Op == "i32.wrap_i64" || un.Op == "i64.wrap_i64")
                    {
                        body.AddStatement(
                            new BoogieCommentCmd("// wrap: no-op under real semantics")
                        );
                    }
                    else if (un.Op == "f32.abs" || un.Op == "f64.abs")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        var absExpr = new BoogieFunctionCall(
                            "abs_real",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { absExpr }, new()));
                    }
                    else if (un.Op == "f32.neg" || un.Op == "f64.neg")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        var negExpr = new BoogieUnaryOperation(
                            BoogieUnaryOperation.Opcode.NEG,
                            new BoogieIdentifierExpr("$tmp1")
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { negExpr }, new()));
                    }
                    else if (un.Op == "f32.sqrt" || un.Op == "f64.sqrt")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        var sqrtExpr = new BoogieFunctionCall(
                            "sqrt_real",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { sqrtExpr }, new()));
                    }
                    else if (un.Op == "f32.nearest" || un.Op == "f64.nearest")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        var nearestExpr = new BoogieFunctionCall(
                            "nearest_real",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );
                        body.AddStatement(new BoogieCallCmd("push", new() { nearestExpr }, new()));
                    }
                    else if (un.Op == "f32.floor" || un.Op == "f64.floor")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        var flo = new BoogieFunctionCall(
                            "floor_real",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );
                        body.AddStatement(new BoogieCallCmd("push", new() { flo }, new()));
                    }
                    else
                    {
                        body.AddStatement(
                            new BoogieCommentCmd($"// unsupported unary op: {un.Op}")
                        );
                    }
                    break;
                }

                case BinaryOpNode bn:
                {
                    TranslateNode(bn.Left, body);
                    TranslateNode(bn.Right, body);

                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                    body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));
                    var tmp1 = new BoogieIdentifierExpr("$tmp1");
                    var tmp2 = new BoogieIdentifierExpr("$tmp2");

                    if (
                        bn.Op
                        is "i32.add"
                            or "i64.add"
                            or "f32.add"
                            or "f64.add"
                            or "i32.sub"
                            or "i64.sub"
                            or "f32.sub"
                            or "f64.sub"
                            or "i32.mul"
                            or "i64.mul"
                            or "f32.mul"
                            or "f64.mul"
                            or "i32.div_s"
                            or "i64.div_s"
                            or "f32.div"
                            or "f64.div"
                            or "i32.div_u"
                            or "i64.div_u"
                    )
                    {
                        var opKind = bn.Op switch
                        {
                            "i32.add" or "i64.add" or "f32.add" or "f64.add" =>
                                BoogieBinaryOperation.Opcode.ADD,
                            "i32.sub" or "i64.sub" or "f32.sub" or "f64.sub" =>
                                BoogieBinaryOperation.Opcode.SUB,
                            "i32.mul" or "i64.mul" or "f32.mul" or "f64.mul" =>
                                BoogieBinaryOperation.Opcode.MUL,
                            _ => BoogieBinaryOperation.Opcode.DIV,
                        };
                        var arithExpr = new BoogieBinaryOperation(opKind, tmp2, tmp1);
                        body.AddStatement(new BoogieCallCmd("push", new() { arithExpr }, new()));
                    }
                    else if (
                        bn.Op
                        is "i32.eq"
                            or "i64.eq"
                            or "f32.eq"
                            or "f64.eq"
                            or "i32.ne"
                            or "i64.ne"
                            or "f32.ne"
                            or "f64.ne"
                            or "i32.lt_s"
                            or "i64.lt_s"
                            or "i32.lt_u"
                            or "i64.lt_u"
                            or "f32.lt"
                            or "f64.lt"
                            or "i32.le_s"
                            or "i64.le_s"
                            or "i32.le_u"
                            or "i64.le_u"
                            or "f32.le"
                            or "f64.le"
                            or "i32.gt_s"
                            or "i64.gt_s"
                            or "i32.gt_u"
                            or "i64.gt_u"
                            or "f32.gt"
                            or "f64.gt"
                            or "i32.ge_s"
                            or "i64.ge_s"
                            or "i32.ge_u"
                            or "i64.ge_u"
                            or "f32.ge"
                            or "f64.ge"
                    )
                    {
                        BoogieExpr cmpExpr = bn.Op switch
                        {
                            "i32.eq" or "i64.eq" or "f32.eq" or "f64.eq" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.EQ,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            "i32.ne" or "i64.ne" or "f32.ne" or "f64.ne" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.NEQ,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            "i32.lt_s"
                            or "i64.lt_s"
                            or "i32.lt_u"
                            or "i64.lt_u"
                            or "f32.lt"
                            or "f64.lt" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.LT,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            "i32.le_s"
                            or "i64.le_s"
                            or "i32.le_u"
                            or "i64.le_u"
                            or "f32.le"
                            or "f64.le" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.LE,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            "i32.gt_s"
                            or "i64.gt_s"
                            or "i32.gt_u"
                            or "i64.gt_u"
                            or "f32.gt"
                            or "f64.gt" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.GT,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            _ // ge
                            => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.GE,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                        };
                        body.AddStatement(new BoogieCallCmd("push", new() { cmpExpr }, new()));
                    }
                    else if (bn.Op == "f32.min" || bn.Op == "f64.min")
                    {
                        // operands already evaluated
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // y
                        body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // x

                        var minExpr = new BoogieFunctionCall(
                            "min_real",
                            new()
                            {
                                new BoogieIdentifierExpr("$tmp2"),
                                new BoogieIdentifierExpr("$tmp1"),
                            }
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { minExpr }, new()));
                    }
                    else if (bn.Op == "f32.max" || bn.Op == "f64.max")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // y
                        body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // x

                        var maxExpr = new BoogieFunctionCall(
                            "max_real",
                            new()
                            {
                                new BoogieIdentifierExpr("$tmp2"),
                                new BoogieIdentifierExpr("$tmp1"),
                            }
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { maxExpr }, new()));
                    }
                    else
                    {
                        body.AddStatement(
                            new BoogieCommentCmd($"// unsupported binary op: {bn.Op}")
                        );
                    }
                    break;
                }

                case BlockNode blk:
                {
                    // Ignore module wrapper and type blocks (not executable). Still traverse func blocks.
                    if (blk.Label == "module")
                    {
                        foreach (var child in blk.Body)
                            TranslateNode(child, body);
                        break;
                    }
                    if (blk.Label == "type")
                    {
                        // not executable: ignore completely
                        break;
                    }
                    if (blk.Label == "func")
                    {
                        foreach (var child in blk.Body)
                            TranslateNode(child, body);
                        break;
                    }

                    string? wat =
                        blk.Label != null && blk.Label.StartsWith("$")
                            ? blk.Label.Substring(1)
                            : null;

                    var ctx = new LabelContext
                    {
                        WatLabel = wat,
                        IsLoop = false,
                        StartLabel = null,
                        EndLabel = GenerateLabel(wat != null ? $"{wat}_end" : "block_end"),
                    };
                    labelStack.Push(ctx);

                    foreach (var child in blk.Body)
                        TranslateNode(child, body);

                    body.AddStatement(new BoogieSkipCmd(ctx.EndLabel + ":"));
                    labelStack.Pop();
                    break;
                }

                case LoopNode loop:
                {
                    string? wat =
                        loop.Label != null && loop.Label.StartsWith("$")
                            ? loop.Label.Substring(1)
                            : null;

                    var ctx = new LabelContext
                    {
                        WatLabel = wat,
                        IsLoop = true,
                        StartLabel = GenerateLabel(wat != null ? $"{wat}_start" : "loop_start"),
                        EndLabel = GenerateLabel(wat != null ? $"{wat}_end" : "loop_end"),
                    };
                    labelStack.Push(ctx);

                    body.AddStatement(new BoogieSkipCmd(ctx.StartLabel + ":")); // continue
                    foreach (var child in loop.Body)
                        TranslateNode(child, body);
                    body.AddStatement(new BoogieSkipCmd(ctx.EndLabel + ":")); // break

                    labelStack.Pop();
                    break;
                }

                case BrNode br:
                {
                    var target = ResolveBranchTarget(br.Label);
                    body.AddStatement(new BoogieGotoCmd(target));
                    break;
                }

                case BrIfNode brIf:
                {
                    TranslateNode(brIf.Condition, body);
                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                    var target = ResolveBranchTarget(brIf.Label);
                    var thenBlk = new BoogieStmtList();
                    thenBlk.AddStatement(new BoogieGotoCmd(target));
                    var cond = new BoogieFunctionCall(
                        "real_to_bool",
                        new() { new BoogieIdentifierExpr("$tmp1") }
                    );
                    body.AddStatement(new BoogieIfCmd(cond, thenBlk, null));
                    break;
                }

                case BrTableNode bt:
                {
                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                    body.AddStatement(
                        new BoogieAssignCmd(
                            new BoogieIdentifierExpr("idx"),
                            new BoogieFunctionCall(
                                "real_to_int",
                                new() { new BoogieIdentifierExpr("$tmp1") }
                            )
                        )
                    );

                    int k = bt.Targets.Count;
                    var idx = new BoogieIdentifierExpr("idx");

                    var outCond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.OR,
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.LT,
                            idx,
                            new BoogieLiteralExpr(0)
                        ),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.GE,
                            idx,
                            new BoogieLiteralExpr(k)
                        )
                    );
                    var outBlk = new BoogieStmtList();
                    outBlk.AddStatement(new BoogieGotoCmd(ResolveBranchTarget(bt.Default)));

                    var inBlk = new BoogieStmtList();
                    for (int i = 0; i < k; i++)
                    {
                        var condEq = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.EQ,
                            idx,
                            new BoogieLiteralExpr(i)
                        );
                        var thenBlk = new BoogieStmtList();
                        thenBlk.AddStatement(new BoogieGotoCmd(ResolveBranchTarget(bt.Targets[i])));
                        inBlk.AddStatement(new BoogieIfCmd(condEq, thenBlk, null));
                    }
                    inBlk.AddStatement(new BoogieGotoCmd(ResolveBranchTarget(bt.Default)));

                    body.AddStatement(new BoogieIfCmd(outCond, outBlk, inBlk));
                    break;
                }

                case UnreachableNode:
                {
                    body.AddStatement(new BoogieAssumeCmd(new BoogieLiteralExpr(false)));
                    break;
                }

                case SelectNode sel:
                {
                    TranslateNode(sel.V1, body);
                    TranslateNode(sel.V2, body);
                    TranslateNode(sel.Cond, body);

                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                    body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));
                    body.AddStatement(new BoogieCallCmd("popToTmp3", new(), new()));

                    var cond = new BoogieFunctionCall(
                        "real_to_bool",
                        new() { new BoogieIdentifierExpr("$tmp1") }
                    );

                    var thenBlk = new BoogieStmtList();
                    thenBlk.AddStatement(
                        new BoogieCallCmd(
                            "push",
                            new() { new BoogieIdentifierExpr("$tmp3") },
                            new()
                        )
                    );

                    var elseBlk = new BoogieStmtList();
                    elseBlk.AddStatement(
                        new BoogieCallCmd(
                            "push",
                            new() { new BoogieIdentifierExpr("$tmp2") },
                            new()
                        )
                    );

                    body.AddStatement(new BoogieIfCmd(cond, thenBlk, elseBlk));
                    break;
                }

                case IfNode ifn:
                {
                    TranslateNode(ifn.Condition, body);
                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                    var thenBlock = new BoogieStmtList();
                    foreach (var stmt in ifn.ThenBody)
                        TranslateNode(stmt, thenBlock);

                    BoogieStmtList? elseBlock = null;
                    if (ifn.ElseBody != null)
                    {
                        elseBlock = new BoogieStmtList();
                        foreach (var stmt in ifn.ElseBody)
                            TranslateNode(stmt, elseBlock);
                    }

                    var cond = new BoogieFunctionCall(
                        "real_to_bool",
                        new() { new BoogieIdentifierExpr("$tmp1") }
                    );
                    body.AddStatement(new BoogieIfCmd(cond, thenBlock, elseBlock));
                    break;
                }

                case ReturnNode:
                {
                    if (functionExitLabel == null)
                        functionExitLabel = GenerateLabel("func_exit");
                    body.AddStatement(new BoogieGotoCmd(functionExitLabel));
                    break;
                }

                case NopNode:
                {
                    body.AddStatement(new BoogieSkipCmd());
                    break;
                }

                case RawInstructionNode raw:
                {
                    var s = raw.Instruction;

                    // ignore noise from parsing non-executable syntax
                    if (
                        s.StartsWith("$", StringComparison.Ordinal)
                        || s.Contains("=>", StringComparison.Ordinal)
                        || s
                            is "module"
                                or "type"
                                or "func"
                                or "param"
                                or "result"
                                or "mut"
                                or "global"
                                or "table"
                                or "elem"
                    )
                    {
                        // ignore
                    }
                    else
                    {
                        body.AddStatement(
                            new BoogieCommentCmd($"// unhandled raw instruction: {s}")
                        );
                    }
                    break;
                }
            }
        }
    }
}*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BoogieAST;
using WasmToBoogie.Parser.Ast;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private readonly string contractName;
        private int labelCounter = 0;

        // Flag: assertion de pile à la sortie de fonction
        public bool EnableFooterStackAssert { get; set; } = false;

        // Module Boogie en construction
        private BoogieProgram? program;

        // Générateurs uniques
        private readonly HashSet<int> popArgsMade = new();
        private readonly HashSet<int> popDiscardMade = new();

        // État par fonction
        private List<BoogieIdentifierExpr>? currentLocalMap; // arg1..argN, loc1..locM
        private WasmFunction? currentFunction;
        private HashSet<string>? neededLoopStartLabels;
        private HashSet<string>? neededBlockEndLabels;
        private readonly Stack<LabelContext> labelStack = new();
        private string? functionExitLabel;

        // Globals created lazily + modifies tracking
        private readonly Dictionary<string, string> globalNameMap = new(StringComparer.Ordinal);
        private readonly HashSet<string> declaredBoogieGlobals = new(StringComparer.Ordinal);
        private HashSet<string>? currentModifiedGlobals;

        private class LabelContext
        {
            public string? WatLabel;
            public string? StartLabel; // loop "continue"
            public string EndLabel = ""; // block/loop end ("break")
            public bool IsLoop;
            public bool MarkEndUsed;
        }

        public WasmAstToBoogie(string contractName) => this.contractName = contractName;

        // ============================================================
        // Helpers
        // ============================================================

        private static bool AllDigits(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            for (int i = 0; i < s.Length; i++)
                if (s[i] < '0' || s[i] > '9')
                    return false;
            return true;
        }

        private static string NormalizeLabel(string raw) =>
            string.IsNullOrEmpty(raw) ? raw : (raw[0] == '$' ? raw[1..] : raw);

        private static string MapCalleeName(string target)
        {
            if (string.IsNullOrEmpty(target))
                return target;
            string name = target[0] == '$' ? target[1..] : target;
            return AllDigits(name) ? "func_" + name : name;
        }

        private static string SanitizeFunctionName(string? watName, string contractName)
        {
            if (!string.IsNullOrEmpty(watName))
            {
                var n = watName![0] == '$' ? watName.Substring(1) : watName;
                if (int.TryParse(n, out _))
                    return $"func_{n}";
                n = Regex.Replace(n, @"[^A-Za-z0-9_]", "_");
                if (!char.IsLetter(n[0]) && n[0] != '_')
                    n = "_" + n;
                return n;
            }
            return $"func_{contractName}";
        }

        private string GenerateLabel(string baseName) => $"{baseName}_{++labelCounter}";

        private static string SanitizeGlobalName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "g";
            var n = raw[0] == '$' ? raw.Substring(1) : raw;
            n = Regex.Replace(n, @"[^A-Za-z0-9_]", "_");
            if (!char.IsLetter(n[0]) && n[0] != '_')
                n = "_" + n;
            return "g_" + n;
        }

        private string ResolveGlobalKey(int? index, string? name)
        {
            if (!string.IsNullOrEmpty(name))
                return name!;
            if (index.HasValue)
                return index.Value.ToString();
            throw new NotSupportedException("Unknown global index/name");
        }

        private string EnsureGlobalVar(string watNameOrIndex)
        {
            string key = watNameOrIndex.StartsWith("$", StringComparison.Ordinal)
                ? watNameOrIndex
                : "$" + watNameOrIndex;

            if (!globalNameMap.TryGetValue(key, out var boogieName))
            {
                boogieName = SanitizeGlobalName(key);
                globalNameMap[key] = boogieName;
            }

            if (program != null && !declaredBoogieGlobals.Contains(boogieName))
            {
                program.Declarations.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent(boogieName, BoogieType.Real))
                );
                declaredBoogieGlobals.Add(boogieName);
            }

            return boogieName;
        }

        private string ResolveBranchTarget(string labOrDepth)
        {
            if (AllDigits(labOrDepth))
            {
                int depth = int.Parse(labOrDepth);
                if (depth < 0 || depth >= labelStack.Count)
                    return functionExitLabel ?? (functionExitLabel = GenerateLabel("func_exit"));

                var arr = labelStack.ToArray(); // top -> bottom
                var ctx = arr[depth];
                ctx.MarkEndUsed = true;

                return ctx.IsLoop ? (ctx.StartLabel ?? ctx.EndLabel) : ctx.EndLabel;
            }

            var norm = NormalizeLabel(labOrDepth);
            var hit = labelStack.FirstOrDefault(c => c.WatLabel == norm);
            if (hit != null)
                return hit.IsLoop ? (hit.StartLabel ?? hit.EndLabel) : hit.EndLabel;

            return functionExitLabel ?? (functionExitLabel = GenerateLabel("func_exit"));
        }

        // ============================================================
        // Public entry
        // ============================================================

        public BoogieProgram Convert(WasmModule wasmModule)
        {
            var p = new BoogieProgram();
            program = p;

            // Prelude est maintenant dans Conversion/Prelude/*
            AddPrelude(p);

            foreach (var func in wasmModule.Functions)
            {
                var (proc, impl) = TranslateFunction(func);
                p.Declarations.Add(proc);
                p.Declarations.Add(impl);
            }

            return p;
        }

        // ============================================================
        // Function translation
        // ============================================================

        private (BoogieProcedure, BoogieImplementation) TranslateFunction(WasmFunction func)
        {
            var inParams = new List<BoogieVariable>();
            var outParams = new List<BoogieVariable>();
            var locals = new List<BoogieVariable>();
            var body = new BoogieStmtList();

            currentFunction = func;
            functionExitLabel = null;
            PrecomputeLabelNeeds(func);

            currentModifiedGlobals = new HashSet<string>(StringComparer.Ordinal);

            int n = func.ParamCount;
            int m = func.LocalCount;
            int r = Math.Max(0, func.ResultCount);

            var indexToId = new List<BoogieIdentifierExpr>(n + m);

            for (int i = 1; i <= n; i++)
            {
                var name = $"arg{i}";
                locals.Add(new BoogieLocalVariable(new BoogieTypedIdent(name, BoogieType.Real)));
                indexToId.Add(new BoogieIdentifierExpr(name));
            }
            for (int i = 1; i <= m; i++)
            {
                var name = $"loc{i}";
                locals.Add(new BoogieLocalVariable(new BoogieTypedIdent(name, BoogieType.Real)));
                indexToId.Add(new BoogieIdentifierExpr(name));
            }

            // Helper locals used by translation
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("idx", BoogieType.Int)));
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("entry_sp", BoogieType.Int)));
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("load_i", BoogieType.Int)));
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("store_i", BoogieType.Int)));

            currentLocalMap = indexToId;

            // Prologue
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("entry_sp"),
                    new BoogieIdentifierExpr("$sp")
                )
            );
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("$tmp1"),
                    new BoogieLiteralExpr(new Pfloat(0))
                )
            );
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("$tmp2"),
                    new BoogieLiteralExpr(new Pfloat(0))
                )
            );
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("$tmp3"),
                    new BoogieLiteralExpr(new Pfloat(0))
                )
            );

            // Args : callee pop ses args
            if (n > 0)
            {
                EnsurePopArgsProc(n);
                body.AddStatement(
                    new BoogieAssumeCmd(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.GE,
                            new BoogieIdentifierExpr("$sp"),
                            new BoogieLiteralExpr(n)
                        )
                    )
                );
                body.AddStatement(
                    new BoogieCallCmd($"popArgs{n}", new(), indexToId.Take(n).ToList())
                );
            }

            // Locals init 0
            for (int i = n; i < n + m; i++)
                body.AddStatement(
                    new BoogieAssignCmd(indexToId[i], new BoogieLiteralExpr(new Pfloat(0)))
                );

            foreach (var node in func.Body)
                TranslateNode(node, body);

            if (!string.IsNullOrEmpty(functionExitLabel))
            {
                body.AddStatement(new BoogieSkipCmd(functionExitLabel + ":"));
                functionExitLabel = null;
            }

            var expected = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.ADD,
                new BoogieIdentifierExpr("entry_sp"),
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.SUB,
                    new BoogieLiteralExpr(r),
                    new BoogieLiteralExpr(n)
                )
            );

            if (EnableFooterStackAssert)
            {
                body.AddStatement(
                    new BoogieAssertCmd(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.EQ,
                            new BoogieIdentifierExpr("$sp"),
                            expected
                        )
                    )
                );
            }
            else
            {
                body.AddStatement(new BoogieCommentCmd("// footer stack assert disabled"));
            }

            string funcName = SanitizeFunctionName(func.Name, contractName);

            var mods = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", BoogieType.Real)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", BoogieType.Real)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", BoogieType.Real)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                new BoogieGlobalVariable(
                    new BoogieTypedIdent(
                        "$stack",
                        new BoogieMapType(BoogieType.Int, BoogieType.Real)
                    )
                ),
                new BoogieGlobalVariable(
                    new BoogieTypedIdent("$mem", new BoogieMapType(BoogieType.Int, BoogieType.Int))
                ),
            };

            if (currentModifiedGlobals != null)
                foreach (var g in currentModifiedGlobals)
                    mods.Add(new BoogieGlobalVariable(new BoogieTypedIdent(g, BoogieType.Real)));

            var proc = new BoogieProcedure(
                funcName,
                inParams,
                outParams,
                new(),
                mods,
                new(),
                new()
            );
            var impl = new BoogieImplementation(proc.Name, inParams, outParams, locals, body);

            // reset état
            currentLocalMap = null;
            currentFunction = null;
            neededLoopStartLabels = null;
            neededBlockEndLabels = null;
            labelStack.Clear();
            currentModifiedGlobals = null;

            return (proc, impl);
        }

        // ============================================================
        // Label pre-scan
        // ============================================================

        private void PrecomputeLabelNeeds(WasmFunction func)
        {
            neededLoopStartLabels = new HashSet<string>(StringComparer.Ordinal);
            neededBlockEndLabels = new HashSet<string>(StringComparer.Ordinal);
            var scope = new Stack<(string label, bool isLoop)>();

            void Walk(WasmNode n)
            {
                switch (n)
                {
                    case BlockNode blk:
                    {
                        bool hasUser =
                            !string.IsNullOrEmpty(blk.Label)
                            && blk.Label!.StartsWith("$", StringComparison.Ordinal);
                        if (hasUser)
                            scope.Push((blk.Label!.Substring(1), false));
                        foreach (var m in blk.Body)
                            Walk(m);
                        if (hasUser)
                            scope.Pop();
                        break;
                    }
                    case LoopNode lp:
                    {
                        bool hasUser =
                            !string.IsNullOrEmpty(lp.Label)
                            && lp.Label!.StartsWith("$", StringComparison.Ordinal);
                        if (hasUser)
                            scope.Push((lp.Label!.Substring(1), true));
                        foreach (var m in lp.Body)
                            Walk(m);
                        if (hasUser)
                            scope.Pop();
                        break;
                    }
                    case IfNode iff:
                        Walk(iff.Condition);
                        foreach (var m in iff.ThenBody)
                            Walk(m);
                        if (iff.ElseBody != null)
                            foreach (var m in iff.ElseBody)
                                Walk(m);
                        break;
                    case BinaryOpNode b:
                        Walk(b.Left);
                        Walk(b.Right);
                        break;
                    case UnaryOpNode u:
                        if (u.Operand != null)
                            Walk(u.Operand);
                        break;
                    case BrNode br:
                    {
                        var target = NormalizeLabel(br.Label);
                        foreach (var (lab, isLoop) in scope)
                        {
                            if (lab == target)
                            {
                                if (isLoop)
                                    neededLoopStartLabels!.Add(lab);
                                else
                                    neededBlockEndLabels!.Add(lab);
                                break;
                            }
                        }
                        break;
                    }
                    case BrIfNode bri:
                    {
                        Walk(bri.Condition);
                        var target = NormalizeLabel(bri.Label);
                        foreach (var (lab, isLoop) in scope)
                        {
                            if (lab == target)
                            {
                                if (isLoop)
                                    neededLoopStartLabels!.Add(lab);
                                else
                                    neededBlockEndLabels!.Add(lab);
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            foreach (var n in func.Body)
                Walk(n);
        }

        // ============================================================
        // Node translation
        // ============================================================

        private int ResolveLocalIndex(int? index, string? name)
        {
            if (index.HasValue)
                return index.Value;

            if (!string.IsNullOrEmpty(name))
            {
                if (
                    currentFunction != null
                    && currentFunction.LocalIndexByName.TryGetValue(name, out var idx)
                )
                    return idx;

                if (name[0] == '$' && int.TryParse(name.AsSpan(1), out var autoIdx))
                    return autoIdx;
            }

            throw new NotSupportedException($"Unknown local index/name: {name ?? "<null>"}");
        }

        private int ResolveIndirectResultCount(string? typeUse) => 0;

        private void TranslateNode(WasmNode node, BoogieStmtList body)
        {
            switch (node)
            {
                case ConstNode cn:
                {
                    if (
                        double.TryParse(
                            cn.Value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var dv
                        )
                    )
                    {
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieLiteralExpr(new Pfloat((float)dv)) },
                                new()
                            )
                        );
                    }
                    else
                    {
                        body.AddStatement(
                            new BoogieCommentCmd($"// unsupported const value: {cn.Value}")
                        );
                    }
                    break;
                }

                case LocalGetNode lg:
                {
                    int idx = ResolveLocalIndex(lg.Index, lg.Name);
                    var id = currentLocalMap![idx];
                    body.AddStatement(new BoogieCallCmd("push", new() { id }, new()));
                    break;
                }

                case LocalSetNode ls:
                {
                    int idx = ResolveLocalIndex(ls.Index, ls.Name);
                    var id = currentLocalMap![idx];
                    if (ls.Value != null)
                        TranslateNode(ls.Value, body);
                    EnsurePopArgsProc(1);
                    body.AddStatement(new BoogieCallCmd("popArgs1", new(), new() { id }));
                    break;
                }

                case LocalTeeNode lt:
                {
                    int idx = ResolveLocalIndex(lt.Index, lt.Name);
                    var id = currentLocalMap![idx];
                    EnsurePopArgsProc(1);
                    body.AddStatement(new BoogieCallCmd("popArgs1", new(), new() { id }));
                    body.AddStatement(new BoogieCallCmd("push", new() { id }, new()));
                    break;
                }

                // ===== NEW: global.get / global.set =====
                case GlobalGetNode gg:
                {
                    string gkey = ResolveGlobalKey(gg.Index, gg.Name);
                    string bname = EnsureGlobalVar(gkey);
                    body.AddStatement(
                        new BoogieCallCmd("push", new() { new BoogieIdentifierExpr(bname) }, new())
                    );
                    break;
                }

                case GlobalSetNode gs:
                {
                    string gkey = ResolveGlobalKey(gs.Index, gs.Name);
                    string bname = EnsureGlobalVar(gkey);

                    // folded form: (global.set $g <expr>)
                    if (gs.Value != null)
                        TranslateNode(gs.Value, body);

                    EnsurePopArgsProc(1);
                    body.AddStatement(
                        new BoogieCallCmd(
                            "popArgs1",
                            new(),
                            new() { new BoogieIdentifierExpr(bname) }
                        )
                    );

                    // IMPORTANT: Boogie framing (modifies)
                    currentModifiedGlobals?.Add(bname);
                    break;
                }

                case CallNode call:
                {
                    if (call.Args != null)
                        foreach (var a in call.Args)
                            TranslateNode(a, body);
                    string target = MapCalleeName(call.Target);
                    body.AddStatement(new BoogieCallCmd(target, new(), new()));
                    break;
                }

                // ===== call_indirect (conservative model) =====
                case CallIndirectNode ci:
                {
                    foreach (var a in ci.Args)
                        TranslateNode(a, body);
                    TranslateNode(ci.CalleeIndex, body);

                    int nArgs = ci.Args.Count;
                    EnsurePopDiscardProc(nArgs + 1); // args + idx
                    body.AddStatement(new BoogieCallCmd($"popDiscard{nArgs + 1}", new(), new()));

                    int r = ResolveIndirectResultCount(ci.TypeUse);
                    for (int i = 0; i < r; i++)
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieFunctionCall("nd_real", new()) },
                                new()
                            )
                        );

                    break;
                }

                // ===== return_call (tail-call) =====
                case ReturnCallNode rc:
                {
                    if (rc.Args != null)
                        foreach (var a in rc.Args)
                            TranslateNode(a, body);

                    string target = MapCalleeName(rc.Target);
                    body.AddStatement(new BoogieCallCmd(target, new(), new()));

                    if (functionExitLabel == null)
                        functionExitLabel = GenerateLabel("func_exit");
                    body.AddStatement(new BoogieGotoCmd(functionExitLabel));
                    break;
                }

                // ===== return_call_indirect (tail-call indirect) =====
                case ReturnCallIndirectNode rci:
                {
                    foreach (var a in rci.Args)
                        TranslateNode(a, body);
                    TranslateNode(rci.CalleeIndex, body);

                    int nArgs = rci.Args.Count;
                    EnsurePopDiscardProc(nArgs + 1);
                    body.AddStatement(new BoogieCallCmd($"popDiscard{nArgs + 1}", new(), new()));

                    int r = ResolveIndirectResultCount(rci.TypeUse);
                    for (int i = 0; i < r; i++)
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieFunctionCall("nd_real", new()) },
                                new()
                            )
                        );

                    if (functionExitLabel == null)
                        functionExitLabel = GenerateLabel("func_exit");
                    body.AddStatement(new BoogieGotoCmd(functionExitLabel));
                    break;
                }

                case MemoryOpNode mem:
                {
                    // Helpers communs
                    BoogieExpr Tmp1() => new BoogieIdentifierExpr("$tmp1");
                    BoogieExpr Tmp2() => new BoogieIdentifierExpr("$tmp2");

                    // --- helper: compute idx from an address already on the stack (top) ---
                    void PopAddrComputeIdx()
                    {
                        // pop address into $tmp1
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        // idx := real_to_int($tmp1) + offset
                        var addrInt = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            new BoogieFunctionCall("real_to_int", new() { (BoogieExpr)Tmp1() }),
                            new BoogieLiteralExpr(mem.Offset)
                        );

                        body.AddStatement(
                            new BoogieAssignCmd(new BoogieIdentifierExpr("idx"), addrInt)
                        );
                    }

                    // --- helper: compute idx from folded address node (mem.Address) ---
                    void EvalAddrComputeIdx()
                    {
                        if (mem.Address != null)
                            TranslateNode(mem.Address, body);
                        // address is now on stack
                        PopAddrComputeIdx();
                    }

                    // --- STORE path ---
                    bool isStore =
                        mem.Op
                        is "i32.store"
                            or "i64.store"
                            or "f32.store"
                            or "f64.store"
                            or "i32.store8"
                            or "i32.store16"
                            or "i64.store8"
                            or "i64.store16"
                            or "i64.store32";

                    if (isStore)
                    {
                        // WebAssembly store expects stack order: ... addr, value
                        // If you have folded nodes, keep the same order:
                        //   evaluate address first, then value, so top-of-stack is value, below is addr.
                        if (mem.Address != null)
                            TranslateNode(mem.Address, body);
                        if (mem.Value != null)
                            TranslateNode(mem.Value, body);

                        // pop value -> $tmp2
                        body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

                        // pop addr -> $tmp1 and compute idx
                        PopAddrComputeIdx();

                        // store_i := real_to_int($tmp2)
                        body.AddStatement(
                            new BoogieAssignCmd(
                                new BoogieIdentifierExpr("store_i"),
                                new BoogieFunctionCall("real_to_int", new() { (BoogieExpr)Tmp2() })
                            )
                        );

                        // dispatch write
                        switch (mem.Op)
                        {
                            case "i32.store":
                            case "f32.store":
                            case "i64.store32":
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "mem_write_u32",
                                        new()
                                        {
                                            new BoogieIdentifierExpr("idx"),
                                            new BoogieIdentifierExpr("store_i"),
                                        },
                                        new()
                                    )
                                );
                                break;

                            case "i64.store":
                            case "f64.store":
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "mem_write_u64",
                                        new()
                                        {
                                            new BoogieIdentifierExpr("idx"),
                                            new BoogieIdentifierExpr("store_i"),
                                        },
                                        new()
                                    )
                                );
                                break;

                            case "i32.store8":
                            case "i64.store8":
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "mem_write_u8",
                                        new()
                                        {
                                            new BoogieIdentifierExpr("idx"),
                                            new BoogieIdentifierExpr("store_i"),
                                        },
                                        new()
                                    )
                                );
                                break;

                            case "i32.store16":
                            case "i64.store16":
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "mem_write_u16",
                                        new()
                                        {
                                            new BoogieIdentifierExpr("idx"),
                                            new BoogieIdentifierExpr("store_i"),
                                        },
                                        new()
                                    )
                                );
                                break;

                            default:
                                body.AddStatement(
                                    new BoogieCommentCmd($"// unsupported store op: {mem.Op}")
                                );
                                break;
                        }

                        break; // done
                    }

                    // --- LOAD path ---
                    // evaluate address (folded) then pop it and compute idx
                    EvalAddrComputeIdx();

                    BoogieExpr idxExpr = new BoogieIdentifierExpr("idx");
                    var loadVar = new BoogieIdentifierExpr("load_i");

                    void CallRead(string procName)
                    {
                        body.AddStatement(
                            new BoogieCallCmd(procName, new() { idxExpr }, new() { loadVar })
                        );
                    }

                    void PushLoadedIntAsReal()
                    {
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieFunctionCall("int_to_real", new() { loadVar }) },
                                new()
                            )
                        );
                    }

                    void PushLoadedBits32AsReal()
                    {
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new()
                                {
                                    new BoogieFunctionCall("bits32_to_real", new() { loadVar }),
                                },
                                new()
                            )
                        );
                    }

                    void PushLoadedBits64AsReal()
                    {
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new()
                                {
                                    new BoogieFunctionCall("bits64_to_real", new() { loadVar }),
                                },
                                new()
                            )
                        );
                    }

                    switch (mem.Op)
                    {
                        case "i32.load":
                            CallRead("mem_read_s32");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load":
                            CallRead("mem_read_s64");
                            PushLoadedIntAsReal();
                            break;

                        case "f32.load":
                            CallRead("mem_read_u32");
                            PushLoadedBits32AsReal();
                            break;

                        case "f64.load":
                            CallRead("mem_read_u64");
                            PushLoadedBits64AsReal();
                            break;

                        case "i32.load8_s":
                            CallRead("mem_read_s8");
                            PushLoadedIntAsReal();
                            break;

                        case "i32.load8_u":
                            CallRead("mem_read_u8");
                            PushLoadedIntAsReal();
                            break;

                        case "i32.load16_s":
                            CallRead("mem_read_s16");
                            PushLoadedIntAsReal();
                            break;

                        case "i32.load16_u":
                            CallRead("mem_read_u16");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load8_s":
                            CallRead("mem_read_s8");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load8_u":
                            CallRead("mem_read_u8");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load16_s":
                            CallRead("mem_read_s16");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load16_u":
                            CallRead("mem_read_u16");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load32_s":
                            CallRead("mem_read_s32");
                            PushLoadedIntAsReal();
                            break;

                        case "i64.load32_u":
                            CallRead("mem_read_u32");
                            PushLoadedIntAsReal();
                            break;

                        default:
                            body.AddStatement(
                                new BoogieCommentCmd($"// unsupported memory op: {mem.Op}")
                            );
                            break;
                    }

                    break;
                }

                case UnaryOpNode un:
                {
                    if (un.Operand != null)
                        TranslateNode(un.Operand, body);

                    if (un.Op == "drop")
                    {
                        body.AddStatement(new BoogieCallCmd("pop", new(), new()));
                    }
                    else if (un.Op == "i32.eqz" || un.Op == "i64.eqz")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        var eqzExpr = new BoogieFunctionCall(
                            "bool_to_real",
                            new()
                            {
                                new BoogieBinaryOperation(
                                    BoogieBinaryOperation.Opcode.EQ,
                                    new BoogieIdentifierExpr("$tmp1"),
                                    new BoogieLiteralExpr(new Pfloat(0))
                                ),
                            }
                        );
                        body.AddStatement(new BoogieCallCmd("push", new() { eqzExpr }, new()));
                    }
                    else if (un.Op == "i32.wrap_i64" || un.Op == "i64.wrap_i64")
                    {
                        body.AddStatement(
                            new BoogieCommentCmd("// wrap: no-op under real semantics")
                        );
                    }
                    else if (un.Op == "f32.abs" || un.Op == "f64.abs")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        var absExpr = new BoogieFunctionCall(
                            "abs_real",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { absExpr }, new()));
                    }
                    else if (un.Op == "f32.neg" || un.Op == "f64.neg")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        var negExpr = new BoogieUnaryOperation(
                            BoogieUnaryOperation.Opcode.NEG,
                            new BoogieIdentifierExpr("$tmp1")
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { negExpr }, new()));
                    }
                    else if (un.Op == "f32.sqrt" || un.Op == "f64.sqrt")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        var sqrtExpr = new BoogieFunctionCall(
                            "sqrt_real",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { sqrtExpr }, new()));
                    }
                    else if (un.Op == "f32.nearest" || un.Op == "f64.nearest")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        var nearestExpr = new BoogieFunctionCall(
                            "nearest_real",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );
                        body.AddStatement(new BoogieCallCmd("push", new() { nearestExpr }, new()));
                    }
                    else if (un.Op == "f32.floor" || un.Op == "f64.floor")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        var flo = new BoogieFunctionCall(
                            "floor_real",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );
                        body.AddStatement(new BoogieCallCmd("push", new() { flo }, new()));
                    }
                    else
                    {
                        body.AddStatement(
                            new BoogieCommentCmd($"// unsupported unary op: {un.Op}")
                        );
                    }
                    break;
                }

                case BinaryOpNode bn:
                {
                    TranslateNode(bn.Left, body);
                    TranslateNode(bn.Right, body);

                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                    body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));
                    var tmp1 = new BoogieIdentifierExpr("$tmp1");
                    var tmp2 = new BoogieIdentifierExpr("$tmp2");

                    if (
                        bn.Op
                        is "i32.add"
                            or "i64.add"
                            or "f32.add"
                            or "f64.add"
                            or "i32.sub"
                            or "i64.sub"
                            or "f32.sub"
                            or "f64.sub"
                            or "i32.mul"
                            or "i64.mul"
                            or "f32.mul"
                            or "f64.mul"
                            or "i32.div_s"
                            or "i64.div_s"
                            or "f32.div"
                            or "f64.div"
                            or "i32.div_u"
                            or "i64.div_u"
                    )
                    {
                        var opKind = bn.Op switch
                        {
                            "i32.add" or "i64.add" or "f32.add" or "f64.add" =>
                                BoogieBinaryOperation.Opcode.ADD,
                            "i32.sub" or "i64.sub" or "f32.sub" or "f64.sub" =>
                                BoogieBinaryOperation.Opcode.SUB,
                            "i32.mul" or "i64.mul" or "f32.mul" or "f64.mul" =>
                                BoogieBinaryOperation.Opcode.MUL,
                            _ => BoogieBinaryOperation.Opcode.DIV,
                        };
                        var arithExpr = new BoogieBinaryOperation(opKind, tmp2, tmp1);
                        body.AddStatement(new BoogieCallCmd("push", new() { arithExpr }, new()));
                    }
                    else if (
                        bn.Op
                        is "i32.eq"
                            or "i64.eq"
                            or "f32.eq"
                            or "f64.eq"
                            or "i32.ne"
                            or "i64.ne"
                            or "f32.ne"
                            or "f64.ne"
                            or "i32.lt_s"
                            or "i64.lt_s"
                            or "i32.lt_u"
                            or "i64.lt_u"
                            or "f32.lt"
                            or "f64.lt"
                            or "i32.le_s"
                            or "i64.le_s"
                            or "i32.le_u"
                            or "i64.le_u"
                            or "f32.le"
                            or "f64.le"
                            or "i32.gt_s"
                            or "i64.gt_s"
                            or "i32.gt_u"
                            or "i64.gt_u"
                            or "f32.gt"
                            or "f64.gt"
                            or "i32.ge_s"
                            or "i64.ge_s"
                            or "i32.ge_u"
                            or "i64.ge_u"
                            or "f32.ge"
                            or "f64.ge"
                    )
                    {
                        BoogieExpr cmpExpr = bn.Op switch
                        {
                            "i32.eq" or "i64.eq" or "f32.eq" or "f64.eq" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.EQ,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            "i32.ne" or "i64.ne" or "f32.ne" or "f64.ne" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.NEQ,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            "i32.lt_s"
                            or "i64.lt_s"
                            or "i32.lt_u"
                            or "i64.lt_u"
                            or "f32.lt"
                            or "f64.lt" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.LT,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            "i32.le_s"
                            or "i64.le_s"
                            or "i32.le_u"
                            or "i64.le_u"
                            or "f32.le"
                            or "f64.le" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.LE,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            "i32.gt_s"
                            or "i64.gt_s"
                            or "i32.gt_u"
                            or "i64.gt_u"
                            or "f32.gt"
                            or "f64.gt" => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.GT,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                            _ // ge
                            => new BoogieFunctionCall(
                                "bool_to_real",
                                new()
                                {
                                    new BoogieBinaryOperation(
                                        BoogieBinaryOperation.Opcode.GE,
                                        tmp2,
                                        tmp1
                                    ),
                                }
                            ),
                        };
                        body.AddStatement(new BoogieCallCmd("push", new() { cmpExpr }, new()));
                    }
                    else if (bn.Op == "f32.min" || bn.Op == "f64.min")
                    {
                        // operands already evaluated
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // y
                        body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // x

                        var minExpr = new BoogieFunctionCall(
                            "min_real",
                            new()
                            {
                                new BoogieIdentifierExpr("$tmp2"),
                                new BoogieIdentifierExpr("$tmp1"),
                            }
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { minExpr }, new()));
                    }
                    else if (bn.Op == "f32.max" || bn.Op == "f64.max")
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // y
                        body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // x

                        var maxExpr = new BoogieFunctionCall(
                            "max_real",
                            new()
                            {
                                new BoogieIdentifierExpr("$tmp2"),
                                new BoogieIdentifierExpr("$tmp1"),
                            }
                        );

                        body.AddStatement(new BoogieCallCmd("push", new() { maxExpr }, new()));
                    }
                    else
                    {
                        body.AddStatement(
                            new BoogieCommentCmd($"// unsupported binary op: {bn.Op}")
                        );
                    }
                    break;
                }

                case BlockNode blk:
                {
                    // Ignore module wrapper and type blocks (not executable). Still traverse func blocks.
                    if (blk.Label == "module")
                    {
                        foreach (var child in blk.Body)
                            TranslateNode(child, body);
                        break;
                    }
                    if (blk.Label == "type")
                    {
                        // not executable: ignore completely
                        break;
                    }
                    if (blk.Label == "func")
                    {
                        foreach (var child in blk.Body)
                            TranslateNode(child, body);
                        break;
                    }

                    string? wat =
                        blk.Label != null && blk.Label.StartsWith("$")
                            ? blk.Label.Substring(1)
                            : null;

                    var ctx = new LabelContext
                    {
                        WatLabel = wat,
                        IsLoop = false,
                        StartLabel = null,
                        EndLabel = GenerateLabel(wat != null ? $"{wat}_end" : "block_end"),
                    };
                    labelStack.Push(ctx);

                    foreach (var child in blk.Body)
                        TranslateNode(child, body);

                    body.AddStatement(new BoogieSkipCmd(ctx.EndLabel + ":"));
                    labelStack.Pop();
                    break;
                }

                case LoopNode loop:
                {
                    string? wat =
                        loop.Label != null && loop.Label.StartsWith("$")
                            ? loop.Label.Substring(1)
                            : null;

                    var ctx = new LabelContext
                    {
                        WatLabel = wat,
                        IsLoop = true,
                        StartLabel = GenerateLabel(wat != null ? $"{wat}_start" : "loop_start"),
                        EndLabel = GenerateLabel(wat != null ? $"{wat}_end" : "loop_end"),
                    };
                    labelStack.Push(ctx);

                    body.AddStatement(new BoogieSkipCmd(ctx.StartLabel + ":")); // continue
                    foreach (var child in loop.Body)
                        TranslateNode(child, body);
                    body.AddStatement(new BoogieSkipCmd(ctx.EndLabel + ":")); // break

                    labelStack.Pop();
                    break;
                }

                case BrNode br:
                {
                    var target = ResolveBranchTarget(br.Label);
                    body.AddStatement(new BoogieGotoCmd(target));
                    break;
                }

                case BrIfNode brIf:
                {
                    TranslateNode(brIf.Condition, body);
                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                    var target = ResolveBranchTarget(brIf.Label);
                    var thenBlk = new BoogieStmtList();
                    thenBlk.AddStatement(new BoogieGotoCmd(target));
                    var cond = new BoogieFunctionCall(
                        "real_to_bool",
                        new() { new BoogieIdentifierExpr("$tmp1") }
                    );
                    body.AddStatement(new BoogieIfCmd(cond, thenBlk, null));
                    break;
                }

                case BrTableNode bt:
                {
                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                    body.AddStatement(
                        new BoogieAssignCmd(
                            new BoogieIdentifierExpr("idx"),
                            new BoogieFunctionCall(
                                "real_to_int",
                                new() { new BoogieIdentifierExpr("$tmp1") }
                            )
                        )
                    );

                    int k = bt.Targets.Count;
                    var idx = new BoogieIdentifierExpr("idx");

                    var outCond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.OR,
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.LT,
                            idx,
                            new BoogieLiteralExpr(0)
                        ),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.GE,
                            idx,
                            new BoogieLiteralExpr(k)
                        )
                    );
                    var outBlk = new BoogieStmtList();
                    outBlk.AddStatement(new BoogieGotoCmd(ResolveBranchTarget(bt.Default)));

                    var inBlk = new BoogieStmtList();
                    for (int i = 0; i < k; i++)
                    {
                        var condEq = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.EQ,
                            idx,
                            new BoogieLiteralExpr(i)
                        );
                        var thenBlk = new BoogieStmtList();
                        thenBlk.AddStatement(new BoogieGotoCmd(ResolveBranchTarget(bt.Targets[i])));
                        inBlk.AddStatement(new BoogieIfCmd(condEq, thenBlk, null));
                    }
                    inBlk.AddStatement(new BoogieGotoCmd(ResolveBranchTarget(bt.Default)));

                    body.AddStatement(new BoogieIfCmd(outCond, outBlk, inBlk));
                    break;
                }

                case UnreachableNode:
                {
                    body.AddStatement(new BoogieAssumeCmd(new BoogieLiteralExpr(false)));
                    break;
                }

                case SelectNode sel:
                {
                    TranslateNode(sel.V1, body);
                    TranslateNode(sel.V2, body);
                    TranslateNode(sel.Cond, body);

                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                    body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));
                    body.AddStatement(new BoogieCallCmd("popToTmp3", new(), new()));

                    var cond = new BoogieFunctionCall(
                        "real_to_bool",
                        new() { new BoogieIdentifierExpr("$tmp1") }
                    );

                    var thenBlk = new BoogieStmtList();
                    thenBlk.AddStatement(
                        new BoogieCallCmd(
                            "push",
                            new() { new BoogieIdentifierExpr("$tmp3") },
                            new()
                        )
                    );

                    var elseBlk = new BoogieStmtList();
                    elseBlk.AddStatement(
                        new BoogieCallCmd(
                            "push",
                            new() { new BoogieIdentifierExpr("$tmp2") },
                            new()
                        )
                    );

                    body.AddStatement(new BoogieIfCmd(cond, thenBlk, elseBlk));
                    break;
                }

                case IfNode ifn:
                {
                    TranslateNode(ifn.Condition, body);
                    body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                    var thenBlock = new BoogieStmtList();
                    foreach (var stmt in ifn.ThenBody)
                        TranslateNode(stmt, thenBlock);

                    BoogieStmtList? elseBlock = null;
                    if (ifn.ElseBody != null)
                    {
                        elseBlock = new BoogieStmtList();
                        foreach (var stmt in ifn.ElseBody)
                            TranslateNode(stmt, elseBlock);
                    }

                    var cond = new BoogieFunctionCall(
                        "real_to_bool",
                        new() { new BoogieIdentifierExpr("$tmp1") }
                    );
                    body.AddStatement(new BoogieIfCmd(cond, thenBlock, elseBlock));
                    break;
                }

                case ReturnNode:
                {
                    if (functionExitLabel == null)
                        functionExitLabel = GenerateLabel("func_exit");
                    body.AddStatement(new BoogieGotoCmd(functionExitLabel));
                    break;
                }

                case NopNode:
                {
                    body.AddStatement(new BoogieSkipCmd());
                    break;
                }

                case RawInstructionNode raw:
                {
                    var s = raw.Instruction;

                    // ignore noise from parsing non-executable syntax
                    if (
                        s.StartsWith("$", StringComparison.Ordinal)
                        || s.Contains("=>", StringComparison.Ordinal)
                        || s
                            is "module"
                                or "type"
                                or "func"
                                or "param"
                                or "result"
                                or "mut"
                                or "global"
                                or "table"
                                or "elem"
                    )
                    {
                        // ignore
                    }
                    else
                    {
                        body.AddStatement(
                            new BoogieCommentCmd($"// unhandled raw instruction: {s}")
                        );
                    }
                    break;
                }
            }
        }
    }
}
