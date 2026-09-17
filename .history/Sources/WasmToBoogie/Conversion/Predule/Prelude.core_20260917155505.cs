using System.Collections.Generic;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {


private static BoogieExpr Mod(BoogieExpr a, BoogieExpr b) =>
    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.MOD, a, b);

private static BoogieExpr Or(BoogieExpr a, BoogieExpr b) =>
    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.OR, a, b);

private static BoogieExpr ForallReal1(string x, BoogieExpr body) =>
    new BoogieQuantifiedExpr(
        true,
        new List<BoogieIdentifierExpr> { BId(x) },
        new List<BoogieType> { BoogieType.Real },
        body
    );
private static BoogieIdentifierExpr BId(string name) =>
    new BoogieIdentifierExpr(name);

private static BoogieLiteralExpr BigIntLit(string value) =>
    new BoogieLiteralExpr(System.Numerics.BigInteger.Parse(value));

private static BoogieExpr Fun(string name, params BoogieExpr[] args) =>
    new BoogieFunctionCall(name, args.ToList());

private static BoogieExpr Forall1(string x, BoogieExpr body) =>
    new BoogieQuantifiedExpr(
        true,
        new List<BoogieIdentifierExpr> { BId(x) },
        new List<BoogieType> { BoogieType.Int },
        body
    );

private static BoogieExpr Forall2(string x, string y, BoogieExpr body) =>
    new BoogieQuantifiedExpr(
        true,
        new List<BoogieIdentifierExpr> { BId(x), BId(y) },
        new List<BoogieType> { BoogieType.Int, BoogieType.Int },
        body
    );


