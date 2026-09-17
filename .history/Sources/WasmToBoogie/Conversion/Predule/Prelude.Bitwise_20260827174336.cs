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
    // ============================================================
    // Typed integer unary functions
    // ============================================================

    AddIntegerUnaryFunction(program, "i32_clz");
    AddIntegerUnaryFunction(program, "i64_clz");

    AddIntegerUnaryFunction(program, "i32_ctz");
    AddIntegerUnaryFunction(program, "i64_ctz");

    AddIntegerUnaryFunction(program, "i32_popcnt");
    AddIntegerUnaryFunction(program, "i64_popcnt");

    // Semantic axioms
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


private static void AddIntegerUnaryBitAxioms(
    BoogieProgram p
)
{
    // ============================================================
    // ZERO CASES
    // ============================================================

    // clz(0) = width
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

    // ctz(0) = width
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

    // popcnt(0) = 0
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
    // RESULT BOUNDS
    // ============================================================

    AddIntegerUnaryBoundAxiom(
        p,
        "i32_clz",
        "is_u32",
        32
    );

    AddIntegerUnaryBoundAxiom(
        p,
        "i64_clz",
        "is_u64",
        64
    );

    AddIntegerUnaryBoundAxiom(
        p,
        "i32_ctz",
        "is_u32",
        32
    );

    AddIntegerUnaryBoundAxiom(
        p,
        "i64_ctz",
        "is_u64",
        64
    );

    AddIntegerUnaryBoundAxiom(
        p,
        "i32_popcnt",
        "is_u32",
        32
    );

    AddIntegerUnaryBoundAxiom(
        p,
        "i64_popcnt",
        "is_u64",
        64
    );


    // ============================================================
    // POPCNT SEMANTICS
    //
    // popcnt(x) =
    //   popcnt(x / 2) + (x mod 2)
    //
    // for x > 0
    // ============================================================

    AddPopcntAxiom(
        p,
        "i32_popcnt",
        "is_u32"
    );

    AddPopcntAxiom(
        p,
        "i64_popcnt",
        "is_u64"
    );


    // ============================================================
    // CTZ SEMANTICS
    //
    // if x is odd:
    //      ctz(x) = 0
    //
    // if x is even:
    //      ctz(x) = 1 + ctz(x / 2)
    //
    // ============================================================

    AddCtzAxiom(
        p,
        "i32_ctz",
        "is_u32"
    );

    AddCtzAxiom(
        p,
        "i64_ctz",
        "is_u64"
    );


    // ============================================================
    // CLZ SEMANTICS
    //
    // For i32:
    //   if x >= 2^31:
    //       clz(x) = 0
    //   else:
    //       clz(x) = 1 + clz(2*x)
    //
    // Same idea for i64 with 2^63.
    // ============================================================

    AddClzAxiom(
        p,
        "i32_clz",
        "is_u32",
        "TWO31"
    );

    AddClzAxiom(
        p,
        "i64_clz",
        "is_u64",
        "TWO63"
    );
}


private static void AddIntegerUnaryBoundAxiom(
    BoogieProgram p,
    string functionName,
    string domainFunction,
    int width
)
{
    var x = BId("x");

    var result =
        Fun(
            functionName,
            x
        );

    var bounds =
        And(
            Ge(
                result,
                BigIntLit("0")
            ),
            Le(
                result,
                BigIntLit(width.ToString())
            )
        );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun(
                        domainFunction,
                        x
                    ),
                    bounds
                )
            )
        )
    );
}


private static void AddPopcntAxiom(
    BoogieProgram p,
    string functionName,
    string domainFunction
)
{
    var x = BId("x");

    var domain =
        And(
            Fun(
                domainFunction,
                x
            ),
            Gt(
                x,
                BigIntLit("0")
            )
        );

    var half =
        Fun(
            "int_div_u",
            x,
            BigIntLit("2")
        );

    var bit =
        Fun(
            "int_rem_u",
            x,
            BigIntLit("2")
        );

    var rhs =
        Add(
            Fun(
                functionName,
                half
            ),
            bit
        );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    domain,
                    Eq(
                        Fun(
                            functionName,
                            x
                        ),
                        rhs
                    )
                )
            )
        )
    );
}


private static void AddCtzAxiom(
    BoogieProgram p,
    string functionName,
    string domainFunction
)
{
    var x = BId("x");

    var domain =
        And(
            Fun(
                domainFunction,
                x
            ),
            Gt(
                x,
                BigIntLit("0")
            )
        );

    var remainder =
        Fun(
            "int_rem_u",
            x,
            BigIntLit("2")
        );

    var half =
        Fun(
            "int_div_u",
            x,
            BigIntLit("2")
        );

    var rhs =
        new BoogieITE(
            Eq(
                remainder,
                BigIntLit("1")
            ),

            // odd
            BigIntLit("0"),

            // even
            Add(
                BigIntLit("1"),
                Fun(
                    functionName,
                    half
                )
            )
        );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    domain,
                    Eq(
                        Fun(
                            functionName,
                            x
                        ),
                        rhs
                    )
                )
            )
        )
    );
}


private static void AddClzAxiom(
    BoogieProgram p,
    string functionName,
    string domainFunction,
    string halfRangeConstant
)
{
    var x = BId("x");

    var domain =
        And(
            Fun(
                domainFunction,
                x
            ),
            Gt(
                x,
                BigIntLit("0")
            )
        );

    var doubled =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.MUL,
            x,
            BigIntLit("2")
        );

    var rhs =
        new BoogieITE(
            Lt(
                x,
                BId(halfRangeConstant)
            ),

            Add(
                BigIntLit("1"),
                Fun(
                    functionName,
                    doubled
                )
            ),

            BigIntLit("0")
        );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    domain,
                    Eq(
                        Fun(
                            functionName,
                            x
                        ),
                        rhs
                    )
                )
            )
        )
    );
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
