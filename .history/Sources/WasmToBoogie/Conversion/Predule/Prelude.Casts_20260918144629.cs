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

private void AddPreludeNumericCasts(
    BoogieProgram program
)
{
    // ============================================================
    // Anciennes fonctions numériques
    // ============================================================

    // real_to_int
    {
        var r =
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    "r",
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
                "real_to_int",
                new() { r },
                new() { result }
            )
        );
    }

    // int_to_real
    {
        var i =
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    "i",
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
                "int_to_real",
                new() { i },
                new() { result }
            )
        );
    }

    // Anciennes fonctions utilisées par la mémoire.
    {
        var i32Bits =
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    "i",
                    BoogieType.Int
                )
            );

        var f32Result =
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    "result",
                    BoogieType.Real
                )
            );

        program.Declarations.Add(
            new BoogieFunction(
                "bits32_to_real",
                new() { i32Bits },
                new() { f32Result }
            )
        );

        var i64Bits =
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    "i",
                    BoogieType.Int
                )
            );

        var f64Result =
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    "result",
                    BoogieType.Real
                )
            );

        program.Declarations.Add(
            new BoogieFunction(
                "bits64_to_real",
                new() { i64Bits },
                new() { f64Result }
            )
        );
    }

    // ============================================================
    // Reinterpret abstrait
    // ============================================================

    AddReinterpretPrelude(
        program,
        bitsToRealName: "f32_bits_to_real",
        realToBitsName: "f32_real_to_bits",
        integerPredicateName: "is_u32",
        floatPredicateName: "is_f32"
    );

    AddReinterpretPrelude(
        program,
        bitsToRealName: "f64_bits_to_real",
        realToBitsName: "f64_real_to_bits",
        integerPredicateName: "is_u64",
        floatPredicateName: "is_f64"
    );
}
private void AddReinterpretPrelude(
    BoogieProgram program,
    string bitsToRealName,
    string realToBitsName,
    string integerPredicateName,
    string floatPredicateName
)
{
    // ============================================================
    // function bits_to_real(bits:int) returns (result:real)
    // ============================================================

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
                bitsToRealName,
                new() { bits },
                new() { result }
            )
        );
    }

    // ============================================================
    // function real_to_bits(value:real) returns (result:int)
    // ============================================================

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
                realToBitsName,
                new() { value },
                new() { result }
            )
        );
    }

    // ============================================================
    // Axiome 1 :
    //
    // forall bits:int ::
    //   is_uXX(bits) ==>
    //     real_to_bits(bits_to_real(bits)) == bits
    //
    // Cet axiome garantit la conservation des bits pour :
    //
    // integer -> float -> integer
    // ============================================================

    {
        var bits =
            new BoogieIdentifierExpr("bits");

        var validBits =
            new BoogieFuncCallExpr(
                integerPredicateName,
                new List<BoogieExpr>
                {
                    bits
                }
            );

        var convertedToReal =
            new BoogieFuncCallExpr(
                bitsToRealName,
                new List<BoogieExpr>
                {
                    bits
                }
            );

        var convertedBackToBits =
            new BoogieFuncCallExpr(
                realToBitsName,
                new List<BoogieExpr>
                {
                    convertedToReal
                }
            );

        var sameBits =
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.EQ,
                convertedBackToBits,
                bits
            );

        var implication =
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.IMPLIES,
                validBits,
                sameBits
            );

        var quantified =
            new BoogieQuantifiedExpr(
                isForall: true,
                qvars: new List<BoogieIdentifierExpr>
                {
                    new BoogieIdentifierExpr("bits")
                },
                qvarTypes: new List<BoogieType>
                {
                    BoogieType.Int
                },
                bodyExpr: implication,
                trigger: new List<BoogieExpr>
                {
                    convertedBackToBits
                }
            );

        program.Declarations.Add(
            new BoogieAxiom(
                quantified
            )
        );
    }

    // ============================================================
    // Axiome 2 :
    //
    // forall value:real ::
    //   is_uXX(real_to_bits(value))
    //
    // Le résultat de reinterpret float -> integer est toujours
    // dans l'intervalle non signé correspondant.
    // ============================================================

    {
        var value =
            new BoogieIdentifierExpr("value");

        var convertedBits =
            new BoogieFuncCallExpr(
                realToBitsName,
                new List<BoogieExpr>
                {
                    value
                }
            );

        var validBits =
            new BoogieFuncCallExpr(
                integerPredicateName,
                new List<BoogieExpr>
                {
                    convertedBits
                }
            );

        var quantified =
            new BoogieQuantifiedExpr(
                isForall: true,
                qvars: new List<BoogieIdentifierExpr>
                {
                    new BoogieIdentifierExpr("value")
                },
                qvarTypes: new List<BoogieType>
                {
                    BoogieType.Real
                },
                bodyExpr: validBits,
                trigger: new List<BoogieExpr>
                {
                    convertedBits
                }
            );

        program.Declarations.Add(
            new BoogieAxiom(
                quantified
            )
        );
    }

    // ============================================================
    // Axiome 3 :
    //
    // forall bits:int ::
    //   is_uXX(bits) ==>
    //     is_fXX(bits_to_real(bits))
    //
    // Le résultat de reinterpret integer -> float est reconnu
    // comme une valeur flottante valide dans notre abstraction.
    // ============================================================

    {
        var bits =
            new BoogieIdentifierExpr("bits");

        var validBits =
            new BoogieFuncCallExpr(
                integerPredicateName,
                new List<BoogieExpr>
                {
                    bits
                }
            );

        var convertedReal =
            new BoogieFuncCallExpr(
                bitsToRealName,
                new List<BoogieExpr>
                {
                    bits
                }
            );

        var validFloat =
            new BoogieFuncCallExpr(
                floatPredicateName,
                new List<BoogieExpr>
                {
                    convertedReal
                }
            );

        var implication =
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.IMPLIES,
                validBits,
                validFloat
            );

        var quantified =
            new BoogieQuantifiedExpr(
                isForall: true,
                qvars: new List<BoogieIdentifierExpr>
                {
                    new BoogieIdentifierExpr("bits")
                },
                qvarTypes: new List<BoogieType>
                {
                    BoogieType.Int
                },
                bodyExpr: implication,
                trigger: new List<BoogieExpr>
                {
                    convertedReal
                }
            );

        program.Declarations.Add(
            new BoogieAxiom(
                quantified
            )
        );
    }
}
    }
}
