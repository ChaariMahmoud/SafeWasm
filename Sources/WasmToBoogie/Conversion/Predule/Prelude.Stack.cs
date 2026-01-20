using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeStackOps(BoogieProgram program)
        {
            AddPush(program);
            AddPopToTmp(program, "popToTmp1", "$tmp1");
            AddPopToTmp(program, "popToTmp2", "$tmp2");
            AddPopToTmp(program, "popToTmp3", "$tmp3");
            AddPop(program);
        }

        private static void AddPush(BoogieProgram program)
        {
            // procedure {:inline true} push(val: real);
            var ins = new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("val", BoogieType.Real)),
            };

            var proc = new BoogieProcedure(
                "push",
                ins,
                new(), // no outputs
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

            // $stack[$sp] := val;
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieMapSelect(
                        new BoogieIdentifierExpr("$stack"),
                        new BoogieIdentifierExpr("$sp")
                    ),
                    new BoogieIdentifierExpr("val")
                )
            );

            // $sp := $sp + 1;
            var sp = new BoogieIdentifierExpr("$sp");
            body.AddStatement(
                new BoogieAssignCmd(
                    sp,
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.ADD,
                        sp,
                        new BoogieLiteralExpr(1)
                    )
                )
            );

            var impl = new BoogieImplementation("push", ins, new(), new(), body);
            program.Declarations.Add(impl);
        }

        private static void AddPopToTmp(BoogieProgram program, string procName, string tmpGlobal)
        {
            // procedure popToTmpX();
            var proc = new BoogieProcedure(
                procName,
                new(), // ins
                new(), // outs
                new(), // attributes (NOT inline in your current code; keep as-is)
                new()
                {
                    new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                    new BoogieGlobalVariable(
                        new BoogieTypedIdent(
                            "$stack",
                            new BoogieMapType(BoogieType.Int, BoogieType.Real)
                        )
                    ),
                    new BoogieGlobalVariable(new BoogieTypedIdent(tmpGlobal, BoogieType.Real)),
                },
                new(),
                new()
            );
            program.Declarations.Add(proc);

            var body = new BoogieStmtList();

            // assume $sp > 0;
            body.AddStatement(
                new BoogieAssumeCmd(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GT,
                        new BoogieIdentifierExpr("$sp"),
                        new BoogieLiteralExpr(0)
                    )
                )
            );

            // $sp := $sp - 1;
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

            // $tmpX := $stack[$sp];
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr(tmpGlobal),
                    new BoogieMapSelect(
                        new BoogieIdentifierExpr("$stack"),
                        new BoogieIdentifierExpr("$sp")
                    )
                )
            );

            var impl = new BoogieImplementation(procName, new(), new(), new(), body);
            program.Declarations.Add(impl);
        }

        private static void AddPop(BoogieProgram program)
        {
            // procedure pop();
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

            // assume $sp > 0;
            body.AddStatement(
                new BoogieAssumeCmd(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GT,
                        new BoogieIdentifierExpr("$sp"),
                        new BoogieLiteralExpr(0)
                    )
                )
            );

            // $sp := $sp - 1;
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
    }
}
