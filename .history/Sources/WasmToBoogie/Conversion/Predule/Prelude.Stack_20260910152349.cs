using System.Numerics;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {

        private static BoogieIdentifierExpr Id(string name) => new BoogieIdentifierExpr(name);

        private static BoogieLiteralExpr Int(int v) => new BoogieLiteralExpr(new BigInteger(v));

        private static BoogieLiteralExpr Real(double v) =>
            new BoogieLiteralExpr(new Pfloat((float)v));

        private static BoogieExpr Le(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LE, a, b);

        private static BoogieExpr Lt(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LT, a, b);

        private static BoogieExpr Ge(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, a, b);

        private static BoogieExpr Gt(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GT, a, b);

        private static BoogieExpr Eq(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, a, b);

        private static BoogieExpr Neq(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.NEQ, a, b);

        private static BoogieExpr Add(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.ADD, a, b);

        private static BoogieExpr Sub(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.SUB, a, b);

        private static BoogieExpr And(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, a, b);

        private static BoogieExpr Imp(BoogieExpr a, BoogieExpr b) =>
            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.IMP, a, b);

        private static BoogieExpr Old(BoogieExpr e) => new BoogieOldExpr(e);

        private static BoogieMapSelect StackAt(BoogieExpr idx) =>
            new BoogieMapSelect(Id("$stack"), idx);

        private void AddPreludeStackOps(BoogieProgram program)
        {
            AddInitRuntime(program);
            AddPush(program);
            AddPopToTmp(program, "popToTmp1", "$tmp1");
            AddPopToTmp(program, "popToTmp2", "$tmp2");
            AddPopToTmp(program, "popToTmp3", "$tmp3");
            AddPop(program);
        }

        private static void AddInitRuntime(BoogieProgram program)
        {
            var attrs = new List<BoogieAttribute> { new BoogieAttribute("inline", 1) };

            var modifies = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", WasmValueBoogieType())),
new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", WasmValueBoogieType())),
new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", WasmValueBoogieType())),
            };

            var ensures = new List<BoogieExpr> { Eq(Id("$sp"), Int(0)), Le(Int(0), Id("$sp")) };

            var proc = new BoogieProcedure(
                "InitRuntime",
                new(),
                new(),
                attrs,
                modifies,
                pre: null,
                post: ensures
            );
            program.Declarations.Add(proc);

            var body = new BoogieStmtList();
            body.AddStatement(new BoogieAssignCmd(Id("$sp"), Int(0)));