private static void AddIntegerWidthAxioms(BoogieProgram p)
{
    // ============================================================
    // Constants for integer widths
    // ============================================================
    p.Declarations.Add(new BoogieConstant(new BoogieTypedIdent("TWO32", BoogieType.Int)));
    p.Declarations.Add(new BoogieConstant(new BoogieTypedIdent("TWO31", BoogieType.Int)));
    p.Declarations.Add(new BoogieConstant(new BoogieTypedIdent("TWO64", BoogieType.Int)));
    p.Declarations.Add(new BoogieConstant(new BoogieTypedIdent("TWO63", BoogieType.Int)));

    p.Declarations.Add(new BoogieAxiom(Eq(BId("TWO32"), BigIntLit("4294967296"))));
    p.Declarations.Add(new BoogieAxiom(Eq(BId("TWO31"), BigIntLit("2147483648"))));
    p.Declarations.Add(new BoogieAxiom(Eq(BId("TWO64"), BigIntLit("18446744073709551616"))));
    p.Declarations.Add(new BoogieAxiom(Eq(BId("TWO63"), BigIntLit("9223372036854775808"))));

    // ============================================================
    // Integer normalization functions
    // ============================================================
    p.Declarations.Add(
        new BoogieFunction(
            "to_u32",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Int))
            }
        )
    );

    p.Declarations.Add(
        new BoogieFunction(
            "to_u64",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Int))
            }
        )
    );

    p.Declarations.Add(
        new BoogieFunction(
            "is_u32",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Bool))
            }
        )
    );

    p.Declarations.Add(
        new BoogieFunction(
            "is_u64",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Bool))
            }
        )
    );

    // ============================================================
    // Signed interpretation functions
    // ============================================================
    p.Declarations.Add(
        new BoogieFunction(
            "i32_signed",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Int))
            }
        )
    );

    p.Declarations.Add(
        new BoogieFunction(
            "i64_signed",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Int))
            }
        )
    );

    // ============================================================
    // Exact modulo semantics for integer wrapping
    //
    // to_u32(x) == x % 2^32
    // to_u64(x) == x % 2^64
    // ============================================================
    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Eq(
                    Fun("to_u32", BId("x")),
                    Mod(BId("x"), BId("TWO32"))
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Eq(
                    Fun("to_u64", BId("x")),
                    Mod(BId("x"), BId("TWO64"))
                )
            )
        )
    );

    // ============================================================
    // Bounds for normalized integer values
    // ============================================================
    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                And(
                    Le(BigIntLit("0"), Fun("to_u32", BId("x"))),
                    Lt(Fun("to_u32", BId("x")), BId("TWO32"))
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                And(
                    Le(BigIntLit("0"), Fun("to_u64", BId("x"))),
                    Lt(Fun("to_u64", BId("x")), BId("TWO64"))
                )
            )
        )
    );

    // ============================================================
    // Domain predicates
    // ============================================================
    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Eq(
                    Fun("is_u32", BId("x")),
                    And(
                        Le(BigIntLit("0"), BId("x")),
                        Lt(BId("x"), BId("TWO32"))
                    )
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Eq(
                    Fun("is_u64", BId("x")),
                    And(
                        Le(BigIntLit("0"), BId("x")),
                        Lt(BId("x"), BId("TWO64"))
                    )
                )
            )
        )
    );

    // ============================================================
    // Identity on already-normalized values
    // ============================================================
    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u32", BId("x")),
                    Eq(Fun("to_u32", BId("x")), BId("x"))
                )
            )
        )
    );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u64", BId("x")),
                    Eq(Fun("to_u64", BId("x")), BId("x"))
                )
            )
        )
    );

    // ============================================================
    // Signed interpretation
    //
    // i32_signed(x):
    //   if x < 2^31 then x else x - 2^32
    //
    // i64_signed(x):
    //   if x < 2^63 then x else x - 2^64
    // ============================================================
    p.Declarations.Add(
        new BoogieAxiom(
            Forall1(
                "x",
                Imp(
                    Fun("is_u32", BId("x")),
                    Eq(
                        Fun("i32_signed", BId("x")),
                        new BoogieITE(
                            Lt(BId("x"), BId("TWO31")),
                            BId("x"),
                            Sub(BId("x"), BId("TWO32"))
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
                    Fun("is_u64", BId("x")),
                    Eq(
                        Fun("i64_signed", BId("x")),
                        new BoogieITE(
                            Lt(BId("x"), BId("TWO63")),
                            BId("x"),
                            Sub(BId("x"), BId("TWO64"))
                        )
                    )
                )
            )
        )
    );

// ============================================================
// Unsigned comparison helpers
// ============================================================
AddUnsignedComparisonAxioms(p);

// ============================================================
// Signed integer arithmetic helpers
// ============================================================
AddIntegerDivisionRemainderHelpers(p);

// ============================================================
// Floating-point abstraction helpers
//
// Important:
// These do NOT model exact IEEE 754 semantics.
// They only keep the distinction between F32 and F64
// when using Boogie real.
// ============================================================
AddFloatingAbstractionAxioms(p);
    

}



private static void AddIntegerDivisionRemainderHelpers(BoogieProgram p)
{
    // ============================================================
    // int_div_u : exact integer division for non-negative integers
    // ============================================================

    var xParam = new BoogieFormalParam(
        new BoogieTypedIdent("x", BoogieType.Int)
    );

    var yParam = new BoogieFormalParam(
        new BoogieTypedIdent("y", BoogieType.Int)
    );

    var qParam = new BoogieFormalParam(
        new BoogieTypedIdent("q", BoogieType.Int)
    );

    p.Declarations.Add(
        new BoogieFunction(
            "int_div_u",
            new() { xParam, yParam },
            new() { qParam }
        )
    );

    var x = BId("x");
    var y = BId("y");

    var div = Fun("int_div_u", x, y);

    // domain:
    // x >= 0 && y > 0
    var domain = And(
        Ge(x, BigIntLit("0")),
        Gt(y, BigIntLit("0"))
    );

    // 0 <= int_div_u(x,y)
    var qNonNegative =
        Ge(div, BigIntLit("0"));

    // q*y <= x
    var lowerBound =
        Le(
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.MUL,
                div,
                y
            ),
            x
        );

    // x < (q+1)*y
    var upperBound =
        Lt(
            x,
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.MUL,
                Add(div, BigIntLit("1")),
                y
            )
        );

    var quotientProperties =
        And(
            qNonNegative,
            And(lowerBound, upperBound)
        );

    p.Declarations.Add(
        new BoogieAxiom(
            Forall2(
                "x",
                "y",
                Imp(
                    domain,
                    quotientProperties
                )
            )
        )
    );

    // ============================================================
    // int_rem_u(x,y) = x - int_div_u(x,y) * y
    // ============================================================

    var rx = new BoogieFormalParam(
        new BoogieTypedIdent("x", BoogieType.Int)
    );

    var ry = new BoogieFormalParam(
        new BoogieTypedIdent("y", BoogieType.Int)
    );

    var remU =
        Sub(
            BId("x"),
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.MUL,
                Fun(
                    "int_div_u",
                    BId("x"),
                    BId("y")
                ),
                BId("y")
            )
        );

    p.Declarations.Add(
        new BoogieFunctionDef(
            "int_rem_u",
            new() { rx, ry },
            BoogieType.Int,
            remU
        )
    );

    // ============================================================
    // Signed division
    // ============================================================

    var sx = new BoogieFormalParam(
        new BoogieTypedIdent("x", BoogieType.Int)
    );

    var sy = new BoogieFormalParam(
        new BoogieTypedIdent("y", BoogieType.Int)
    );

    var absX = new BoogieITE(
        Ge(BId("x"), BigIntLit("0")),
        BId("x"),
        new BoogieUnaryOperation(
            BoogieUnaryOperation.Opcode.NEG,
            BId("x")
        )
    );

    var absY = new BoogieITE(
        Ge(BId("y"), BigIntLit("0")),
        BId("y"),
        new BoogieUnaryOperation(
            BoogieUnaryOperation.Opcode.NEG,
            BId("y")
        )
    );

    // IMPORTANT:
    // no Opcode.DIV here anymore
    var magnitude =
        Fun(
            "int_div_u",
            absX,
            absY
        );

    // Different signs:
    //
    // (x < 0 && y >= 0) || (x >= 0 && y < 0)
    //
    // Avoid boolean != as well.
    var differentSigns =
        Or(
            And(
                Lt(BId("x"), BigIntLit("0")),
                Ge(BId("y"), BigIntLit("0"))
            ),
            And(
                Ge(BId("x"), BigIntLit("0")),
                Lt(BId("y"), BigIntLit("0"))
            )
        );

    var divSBody =
        new BoogieITE(
            differentSigns,
            new BoogieUnaryOperation(
                BoogieUnaryOperation.Opcode.NEG,
                magnitude
            ),
            magnitude
        );

    p.Declarations.Add(
        new BoogieFunctionDef(
            "int_div_s",
            new() { sx, sy },
            BoogieType.Int,
            divSBody
        )
    );

    // ============================================================
    // Signed remainder
    //
    // x rem_s y = x - trunc(x/y)*y
    // ============================================================

    var rxs = new BoogieFormalParam(
        new BoogieTypedIdent("x", BoogieType.Int)
    );

    var rys = new BoogieFormalParam(
        new BoogieTypedIdent("y", BoogieType.Int)
    );

    var remSBody =
        Sub(
            BId("x"),
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.MUL,
                Fun(
                    "int_div_s",
                    BId("x"),
                    BId("y")
                ),
                BId("y")
            )
        );

    p.Declarations.Add(
        new BoogieFunctionDef(
            "int_rem_s",
            new() { rxs, rys },
            BoogieType.Int,
            remSBody
        )
    );
}

