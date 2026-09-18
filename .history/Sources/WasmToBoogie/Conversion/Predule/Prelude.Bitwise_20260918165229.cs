using BoogieAST;
using System.Collections.Generic;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
private void AddPreludeBitwise(BoogieProgram program)
{
var bv8 = new BoogieCtorType("bv8");
var bv16 = new BoogieCtorType("bv16");
var bv24 = new BoogieCtorType("bv24");
var bv32 = new BoogieCtorType("bv32");
var bv64 = new BoogieCtorType("bv64");


AddBuiltinFunction(
    program,
    "bv8_concat_bv8",
    new List<BoogieType>
    {
        bv8,
        bv8
    },
    bv16,
    "concat"
);

AddBuiltinFunction(
    program,
    "bv16_concat_bv8",
    new List<BoogieType>
    {
        bv16,
        bv8
    },
    bv24,
    "concat"
);

AddBuiltinFunction(
    program,
    "bv24_concat_bv8",
    new List<BoogieType>
    {
        bv24,
        bv8
    },
    bv32,
    "concat"
);

AddBuiltinFunction(
    program,
    "bv32_concat_bv32",
    new List<BoogieType>
    {
        bv32,
        bv32
    },
    bv64,
    "concat"
);

AddBuiltinFunction(
    program,
    "int_to_bv8",
    new List<BoogieType>
    {
        BoogieType.Int
    },
    bv8,
    "(_ int2bv 8)"
);

AddBuiltinFunction(
    program,
    "bv8_to_int",
    new List<BoogieType>
    {
        bv8
    },
    BoogieType.Int,
    "bv2int"
);
    // int <-> bv32
    AddBuiltinFunction(
        program,
        "int_to_bv32",
        new List<BoogieType> { BoogieType.Int },
        bv32,
        "(_ int2bv 32)"
    );

    AddBuiltinFunction(
        program,
        "bv32_to_int",
        new List<BoogieType> { bv32 },
        BoogieType.Int,
        "bv2int"
    );

AddBuiltinFunction(
    program,
    "bv32_byte0",
    new List<BoogieType> { bv32 },
    bv8,
    "(_ extract 7 0)"
);

AddBuiltinFunction(
    program,
    "bv32_byte1",
    new List<BoogieType> { bv32 },
    bv8,
    "(_ extract 15 8)"
);

AddBuiltinFunction(
    program,
    "bv32_byte2",
    new List<BoogieType> { bv32 },
    bv8,
    "(_ extract 23 16)"
);

AddBuiltinFunction(
    program,
    "bv32_byte3",
    new List<BoogieType> { bv32 },
    bv8,
    "(_ extract 31 24)"
);

    // int <-> bv64
    AddBuiltinFunction(
        program,
        "int_to_bv64",
        new List<BoogieType> { BoogieType.Int },
        bv64,
        "(_ int2bv 64)"
    );

    AddBuiltinFunction(
        program,
        "bv64_to_int",
        new List<BoogieType> { bv64 },
        BoogieType.Int,
        "bv2int"
    );

AddBuiltinFunction(
    program,
    "bv64_byte0",
    new List<BoogieType> { bv64 },
    bv8,
    "(_ extract 7 0)"
);

AddBuiltinFunction(
    program,
    "bv64_byte1",
    new List<BoogieType> { bv64 },
    bv8,
    "(_ extract 15 8)"
);

AddBuiltinFunction(
    program,
    "bv64_byte2",
    new List<BoogieType> { bv64 },
    bv8,
    "(_ extract 23 16)"
);

AddBuiltinFunction(
    program,
    "bv64_byte3",
    new List<BoogieType> { bv64 },
    bv8,
    "(_ extract 31 24)"
);

AddBuiltinFunction(
    program,
    "bv64_byte4",
    new List<BoogieType> { bv64 },
    bv8,
    "(_ extract 39 32)"
);

AddBuiltinFunction(
    program,
    "bv64_byte5",
    new List<BoogieType> { bv64 },
    bv8,
    "(_ extract 47 40)"
);

AddBuiltinFunction(
    program,
    "bv64_byte6",
    new List<BoogieType> { bv64 },
    bv8,
    "(_ extract 55 48)"
);

AddBuiltinFunction(
    program,
    "bv64_byte7",
    new List<BoogieType> { bv64 },
    bv8,
    "(_ extract 63 56)"
);

    // i32.and/or/xor
    AddBuiltinFunction(
        program,
        "bv32_and",
        new List<BoogieType> { bv32, bv32 },
        bv32,
        "bvand"
    );

    AddBuiltinFunction(
        program,
        "bv32_or",
        new List<BoogieType> { bv32, bv32 },
        bv32,
        "bvor"
    );

    AddBuiltinFunction(
        program,
        "bv32_xor",
        new List<BoogieType> { bv32, bv32 },
        bv32,
        "bvxor"
    );

    // i64.and/or/xor
    AddBuiltinFunction(
        program,
        "bv64_and",
        new List<BoogieType> { bv64, bv64 },
        bv64,
        "bvand"
    );

    AddBuiltinFunction(
        program,
        "bv64_or",
        new List<BoogieType> { bv64, bv64 },
        bv64,
        "bvor"
    );

    AddBuiltinFunction(
        program,
        "bv64_xor",
        new List<BoogieType> { bv64, bv64 },
        bv64,
        "bvxor"
    );

    // i32 shifts
AddBuiltinFunction(
    program,
    "bv32_shl",
    new List<BoogieType> { bv32, bv32 },
    bv32,
    "bvshl"
);

AddBuiltinFunction(
    program,
    "bv32_shr_u",
    new List<BoogieType> { bv32, bv32 },
    bv32,
    "bvlshr"
);

AddBuiltinFunction(
    program,
    "bv32_shr_s",
    new List<BoogieType> { bv32, bv32 },
    bv32,
    "bvashr"
);

// i64 shifts
AddBuiltinFunction(
    program,
    "bv64_shl",
    new List<BoogieType> { bv64, bv64 },
    bv64,
    "bvshl"
);

AddBuiltinFunction(
    program,
    "bv64_shr_u",
    new List<BoogieType> { bv64, bv64 },
    bv64,
    "bvlshr"
);

AddBuiltinFunction(
    program,
    "bv64_shr_s",
    new List<BoogieType> { bv64, bv64 },
    bv64,
    "bvashr"
);
}

