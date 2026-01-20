using System;
using System.Collections.Generic;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeMemory(BoogieProgram program)
        {
            AddMemoryGlobals(program);

            // Helpers memory-level
            AddToU8(program);

            // Read/write procedures
            AddMemoryReadProcedures(program);
            AddMemoryWriteProcedures(program);
        }

        private static void AddMemoryGlobals(BoogieProgram program)
        {
            // byte-addressed memory: map int -> int (0..255)
            program.Declarations.Add(
                new BoogieGlobalVariable(
                    new BoogieTypedIdent("$mem", new BoogieMapType(BoogieType.Int, BoogieType.Int))
                )
            );

            // pages of 64KiB (optional usage later)
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$mem_pages", BoogieType.Int))
            );
        }

        // ------------------------------------------------------------
        // to_u8(x): int -> int in [0,256)
        // ------------------------------------------------------------
        private static void AddToU8(BoogieProgram program)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int));
            var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Int));
            program.Declarations.Add(new BoogieFunction("to_u8", new() { x }, new() { r }));

            // axiom forall x:int :: 0 <= to_u8(x) < 256
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

        // ------------------------------------------------------------
        // Byte extractor helpers: byte<i>_<w>(x) in [0,256)
        // ------------------------------------------------------------
        private static void AddByteExtractor(BoogieProgram program, string name)
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

        private static void AddAllByteExtractors(BoogieProgram program)
        {
            // 16-bit
            AddByteExtractor(program, "byte0_16");
            AddByteExtractor(program, "byte1_16");

            // 32-bit
            AddByteExtractor(program, "byte0_32");
            AddByteExtractor(program, "byte1_32");
            AddByteExtractor(program, "byte2_32");
            AddByteExtractor(program, "byte3_32");

            // 64-bit
            for (int i = 0; i < 8; i++)
                AddByteExtractor(program, $"byte{i}_64");
        }

        // ------------------------------------------------------------
        // Memory WRITE procedures (u8/u16/u32/u64)
        // ------------------------------------------------------------
        private static void AddMemoryWriteProcedures(BoogieProgram program)
        {
            // Declare extractors (abstract) first
            AddAllByteExtractors(program);

            // modifies set for $mem
            var memModSet = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(
                    new BoogieTypedIdent("$mem", new BoogieMapType(BoogieType.Int, BoogieType.Int))
                ),
            };

            // mem_write_u8(a:int, v:int)
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
                    memModSet,
                    new(),
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

            // mem_write_u16(a, v) little-endian
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
                    memModSet,
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
                    memModSet,
                    new(),
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
                    memModSet,
                    new(),
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

        // ------------------------------------------------------------
        // Memory READ procedures (u8/s8/u16/s16/u32/s32/u64/s64)
        // ------------------------------------------------------------
        private static void AddMemoryReadProcedures(BoogieProgram program)
        {
            static List<BoogieVariable> InsA() =>
                new() { new BoogieFormalParam(new BoogieTypedIdent("a", BoogieType.Int)) };

            static List<BoogieVariable> OutsResultInt() =>
                new() { new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Int)) };

            // mem_read_u8(a): int (0..255)
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_u8",
                    ins,
                    outs,
                    new() { new BoogieAttribute("inline", true) },
                    new(), // no modifies
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

                program.Declarations.Add(
                    new BoogieImplementation("mem_read_u8", ins, outs, new(), body)
                );
            }

            // mem_read_s8(a): int (-128..127)
            {
                var ins = InsA();
                var outs = OutsResultInt();

                var proc = new BoogieProcedure(
                    "mem_read_s8",
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

                program.Declarations.Add(
                    new BoogieImplementation("mem_read_s8", ins, outs, locals, body)
                );
            }

            // mem_read_u16(a): int (0..65535) little-endian
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

                program.Declarations.Add(
                    new BoogieImplementation("mem_read_u16", ins, outs, locals, body)
                );
            }

            // mem_read_s16(a): int (-32768..32767)
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

                program.Declarations.Add(
                    new BoogieImplementation("mem_read_s16", ins, outs, locals, body)
                );
            }

            // mem_read_u32(a): int (0..2^32-1) little-endian
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

                program.Declarations.Add(
                    new BoogieImplementation("mem_read_u32", ins, outs, locals, body)
                );
            }

            // mem_read_s32(a): int (signed 32)
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

                program.Declarations.Add(
                    new BoogieImplementation("mem_read_s32", ins, outs, locals, body)
                );
            }

            // mem_read_u64(a): int (little-endian) (no upper bound axiom)
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

                program.Declarations.Add(
                    new BoogieImplementation("mem_read_u64", ins, outs, locals, body)
                );
            }

            // mem_read_s64(a): int (abstraction: result := mem_read_u64(a))
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

                program.Declarations.Add(
                    new BoogieImplementation("mem_read_s64", ins, outs, locals, body)
                );
            }
        }
    }
}