private static void AddSignedIntegerArithmeticHelpers(BoogieProgram p)
{
    var x = new BoogieFormalParam(
        new BoogieTypedIdent("x", BoogieType.Int)
    );

    var y = new BoogieFormalParam(
        new BoogieTypedIdent("y", BoogieType.Int)
    );

    // abs(x)
    var absX = new BoogieITE(
        Ge(BId("x"), new BoogieLiteralExpr(0)),
        BId("x"),
        new BoogieUnaryOperation(
            BoogieUnaryOperation.Opcode.NEG,
            BId("x")
        )
    );

    // abs(y)
    var absY = new BoogieITE(
        Ge(BId("y"), new BoogieLiteralExpr(0)),
        BId("y"),
        new BoogieUnaryOperation(
            BoogieUnaryOperation.Opcode.NEG,
            BId("y")
        )
    );

    // abs(x) / abs(y)
    var magnitude = new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.DIV,
        absX,
        absY
    );

    // x et y ont-ils des signes différents ?
    var differentSigns = new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.NEQ,
        Lt(BId("x"), new BoogieLiteralExpr(0)),
        Lt(BId("y"), new BoogieLiteralExpr(0))
    );

    var divBody = new BoogieITE(
        differentSigns,
        new BoogieUnaryOperation(
            BoogieUnaryOperation.Opcode.NEG,
            magnitude
        ),
        magnitude
    );

    p.Declarations.Add(
        new BoogieFunctionDef(
            "int_div_s",
            new() { x, y },
            BoogieType.Int,
            divBody
        )
    );

    // WebAssembly remainder:
    //
    // x rem_s y = x - trunc(x/y) * y
    //
    var remBody = new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.SUB,
        BId("x"),
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.MUL,
            new BoogieFunctionCall(
                "int_div_s",
                new() { BId("x"), BId("y") }
            ),
            BId("y")
        )
    );

    p.Declarations.Add(
        new BoogieFunctionDef(
            "int_rem_s",
            new() { x, y },
            BoogieType.Int,
            remBody
        )
    );
}