private static void AddBuiltinFunction(
    BoogieProgram program,
    string name,
    List<BoogieType> inputTypes,
    BoogieType outputType,
    string builtinName
)
{
    var inputParameters = new List<BoogieVariable>();

    for (int i = 0; i < inputTypes.Count; i++)
    {
        inputParameters.Add(
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    $"x{i}",
                    inputTypes[i]
                )
            )
        );
    }

    var resultParameter =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "result",
                outputType
            )
        );

    var attributes =
        new List<BoogieAttribute>
        {
            // BoogieAttribute n'ajoute pas automatiquement
            // les guillemets autour des chaînes.
            new BoogieAttribute(
                "bvbuiltin",
                $"\"{builtinName}\""
            )
        };

    program.Declarations.Add(
        new BoogieFunction(
            name,
            inputParameters,
            new List<BoogieVariable> { resultParameter },
            attributes
        )
    );
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
    new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.INT_DIV,
        x,
        BigIntLit("2")
    );

var bit =
    new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.MOD,
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
    new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.MOD,
        x,
        BigIntLit("2")
    );

var half =
    new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.INT_DIV,
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




  

        private static void AddUnaryRealFunction(BoogieProgram program, string name)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
            var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));

            program.Declarations.Add(new BoogieFunction(name, new() { x }, new() { r }));
        }


    }
}
