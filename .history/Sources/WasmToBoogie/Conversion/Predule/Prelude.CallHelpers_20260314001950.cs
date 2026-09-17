using System.Collections.Generic;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        // popArgsN (inline, returns a1..aN) — callee pops args
        private void EnsurePopArgsProc(int n)
        {
            if (n <= 0 || program == null || popArgsMade.Contains(n))
                return;
            popArgsMade.Add(n);

            var outs = new List<BoogieVariable>();
            for (int i = 1; i <= n; i++)
                outs.Add(new BoogieFormalParam(new BoogieTypedIdent($"a{i}", BoogieType.Real)));

            // -------- requires --------
            var requires = new List<BoogieExpr>
            {
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.GE,
                    new BoogieIdentifierExpr("$sp"),
                    new BoogieLiteralExpr(n)
                ),
            };

            // -------- ensures --------
            var ensures = new List<BoogieExpr>();

            // $sp == old($sp) - n
            ensures.Add(
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    new BoogieIdentifierExpr("$sp"),
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.SUB,
                        new BoogieOldExpr(new BoogieIdentifierExpr("$sp")),
                        new BoogieLiteralExpr(n)
                    )
                )
            );

            // 0 <= $sp
            ensures.Add(
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieLiteralExpr(0),
                    new BoogieIdentifierExpr("$sp")
                )
            );

            // forall i:int :: $stack[i] == old($stack)[i]
            var qi = new BoogieIdentifierExpr("i");
            ensures.Add(
                new BoogieQuantifiedExpr(
                    isForall: true,
                    qvars: new List<BoogieIdentifierExpr> { qi },
                    qvarTypes: new List<BoogieType> { BoogieType.Int },
                    bodyExpr: new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        new BoogieMapSelect(new BoogieIdentifierExpr("$stack"), qi),
                        new BoogieMapSelect(
                            new BoogieOldExpr(new BoogieIdentifierExpr("$stack")),
                            qi
                        )
                    )
                )
            );

            // ensures a_i == old($stack)[ old($sp) - (n - i + 1) ]
            for (int i = 1; i <= n; i++)
            {
                int offset = n - i + 1;
                ensures.Add(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        new BoogieIdentifierExpr($"a{i}"),
                        new BoogieMapSelect(
                            new BoogieOldExpr(new BoogieIdentifierExpr("$stack")),
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.SUB,
                                new BoogieOldExpr(new BoogieIdentifierExpr("$sp")),
                                new BoogieLiteralExpr(offset)
                            )
                        )
                    )
                );
            }

            var proc = new BoogieProcedure(
                $"popArgs{n}",
                new(),
                outs,
                new() { new BoogieAttribute("inline", 1) },
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
                requires,
                ensures
            );
            program.Declarations.Add(proc);

            // -------- implementation --------
            var body = new BoogieStmtList();

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
            program.Declarations.Add(impl);
        }

        // popDiscardN (inline, just decreases $sp)
        private void EnsurePopDiscardProc(int n)
        {
            if (n <= 0 || program == null || popDiscardMade.Contains(n))
                return;
            popDiscardMade.Add(n);

            var requires = new List<BoogieExpr>
            {
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.GE,
                    new BoogieIdentifierExpr("$sp"),
                    new BoogieLiteralExpr(n)
                ),
            };

            var ensures = new List<BoogieExpr>
            {
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    new BoogieIdentifierExpr("$sp"),
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.SUB,
                        new BoogieOldExpr(new BoogieIdentifierExpr("$sp")),
                        new BoogieLiteralExpr(n)
                    )
                ),
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieLiteralExpr(0),
                    new BoogieIdentifierExpr("$sp")
                ),
            };

            var proc = new BoogieProcedure(
                $"popDiscard{n}",
                new(),
                new(),
                new() { new BoogieAttribute("inline", 1) },
                new() { new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)) },
                requires,
                ensures
            );
            program.Declarations.Add(proc);

            var body = new BoogieStmtList();

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
            program.Declarations.Add(impl);
        }
    }
}