private static void AddUnsignedComparisonAxioms(BoogieProgram p)
{
    string[] suffixes = { "lt", "le", "gt", "ge" };

    foreach (var suffix in suffixes)
    {
        p.Declarations.Add(
            new BoogieFunction(
                $"u32_{suffix}",
                new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int)),
                    new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Int))
                },
                new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Bool))
                }
            )
        );

        p.Declarations.Add(
            new BoogieFunction(
                $"u64_{suffix}",
                new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Int)),
                    new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Int))
                },
                new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Bool))
                }
            )
        );
    }

    AddUnsignedComparisonAxiom(p, "u32_lt", "is_u32", BoogieBinaryOperation.Opcode.LT);
    AddUnsignedComparisonAxiom(p, "u32_le", "is_u32", BoogieBinaryOperation.Opcode.LE);
    AddUnsignedComparisonAxiom(p, "u32_gt", "is_u32", BoogieBinaryOperation.Opcode.GT);
    AddUnsignedComparisonAxiom(p, "u32_ge", "is_u32", BoogieBinaryOperation.Opcode.GE);

    AddUnsignedComparisonAxiom(p, "u64_lt", "is_u64", BoogieBinaryOperation.Opcode.LT);
    AddUnsignedComparisonAxiom(p, "u64_le", "is_u64", BoogieBinaryOperation.Opcode.LE);
    AddUnsignedComparisonAxiom(p, "u64_gt", "is_u64", BoogieBinaryOperation.Opcode.GT);
    AddUnsignedComparisonAxiom(p, "u64_ge", "is_u64", BoogieBinaryOperation.Opcode.GE);
}

private static void AddUnsignedComparisonAxiom(
    BoogieProgram p,
    string functionName,
    string domainPredicate,
    BoogieBinaryOperation.Opcode cmp
)
{
    var x = BId("x");
    var y = BId("y");

    var domain =
        And(
            Fun(domainPredicate, x),
            Fun(domainPredicate, y)
        );

    var comparison =
        new BoogieBinaryOperation(cmp, x, y);

    p.Declarations.Add(
        new BoogieAxiom(
            Forall2(
                "x",
                "y",
                Imp(
                    domain,
                    Eq(
                        Fun(functionName, x, y),
                        comparison
                    )
                )
            )
        )
    );
}

