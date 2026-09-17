using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeBoolCasts(BoogieProgram program)
        {
            // bool_to_real
            {
                var b = new BoogieFormalParam(new BoogieTypedIdent("b", BoogieType.Bool));
                var body = new BoogieITE(
                    new BoogieIdentifierExpr("b"),
                    new BoogieLiteralExpr(new Pfloat(1)),
                    new BoogieLiteralExpr(new Pfloat(0))
                );
                program.Declarations.Add(
                    new BoogieFunctionDef("bool_to_real", new() { b }, BoogieType.Real, body)
                );
            }

            // real_to_bool
            {
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                var body = new BoogieITE(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        new BoogieIdentifierExpr("r"),
                        new BoogieLiteralExpr(new Pfloat(0))
                    ),
                    new BoogieLiteralExpr(false),
                    new BoogieLiteralExpr(true)
                );
                program.Declarations.Add(
                    new BoogieFunctionDef("real_to_bool", new() { r }, BoogieType.Bool, body)
                );
            }
            // =====================
            // Axioms for bool_to_real / real_to_bool
            // =====================

            // axiom: forall b: bool :: bool_to_real(b) == 0.0 || bool_to_real(b) == 1.0
            {
                var bVar = new BoogieIdentifierExpr("b");

                var boolToReal_b = new BoogieFuncCallExpr(
                    "bool_to_real",
                    new List<BoogieExpr> { bVar }
                );

                var eq0 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    boolToReal_b,
                    new BoogieLiteralExpr(new Pfloat(0))
                );

                // refaire l'appel (c'est OK, ou tu peux réutiliser la même expr)
                var boolToReal_b2 = new BoogieFuncCallExpr(
                    "bool_to_real",
                    new List<BoogieExpr> { new BoogieIdentifierExpr("b") }
                );

                var eq1 = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    boolToReal_b2,
                    new BoogieLiteralExpr(new Pfloat(1))
                );

                var body = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.OR, eq0, eq1);

                var forall = new BoogieQuantifiedExpr(
                    isForall: true,
                    qvars: new List<BoogieIdentifierExpr> { new BoogieIdentifierExpr("b") },
                    qvarTypes: new List<BoogieType> { BoogieType.Bool },
                    bodyExpr: body,
                    trigger: new List<BoogieExpr> { boolToReal_b } // trigger utile
                );

                program.Declarations.Add(new BoogieAxiom(forall));
            }

            // axiom: forall b: bool :: real_to_bool(bool_to_real(b)) == b
            {
                var bVar = new BoogieIdentifierExpr("b");

                var boolToReal_b = new BoogieFuncCallExpr(
                    "bool_to_real",
                    new List<BoogieExpr> { bVar }
                );

                var realToBool_boolToReal_b = new BoogieFuncCallExpr(
                    "real_to_bool",
                    new List<BoogieExpr> { boolToReal_b }
                );

                var body = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    realToBool_boolToReal_b,
                    bVar
                );

                var forall = new BoogieQuantifiedExpr(
                    isForall: true,
                    qvars: new List<BoogieIdentifierExpr> { new BoogieIdentifierExpr("b") },
                    qvarTypes: new List<BoogieType> { BoogieType.Bool },
                    bodyExpr: body,
                    trigger: new List<BoogieExpr> { realToBool_boolToReal_b } // trigger utile
                );

                program.Declarations.Add(new BoogieAxiom(forall));
            }

            // (optionnel mais souvent très utile)
            // axiom: forall r: real :: (real_to_bool(r) == false) <==> (r == 0.0)
            {
                var rVar = new BoogieIdentifierExpr("r");

                var realToBool_r = new BoogieFuncCallExpr(
                    "real_to_bool",
                    new List<BoogieExpr> { rVar }
                );

                var lhs = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    realToBool_r,
                    new BoogieLiteralExpr(false)
                );

                var rhs = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    rVar,
                    new BoogieLiteralExpr(new Pfloat(0))
                );

                var body = new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.IFF, lhs, rhs);

                var forall = new BoogieQuantifiedExpr(
                    isForall: true,
                    qvars: new List<BoogieIdentifierExpr> { new BoogieIdentifierExpr("r") },
                    qvarTypes: new List<BoogieType> { BoogieType.Real },
                    bodyExpr: body,
                    trigger: new List<BoogieExpr> { realToBool_r }
                );

                program.Declarations.Add(new BoogieAxiom(forall));
            }
        }

        private void AddPreludeNumericCasts(BoogieProgram program)
        {
            // real_to_int
            {
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Int));
                program.Declarations.Add(
                    new BoogieFunction("real_to_int", new() { r }, new() { res })
                );
            }

            // int_to_real
            {
                var i = new BoogieFormalParam(new BoogieTypedIdent("i", BoogieType.Int));
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("int_to_real", new() { i }, new() { res })
                );
            }

            // bits32/64_to_real
            {
                var i = new BoogieFormalParam(new BoogieTypedIdent("i", BoogieType.Int));
                var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("bits32_to_real", new() { i }, new() { r })
                );
                program.Declarations.Add(
                    new BoogieFunction("bits64_to_real", new() { i }, new() { r })
                );
            }

            {
    var bits =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "bits",
                BoogieType.Int
            )
        );

    var result =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "result",
                BoogieType.Real
            )
        );

    program.Declarations.Add(
        new BoogieFunction(
            "f32_bits_to_real",
            new() { bits },
            new() { result }
        )
    );
}

// f32_real_to_bits
{
    var value =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "value",
                BoogieType.Real
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
            "f32_real_to_bits",
            new() { value },
            new() { result }
        )
    );
}

// f64_bits_to_real
{
    var bits =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "bits",
                BoogieType.Int
            )
        );

    var result =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "result",
                BoogieType.Real
            )
        );

    program.Declarations.Add(
        new BoogieFunction(
            "f64_bits_to_real",
            new() { bits },
            new() { result }
        )
    );
}

// f64_real_to_bits
{
    var value =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "value",
                BoogieType.Real
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
            "f64_real_to_bits",
            new() { value },
            new() { result }
        )
    );
}

        }
    }
}
