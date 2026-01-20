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
            program.Declarations.Add(proc);

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
            program.Declarations.Add(impl);
        }

        // popDiscardN (inline, just decreases $sp)
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
            program.Declarations.Add(proc);

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
            program.Declarations.Add(impl);
        }
    }
}