private static void AddFloatingAbstractionAxioms(BoogieProgram p)
{
    // function to_f32(x: real) returns (r: real);
    p.Declarations.Add(
        new BoogieFunction(
            "to_f32",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real))
            }
        )
    );

    // function to_f64(x: real) returns (r: real);
    p.Declarations.Add(
        new BoogieFunction(
            "to_f64",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real))
            }
        )
    );

    // function is_f32(x: real) returns (r: bool);
    p.Declarations.Add(
        new BoogieFunction(
            "is_f32",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Bool))
            }
        )
    );

    // function is_f64(x: real) returns (r: bool);
    p.Declarations.Add(
        new BoogieFunction(
            "is_f64",
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real))
            },
            new List<BoogieVariable>
            {
                new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Bool))
            }
        )
    );

    // axiom forall x:real :: is_f32(to_f32(x));
    p.Declarations.Add(
        new BoogieAxiom(
            ForallReal1(
                "x",
                Fun("is_f32", Fun("to_f32", BId("x")))
            )
        )
    );

    // axiom forall x:real :: is_f64(to_f64(x));
    p.Declarations.Add(
        new BoogieAxiom(
            ForallReal1(
                "x",
                Fun("is_f64", Fun("to_f64", BId("x")))
            )
        )
    );

    // axiom forall x:real :: is_f32(x) ==> to_f32(x) == x;
    p.Declarations.Add(
        new BoogieAxiom(
            ForallReal1(
                "x",
                Imp(
                    Fun("is_f32", BId("x")),
                    Eq(Fun("to_f32", BId("x")), BId("x"))
                )
            )
        )
    );

    // axiom forall x:real :: is_f64(x) ==> to_f64(x) == x;
    p.Declarations.Add(
        new BoogieAxiom(
            ForallReal1(
                "x",
                Imp(
                    Fun("is_f64", BId("x")),
                    Eq(Fun("to_f64", BId("x")), BId("x"))
                )
            )
        )
    );

    p.Declarations.Add(
    new BoogieAxiom(
        ForallReal1(
            "x",
            Imp(
                Fun(
                    "is_f32",
                    BId("x")
                ),
                Fun(
                    "is_f64",
                    BId("x")
                )
            )
        )
    )
);

p.Declarations.Add(
    new BoogieAxiom(
        ForallReal1(
            "x",
            Eq(
                Fun(
                    "to_f32",
                    BId("x")
                ),
                BId("x")
            )
        )
    )
);

// forall x:real :: to_f64(x) == x;
p.Declarations.Add(
    new BoogieAxiom(
        ForallReal1(
            "x",
            Eq(
                Fun(
                    "to_f64",
                    BId("x")
                ),
                BId("x")
            )
        )
    )
);
}

        private void AddPreludeCoreGlobals(BoogieProgram program)
        {
            var wasmValueType = new BoogieCtorType("WasmValue");

            program.Declarations.Add(
                new BoogieDatatypeDecl(
                    "WasmValue",
                    new List<BoogieDatatypeConstructor>
                    {
                        new BoogieDatatypeConstructor("Undef"),
                        new BoogieDatatypeConstructor(
                            "I32",
                            new List<BoogieDatatypeField>
                            {
                                new BoogieDatatypeField("value_i32", BoogieType.Int),
                            }
                        ),
                        new BoogieDatatypeConstructor(
                            "I64",
                            new List<BoogieDatatypeField>
                            {
                                new BoogieDatatypeField("value_i64", BoogieType.Int),
                            }
                        ),
                        new BoogieDatatypeConstructor(
                            "F32",
                            new List<BoogieDatatypeField>
                            {
                                new BoogieDatatypeField("value_f32", BoogieType.Real),
                            }
                        ),
                        new BoogieDatatypeConstructor(
                            "F64",
                            new List<BoogieDatatypeField>
                            {
                                new BoogieDatatypeField("value_f64", BoogieType.Real),
                            }
                        ),
                    }
                )
            );

            // Stack: [int]WasmValue
            program.Declarations.Add(
                new BoogieGlobalVariable(
                    new BoogieTypedIdent("$stack", new BoogieMapType(BoogieType.Int, wasmValueType))
                )
            );

            // Stack pointer
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int))
            );

            // Temporaries: WasmValue
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", wasmValueType))
            );

            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", wasmValueType))
            );

            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", wasmValueType))
            );
        }
    }
}
