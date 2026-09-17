using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeBitwise(BoogieProgram program)
        {
            AddBitwiseFunction(program, "bv_and");
            AddBitwiseFunction(program, "bv_or");
            AddBitwiseFunction(program, "bv_xor");
            AddBitwiseFunction(program, "bv_shl");
            AddBitwiseFunction(program, "bv_shr_s");
            AddBitwiseFunction(program, "bv_shr_u");
            AddBitwiseFunction(program, "bv_rotl");
            AddBitwiseFunction(program, "bv_rotr");
        }
/*
        private void AddPreludeIntOps(BoogieProgram program)
        {
            AddBinaryRealFunction(program, "int_rem");
        }

        private static void AddBinaryRealFunction(BoogieProgram program, string name)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
            var y = new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Real));
            var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));

            program.Declarations.Add(new BoogieFunction(name, new() { x, y }, new() { r }));
        }*/

private void AddPreludeIntUnaryOps(BoogieProgram program)
{
    AddIntegerUnaryFunction(program, "i32_clz");
    AddIntegerUnaryFunction(program, "i64_clz");

    AddIntegerUnaryFunction(program, "i32_ctz");
    AddIntegerUnaryFunction(program, "i64_ctz");

    AddIntegerUnaryFunction(program, "i32_popcnt");
    AddIntegerUnaryFunction(program, "i64_popcnt");

    AddIntegerUnaryBitAxioms(program);
}

private static void AddIntegerUnaryFunction(
    BoogieProgram program,
    string name
)
{
    var x =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "x",
                BoogieType.Int
            )
        );

    var result =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "result",
                BoogieType.Int
            )
        );

    program.Declarations.Add(
        new BoogieFunction(
            name,
            new() { x },
            new() { result }
        )
    );
}

private static BoogieExpr Fun1(
    string name,
    BoogieExpr arg
) =>
    new BoogieFunctionCall(
        name,
        new List<BoogieExpr> { arg }
    );

private static void AddIntegerUnaryBitAxioms(
    BoogieProgram p
)
{
    var x = BId("x");

    // ============================================================
    // ZERO cases
    // ============================================================

    p.Declarations.Add(
        new BoogieAxiom(
            Eq(
                Fun("i32_clz", BigIntLit("0")),
                BigIntLit("32")
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Eq(
                Fun("i64_clz", BigIntLit("0")),
                BigIntLit("64")
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Eq(
                Fun("i32_ctz", BigIntLit("0")),
                BigIntLit("32")
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Eq(
                Fun("i64_ctz", BigIntLit("0")),
                BigIntLit("64")
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Eq(
                Fun("i32_popcnt", BigIntLit("0")),
                BigIntLit("0")
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Eq(
                Fun("i64_popcnt", BigIntLit("0")),
                BigIntLit("0")
            )
        )
    );

    // ============================================================
    // Result bounds
    // ============================================================

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u32", x),
                    And(
                        Ge(
                            Fun("i32_clz", x),
                            BigIntLit("0")
                        ),
                        Le(
                            Fun("i32_clz", x),
                            BigIntLit("32")
                        )
                    )
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u64", x),
                    And(
                        Ge(
                            Fun("i64_clz", x),
                            BigIntLit("0")
                        ),
                        Le(
                            Fun("i64_clz", x),
                            BigIntLit("64")
                        )
                    )
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u32", x),
                    And(
                        Ge(
                            Fun("i32_ctz", x),
                            BigIntLit("0")
                        ),
                        Le(
                            Fun("i32_ctz", x),
                            BigIntLit("32")
                        )
                    )
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u64", x),
                    And(
                        Ge(
                            Fun("i64_ctz", x),
                            BigIntLit("0")
                        ),
                        Le(
                            Fun("i64_ctz", x),
                            BigIntLit("64")
                        )
                    )
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u32", x),
                    And(
                        Ge(
                            Fun("i32_popcnt", x),
                            BigIntLit("0")
                        ),
                        Le(
                            Fun("i32_popcnt", x),
                            BigIntLit("32")
                        )
                    )
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u64", x),
                    And(
                        Ge(
                            Fun("i64_popcnt", x),
                            BigIntLit("0")
                        ),
                        Le(
                            Fun("i64_popcnt", x),
                            BigIntLit("64")
                        )
                    )
                )
            )
        )
    );
}    

        private static void AddUnaryRealFunction(BoogieProgram program, string name)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
            var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));

            program.Declarations.Add(new BoogieFunction(name, new() { x }, new() { r }));
        }

        private static void AddBitwiseFunction(BoogieProgram program, string name)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
            var y = new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Real));
            var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));

            program.Declarations.Add(new BoogieFunction(name, new() { x, y }, new() { r }));
        }
    }
}