body.AddStatement(new BoogieAssignCmd(Id("$tmp1"), UndefValue()));
body.AddStatement(new BoogieAssignCmd(Id("$tmp2"), UndefValue()));
body.AddStatement(new BoogieAssignCmd(Id("$tmp3"), UndefValue()));

            var impl = new BoogieImplementation("InitRuntime", new(), new(), new(), body);
            program.Declarations.Add(impl);
        }

        private static void AddPush(BoogieProgram program)
        {
            var ins = new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("val", WasmValueBoogieType())),
            };

            var attrs = new List<BoogieAttribute> { new BoogieAttribute("inline", 1) };

            var modifies = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                new BoogieGlobalVariable(
                    new BoogieTypedIdent(
                        "$stack",
                        WasmStackBoogieType()
                    )
                ),
            };

            // requires 0 <= $sp
            var requires = new List<BoogieExpr> { Le(Int(0), Id("$sp")) };

            // ensures $sp == old($sp) + 1
            var post1 = Eq(Id("$sp"), Add(Old(Id("$sp")), Int(1)));

            // ensures $stack[old($sp)] == val
            var post2 = Eq(StackAt(Old(Id("$sp"))), Id("val"));

            // ensures (forall i:int :: i != old($sp) ==> $stack[i] == old($stack)[i])
            var i = Id("i");
            var oldSp = Old(Id("$sp"));
            var forallBody = Imp(
                Neq(i, oldSp),
                Eq(
                    StackAt(i),
                    new BoogieMapSelect(Old(Id("$stack")), i) // old($stack)[i]
                )
            );

            var post3 = new BoogieQuantifiedExpr(
                isForall: true,
                qvars: new List<BoogieIdentifierExpr> { i },
                qvarTypes: new List<BoogieType> { BoogieType.Int },
                bodyExpr: forallBody
            );

            // ensures 0 <= $sp
            var post4 = Le(Int(0), Id("$sp"));

            var ensures = new List<BoogieExpr> { post1, post2, post3, post4 };

            var proc = new BoogieProcedure(
                "push",
                ins,
                new(),
                attrs,
                modifies,
                pre: requires,
                post: ensures
            );
            program.Declarations.Add(proc);

            // implementation push(val: real)
            var body = new BoogieStmtList();

            body.AddStatement(
                new BoogieAssignCmd(new BoogieMapSelect(Id("$stack"), Id("$sp")), Id("val"))
            );

            body.AddStatement(new BoogieAssignCmd(Id("$sp"), Add(Id("$sp"), Int(1))));

            var impl = new BoogieImplementation("push", ins, new(), new(), body);
            program.Declarations.Add(impl);
        }

        private static void AddPopToTmp(BoogieProgram program, string procName, string tmpGlobal)
        {
            var attrs = new List<BoogieAttribute> { new BoogieAttribute("inline", 1) };

            var modifies = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                new BoogieGlobalVariable(new BoogieTypedIdent(tmpGlobal, WasmValueBoogieType())),
                // $stack n'est pas modifié
            };

            var requires = new List<BoogieExpr>
            {
                Gt(Id("$sp"), Int(0)), // $sp > 0
            };

            var oldSp = Old(Id("$sp"));
            var newSp = Sub(oldSp, Int(1));

            // ensures $sp == old($sp) - 1
            var post1 = Eq(Id("$sp"), newSp);

            // ensures $tmpX == old($stack)[old($sp) - 1]
            var post2 = Eq(
                Id(tmpGlobal),
                new BoogieMapSelect(Old(Id("$stack")), Sub(oldSp, Int(1)))
            );

            // ensures (forall i:int :: $stack[i] == old($stack)[i])
            var i = Id("i");
            var post3 = new BoogieQuantifiedExpr(
                isForall: true,
                qvars: new List<BoogieIdentifierExpr> { i },
                qvarTypes: new List<BoogieType> { BoogieType.Int },
                bodyExpr: Eq(StackAt(i), new BoogieMapSelect(Old(Id("$stack")), i))
            );

            // ensures 0 <= $sp
            var post4 = Le(Int(0), Id("$sp"));

            var ensures = new List<BoogieExpr> { post1, post3, post2, post4 };

            var proc = new BoogieProcedure(
                procName,
                new(),
                new(),
                attrs,
                modifies,
                pre: requires,
                post: ensures
            );
            program.Declarations.Add(proc);

            // implementation
            var body = new BoogieStmtList();

            // $sp := $sp - 1;
            body.AddStatement(new BoogieAssignCmd(Id("$sp"), Sub(Id("$sp"), Int(1))));

            // $tmpX := $stack[$sp];
            body.AddStatement(new BoogieAssignCmd(Id(tmpGlobal), StackAt(Id("$sp"))));

            var impl = new BoogieImplementation(procName, new(), new(), new(), body);
            program.Declarations.Add(impl);
        }

        private static void AddPop(BoogieProgram program)
        {
            var attrs = new List<BoogieAttribute> { new BoogieAttribute("inline", 1) };

            var modifies = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
            };

            var requires = new List<BoogieExpr> { Gt(Id("$sp"), Int(0)) };

            var ensures = new List<BoogieExpr>
            {
                Eq(Id("$sp"), Sub(Old(Id("$sp")), Int(1))),
                Le(Int(0), Id("$sp")),
            };

            var proc = new BoogieProcedure(
                "pop",
                new(),
                new(),
                attrs,
                modifies,
                pre: requires,
                post: ensures
            );
            program.Declarations.Add(proc);

            var body = new BoogieStmtList();
            body.AddStatement(new BoogieAssignCmd(Id("$sp"), Sub(Id("$sp"), Int(1))));

            var impl = new BoogieImplementation("pop", new(), new(), new(), body);
            program.Declarations.Add(impl);
        }
    }
}
