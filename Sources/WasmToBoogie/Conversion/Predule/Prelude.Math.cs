using System.Collections.Generic;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeMath(BoogieProgram program, PreludeOptions opt)
        {
            AddMinReal(program);
            AddMaxReal(program);
            AddAbsReal(program, opt);
            AddSqrtReal(program, opt);
            AddNearestReal(program, opt);
            AddFloorReal(program, opt);
        }

        private static void AddMinReal(BoogieProgram program)
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

        private static void AddMaxReal(BoogieProgram program)
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

            program.Declarations.Add(
                new BoogieFunctionDef("max_real", new() { x, y }, BoogieType.Real, body)
            );
        }

        private static void AddAbsReal(BoogieProgram program, PreludeOptions opt)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));

            if (opt.DefineAbsWithITE)
            {
                // abs_real(x) = if x >= 0 then x else -x
                var zero = new BoogieLiteralExpr(new Pfloat(0));
                var cond = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.GE,
                    new BoogieIdentifierExpr("x"),
                    zero
                );
                var neg = new BoogieUnaryOperation(
                    BoogieUnaryOperation.Opcode.NEG,
                    new BoogieIdentifierExpr("x")
                );

                var body = new BoogieITE(cond, new BoogieIdentifierExpr("x"), neg);

                program.Declarations.Add(
                    new BoogieFunctionDef("abs_real", new() { x }, BoogieType.Real, body)
                );
            }
            else
            {
                // uninterpreted abs_real(x): real -> real
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("abs_real", new() { x }, new() { res })
                );

                // Axioms (safe + useful):
                // 1) abs(x) >= 0
                // 2) abs(x) == abs(-x)
                // 3) (x >= 0) ==> abs(x) == x
                // 4) (x <= 0) ==> abs(x) == -x
                var xx = new BoogieIdentifierExpr("x");
                var absx = new BoogieFunctionCall("abs_real", new() { xx });
                var zero = new BoogieLiteralExpr(new Pfloat(0));
                var negx = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NEG, xx);

                // abs(x) >= 0
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new List<BoogieIdentifierExpr> { xx },
                            new List<BoogieType> { BoogieType.Real },
                            new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, absx, zero),
                            trigger: new List<BoogieExpr> { absx }
                        )
                    )
                );

                // abs(x) == abs(-x)
                var absNegX = new BoogieFunctionCall("abs_real", new() { (BoogieExpr)negx });
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new List<BoogieIdentifierExpr> { xx },
                            new List<BoogieType> { BoogieType.Real },
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.EQ,
                                absx,
                                absNegX
                            ),
                            trigger: new List<BoogieExpr> { absx }
                        )
                    )
                );

                // x >= 0 ==> abs(x) == x
                var ge0 = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, xx, zero);
                var imp1 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.IMP,
                    ge0,
                    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, absx, xx)
                );
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new List<BoogieIdentifierExpr> { xx },
                            new List<BoogieType> { BoogieType.Real },
                            imp1,
                            trigger: new List<BoogieExpr> { absx }
                        )
                    )
                );

                // x <= 0 ==> abs(x) == -x
                var le0 = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LE, xx, zero);
                var imp2 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.IMP,
                    le0,
                    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, absx, negx)
                );
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new List<BoogieIdentifierExpr> { xx },
                            new List<BoogieType> { BoogieType.Real },
                            imp2,
                            trigger: new List<BoogieExpr> { absx }
                        )
                    )
                );
            }
        }

        private static void AddSqrtReal(BoogieProgram program, PreludeOptions opt)
        {
            // uninterpreted sqrt_real(r) : real -> real
            var rParam = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
            var resParam = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
            program.Declarations.Add(
                new BoogieFunction("sqrt_real", new() { rParam }, new() { resParam })
            );

            if (!opt.EnableSqrtAxioms)
                return;

            var r = new BoogieIdentifierExpr("r");
            var zero = new BoogieLiteralExpr(new Pfloat(0));
            var guard = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, r, zero);
            var sqrt_r = new BoogieFunctionCall("sqrt_real", new List<BoogieExpr> { r });

            // r >= 0 ==> sqrt(r) >= 0
            {
                var rhs = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.GE, sqrt_r, zero);
                var imp = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.IMP, guard, rhs);
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new() { r },
                            new() { BoogieType.Real },
                            imp,
                            trigger: new List<BoogieExpr> { sqrt_r }
                        )
                    )
                );
            }

            // r >= 0 ==> sqrt(r)*sqrt(r) == r
            {
                var mul = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.MUL,
                    sqrt_r,
                    sqrt_r
                );
                var eq = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, mul, r);
                var imp = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.IMP, guard, eq);
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new() { r },
                            new() { BoogieType.Real },
                            imp,
                            trigger: new List<BoogieExpr> { sqrt_r }
                        )
                    )
                );
            }
        }

        private static void AddNearestReal(BoogieProgram program, PreludeOptions opt)
        {
            var rIn = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
            var rOut = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
            program.Declarations.Add(
                new BoogieFunction("nearest_real", new() { rIn }, new() { rOut })
            );

            if (!opt.EnableNearestAxioms)
                return;

            var r = new BoogieIdentifierExpr("r");
            BoogieExpr nr = new BoogieFunctionCall("nearest_real", new() { r });
            var half = new BoogieLiteralExpr(new Pfloat(0.5f));

            // (nearest(r) - 0.5 <= r) && (r <= nearest(r) + 0.5)
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

            program.Declarations.Add(
                new BoogieAxiom(
                    new BoogieQuantifiedExpr(
                        true,
                        new() { new BoogieIdentifierExpr("r") },
                        new() { BoogieType.Real },
                        boundAxiomBody,
                        trigger: new List<BoogieExpr> { nr }
                    )
                )
            );

            // nearest(nearest(r)) == nearest(r)
            var nnr = new BoogieFunctionCall("nearest_real", new() { (BoogieExpr)nr });
            var idemBody = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, nnr, nr);

            program.Declarations.Add(
                new BoogieAxiom(
                    new BoogieQuantifiedExpr(
                        true,
                        new() { new BoogieIdentifierExpr("r") },
                        new() { BoogieType.Real },
                        idemBody,
                        trigger: new List<BoogieExpr> { nr }
                    )
                )
            );
        }

        private static void AddFloorReal(BoogieProgram program, PreludeOptions opt)
        {
            var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
            var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
            program.Declarations.Add(new BoogieFunction("floor_real", new() { r }, new() { res }));

            if (!opt.EnableFloorAxioms)
                return;

            // forall r :: floor(r) <= r
            {
                var rId = new BoogieIdentifierExpr("r");
                var body = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LE,
                    new BoogieFunctionCall("floor_real", new() { rId }),
                    rId
                );
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new() { rId },
                            new() { BoogieType.Real },
                            body
                        )
                    )
                );
            }

            // forall r :: r < floor(r) + 1
            {
                var rId = new BoogieIdentifierExpr("r");
                var rhs = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.ADD,
                    new BoogieFunctionCall("floor_real", new() { rId }),
                    new BoogieLiteralExpr(new Pfloat(1))
                );
                var body = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LT, rId, rhs);
                program.Declarations.Add(
                    new BoogieAxiom(
                        new BoogieQuantifiedExpr(
                            true,
                            new() { rId },
                            new() { BoogieType.Real },
                            body
                        )
                    )
                );
            }
        }
    }
}
