using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BoogieAST;
using WasmToBoogie.Parser.Ast;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private static System.Numerics.BigInteger NormalizeUnsignedInteger(
            System.Numerics.BigInteger value,
            int bitWidth
        )
        {
            System.Numerics.BigInteger modulus = System.Numerics.BigInteger.One << bitWidth;

            System.Numerics.BigInteger result = value % modulus;

            if (result.Sign < 0)
            {
                result += modulus;
            }

            return result;
        }

        private static BoogieExpr Tmp1() => Id("$tmp1");

        private static BoogieExpr Tmp2() => Id("$tmp2");

        private static BoogieExpr Tmp3() => Id("$tmp3");

        private static BoogieExpr I32Value(BoogieExpr e) => Field(e, "value_i32");

        private static BoogieExpr I64Value(BoogieExpr e) => Field(e, "value_i64");

        private static BoogieExpr F32Value(BoogieExpr e) => Field(e, "value_f32");

        private static BoogieExpr F64Value(BoogieExpr e) => Field(e, "value_f64");

        private static BoogieExpr BoolToI32(BoogieExpr cond) =>
            I32(
                ToU32(
                    new BoogieITE(
                        cond,
                        new BoogieLiteralExpr(new System.Numerics.BigInteger(1)),
                        new BoogieLiteralExpr(new System.Numerics.BigInteger(0))
                    )
                )
            );

        private static BoogieExpr ToU32(BoogieExpr e) =>
            new BoogieFunctionCall("to_u32", new List<BoogieExpr> { e });

        private static BoogieExpr ToU64(BoogieExpr e) =>
            new BoogieFunctionCall("to_u64", new List<BoogieExpr> { e });

        private static BoogieExpr I32Signed(BoogieExpr e) =>
            new BoogieFunctionCall("i32_signed", new List<BoogieExpr> { e });

        private static BoogieExpr I64Signed(BoogieExpr e) =>
            new BoogieFunctionCall("i64_signed", new List<BoogieExpr> { e });

        private static BoogieExpr IsU32(BoogieExpr e) =>
            new BoogieFunctionCall("is_u32", new List<BoogieExpr> { e });

        private static BoogieExpr IsU64(BoogieExpr e) =>
            new BoogieFunctionCall("is_u64", new List<BoogieExpr> { e });

        private static BoogieExpr ToF32(BoogieExpr e) =>
            new BoogieFunctionCall("to_f32", new List<BoogieExpr> { e });

        private static BoogieExpr ToF64(BoogieExpr e) =>
            new BoogieFunctionCall("to_f64", new List<BoogieExpr> { e });

        private static BoogieExpr IsF32(BoogieExpr e) =>
            new BoogieFunctionCall("is_f32", new List<BoogieExpr> { e });

        private static BoogieExpr IsF64(BoogieExpr e) =>
            new BoogieFunctionCall("is_f64", new List<BoogieExpr> { e });

        private static BoogieExpr U32Lt(BoogieExpr x, BoogieExpr y) =>
            new BoogieFunctionCall("u32_lt", new List<BoogieExpr> { x, y });

        private static BoogieExpr U32Le(BoogieExpr x, BoogieExpr y) =>
            new BoogieFunctionCall("u32_le", new List<BoogieExpr> { x, y });

        private static BoogieExpr U32Gt(BoogieExpr x, BoogieExpr y) =>
            new BoogieFunctionCall("u32_gt", new List<BoogieExpr> { x, y });

        private static BoogieExpr U32Ge(BoogieExpr x, BoogieExpr y) =>
            new BoogieFunctionCall("u32_ge", new List<BoogieExpr> { x, y });

        private static BoogieExpr U64Lt(BoogieExpr x, BoogieExpr y) =>
            new BoogieFunctionCall("u64_lt", new List<BoogieExpr> { x, y });

        private static BoogieExpr U64Le(BoogieExpr x, BoogieExpr y) =>
            new BoogieFunctionCall("u64_le", new List<BoogieExpr> { x, y });

        private static BoogieExpr U64Gt(BoogieExpr x, BoogieExpr y) =>
            new BoogieFunctionCall("u64_gt", new List<BoogieExpr> { x, y });

        private static BoogieExpr U64Ge(BoogieExpr x, BoogieExpr y) =>
            new BoogieFunctionCall("u64_ge", new List<BoogieExpr> { x, y });

        private static BoogieExpr MakeNondetWasmValue(WasmValueType type)
        {
            return type switch
            {
                WasmValueType.I32 => I32(ToU32(new BoogieFunctionCall("nd_i32", new()))),

                WasmValueType.I64 => I64(ToU64(new BoogieFunctionCall("nd_i64", new()))),

                WasmValueType.F32 => F32(ToF32(new BoogieFunctionCall("nd_f32", new()))),

                WasmValueType.F64 => F64(ToF64(new BoogieFunctionCall("nd_f64", new()))),

                _ => throw new NotSupportedException(
                    $"Unsupported nondeterministic Wasm type: {type}"
                ),
            };
        }

private void EmitTypedRefIsNull(
    BoogieStmtList body
)
{
    body.AddStatement(
        new BoogieCallCmd(
            "popToTmp1",
            new(),
            new()
        )
    );

    var value =
        new BoogieIdentifierExpr("$tmp1");

    // ref.is_null accepte une valeur de référence.
    body.AddStatement(
        new BoogieAssertCmd(
            IsReferenceValue(value)
        )
    );

    var isNullReference =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.OR,

            IsCtor(
                value,
                "NullFuncRef"
            ),

            IsCtor(
                value,
                "NullExternRef"
            )
        );

    var result =
        new BoogieITE(
            isNullReference,
            new BoogieLiteralExpr(1),
            new BoogieLiteralExpr(0)
        );

    body.AddStatement(
        new BoogieCallCmd(
            "push",

            new List<BoogieExpr>
            {
                I32(
                    ToU32(result)
                ),
            },

            new()
        )
    );
}

        private void EmitTypedReinterpretCast(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            switch (op)
            {
                // ============================================================
                // i32 bits -> f32 abstrait
                // ============================================================

                case "f32.reinterpret_i32":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I32")));

                    BoogieExpr bits = I32Value(Tmp1());

                    body.AddStatement(new BoogieAssertCmd(IsU32(bits)));

                    BoogieExpr floatValue = new BoogieFunctionCall(
                        "f32_bits_to_real",
                        new List<BoogieExpr> { bits }
                    );

                    body.AddStatement(new BoogieAssertCmd(IsF32(floatValue)));

                    body.AddStatement(
                        new BoogieCallCmd("push", new List<BoogieExpr> { F32(floatValue) }, new())
                    );

                    break;
                }

                // ============================================================
                // f32 abstrait -> i32 bits
                // ============================================================

                case "i32.reinterpret_f32":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "F32")));

                    BoogieExpr floatValue = F32Value(Tmp1());

                    body.AddStatement(new BoogieAssertCmd(IsF32(floatValue)));

                    BoogieExpr bits = new BoogieFunctionCall(
                        "f32_real_to_bits",
                        new List<BoogieExpr> { floatValue }
                    );

                    body.AddStatement(new BoogieAssertCmd(IsU32(bits)));

                    body.AddStatement(
                        new BoogieCallCmd("push", new List<BoogieExpr> { I32(bits) }, new())
                    );

                    break;
                }

                // ============================================================
                // i64 bits -> f64 abstrait
                // ============================================================

                case "f64.reinterpret_i64":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I64")));

                    BoogieExpr bits = I64Value(Tmp1());

                    body.AddStatement(new BoogieAssertCmd(IsU64(bits)));

                    BoogieExpr floatValue = new BoogieFunctionCall(
                        "f64_bits_to_real",
                        new List<BoogieExpr> { bits }
                    );

                    body.AddStatement(new BoogieAssertCmd(IsF64(floatValue)));

                    body.AddStatement(
                        new BoogieCallCmd("push", new List<BoogieExpr> { F64(floatValue) }, new())
                    );

                    break;
                }

                // ============================================================
                // f64 abstrait -> i64 bits
                // ============================================================

                case "i64.reinterpret_f64":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "F64")));

                    BoogieExpr floatValue = F64Value(Tmp1());

                    body.AddStatement(new BoogieAssertCmd(IsF64(floatValue)));

                    BoogieExpr bits = new BoogieFunctionCall(
                        "f64_real_to_bits",
                        new List<BoogieExpr> { floatValue }
                    );

                    body.AddStatement(new BoogieAssertCmd(IsU64(bits)));

                    body.AddStatement(
                        new BoogieCallCmd("push", new List<BoogieExpr> { I64(bits) }, new())
                    );

                    break;
                }

                default:
                {
                    throw new NotSupportedException($"Unsupported reinterpret cast: {op}");
                }
            }
        }

        private void EmitTypedIntegerToFloatCast(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            bool sourceIsI32 = op.Contains("i32", StringComparison.Ordinal);

            bool targetIsF32 = op.StartsWith("f32.", StringComparison.Ordinal);

            bool sourceIsSigned = op.EndsWith("_s", StringComparison.Ordinal);

            string sourceConstructor = sourceIsI32 ? "I32" : "I64";

            string sourceField = sourceIsI32 ? "value_i32" : "value_i64";

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), sourceConstructor)));

            BoogieExpr unsignedValue = Field(Tmp1(), sourceField);

            body.AddStatement(
                new BoogieAssertCmd(sourceIsI32 ? IsU32(unsignedValue) : IsU64(unsignedValue))
            );

            /*
             * Les payloads I32/I64 sont toujours non signés.
             * Pour une conversion signée, on récupère d'abord
             * la valeur mathématique signée.
             */
            BoogieExpr integerValue;

            if (sourceIsSigned)
            {
                integerValue = sourceIsI32 ? I32Signed(unsignedValue) : I64Signed(unsignedValue);
            }
            else
            {
                integerValue = unsignedValue;
            }

            /*
             * Conversion native Boogie int -> real.
             */
            BoogieExpr realValue = new BoogieFunctionCall(
                "real",
                new List<BoogieExpr> { integerValue }
            );

            /*
             * Abstraction de l'arrondi vers le format cible.
             */
            BoogieExpr convertedValue = targetIsF32 ? ToF32(realValue) : ToF64(realValue);

            BoogieExpr typedResult = targetIsF32 ? F32(convertedValue) : F64(convertedValue);

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedIntegerSignExtensionOp(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            bool targetIsI32 = op.StartsWith("i32.", StringComparison.Ordinal);

            string constructor = targetIsI32 ? "I32" : "I64";

            string field = targetIsI32 ? "value_i32" : "value_i64";

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), constructor)));

            BoogieExpr value = Field(Tmp1(), field);

            body.AddStatement(new BoogieAssertCmd(targetIsI32 ? IsU32(value) : IsU64(value)));

            int sourceWidth = op switch
            {
                "i32.extend8_s" => 8,
                "i32.extend16_s" => 16,

                "i64.extend8_s" => 8,
                "i64.extend16_s" => 16,
                "i64.extend32_s" => 32,

                _ => throw new NotSupportedException($"Unsupported integer sign extension: {op}"),
            };

            System.Numerics.BigInteger modulusValue = System.Numerics.BigInteger.One << sourceWidth;

            System.Numerics.BigInteger signBitValue =
                System.Numerics.BigInteger.One << (sourceWidth - 1);

            BoogieExpr modulus = new BoogieLiteralExpr(modulusValue);

            BoogieExpr signBit = new BoogieLiteralExpr(signBitValue);

            /*
             * On conserve uniquement les sourceWidth bits de poids faible.
             *
             * Exemple pour extend8_s :
             *     lowBits = value mod 256
             */
            BoogieExpr lowBits = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.MOD,
                value,
                modulus
            );

            /*
             * Interprétation signée :
             *
             * si lowBits < 2^(sourceWidth-1)
             *     résultat signé = lowBits
             * sinon
             *     résultat signé = lowBits - 2^sourceWidth
             */
            BoogieExpr signedValue = new BoogieITE(
                new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LT, lowBits, signBit),
                lowBits,
                new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.SUB, lowBits, modulus)
            );

            /*
             * Le résultat signé est reconverti vers la représentation
             * non signée du type cible.
             */
            BoogieExpr normalizedValue = targetIsI32 ? ToU32(signedValue) : ToU64(signedValue);

            BoogieExpr typedResult = targetIsI32 ? I32(normalizedValue) : I64(normalizedValue);

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedIntegerWidthCast(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            switch (op)
            {
                // ============================================================
                // i64 -> i32 : conservation des 32 bits de poids faible
                // ============================================================

                case "i32.wrap_i64":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I64")));

                    BoogieExpr value = Field(Tmp1(), "value_i64");

                    body.AddStatement(new BoogieAssertCmd(IsU64(value)));

                    body.AddStatement(
                        new BoogieCallCmd("push", new List<BoogieExpr> { I32(ToU32(value)) }, new())
                    );

                    break;
                }

                // ============================================================
                // i32 -> i64 : extension non signée
                // ============================================================

                case "i64.extend_i32_u":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I32")));

                    BoogieExpr value = Field(Tmp1(), "value_i32");

                    body.AddStatement(new BoogieAssertCmd(IsU32(value)));

                    /*
                     * Le payload i32 est déjà compris entre 0 et 2^32-1.
                     * Il est donc directement valide comme payload i64.
                     */
                    body.AddStatement(
                        new BoogieCallCmd("push", new List<BoogieExpr> { I64(value) }, new())
                    );

                    break;
                }

                // ============================================================
                // i32 -> i64 : extension signée
                // ============================================================

                case "i64.extend_i32_s":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I32")));

                    BoogieExpr value = Field(Tmp1(), "value_i32");

                    body.AddStatement(new BoogieAssertCmd(IsU32(value)));

                    /*
                     * i32_signed reconstruit la valeur mathématique signée,
                     * puis to_u64 fournit sa représentation sur 64 bits.
                     *
                     * Exemple :
                     *   I32(4294967295) représente -1
                     *   i32_signed(...) = -1
                     *   to_u64(-1) = 18446744073709551615
                     */
                    BoogieExpr signedValue = I32Signed(value);

                    BoogieExpr extendedValue = ToU64(signedValue);

                    body.AddStatement(
                        new BoogieCallCmd(
                            "push",
                            new List<BoogieExpr> { I64(extendedValue) },
                            new()
                        )
                    );

                    break;
                }

                default:
                    throw new NotSupportedException($"Unsupported integer width cast: {op}");
            }
        }

        private void EmitTypedFloatToIntegerCast(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            bool sourceIsF32 = op.Contains("_f32_", StringComparison.Ordinal);

            bool targetIsI32 = op.StartsWith("i32.", StringComparison.Ordinal);

            bool targetIsSigned = op.EndsWith("_s", StringComparison.Ordinal);

            string sourceConstructor = sourceIsF32 ? "F32" : "F64";

            string sourceField = sourceIsF32 ? "value_f32" : "value_f64";

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), sourceConstructor)));

            BoogieExpr realValue = Field(Tmp1(), sourceField);

            body.AddStatement(
                new BoogieAssertCmd(sourceIsF32 ? IsF32(realValue) : IsF64(realValue))
            );

            BoogieExpr zeroReal = new BoogieLiteralExpr(new Pfloat(0.0f));

            /*
             * La conversion native int(real) correspond au plancher.
             *
             * WebAssembly exige une troncature vers zéro :
             *
             *   x >= 0 : int(x)
             *   x <  0 : -int(-x)
             */
            BoogieExpr nonNegative = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.GE,
                realValue,
                zeroReal
            );

            BoogieExpr positiveTruncation = new BoogieFunctionCall(
                "int",
                new List<BoogieExpr> { realValue }
            );

            BoogieExpr negativeRealValue = new BoogieUnaryOperation(
                BoogieUnaryOperation.Opcode.NEG,
                realValue
            );

            BoogieExpr negativeTruncation = new BoogieUnaryOperation(
                BoogieUnaryOperation.Opcode.NEG,
                new BoogieFunctionCall("int", new List<BoogieExpr> { negativeRealValue })
            );

            BoogieExpr truncatedValue = new BoogieITE(
                nonNegative,
                positiveTruncation,
                negativeTruncation
            );

            // ============================================================
            // WebAssembly trap: result outside target integer range
            // ============================================================

            BoogieExpr lowerBound;
            BoogieExpr upperBound;

            if (targetIsI32)
            {
                if (targetIsSigned)
                {
                    lowerBound = new BoogieUnaryOperation(
                        BoogieUnaryOperation.Opcode.NEG,
                        BId("TWO31")
                    );

                    upperBound = BId("TWO31");
                }
                else
                {
                    lowerBound = new BoogieLiteralExpr(0);

                    upperBound = BId("TWO32");
                }
            }
            else
            {
                if (targetIsSigned)
                {
                    lowerBound = new BoogieUnaryOperation(
                        BoogieUnaryOperation.Opcode.NEG,
                        BId("TWO63")
                    );

                    upperBound = BId("TWO63");
                }
                else
                {
                    lowerBound = new BoogieLiteralExpr(0);

                    upperBound = BId("TWO64");
                }
            }

            BoogieExpr resultInRange = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.AND,
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.GE,
                    truncatedValue,
                    lowerBound
                ),
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.LT,
                    truncatedValue,
                    upperBound
                )
            );

            /*
             * Comme pour une division par zéro, le trap WebAssembly
             * devient une obligation de vérification.
             */
            body.AddStatement(new BoogieAssertCmd(resultInRange));

            /*
             * Les conversions signées doivent être remappées vers
             * le payload non signé de I32/I64.
             *
             * Les conversions non signées produisent déjà un payload
             * dans l'intervalle attendu.
             */
            BoogieExpr payload;

            if (targetIsSigned)
            {
                payload = targetIsI32 ? ToU32(truncatedValue) : ToU64(truncatedValue);
            }
            else
            {
                payload = truncatedValue;
            }

            BoogieExpr typedResult = targetIsI32 ? I32(payload) : I64(payload);

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedFloatWidthCast(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            switch (op)
            {
                // ============================================================
                // f32 -> f64
                // ============================================================

                case "f64.promote_f32":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "F32")));

                    BoogieExpr value = Field(Tmp1(), "value_f32");

                    body.AddStatement(new BoogieAssertCmd(IsF32(value)));

                    /*
                     * La promotion f32 -> f64 est exacte.
                     * L'axiome is_f32(x) ==> is_f64(x) garantit
                     * que le même payload est valide comme f64.
                     */
                    body.AddStatement(
                        new BoogieCallCmd("push", new List<BoogieExpr> { F64(value) }, new())
                    );

                    break;
                }

                // ============================================================
                // f64 -> f32
                // ============================================================

                case "f32.demote_f64":
                {
                    body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "F64")));

                    BoogieExpr value = Field(Tmp1(), "value_f64");

                    body.AddStatement(new BoogieAssertCmd(IsF64(value)));

                    /*
                     * La démotion peut perdre de la précision.
                     * to_f32 représente abstraitement l'arrondi vers f32.
                     */
                    BoogieExpr demotedValue = ToF32(value);

                    body.AddStatement(
                        new BoogieCallCmd("push", new List<BoogieExpr> { F32(demotedValue) }, new())
                    );

                    break;
                }

                default:
                    throw new NotSupportedException($"Unsupported float width cast: {op}");
            }
        }

        private void EmitTypedFloatBinaryMathOp(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

            bool isF32 = op.StartsWith("f32.");

            string ctor = isF32 ? "F32" : "F64";

            string field = isF32 ? "value_f32" : "value_f64";

            // ============================================================
            // Type checks
            // ============================================================

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), ctor)));

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), ctor)));

            var lhs = Field(Tmp2(), field);

            var rhs = Field(Tmp1(), field);

            // ============================================================
            // Floating abstraction invariants
            // ============================================================

            body.AddStatement(new BoogieAssertCmd(isF32 ? IsF32(lhs) : IsF64(lhs)));

            body.AddStatement(new BoogieAssertCmd(isF32 ? IsF32(rhs) : IsF64(rhs)));

            // ============================================================
            // Select semantic helper
            // ============================================================

            string fun = op switch
            {
                "f32.min" or "f64.min" => "min_real",

                "f32.max" or "f64.max" => "max_real",

                "f32.copysign" or "f64.copysign" => "copysign_real",

                _ => throw new NotSupportedException(
                    $"Unsupported typed float binary operation: {op}"
                ),
            };

            // ============================================================
            // Apply operation on real payloads
            // ============================================================

            var rawResult = new BoogieFunctionCall(fun, new List<BoogieExpr> { lhs, rhs });

            // ============================================================
            // Normalize according to Wasm type
            // ============================================================

            BoogieExpr normalized = isF32 ? ToF32(rawResult) : ToF64(rawResult);

            BoogieExpr typedResult = isF32 ? F32(normalized) : F64(normalized);

            // ============================================================
            // Push WasmValue
            // ============================================================

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedIntegerShiftOp(string op, BoogieStmtList body)
        {
            // Opérande droit : nombre de bits du décalage.
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            // Opérande gauche : valeur à décaler.
            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

            bool isI32 = op.StartsWith("i32.", StringComparison.Ordinal);

            string constructor = isI32 ? "I32" : "I64";
            string field = isI32 ? "value_i32" : "value_i64";

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), constructor)));

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), constructor)));

            BoogieExpr lhs = Field(Tmp2(), field);
            BoogieExpr rhs = Field(Tmp1(), field);

            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(lhs) : IsU64(lhs)));

            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(rhs) : IsU64(rhs)));

            string shiftFunction = op switch
            {
                "i32.shl" => "bv32_shl",
                "i32.shr_u" => "bv32_shr_u",
                "i32.shr_s" => "bv32_shr_s",

                "i64.shl" => "bv64_shl",
                "i64.shr_u" => "bv64_shr_u",
                "i64.shr_s" => "bv64_shr_s",

                _ => throw new NotSupportedException($"Unsupported integer shift operation: {op}"),
            };

            string intToBvFunction = isI32 ? "int_to_bv32" : "int_to_bv64";

            string bvToIntFunction = isI32 ? "bv32_to_int" : "bv64_to_int";

            BoogieExpr lhsBitVector = new BoogieFunctionCall(
                intToBvFunction,
                new List<BoogieExpr> { lhs }
            );

            /*
             * WebAssembly masque le nombre de positions :
             *
             * i32 : rhs mod 32
             * i64 : rhs mod 64
             *
             * Cette normalisation est indispensable. Un bvshl brut avec
             * un décalage >= largeur produirait un comportement différent.
             */
            BoogieExpr normalizedShiftAmount = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.MOD,
                rhs,
                new BoogieLiteralExpr(isI32 ? 32 : 64)
            );

            BoogieExpr shiftBitVector = new BoogieFunctionCall(
                intToBvFunction,
                new List<BoogieExpr> { normalizedShiftAmount }
            );

            BoogieExpr bitVectorResult = new BoogieFunctionCall(
                shiftFunction,
                new List<BoogieExpr> { lhsBitVector, shiftBitVector }
            );

            BoogieExpr integerResult = new BoogieFunctionCall(
                bvToIntFunction,
                new List<BoogieExpr> { bitVectorResult }
            );

            BoogieExpr typedResult = isI32 ? I32(integerResult) : I64(integerResult);

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedIntegerRotateOp(string op, BoogieStmtList body)
        {
            // Nombre de positions.
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            // Valeur à faire tourner.
            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

            bool isI32 = op.StartsWith("i32.", StringComparison.Ordinal);

            bool isRotateLeft = op.EndsWith(".rotl", StringComparison.Ordinal);

            string constructor = isI32 ? "I32" : "I64";
            string field = isI32 ? "value_i32" : "value_i64";

            int width = isI32 ? 32 : 64;

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), constructor)));

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), constructor)));

            BoogieExpr value = Field(Tmp2(), field);
            BoogieExpr amount = Field(Tmp1(), field);

            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(value) : IsU64(value)));

            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(amount) : IsU64(amount)));

            string intToBv = isI32 ? "int_to_bv32" : "int_to_bv64";

            string bvToInt = isI32 ? "bv32_to_int" : "bv64_to_int";

            string shiftLeft = isI32 ? "bv32_shl" : "bv64_shl";

            string shiftRight = isI32 ? "bv32_shr_u" : "bv64_shr_u";

            string bitwiseOr = isI32 ? "bv32_or" : "bv64_or";

            BoogieExpr widthLiteral = new BoogieLiteralExpr(width);

            // n = amount mod width
            BoogieExpr normalizedAmount = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.MOD,
                amount,
                widthLiteral
            );

            /*
             * Le second décalage utilise :
             *
             * (width - n) mod width
             *
             * Le modulo final garantit que n = 0 produit également
             * un décalage de zéro.
             */
            BoogieExpr complementaryAmount = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.MOD,
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.SUB,
                    new BoogieLiteralExpr(width),
                    normalizedAmount
                ),
                new BoogieLiteralExpr(width)
            );

            BoogieExpr valueBv = new BoogieFunctionCall(intToBv, new List<BoogieExpr> { value });

            BoogieExpr amountBv = new BoogieFunctionCall(
                intToBv,
                new List<BoogieExpr> { normalizedAmount }
            );

            BoogieExpr complementaryAmountBv = new BoogieFunctionCall(
                intToBv,
                new List<BoogieExpr> { complementaryAmount }
            );

            BoogieExpr firstPart;
            BoogieExpr secondPart;

            if (isRotateLeft)
            {
                // rotl(x,n) = (x << n) | (x >>u (width-n))
                firstPart = new BoogieFunctionCall(
                    shiftLeft,
                    new List<BoogieExpr> { valueBv, amountBv }
                );

                secondPart = new BoogieFunctionCall(
                    shiftRight,
                    new List<BoogieExpr> { valueBv, complementaryAmountBv }
                );
            }
            else
            {
                // rotr(x,n) = (x >>u n) | (x << (width-n))
                firstPart = new BoogieFunctionCall(
                    shiftRight,
                    new List<BoogieExpr> { valueBv, amountBv }
                );

                secondPart = new BoogieFunctionCall(
                    shiftLeft,
                    new List<BoogieExpr> { valueBv, complementaryAmountBv }
                );
            }

            BoogieExpr rotatedBv = new BoogieFunctionCall(
                bitwiseOr,
                new List<BoogieExpr> { firstPart, secondPart }
            );

            BoogieExpr integerResult = new BoogieFunctionCall(
                bvToInt,
                new List<BoogieExpr> { rotatedBv }
            );

            BoogieExpr typedResult = isI32 ? I32(integerResult) : I64(integerResult);

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedIntegerLogicalBitwiseOp(string op, BoogieStmtList body)
        {
            // Le dernier opérande empilé est l'opérande droit.
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            // L'opérande gauche est récupéré ensuite.
            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

            bool isI32 = op.StartsWith("i32.", StringComparison.Ordinal);

            string constructor = isI32 ? "I32" : "I64";
            string field = isI32 ? "value_i32" : "value_i64";

            // Vérification dynamique des types WasmValue.
            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), constructor)));

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), constructor)));

            BoogieExpr lhs = Field(Tmp2(), field);
            BoogieExpr rhs = Field(Tmp1(), field);

            // Invariants de représentation.
            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(lhs) : IsU64(lhs)));

            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(rhs) : IsU64(rhs)));

            string bitwiseFunction = op switch
            {
                "i32.and" => "bv32_and",
                "i32.or" => "bv32_or",
                "i32.xor" => "bv32_xor",

                "i64.and" => "bv64_and",
                "i64.or" => "bv64_or",
                "i64.xor" => "bv64_xor",

                _ => throw new NotSupportedException(
                    $"Unsupported logical bitwise operation: {op}"
                ),
            };

            string intToBvFunction = isI32 ? "int_to_bv32" : "int_to_bv64";

            string bvToIntFunction = isI32 ? "bv32_to_int" : "bv64_to_int";

            BoogieExpr lhsBitVector = new BoogieFunctionCall(
                intToBvFunction,
                new List<BoogieExpr> { lhs }
            );

            BoogieExpr rhsBitVector = new BoogieFunctionCall(
                intToBvFunction,
                new List<BoogieExpr> { rhs }
            );

            BoogieExpr bitVectorResult = new BoogieFunctionCall(
                bitwiseFunction,
                new List<BoogieExpr> { lhsBitVector, rhsBitVector }
            );

            BoogieExpr integerResult = new BoogieFunctionCall(
                bvToIntFunction,
                new List<BoogieExpr> { bitVectorResult }
            );

            // La conversion bv -> int est non signée.
            // On conserve néanmoins la normalisation explicite pour
            // maintenir l'invariant de WasmValue.
            BoogieExpr typedResult = isI32 ? I32(integerResult) : I64(integerResult);

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedFloatRoundingOp(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            bool isF32 = op.StartsWith("f32.");

            string ctor = isF32 ? "F32" : "F64";
            string field = isF32 ? "value_f32" : "value_f64";

            // ---------------------------------------------------------
            // Type check
            // ---------------------------------------------------------

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), ctor)));

            var raw = Field(Tmp1(), field);

            // ---------------------------------------------------------
            // Floating abstraction domain
            // ---------------------------------------------------------

            body.AddStatement(new BoogieAssertCmd(isF32 ? IsF32(raw) : IsF64(raw)));

            // ---------------------------------------------------------
            // Operation
            // ---------------------------------------------------------

            string fun = op switch
            {
                "f32.ceil" or "f64.ceil" => "ceil_real",

                "f32.trunc" or "f64.trunc" => "trunc_real",

                _ => throw new NotSupportedException($"Unsupported float rounding operation: {op}"),
            };

            var rawResult = new BoogieFunctionCall(fun, new List<BoogieExpr> { raw });

            // ---------------------------------------------------------
            // Re-normalize according to Wasm type
            // ---------------------------------------------------------

            var normalized = isF32 ? ToF32(rawResult) : ToF64(rawResult);

            var typedResult = isF32 ? F32(normalized) : F64(normalized);

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedIntegerUnaryBitOp(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            bool isI32 = op.StartsWith("i32.");

            string ctor = isI32 ? "I32" : "I64";
            string field = isI32 ? "value_i32" : "value_i64";

            // Type check
            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), ctor)));

            var raw = Field(Tmp1(), field);

            // Representation invariant
            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(raw) : IsU64(raw)));

            string fun = op switch
            {
                "i32.clz" => "i32_clz",
                "i64.clz" => "i64_clz",

                "i32.ctz" => "i32_ctz",
                "i64.ctz" => "i64_ctz",

                "i32.popcnt" => "i32_popcnt",
                "i64.popcnt" => "i64_popcnt",

                _ => throw new NotSupportedException($"Unsupported typed integer unary op: {op}"),
            };

            var result = new BoogieFunctionCall(fun, new List<BoogieExpr> { raw });

            // WebAssembly result has same integer type as operand
            var typedResult = isI32 ? I32(result) : I64(result);

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private void EmitTypedComparison(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

            string ctor;
            string field;

            BoogieExpr? cond = null;

            switch (op)
            {
                // ============================================================
                // i32 comparisons
                // ============================================================

                case "i32.eq":
                {
                    ctor = "I32";
                    field = "value_i32";
                    break;
                }

                case "i32.ne":
                {
                    ctor = "I32";
                    field = "value_i32";
                    break;
                }

                case "i32.lt_s":
                case "i32.gt_s":
                case "i32.le_s":
                case "i32.ge_s":
                case "i32.lt_u":
                case "i32.gt_u":
                case "i32.le_u":
                case "i32.ge_u":
                {
                    ctor = "I32";
                    field = "value_i32";
                    break;
                }

                // ============================================================
                // i64 comparisons
                // ============================================================

                case "i64.eq":
                {
                    ctor = "I64";
                    field = "value_i64";
                    break;
                }

                case "i64.ne":
                {
                    ctor = "I64";
                    field = "value_i64";
                    break;
                }

                case "i64.lt_s":
                case "i64.gt_s":
                case "i64.le_s":
                case "i64.ge_s":
                case "i64.lt_u":
                case "i64.gt_u":
                case "i64.le_u":
                case "i64.ge_u":
                {
                    ctor = "I64";
                    field = "value_i64";
                    break;
                }

                // ============================================================
                // f32 comparisons
                // ============================================================

                case "f32.eq":
                case "f32.ne":
                case "f32.lt":
                case "f32.gt":
                case "f32.le":
                case "f32.ge":
                {
                    ctor = "F32";
                    field = "value_f32";
                    break;
                }

                // ============================================================
                // f64 comparisons
                // ============================================================

                case "f64.eq":
                case "f64.ne":
                case "f64.lt":
                case "f64.gt":
                case "f64.le":
                case "f64.ge":
                {
                    ctor = "F64";
                    field = "value_f64";
                    break;
                }

                default:
                {
                    body.AddStatement(
                        new BoogieCommentCmd($"// unsupported typed comparison op: {op}")
                    );
                    return;
                }
            }

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), ctor)));
            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), ctor)));

            var lhsRaw = Field(Tmp2(), field);
            var rhsRaw = Field(Tmp1(), field);

            // ============================================================
            // Domain assertions
            // ============================================================

            if (ctor == "I32")
            {
                body.AddStatement(new BoogieAssertCmd(IsU32(lhsRaw)));
                body.AddStatement(new BoogieAssertCmd(IsU32(rhsRaw)));
            }
            else if (ctor == "I64")
            {
                body.AddStatement(new BoogieAssertCmd(IsU64(lhsRaw)));
                body.AddStatement(new BoogieAssertCmd(IsU64(rhsRaw)));
            }
            else if (ctor == "F32")
            {
                body.AddStatement(new BoogieAssertCmd(IsF32(lhsRaw)));
                body.AddStatement(new BoogieAssertCmd(IsF32(rhsRaw)));
            }
            else if (ctor == "F64")
            {
                body.AddStatement(new BoogieAssertCmd(IsF64(lhsRaw)));
                body.AddStatement(new BoogieAssertCmd(IsF64(rhsRaw)));
            }

            // ============================================================
            // Build the comparison condition
            // ============================================================

            switch (op)
            {
                // ----------------------------
                // i32 equality
                // ----------------------------
                case "i32.eq":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "i32.ne":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.NEQ,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                // ----------------------------
                // i32 signed comparisons
                // ----------------------------
                case "i32.lt_s":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LT,
                        I32Signed(lhsRaw),
                        I32Signed(rhsRaw)
                    );
                    break;

                case "i32.gt_s":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GT,
                        I32Signed(lhsRaw),
                        I32Signed(rhsRaw)
                    );
                    break;

                case "i32.le_s":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LE,
                        I32Signed(lhsRaw),
                        I32Signed(rhsRaw)
                    );
                    break;

                case "i32.ge_s":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GE,
                        I32Signed(lhsRaw),
                        I32Signed(rhsRaw)
                    );
                    break;

                // ----------------------------
                // i32 unsigned comparisons
                // ----------------------------
                case "i32.lt_u":
                    cond = U32Lt(lhsRaw, rhsRaw);
                    break;

                case "i32.gt_u":
                    cond = U32Gt(lhsRaw, rhsRaw);
                    break;

                case "i32.le_u":
                    cond = U32Le(lhsRaw, rhsRaw);
                    break;

                case "i32.ge_u":
                    cond = U32Ge(lhsRaw, rhsRaw);
                    break;

                // ----------------------------
                // i64 equality
                // ----------------------------
                case "i64.eq":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "i64.ne":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.NEQ,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                // ----------------------------
                // i64 signed comparisons
                // ----------------------------
                case "i64.lt_s":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LT,
                        I64Signed(lhsRaw),
                        I64Signed(rhsRaw)
                    );
                    break;

                case "i64.gt_s":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GT,
                        I64Signed(lhsRaw),
                        I64Signed(rhsRaw)
                    );
                    break;

                case "i64.le_s":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LE,
                        I64Signed(lhsRaw),
                        I64Signed(rhsRaw)
                    );
                    break;

                case "i64.ge_s":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GE,
                        I64Signed(lhsRaw),
                        I64Signed(rhsRaw)
                    );
                    break;

                // ----------------------------
                // i64 unsigned comparisons
                // ----------------------------
                case "i64.lt_u":
                    cond = U64Lt(lhsRaw, rhsRaw);
                    break;

                case "i64.gt_u":
                    cond = U64Gt(lhsRaw, rhsRaw);
                    break;

                case "i64.le_u":
                    cond = U64Le(lhsRaw, rhsRaw);
                    break;

                case "i64.ge_u":
                    cond = U64Ge(lhsRaw, rhsRaw);
                    break;

                // ----------------------------
                // f32 comparisons over real abstraction
                // ----------------------------
                case "f32.eq":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f32.ne":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.NEQ,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f32.lt":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LT,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f32.gt":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GT,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f32.le":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LE,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f32.ge":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GE,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                // ----------------------------
                // f64 comparisons over real abstraction
                // ----------------------------
                case "f64.eq":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f64.ne":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.NEQ,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f64.lt":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LT,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f64.gt":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GT,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f64.le":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.LE,
                        lhsRaw,
                        rhsRaw
                    );
                    break;

                case "f64.ge":
                    cond = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.GE,
                        lhsRaw,
                        rhsRaw
                    );
                    break;
            }

            if (cond == null)
            {
                body.AddStatement(
                    new BoogieCommentCmd($"// failed to build comparison condition for: {op}")
                );
                return;
            }

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { BoolToI32(cond) }, new())
            );
        }

        private void EmitTypedIntegerDivRem(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

            bool isI32 = op.StartsWith("i32.");
            bool isSigned = op.EndsWith("_s");

            bool isDiv = op.Contains(".div_");

            string ctor = isI32 ? "I32" : "I64";
            string field = isI32 ? "value_i32" : "value_i64";

            // ---------------------------------------------------------
            // Type checks
            // ---------------------------------------------------------

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), ctor)));

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), ctor)));

            var lhsRaw = Field(Tmp2(), field);
            var rhsRaw = Field(Tmp1(), field);

            // ---------------------------------------------------------
            // Representation invariant
            // ---------------------------------------------------------

            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(lhsRaw) : IsU64(lhsRaw)));

            body.AddStatement(new BoogieAssertCmd(isI32 ? IsU32(rhsRaw) : IsU64(rhsRaw)));

            // ---------------------------------------------------------
            // WebAssembly trap: division/remainder by zero
            // ---------------------------------------------------------

            body.AddStatement(
                new BoogieAssertCmd(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.NEQ,
                        rhsRaw,
                        new BoogieLiteralExpr(0)
                    )
                )
            );

            BoogieExpr result;

            // =========================================================
            // UNSIGNED
            // =========================================================

            if (!isSigned)
            {
                result = new BoogieFunctionCall(
                    isDiv ? "int_div_u" : "int_rem_u",
                    new List<BoogieExpr> { lhsRaw, rhsRaw }
                );
            }
            // =========================================================
            // SIGNED
            // =========================================================

            else
            {
                var lhsSigned = isI32 ? I32Signed(lhsRaw) : I64Signed(lhsRaw);

                var rhsSigned = isI32 ? I32Signed(rhsRaw) : I64Signed(rhsRaw);

                body.AddStatement(
                    new BoogieAssertCmd(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.NEQ,
                            rhsSigned,
                            new BoogieLiteralExpr(0)
                        )
                    )
                );

                if (isDiv)
                {
                    BoogieExpr minValue = isI32
                        ? new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NEG, BId("TWO31"))
                        : new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NEG, BId("TWO63"));

                    var lhsIsMin = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        lhsSigned,
                        minValue
                    );

                    var rhsIsMinusOne = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        rhsSigned,
                        new BoogieLiteralExpr(-1)
                    );

                    var overflow = new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.AND,
                        lhsIsMin,
                        rhsIsMinusOne
                    );

                    body.AddStatement(
                        new BoogieAssertCmd(
                            new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, overflow)
                        )
                    );
                }

                result = new BoogieFunctionCall(
                    isDiv ? "int_div_s" : "int_rem_s",
                    new List<BoogieExpr> { lhsSigned, rhsSigned }
                );
            }

            // ---------------------------------------------------------
            // Convert signed mathematical result back to Wasm bits
            // ---------------------------------------------------------

            BoogieExpr normalized = isI32 ? ToU32(result) : ToU64(result);

            BoogieExpr typedResult = isI32 ? I32(normalized) : I64(normalized);

            body.AddStatement(new BoogieCallCmd("push", new() { typedResult }, new()));
        }

        private void EmitTypedEqz(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            string ctor;
            string field;

            switch (op)
            {
                case "i32.eqz":
                    ctor = "I32";
                    field = "value_i32";
                    break;

                case "i64.eqz":
                    ctor = "I64";
                    field = "value_i64";
                    break;

                default:
                    body.AddStatement(new BoogieCommentCmd($"// unsupported eqz op: {op}"));
                    return;
            }

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), ctor)));

            var value = Field(Tmp1(), field);

            var cond = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.EQ,
                value,
                new BoogieLiteralExpr(new System.Numerics.BigInteger(0))
            );

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { BoolToI32(cond) }, new())
            );
        }

        private void EmitTypedFloatUnary(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

            string ctor;
            string field;
            BoogieExpr rawResult;

            bool isF32 = op.StartsWith("f32.", StringComparison.Ordinal);
            bool isF64 = op.StartsWith("f64.", StringComparison.Ordinal);

            if (!isF32 && !isF64)
            {
                body.AddStatement(new BoogieCommentCmd($"// unsupported float unary op: {op}"));
                return;
            }

            ctor = isF32 ? "F32" : "F64";
            field = isF32 ? "value_f32" : "value_f64";

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), ctor)));

            var value = Field(Tmp1(), field);

            switch (op)
            {
                case "f32.abs":
                case "f64.abs":
                    rawResult = new BoogieFunctionCall("abs_real", new List<BoogieExpr> { value });
                    break;

                case "f32.neg":
                case "f64.neg":
                    rawResult = new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NEG, value);
                    break;

                case "f32.sqrt":
                case "f64.sqrt":
                    rawResult = new BoogieFunctionCall("sqrt_real", new List<BoogieExpr> { value });
                    break;

                case "f32.floor":
                case "f64.floor":
                    rawResult = new BoogieFunctionCall(
                        "floor_real",
                        new List<BoogieExpr> { value }
                    );
                    break;

                case "f32.nearest":
                case "f64.nearest":
                    rawResult = new BoogieFunctionCall(
                        "nearest_real",
                        new List<BoogieExpr> { value }
                    );
                    break;

                default:
                    body.AddStatement(new BoogieCommentCmd($"// unsupported float unary op: {op}"));
                    return;
            }

            BoogieExpr normalizedResult = ctor == "F32" ? ToF32(rawResult) : ToF64(rawResult);

            var typedResult = new BoogieFuncCallExpr(
                ctor,
                new List<BoogieExpr> { normalizedResult }
            );

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private static bool IsTypedReinterpretCast(string op) =>
            op
                is "f32.reinterpret_i32"
                    or "i32.reinterpret_f32"
                    or "f64.reinterpret_i64"
                    or "i64.reinterpret_f64";

        private static bool IsTypedFloatToIntegerCast(string op) =>
            op
                is "i32.trunc_f32_s"
                    or "i32.trunc_f32_u"
                    or "i32.trunc_f64_s"
                    or "i32.trunc_f64_u"
                    or "i64.trunc_f32_s"
                    or "i64.trunc_f32_u"
                    or "i64.trunc_f64_s"
                    or "i64.trunc_f64_u";

        private static bool IsTypedFloatWidthCast(string op) =>
            op is "f32.demote_f64" or "f64.promote_f32";

        private static bool IsTypedIntegerToFloatCast(string op) =>
            op
                is "f32.convert_i32_s"
                    or "f32.convert_i32_u"
                    or "f32.convert_i64_s"
                    or "f32.convert_i64_u"
                    or "f64.convert_i32_s"
                    or "f64.convert_i32_u"
                    or "f64.convert_i64_s"
                    or "f64.convert_i64_u";

        private static bool IsTypedIntegerSignExtensionOp(string op) =>
            op
                is "i32.extend8_s"
                    or "i32.extend16_s"
                    or "i64.extend8_s"
                    or "i64.extend16_s"
                    or "i64.extend32_s";

        private static bool IsTypedIntegerWidthCast(string op) =>
            op is "i32.wrap_i64" or "i64.extend_i32_u" or "i64.extend_i32_s";

        private static bool IsTypedIntegerRotateOp(string op) =>
            op is "i32.rotl" or "i32.rotr" or "i64.rotl" or "i64.rotr";

        private static bool IsTypedIntegerDivRemOp(string op) =>
            op
                is "i32.div_s"
                    or "i32.div_u"
                    or "i32.rem_s"
                    or "i32.rem_u"
                    or "i64.div_s"
                    or "i64.div_u"
                    or "i64.rem_s"
                    or "i64.rem_u";

        private static bool IsTypedIntegerShiftOp(string op) =>
            op
                is "i32.shl"
                    or "i32.shr_u"
                    or "i32.shr_s"
                    or "i64.shl"
                    or "i64.shr_u"
                    or "i64.shr_s";

        private static bool IsTypedIntegerLogicalBitwiseOp(string op) =>
            op is "i32.and" or "i32.or" or "i32.xor" or "i64.and" or "i64.or" or "i64.xor";

        private static bool IsTypedEqzOp(string op) => op is "i32.eqz" or "i64.eqz";

        private static bool IsTypedIntegerUnaryBitOp(string op) =>
            op is "i32.clz" or "i64.clz" or "i32.ctz" or "i64.ctz" or "i32.popcnt" or "i64.popcnt";

        private static bool IsTypedFloatUnaryOp(string op) =>
            op
                is "f32.abs"
                    or "f64.abs"
                    or "f32.neg"
                    or "f64.neg"
                    or "f32.sqrt"
                    or "f64.sqrt"
                    or "f32.floor"
                    or "f64.floor"
                    or "f32.nearest"
                    or "f64.nearest";

        private static bool IsTypedFloatRoundingOp(string op) =>
            op is "f32.ceil" or "f64.ceil" or "f32.trunc" or "f64.trunc";

        private static bool IsTypedFloatBinaryMathOp(string op) =>
            op
                is "f32.min"
                    or "f64.min"
                    or "f32.max"
                    or "f64.max"
                    or "f32.copysign"
                    or "f64.copysign";

        private static bool IsTypedArithmeticOp(string op) =>
            op
                is "i32.add"
                    or "i32.sub"
                    or "i32.mul"
                    or "i64.add"
                    or "i64.sub"
                    or "i64.mul"
                    or "f32.add"
                    or "f32.sub"
                    or "f32.mul"
                    or "f32.div"
                    or "f64.add"
                    or "f64.sub"
                    or "f64.mul"
                    or "f64.div";

        private static bool IsTypedComparisonOp(string op) =>
            op
                is "i32.eq"
                    or "i32.ne"
                    or "i32.lt_s"
                    or "i32.lt_u"
                    or "i32.gt_s"
                    or "i32.gt_u"
                    or "i32.le_s"
                    or "i32.le_u"
                    or "i32.ge_s"
                    or "i32.ge_u"
                    or "i64.eq"
                    or "i64.ne"
                    or "i64.lt_s"
                    or "i64.lt_u"
                    or "i64.gt_s"
                    or "i64.gt_u"
                    or "i64.le_s"
                    or "i64.le_u"
                    or "i64.ge_s"
                    or "i64.ge_u"
                    or "f32.eq"
                    or "f32.ne"
                    or "f32.lt"
                    or "f32.gt"
                    or "f32.le"
                    or "f32.ge"
                    or "f64.eq"
                    or "f64.ne"
                    or "f64.lt"
                    or "f64.gt"
                    or "f64.le"
                    or "f64.ge";

        private void EmitTypedBinaryArithmetic(string op, BoogieStmtList body)
        {
            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

            string ctor;
            string field;
            BoogieBinaryOperation.Opcode bop;

            switch (op)
            {
                case "i32.add":
                    ctor = "I32";
                    field = "value_i32";
                    bop = BoogieBinaryOperation.Opcode.ADD;
                    break;

                case "i32.sub":
                    ctor = "I32";
                    field = "value_i32";
                    bop = BoogieBinaryOperation.Opcode.SUB;
                    break;

                case "i32.mul":
                    ctor = "I32";
                    field = "value_i32";
                    bop = BoogieBinaryOperation.Opcode.MUL;
                    break;

                case "i64.add":
                    ctor = "I64";
                    field = "value_i64";
                    bop = BoogieBinaryOperation.Opcode.ADD;
                    break;

                case "i64.sub":
                    ctor = "I64";
                    field = "value_i64";
                    bop = BoogieBinaryOperation.Opcode.SUB;
                    break;

                case "i64.mul":
                    ctor = "I64";
                    field = "value_i64";
                    bop = BoogieBinaryOperation.Opcode.MUL;
                    break;

                case "f32.add":
                    ctor = "F32";
                    field = "value_f32";
                    bop = BoogieBinaryOperation.Opcode.ADD;
                    break;

                case "f32.sub":
                    ctor = "F32";
                    field = "value_f32";
                    bop = BoogieBinaryOperation.Opcode.SUB;
                    break;

                case "f32.mul":
                    ctor = "F32";
                    field = "value_f32";
                    bop = BoogieBinaryOperation.Opcode.MUL;
                    break;

                case "f64.add":
                    ctor = "F64";
                    field = "value_f64";
                    bop = BoogieBinaryOperation.Opcode.ADD;
                    break;

                case "f64.sub":
                    ctor = "F64";
                    field = "value_f64";
                    bop = BoogieBinaryOperation.Opcode.SUB;
                    break;

                case "f64.mul":
                    ctor = "F64";
                    field = "value_f64";
                    bop = BoogieBinaryOperation.Opcode.MUL;
                    break;
                case "f32.div":
                    ctor = "F32";
                    field = "value_f32";
                    bop = BoogieBinaryOperation.Opcode.DIV;
                    break;

                case "f64.div":
                    ctor = "F64";
                    field = "value_f64";
                    bop = BoogieBinaryOperation.Opcode.DIV;
                    break;

                default:
                    body.AddStatement(
                        new BoogieCommentCmd($"// unsupported typed arithmetic op: {op}")
                    );
                    return;
            }

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), ctor)));

            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), ctor)));

            var lhs = Field(Tmp2(), field);
            var rhs = Field(Tmp1(), field);

            if (ctor == "I32")
            {
                body.AddStatement(new BoogieAssertCmd(IsU32(lhs)));
                body.AddStatement(new BoogieAssertCmd(IsU32(rhs)));
            }
            else if (ctor == "I64")
            {
                body.AddStatement(new BoogieAssertCmd(IsU64(lhs)));
                body.AddStatement(new BoogieAssertCmd(IsU64(rhs)));
            }
            else if (ctor == "F32")
            {
                body.AddStatement(new BoogieAssertCmd(IsF32(lhs)));
                body.AddStatement(new BoogieAssertCmd(IsF32(rhs)));
            }
            else if (ctor == "F64")
            {
                body.AddStatement(new BoogieAssertCmd(IsF64(lhs)));
                body.AddStatement(new BoogieAssertCmd(IsF64(rhs)));
            }

            var rawResult = new BoogieBinaryOperation(bop, lhs, rhs);

            BoogieExpr normalizedResult = ctor switch
            {
                "I32" => ToU32(rawResult),
                "I64" => ToU64(rawResult),
                "F32" => ToF32(rawResult),
                "F64" => ToF64(rawResult),
                _ => rawResult,
            };

            var typedResult = new BoogieFuncCallExpr(
                ctor,
                new List<BoogieExpr> { normalizedResult }
            );

            body.AddStatement(
                new BoogieCallCmd("push", new List<BoogieExpr> { typedResult }, new())
            );
        }

        private int translateDepth = 0;

        private string BoogieFuncName(WasmFunction f) => SanitizeFunctionName(f.Name, contractName);

        private ModuleSpec? moduleSpec;
        private SpecToBoogieTranslator? specTranslator;

        private SpecToBoogieTranslator GetSpecTranslator()
        {
            return specTranslator
                ?? throw new InvalidOperationException(
                    "The specification translator is not initialized."
                );
        }

        private static string SanitizeIdentifier(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "contract";

            var s = Regex.Replace(raw, @"[^A-Za-z0-9_]", "_");

            if (!char.IsLetter(s[0]) && s[0] != '_')
                s = "_" + s;

            return s;
        }

        private static readonly List<BoogieAttribute> InlineAttrs = new()
        {
            new BoogieAttribute("inline", 1),
        };

        private List<BoogieAttribute>? InlineAttrsIfNotEntry(string name)
        {
            if (name.StartsWith("BoogieEntry_", StringComparison.Ordinal))
                return null;
            if (name.StartsWith("CorralEntry_", StringComparison.Ordinal))
                return null;
            return new List<BoogieAttribute>(InlineAttrs); // copie safe
        }

        private List<BoogieGlobalVariable> BuildEntryModSet(WasmModule m)
        {
            var mods = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$stack", WasmStackBoogieType())),
            };
            bool tableEnabled = PreludeOptions.Sections.HasFlag(PreludeSection.Table);

            if (tableEnabled)
            {
                mods.Add(
                    new BoogieGlobalVariable(
                        new BoogieTypedIdent(
                            "$table",
                            new BoogieMapType(BoogieType.Int, WasmValueBoogieType())
                        )
                    )
                );

                mods.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent("$table_size", BoogieType.Int))
                );

                mods.Add(
    new BoogieGlobalVariable(
        new BoogieTypedIdent(
            "$table_max",
            BoogieType.Int
        )
    )
);
            }
            bool memEnabled =
                PreludeOptions.Sections.HasFlag(PreludeSection.Memory)
                && PreludeOptions.EnableMemory;

            if (memEnabled)
            {
                mods.Add(
                    new BoogieGlobalVariable(
                        new BoogieTypedIdent(
                            "$mem",
                            new BoogieMapType(BoogieType.Int, new BoogieCtorType("bv8"))
                        )
                    )
                );

                mods.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent("$mem_pages", BoogieType.Int))
                );
            }

           

            // ✅ Tous les globals mutables du module (framing-safe)
            foreach (var g in m.Globals)
            {
                if (!g.IsMutable)
                    continue;
                var key = ResolveGlobalKey(g.Index, g.Name);
                var bname = EnsureGlobalDecl(g, key); // garantit déclaration + map stable
                mods.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent(bname, WasmValueBoogieType()))
                );
            }

            return mods;
        }

        private static BoogieType WasmValueBoogieType() => new BoogieCtorType("WasmValue");

        private static BoogieMapType WasmStackBoogieType() =>
            new BoogieMapType(BoogieType.Int, WasmValueBoogieType());

        private static BoogieExpr UndefValue() =>
            new BoogieFuncCallExpr("Undef", new List<BoogieExpr>());

        private static BoogieExpr I32(BoogieExpr e) =>
            new BoogieFuncCallExpr("I32", new List<BoogieExpr> { e });

        private static BoogieExpr I64(BoogieExpr e) =>
            new BoogieFuncCallExpr("I64", new List<BoogieExpr> { e });

        private static BoogieExpr F32(BoogieExpr e) =>
            new BoogieFuncCallExpr("F32", new List<BoogieExpr> { e });

        private static BoogieExpr F64(BoogieExpr e) =>
            new BoogieFuncCallExpr("F64", new List<BoogieExpr> { e });
private static BoogieExpr NullFuncRefValue() =>
    new BoogieFuncCallExpr(
        "NullFuncRef",
        new List<BoogieExpr>()
    );

private static BoogieExpr NullExternRefValue() =>
    new BoogieFuncCallExpr(
        "NullExternRef",
        new List<BoogieExpr>()
    );

private static BoogieExpr FuncRefValue(
    BoogieExpr functionIndex
) =>
    new BoogieFuncCallExpr(
        "FuncRef",
        new List<BoogieExpr>
        {
            functionIndex
        }
    );

private static BoogieExpr ExternRefValue(
    BoogieExpr referenceId
) =>
    new BoogieFuncCallExpr(
        "ExternRef",
        new List<BoogieExpr>
        {
            referenceId
        }
    );

        private static BoogieExpr IsCtor(BoogieExpr e, string ctor) =>
            new BoogieIsConstructorExpr(e, ctor);

        private static BoogieExpr Field(BoogieExpr e, string field) =>
            new BoogieDatatypeFieldAccessExpr(e, field);

        private static BoogieExpr ZeroValueForType(WasmValueType type)
        {
            var zeroInt = new BoogieLiteralExpr(new System.Numerics.BigInteger(0));

            var zeroReal = new BoogieLiteralExpr(new Pfloat(0));

            return type switch
            {
                WasmValueType.I32 => I32(ToU32(zeroInt)),

                WasmValueType.I64 => I64(ToU64(zeroInt)),

                WasmValueType.F32 => F32(ToF32(zeroReal)),

                WasmValueType.F64 => F64(ToF64(zeroReal)),

                _ => throw new NotSupportedException($"Unsupported local type: {type}"),
            };
        }

        //private static BoogieIdentifierExpr Id(string x) => new BoogieIdentifierExpr(x);
        private static BoogieLiteralExpr IntLit(int v) =>
            new BoogieLiteralExpr(new System.Numerics.BigInteger(v));

        private void EmitHavocPushArgs(WasmFunction function, BoogieStmtList body)
        {
            if (function.ParamTypes.Count != function.ParamCount)
            {
                throw new InvalidOperationException(
                    $"Function {function.Name}: expected {function.ParamCount} "
                        + $"parameter types, but found {function.ParamTypes.Count}."
                );
            }

            foreach (var parameterType in function.ParamTypes)
            {
                body.AddStatement(
                    new BoogieCallCmd(
                        "push",
                        new List<BoogieExpr> { MakeNondetWasmValue(parameterType) },
                        new()
                    )
                );
            }
        }

        private (BoogieProcedure proc, BoogieImplementation impl) BuildCorralChoice(WasmModule m)
        {
            var invs = GetGlobalInvariantExprs();
            string name = $"CorralChoice_{contractName}";
            var body = new BoogieStmtList();
            var locals = new List<BoogieVariable>();

            int N = m.Functions.Count;

            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("c", BoogieType.Int)));

            body.AddStatement(new BoogieHavocCmd(Id("c")));
            body.AddStatement(
                new BoogieAssumeCmd(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.AND,
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.LE,
                            IntLit(0),
                            Id("c")
                        ),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.LT,
                            Id("c"),
                            IntLit(N)
                        )
                    )
                )
            );

            BoogieStmtList? elseChain = null;

            for (int i = N - 1; i >= 0; i--)
            {
                var f = m.Functions[i];
                string fname = BoogieFuncName(f);

                var thenBlk = new BoogieStmtList();

                // Havoc/push des arguments éventuels
                EmitHavocPushArgs(f, thenBlk);

                thenBlk.AddStatement(new BoogieCallCmd(fname, new(), new()));

                if (f.ResultCount > 0)
                {
                    EnsurePopDiscardProc(f.ResultCount);
                    thenBlk.AddStatement(
                        new BoogieCallCmd($"popDiscard{f.ResultCount}", new(), new())
                    );
                }

                // Très important pour Corral : assert des invariants après chaque transition
                foreach (var inv in invs)
                    thenBlk.AddStatement(new BoogieAssertCmd(inv));

                var cond = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    Id("c"),
                    IntLit(i)
                );

                var ifcmd = new BoogieIfCmd(cond, thenBlk, elseChain);
                var wrap = new BoogieStmtList();
                wrap.AddStatement(ifcmd);
                elseChain = wrap;
            }

            if (elseChain != null)
                body.AppendStmtList(elseChain);

            var mods = BuildEntryModSet(m);

            var proc = new BoogieProcedure(
                name,
                new(),
                new(),
                attributes: InlineAttrsIfNotEntry(name),
                modSet: mods,
                pre: invs,
                post: invs
            );

            var impl = new BoogieImplementation(name, new(), new(), locals, body, attributes: null);
            return (proc, impl);
        }

        private List<BoogieExpr> GetGlobalInvariantExprs()
        {
            var invariants = new List<BoogieExpr>();

            if (moduleSpec is null)
                return invariants;

            SpecToBoogieTranslator translator = GetSpecTranslator();

            foreach (var invariant in moduleSpec.GlobalInvariants)
            {
                invariants.Add(translator.Translate(invariant));
            }

            return invariants;
        }

        private List<BoogieExpr> GetIntegerCastingInvariants(WasmModule m)
        {
            var invs = new List<BoogieExpr>();

            foreach (var g in m.Globals)
            {
                // seulement les globals entiers
                if (g.ValType == "i32" || g.ValType == "i64")
                {
                    string name = ResolveGlobalKey(g.Index, g.Name);

                    invs.Add(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.EQ,
                            Id(name),
                            new BoogieFunctionCall(
                                "real",
                                new List<BoogieExpr>
                                {
                                    new BoogieFunctionCall(
                                        "int",
                                        new List<BoogieExpr> { Id(name) }
                                    ),
                                }
                            )
                        )
                    );
                }
            }

            return invs;
        }

        private (BoogieProcedure proc, BoogieImplementation impl) BuildCorralEntry(WasmModule m)
        {
            var invs = GetGlobalInvariantExprs();
            string name = $"CorralEntry_{contractName}";
            var body = new BoogieStmtList();
            var locals = new List<BoogieVariable>();

            body.AddStatement(new BoogieCallCmd("InitRuntime", new(), new()));
            body.AddStatement(new BoogieCallCmd("initGlobals", new(), new()));

            var loopBody = new BoogieStmtList();
            loopBody.AddStatement(new BoogieCallCmd($"CorralChoice_{contractName}", new(), new()));

            body.AddStatement(
                new BoogieWhileCmd(new BoogieLiteralExpr(true), loopBody, new List<BoogieExpr>())
            );

            var mods = BuildEntryModSet(m);

            var proc = new BoogieProcedure(
                name,
                new(),
                new(),
                attributes: null,
                modSet: mods,
                pre: invs,
                post: invs
            );

            var impl = new BoogieImplementation(name, new(), new(), locals, body, attributes: null);
            return (proc, impl);
        }

        private (BoogieProcedure proc, BoogieImplementation impl) BuildBoogieEntry(WasmModule m)
        {
            var invs = GetGlobalInvariantExprs();
            string name = $"BoogieEntry_{contractName}";

            var body = new BoogieStmtList();
            var locals = new List<BoogieVariable>();

            int N = m.Functions.Count;

            // Ancien style: seulement c
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("c", BoogieType.Int)));

            // Même ordre que ton ancien code
            body.AddStatement(new BoogieCallCmd("initGlobals", new(), new()));
            body.AddStatement(new BoogieCallCmd("InitRuntime", new(), new()));

            var loopBody = new BoogieStmtList();

            loopBody.AddStatement(new BoogieHavocCmd(Id("c")));
            loopBody.AddStatement(
                new BoogieAssumeCmd(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.AND,
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.LE,
                            IntLit(0),
                            Id("c")
                        ),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.LT,
                            Id("c"),
                            IntLit(N)
                        )
                    )
                )
            );

            BoogieStmtList? elseChain = null;

            for (int i = N - 1; i >= 0; i--)
            {
                var f = m.Functions[i];
                string fname = BoogieFuncName(f);

                var thenBlk = new BoogieStmtList();

                EmitHavocPushArgs(f, thenBlk);

                thenBlk.AddStatement(new BoogieCallCmd(fname, new(), new()));

                if (f.ResultCount > 0)
                {
                    EnsurePopDiscardProc(f.ResultCount);
                    thenBlk.AddStatement(
                        new BoogieCallCmd($"popDiscard{f.ResultCount}", new(), new())
                    );
                }

                var cond = new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.EQ,
                    Id("c"),
                    IntLit(i)
                );

                var ifcmd = new BoogieIfCmd(cond, thenBlk, elseChain);
                var wrap = new BoogieStmtList();
                wrap.AddStatement(ifcmd);
                elseChain = wrap;
            }

            if (elseChain != null)
                loopBody.AppendStmtList(elseChain);

            var loopInvs = new List<BoogieExpr>();

            // invariant 0 <= $sp
            loopInvs.Add(
                new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.LE, IntLit(0), Id("$sp"))
            );

            // invariants métier
            loopInvs.AddRange(invs);
            // integer casting invariants
            //loopInvs.AddRange(GetIntegerCastingInvariants(m));

            body.AddStatement(new BoogieWhileCmd(new BoogieLiteralExpr(true), loopBody, loopInvs));

            var mods = BuildEntryModSet(m);

            var proc = new BoogieProcedure(
                name,
                new(),
                new(),
                attributes: null,
                modSet: mods,
                pre: invs,
                post: invs
            );

            var impl = new BoogieImplementation(name, new(), new(), locals, body, attributes: null);
            return (proc, impl);
        }

        private static void RemoveUnusedLabels(BoogieStmtList body)
        {
            if (body == null)
                return;

            // 1) Collect all referenced labels from goto targets
            var used = new HashSet<string>(StringComparer.Ordinal);
            CollectGotoTargets(body, used);

            // 2) Remove BoogieSkipCmd labels not referenced
            PruneDeadLabelSkips(body, used);
        }

        private static void CheckNoCycles(WasmNode node, HashSet<WasmNode> seen)
        {
            if (!seen.Add(node))
                throw new Exception($"Cycle detected in AST at node {node.GetType().Name}");

            switch (node)
            {
                case BlockNode b:
                    foreach (var x in b.Body)
                        CheckNoCycles(x, seen);
                    break;

                case LoopNode l:
                    foreach (var x in l.Body)
                        CheckNoCycles(x, seen);
                    break;

                case IfNode i:
                    CheckNoCycles(i.Condition, seen);
                    foreach (var x in i.ThenBody)
                        CheckNoCycles(x, seen);
                    if (i.ElseBody != null)
                        foreach (var x in i.ElseBody)
                            CheckNoCycles(x, seen);
                    break;

                case UnaryOpNode u:
                    if (u.Operand != null)
                        CheckNoCycles(u.Operand, seen);
                    break;

                case BinaryOpNode b2:
                    CheckNoCycles(b2.Left, seen);
                    CheckNoCycles(b2.Right, seen);
                    break;

                case LocalSetNode s:
                    if (s.Value != null)
                        CheckNoCycles(s.Value, seen);
                    break;

                case GlobalSetNode gs:
                    if (gs.Value != null)
                        CheckNoCycles(gs.Value, seen);
                    break;

                case CallNode c:
                    if (c.Args != null)
                        foreach (var a in c.Args)
                            CheckNoCycles(a, seen);
                    break;
            }

            seen.Remove(node);
        }

        private static void CollectGotoTargets(BoogieStmtList stmts, HashSet<string> used)
        {
            if (stmts == null)
                return;
            if (stmts.BigBlocks == null)
                return;

            foreach (var bb in stmts.BigBlocks)
            {
                if (bb?.SimpleCmds == null)
                    continue;

                foreach (var cmd in bb.SimpleCmds)
                {
                    switch (cmd)
                    {
                        case BoogieGotoCmd g:
                        {
                            if (g.LabelNames != null)
                            {
                                foreach (var lab in g.LabelNames)
                                {
                                    if (!string.IsNullOrWhiteSpace(lab))
                                        used.Add(lab.Trim());
                                }
                            }
                            break;
                        }

                        case BoogieIfCmd iff:
                        {
                            if (iff.ThenBody != null)
                                CollectGotoTargets(iff.ThenBody, used);
                            if (iff.ElseBody != null)
                                CollectGotoTargets(iff.ElseBody, used);
                            break;
                        }

                        case BoogieWhileCmd wh:
                        {
                            if (wh.Body != null)
                                CollectGotoTargets(wh.Body, used);
                            break;
                        }

                        default:
                            break;
                    }
                }
            }
        }

        private static void PruneDeadLabelSkips(BoogieStmtList stmts, HashSet<string> used)
        {
            if (stmts == null)
                return;
            if (stmts.BigBlocks == null)
                return;

            foreach (var bb in stmts.BigBlocks)
            {
                if (bb?.SimpleCmds == null)
                    continue;

                // rebuild list
                var kept = new List<BoogieCmd>(bb.SimpleCmds.Count);

                foreach (var cmd in bb.SimpleCmds)
                {
                    // recurse into structured commands first
                    if (cmd is BoogieIfCmd iff)
                    {
                        if (iff.ThenBody != null)
                            PruneDeadLabelSkips(iff.ThenBody, used);
                        if (iff.ElseBody != null)
                            PruneDeadLabelSkips(iff.ElseBody, used);

                        kept.Add(cmd);
                        continue;
                    }

                    if (cmd is BoogieWhileCmd wh)
                    {
                        if (wh.Body != null)
                            PruneDeadLabelSkips(wh.Body, used);

                        kept.Add(cmd);
                        continue;
                    }

                    // remove unused labels
                    if (cmd is BoogieSkipCmd sk)
                    {
                        // In your AST, a "label command" is a skip whose Label is non-empty.
                        if (!string.IsNullOrWhiteSpace(sk.Label))
                        {
                            var lab = NormalizeBoogieLabel(sk.Label);
                            if (used.Contains(lab))
                            {
                                // keep it, but normalize to consistent form (optional)
                                sk.Label = lab;
                                kept.Add(cmd);
                            }
                            // else: drop it
                            continue;
                        }

                        // empty skip => keep (harmless)
                        kept.Add(cmd);
                        continue;
                    }

                    // other commands: keep
                    kept.Add(cmd);
                }

                bb.SimpleCmds = kept;
            }
        }

        private static string NormalizeBoogieLabel(string raw)
        {
            // Your BoogieSkipCmd.ToString() adds ":" if missing,
            // but goto uses bare label names. So normalize to bare.
            var s = raw.Trim();
            if (s.EndsWith(":"))
                s = s.Substring(0, s.Length - 1).Trim();
            return s;
        }

        private readonly string contractName;
        private int labelCounter = 0;

        // map boogieName -> isMutable
        private readonly Dictionary<string, bool> boogieGlobalIsMutable = new(
            StringComparer.Ordinal
        );

        // (optionnel) boogieName -> init literal si connu (pour const)
        private readonly Dictionary<string, float> boogieGlobalInitValue = new(
            StringComparer.Ordinal
        );

        // Module Boogie en construction
        private BoogieProgram? program;

        private static bool ContainsReturn(WasmNode n)
        {
            switch (n)
            {
                case ReturnNode:
                    return true;

                case BlockNode b:
                    foreach (var x in b.Body)
                        if (ContainsReturn(x))
                            return true;
                    return false;

                case LoopNode l:
                    foreach (var x in l.Body)
                        if (ContainsReturn(x))
                            return true;
                    return false;

                case IfNode iff:
                    if (ContainsReturn(iff.Condition))
                        return true;
                    foreach (var x in iff.ThenBody)
                        if (ContainsReturn(x))
                            return true;
                    if (iff.ElseBody != null)
                        foreach (var x in iff.ElseBody)
                            if (ContainsReturn(x))
                                return true;
                    return false;

                case UnaryOpNode u:
                    return u.Operand != null && ContainsReturn(u.Operand);

                case BinaryOpNode b2:
                    return ContainsReturn(b2.Left) || ContainsReturn(b2.Right);

                case LocalSetNode ls:
                    return ls.Value != null && ContainsReturn(ls.Value);

                case GlobalSetNode gs:
                    return gs.Value != null && ContainsReturn(gs.Value);

                case CallNode c:
                    return c.Args != null && c.Args.Any(ContainsReturn);

                case SelectNode s:
                    return ContainsReturn(s.V1) || ContainsReturn(s.V2) || ContainsReturn(s.Cond);

                case MemoryOpNode m:
                    return (m.Address != null && ContainsReturn(m.Address))
                        || (m.Value != null && ContainsReturn(m.Value));

                default:
                    return false;
            }
        }

        private static bool ContainsReturn(List<WasmNode> xs)
        {
            foreach (var x in xs)
                if (ContainsReturn(x))
                    return true;
            return false;
        }

        // Générateurs uniques
        private readonly HashSet<int> popArgsMade = new();
        private readonly HashSet<int> popDiscardMade = new();

        private BoogieExpr ExtractI32Condition(BoogieStmtList body, BoogieExpr value)
        {
            // WebAssembly conditions must be i32.
            body.AddStatement(new BoogieAssertCmd(IsCtor(value, "I32")));

            var raw = Field(value, "value_i32");

            // Preserve our canonical i32 representation invariant.
            body.AddStatement(new BoogieAssertCmd(IsU32(raw)));

            // WebAssembly truth semantics:
            // 0     -> false
            // != 0  -> true
            return new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.NEQ, raw, BigIntLit("0"));
        }

        private BoogieExpr? BuildTypedGlobalInit(WasmGlobal g)
        {
            if (string.IsNullOrWhiteSpace(g.InitConst))
                return null;

            var s = g.InitConst.Trim();

            var parts = s.Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );

            if (parts.Length >= 2)
                s = parts[^1];

            switch (g.ValType)
            {
                case "i32":
                {
                    if (System.Numerics.BigInteger.TryParse(s, out var v))
                        return I32(ToU32(new BoogieLiteralExpr(v)));

                    break;
                }

                case "i64":
                {
                    if (System.Numerics.BigInteger.TryParse(s, out var v))
                        return I64(ToU64(new BoogieLiteralExpr(v)));

                    break;
                }

                case "f32":
                {
                    if (
                        float.TryParse(
                            s,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var v
                        )
                    )
                    {
                        return F32(ToF32(new BoogieLiteralExpr(new Pfloat(v))));
                    }

                    break;
                }

                case "f64":
                {
                    if (
                        double.TryParse(
                            s,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var v
                        )
                    )
                    {
                        return F64(ToF64(new BoogieLiteralExpr(new Pfloat(s))));
                    }

                    break;
                }
            }

            return null;
        }

        // État par fonction
        private List<BoogieIdentifierExpr>? currentLocalMap; // arg1..argN, loc1..locM
        private List<string> functionIndexToBoogieName = new();
        private WasmFunction? currentFunction;
        private HashSet<string>? neededLoopStartLabels;
        private HashSet<string>? neededBlockEndLabels;
        private readonly Stack<LabelContext> labelStack = new();
        private string? functionExitLabel;

        private BoogieIdentifierExpr ResolveLocalBoogieId(int? index, string? name, string op)
        {
            int idx = ResolveLocalIndex(index, name);

            if (currentLocalMap == null || idx < 0 || idx >= currentLocalMap.Count)
                throw new Exception(
                    $"{op} out of range: idx={idx}, localMap.Count={currentLocalMap?.Count ?? -1}, "
                        + $"func={currentFunction?.Name}, params={currentFunction?.ParamCount}, locals={currentFunction?.LocalCount}, "
                        + $"rawIndex={index}, rawName={name}"
                );

            return currentLocalMap[idx];
        }

        // Globals created lazily + modifies tracking
        private readonly Dictionary<string, string> globalNameMap = new(StringComparer.Ordinal);
        private readonly HashSet<string> declaredBoogieGlobals = new(StringComparer.Ordinal);
        private HashSet<string>? currentModifiedGlobals;
        private WasmModule? currentModule;

        private sealed class LabelContext
        {
            // WAT label WITHOUT '$' (e.g., "$L" => "L"), null if unnamed
            public string? WatLabel;

            // True for loop, false for block/if
            public bool IsLoop;

            // Boogie labels
            // - For loops: StartLabel is the "continue" target.
            // - For blocks: StartLabel is unused (can remain null).
            public string? StartLabel;

            // EndLabel is the "break" target for blocks and loops.
            public string EndLabel = "";

            // Precomputed usage flags (set by PrecomputeLabelNeeds)
            public bool NeedStartLabel; // only meaningful for loops
            public bool NeedEndLabel; // block end OR loop end
        }

        private LabelContext ResolveTargetContext(string labOrDepth)
        {
            // Numeric depth: "0", "1", ...
            if (AllDigits(labOrDepth))
            {
                int depth = int.Parse(labOrDepth);
                if (depth < 0 || depth >= labelStack.Count)
                    throw new Exception($"br depth out of range: {labOrDepth}");

                // labelStack.ToArray() is top -> bottom, depth 0 = innermost
                return labelStack.ToArray()[depth];
            }

            // Named label: "$L" or "L"
            var norm = NormalizeLabel(labOrDepth);

            foreach (var ctx in labelStack) // top -> bottom
                if (ctx.WatLabel == norm)
                    return ctx;

            throw new Exception($"Unknown label target: {labOrDepth}");
        }

        private string ResolveBranchTargetLabel(string labOrDepth)
        {
            var ctx = ResolveTargetContext(labOrDepth);

            // WASM semantics:
            // - br to loop label => continue => jump to START
            // - br to block/if   => break    => jump to END
            if (ctx.IsLoop)
                return ctx.StartLabel ?? ctx.EndLabel;
            else
                return ctx.EndLabel;
        }

        public WasmAstToBoogie(string contractName) =>
            this.contractName = SanitizeIdentifier(contractName);

        // ============================================================
        // Helpers
        // ============================================================
        // ============================
        // Globals init (module-level)
        // ============================

        private void DeclareAllGlobals(WasmModule wasmModule)
        {
            foreach (var g in wasmModule.Globals)
            {
                var key = ResolveGlobalKey(g.Index, g.Name);
                EnsureGlobalDecl(g, key);
            }
        }

        private string EnsureGlobalDecl(WasmGlobal g, string watKey)
        {
            // mapping stable
            string key = watKey.StartsWith("$", StringComparison.Ordinal) ? watKey : "$" + watKey;

            if (!globalNameMap.TryGetValue(key, out var boogieName))
            {
                boogieName = SanitizeGlobalName(key);
                globalNameMap[key] = boogieName;
            }

            // mémorise mutabilité
            boogieGlobalIsMutable[boogieName] = g.IsMutable;

            if (program != null && !declaredBoogieGlobals.Contains(boogieName))
            {
                if (g.IsMutable)
                {
                    program.Declarations.Add(
                        new BoogieGlobalVariable(
                            new BoogieTypedIdent(boogieName, WasmValueBoogieType())
                        )
                    );
                }
                else
                {
                    program.Declarations.Add(
                        new BoogieConstant(new BoogieTypedIdent(boogieName, WasmValueBoogieType()))
                    );

                    var init = BuildTypedGlobalInit(g);

                    if (init != null)
                    {
                        program.Declarations.Add(
                            new BoogieAxiom(
                                new BoogieBinaryOperation(
                                    BoogieBinaryOperation.Opcode.EQ,
                                    new BoogieIdentifierExpr(boogieName),
                                    init
                                )
                            )
                        );
                    }
                }
                declaredBoogieGlobals.Add(boogieName);
            }

            return boogieName;
        }

        // Parse Binaryen wrapper init const string -> float
        // Accepts: "5", "-3", "12.5", maybe "i32.const 5" (rare), etc.
        private bool TryParseInitConst(string? init, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(init))
                return false;

            var s = init.Trim();

            // Sometimes wrappers return something like "i32.const 5"
            // Keep only the last token if it's numeric.
            var parts = s.Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );
            if (parts.Length >= 2)
                s = parts[^1];

            return float.TryParse(
                s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value
            );
        }

        private (BoogieProcedure proc, BoogieImplementation impl) BuildInitGlobals(
            WasmModule wasmModule
        )
        {
            var body = new BoogieStmtList();
            var locals = new List<BoogieVariable>();

            var mods = new List<BoogieGlobalVariable>();
            var post = new List<BoogieExpr>();


            bool memEnabled =
                PreludeOptions.Sections.HasFlag(PreludeSection.Memory)
                && PreludeOptions.EnableMemory;



            if (memEnabled)
            {
                mods.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent("$mem_pages", BoogieType.Int))
                );

                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("$mem_pages"),
                        new BoogieLiteralExpr(wasmModule.InitialMemoryPages)
                    )
                );

                post.Add(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        new BoogieIdentifierExpr("$mem_pages"),
                        new BoogieLiteralExpr(wasmModule.InitialMemoryPages)
                    )
                );
            }

            bool initTableEnabled =
    PreludeOptions.Sections.HasFlag(
        PreludeSection.Table
    )
    && wasmModule.HasTable;

if (initTableEnabled)
{
    var tableType =
        new BoogieMapType(
            BoogieType.Int,
            WasmValueBoogieType()
        );

    mods.Add(
        new BoogieGlobalVariable(
            new BoogieTypedIdent(
                "$table",
                tableType
            )
        )
    );

    mods.Add(
        new BoogieGlobalVariable(
            new BoogieTypedIdent(
                "$table_size",
                BoogieType.Int
            )
        )
    );

    mods.Add(
    new BoogieGlobalVariable(
        new BoogieTypedIdent(
            "$table_max",
            BoogieType.Int
        )
    )
);

    body.AddStatement(
        new BoogieAssignCmd(
            new BoogieIdentifierExpr("$table_size"),
            new BoogieLiteralExpr(
                wasmModule.InitialTableSize
            )
        )
    );

 int initialTableMaximum =
    wasmModule.MaximumTableSize
    ?? -1;

body.AddStatement(
    new BoogieAssignCmd(
        new BoogieIdentifierExpr("$table_max"),
        new BoogieLiteralExpr(
            initialTableMaximum
        )
    )
);   

    body.AddStatement(
        new BoogieHavocCmd(
            new BoogieIdentifierExpr("$table")
        )
    );

    BoogieExpr initialReference =
        wasmModule.TableReferenceKind
        == WasmReferenceKind.Func
            ? NullFuncRefValue()
            : NullExternRefValue();

    var tableIndex =
        new BoogieIdentifierExpr("table_init_i");

    var tableAtIndex =
        new BoogieMapSelect(
            new BoogieIdentifierExpr("$table"),
            tableIndex
        );

    var lowerBound =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.LE,
            new BoogieLiteralExpr(0),
            tableIndex
        );

    var upperBound =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.LT,
            tableIndex,
            new BoogieLiteralExpr(
                wasmModule.InitialTableSize
            )
        );

    var insideInitialTable =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.AND,
            lowerBound,
            upperBound
        );

    var tableCellIsNull =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.EQ,
            tableAtIndex,
            initialReference
        );

    var initializedCell =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.IMP,
            insideInitialTable,
            tableCellIsNull
        );

    var initializedTable =
        new BoogieQuantifiedExpr(
            isForall: true,
            qvars: new List<BoogieIdentifierExpr>
            {
                tableIndex,
            },
            qvarTypes: new List<BoogieType>
            {
                BoogieType.Int,
            },
            bodyExpr: initializedCell,
            trigger: new List<BoogieExpr>
            {
                tableAtIndex,
            }
        );

    body.AddStatement(
        new BoogieAssumeCmd(
            initializedTable
        )
    );

    post.Add(
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.EQ,
            new BoogieIdentifierExpr("$table_size"),
            new BoogieLiteralExpr(
                wasmModule.InitialTableSize
            )
        )
    );

post.Add(
    new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.EQ,
        new BoogieIdentifierExpr("$table_max"),
        new BoogieLiteralExpr(
            initialTableMaximum
        )
    )
);

    // ========================================================
// Appliquer les segments elem actifs.
// La dernière écriture gagne si plusieurs segments
// initialisent la même case.
// ========================================================

var initializedElementCells =
    new Dictionary<int, int>();

foreach (
    var segment
    in wasmModule.ElementSegments
)
{
    if (!segment.IsActive)
        continue;

    if (segment.TableIndex != 0)
    {
        throw new NotSupportedException(
            "Only table 0 is supported for element segments."
        );
    }

    for (
        int elementIndex = 0;
        elementIndex < segment.FunctionTargets.Count;
        elementIndex++
    )
    {
        int tableCell =
            segment.Offset
            + elementIndex;

        if (
            tableCell < 0
            || tableCell >= wasmModule.InitialTableSize
        )
        {
            throw new InvalidOperationException(
                "Active element segment is outside "
                + "the initial table bounds: "
                + $"cell={tableCell}, "
                + $"tableSize={wasmModule.InitialTableSize}."
            );
        }

        string functionTarget =
            segment.FunctionTargets[
                elementIndex
            ];

        int functionIndex =
            ResolveFunctionReferenceIndex(
                functionTarget
            );

        initializedElementCells[tableCell] =
            functionIndex;
    }
}

foreach (
    var elementCellEntry
    in initializedElementCells.OrderBy(
        entry => entry.Key
    )
)
{
    int tableCell =
        elementCellEntry.Key;

    int functionIndex =
        elementCellEntry.Value;

    var tableCellExpression =
        new BoogieMapSelect(
            new BoogieIdentifierExpr("$table"),
            new BoogieLiteralExpr(
                tableCell
            )
        );

    var functionReference =
        FuncRefValue(
            new BoogieLiteralExpr(
                functionIndex
            )
        );

    body.AddStatement(
        new BoogieAssignCmd(
            tableCellExpression,
            functionReference
        )
    );

    // Postcondition de la case initialisée.
    post.Add(
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.EQ,
            new BoogieMapSelect(
                new BoogieIdentifierExpr("$table"),
                new BoogieLiteralExpr(
                    tableCell
                )
            ),
            FuncRefValue(
                new BoogieLiteralExpr(
                    functionIndex
                )
            )
        )
    );
}

// Les cases initiales qui ne sont couvertes par aucun
// segment actif restent NullFuncRef().
{
    var remainingIndex =
        new BoogieIdentifierExpr(
            "table_remaining_i"
        );

    var remainingCell =
        new BoogieMapSelect(
            new BoogieIdentifierExpr("$table"),
            remainingIndex
        );

    BoogieExpr remainingGuard =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.AND,

            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.LE,
                new BoogieLiteralExpr(0),
                remainingIndex
            ),

            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.LT,
                remainingIndex,
                new BoogieLiteralExpr(
                    wasmModule.InitialTableSize
                )
            )
        );

    // Exclure les cases initialisées par elem.
    foreach (
        int initializedIndex
        in initializedElementCells.Keys.OrderBy(
            value => value
        )
    )
    {
        remainingGuard =
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.AND,
                remainingGuard,

                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.NEQ,
                    remainingIndex,
                    new BoogieLiteralExpr(
                        initializedIndex
                    )
                )
            );
    }

    var remainingCellIsNull =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.EQ,
            remainingCell,
            initialReference
        );

    var remainingTableInitialized =
        new BoogieQuantifiedExpr(
            isForall: true,

            qvars:
                new List<BoogieIdentifierExpr>
                {
                    remainingIndex,
                },

            qvarTypes:
                new List<BoogieType>
                {
                    BoogieType.Int,
                },

            bodyExpr:
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.IMP,
                    remainingGuard,
                    remainingCellIsNull
                ),

            trigger:
                new List<BoogieExpr>
                {
                    remainingCell,
                }
        );

    post.Add(
        remainingTableInitialized
    );
}
}

            foreach (var g in wasmModule.Globals)
            {
                var key = ResolveGlobalKey(g.Index, g.Name);
                string bname = EnsureGlobalDecl(g, key);

                // uniquement mutables
                if (!g.IsMutable)
                    continue;

                mods.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent(bname, WasmValueBoogieType()))
                );

                var init = BuildTypedGlobalInit(g);

                if (init != null)
                {
                    body.AddStatement(new BoogieAssignCmd(new BoogieIdentifierExpr(bname), init));

                    post.Add(
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.EQ,
                            new BoogieIdentifierExpr(bname),
                            init
                        )
                    );
                }
                else
                {
                    body.AddStatement(
                        new BoogieAssignCmd(new BoogieIdentifierExpr(bname), UndefValue())
                    );
                }
            }

            var proc = new BoogieProcedure(
                "initGlobals",
                new List<BoogieVariable>(),
                new List<BoogieVariable>(),
                attributes: InlineAttrsIfNotEntry("initGlobals"),
                modSet: mods,
                pre: new List<BoogieExpr>(),
                post: post // ✅ ensures
            );

            var impl = new BoogieImplementation(
                "initGlobals",
                new List<BoogieVariable>(),
                new List<BoogieVariable>(),
                locals,
                body,
                attributes: null
            );

            return (proc, impl);
        }

        private static bool AllDigits(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            for (int i = 0; i < s.Length; i++)
                if (s[i] < '0' || s[i] > '9')
                    return false;
            return true;
        }

        private static string NormalizeLabel(string raw) =>
            string.IsNullOrEmpty(raw) ? raw : (raw[0] == '$' ? raw[1..] : raw);

        private static string MapCalleeName(string target)
        {
            if (string.IsNullOrEmpty(target))
                return target;

            string name = target[0] == '$' ? target[1..] : target;

            if (AllDigits(name))
                return "func_" + name;

            name = Regex.Replace(name, @"[^A-Za-z0-9_]", "_");

            if (!char.IsLetter(name[0]) && name[0] != '_')
                name = "_" + name;

            return name;
        }

        private static string SanitizeFunctionName(string? watName, string contractName)
        {
            if (!string.IsNullOrEmpty(watName))
            {
                var n = watName![0] == '$' ? watName.Substring(1) : watName;
                if (int.TryParse(n, out _))
                    return $"func_{n}";
                n = Regex.Replace(n, @"[^A-Za-z0-9_]", "_");
                if (!char.IsLetter(n[0]) && n[0] != '_')
                    n = "_" + n;
                return n;
            }
            return $"func_{contractName}";
        }

        private string GenerateLabel(string baseName) => $"{baseName}_{++labelCounter}";

        private static string SanitizeGlobalName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "g";
            var n = raw[0] == '$' ? raw.Substring(1) : raw;
            n = Regex.Replace(n, @"[^A-Za-z0-9_]", "_");
            if (!char.IsLetter(n[0]) && n[0] != '_')
                n = "_" + n;
            return n;
        }

        private string ResolveSpecIdentifier(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            return name switch
            {
                "stack" => "$stack",
                "sp" => "$sp",

                "tmp1" => "$tmp1",
                "tmp2" => "$tmp2",
                "tmp3" => "$tmp3",

                "mem" => "$mem",
                "mem_pages" => "$mem_pages",
                "result" => "result",
                _ => EnsureGlobalVar(name),
            };
        }

        private string ResolveGlobalKey(int? index, string? name)
        {
            if (!string.IsNullOrEmpty(name))
                return name!;
            if (index.HasValue)
                return index.Value.ToString();
            throw new NotSupportedException("Unknown global index/name");
        }

        private string ResolveCalleeName(string target)
        {
            if (AllDigits(target))
            {
                int idx = int.Parse(target);

                if (idx < 0 || idx >= functionIndexToBoogieName.Count)
                    throw new Exception(
                        $"call index out of range: {idx}, function index space size={functionIndexToBoogieName.Count}"
                    );

                return functionIndexToBoogieName[idx];
            }

            return MapCalleeName(target);
        }

        private string EnsureGlobalVar(string watNameOrIndex)
        {
            string key = watNameOrIndex.StartsWith("$", StringComparison.Ordinal)
                ? watNameOrIndex
                : "$" + watNameOrIndex;

            if (!globalNameMap.TryGetValue(key, out var boogieName))
            {
                boogieName = SanitizeGlobalName(key);
                globalNameMap[key] = boogieName;
            }

            return boogieName;
        }

        // ============================================================
        // Public entry
        // ============================================================

        public BoogieProgram Convert(WasmModule wasmModule)
        {
            currentModule = wasmModule;
            var p = new BoogieProgram();
            program = p;
            moduleSpec = wasmModule.Spec;

            specTranslator = moduleSpec is null
                ? null
                : new SpecToBoogieTranslator(ResolveSpecIdentifier);

            if (PreludeOptions.AutoDetect)
                if (PreludeOptions.AutoDetect)
                {
                    var (sections, enableMemory) = PreludeAutoDetector.ComputeSections(wasmModule);

                    PreludeOptions = new PreludeOptions
                    {
                        // on override dynamiquement
                        Sections = sections,
                        EnableMemory = enableMemory,

                        // on conserve le reste des flags (math options)
                        EnableSqrtAxioms = PreludeOptions.EnableSqrtAxioms,
                        EnableNearestAxioms = PreludeOptions.EnableNearestAxioms,
                        EnableFloorAxioms = PreludeOptions.EnableFloorAxioms,
                        DefineAbsWithITE = PreludeOptions.DefineAbsWithITE,

                        // on garde AutoDetect (si tu l’as ajouté)
                        AutoDetect = PreludeOptions.AutoDetect,
                    };
                }
            AddIntegerWidthAxioms(p);
            AddPrelude(p);

            DeclareAllGlobals(wasmModule);
            var (igProc, igImpl) = BuildInitGlobals(wasmModule);
            p.Declarations.Add(igProc);
            p.Declarations.Add(igImpl);
functionIndexToBoogieName.Clear();

// Étape 1 : préparer tous les noms selon l’espace
// d’indices WebAssembly complet.
foreach (
    var functionReference
    in wasmModule.FunctionIndexSpace
)
{
    string boogieName;

    if (
        functionReference.IsImport
        && functionReference.Import != null
    )
    {
        boogieName =
            SanitizeFunctionName(
                functionReference.Import.InternalName,
                contractName
            );
    }
    else if (
        functionReference.Function != null
    )
    {
        boogieName =
            SanitizeFunctionName(
                functionReference.Function.Name,
                contractName
            );
    }
    else
    {
        throw new InvalidOperationException(
            $"Function index {functionReference.Index} "
            + "has neither an import nor a defined function."
        );
    }

    functionIndexToBoogieName.Add(
        boogieName
    );
}

// Étape 2 : traduire chaque import exactement une fois.
foreach (
    var import
    in wasmModule.Imports.Where(
        i => i.Kind == WasmImportKind.Func
    )
)
{
    var (proc, impl) =
        TranslateImportedFunction(import);

    p.Declarations.Add(proc);
    p.Declarations.Add(impl);
}

// Étape 3 : traduire chaque fonction exactement une fois.
foreach (
    var func
    in wasmModule.Functions
)
{
    var (proc, impl) =
        TranslateFunction(func);

    p.Declarations.Add(proc);
    p.Declarations.Add(impl);
}

// Construction de l’entrée, une seule fois.
var (beP, beI) =
    BuildBoogieEntry(wasmModule);

p.Declarations.Add(beP);
p.Declarations.Add(beI);
            return p;
        }

        // ============================================================
        // Function translation
        // ============================================================
        private (BoogieProcedure, BoogieImplementation) TranslateImportedFunction(WasmImport import)
        {
            string name = SanitizeFunctionName(import.InternalName, contractName);

            var locals = new List<BoogieVariable>();
            var body = new BoogieStmtList();

            int n = import.ParamCount;
            int r = import.ResultCount;

            if (n > 0)
            {
                EnsurePopDiscardProc(n);
                body.AddStatement(new BoogieCallCmd($"popDiscard{n}", new(), new()));
            }

            for (int i = 0; i < r; i++)
            {
                if (i >= import.ResultTypes.Count)
                {
                    throw new InvalidOperationException(
                        $"Imported function {name}: missing result type for result {i}"
                    );
                }

                var resultType = import.ResultTypes[i];

                var nondetValue = MakeNondetWasmValue(resultType);

                body.AddStatement(
                    new BoogieCallCmd("push", new List<BoogieExpr> { nondetValue }, new())
                );
            }

            var mods = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$stack", WasmStackBoogieType())),
            };

            var proc = new BoogieProcedure(
                name,
                new(),
                new(),
                attributes: InlineAttrsIfNotEntry(name),
                modSet: mods,
                pre: new List<BoogieExpr>(),
                post: new List<BoogieExpr>()
            );

            var impl = new BoogieImplementation(name, new(), new(), locals, body, attributes: null);

            return (proc, impl);
        }

        private (BoogieProcedure, BoogieImplementation) TranslateFunction(WasmFunction func)
        {
            var inParams = new List<BoogieVariable>();
            var outParams = new List<BoogieVariable>();
            var locals = new List<BoogieVariable>();
            var body = new BoogieStmtList();

            currentFunction = func;
            functionExitLabel = null;
            PrecomputeLabelNeeds(func);

            currentModifiedGlobals = new HashSet<string>(StringComparer.Ordinal);

            int n = func.ParamCount;
            int m = func.LocalCount;
            int r = Math.Max(0, func.ResultCount);

            var indexToId = new List<BoogieIdentifierExpr>(n + m);

            for (int i = 1; i <= n; i++)
            {
                var name = $"arg{i}";
                locals.Add(
                    new BoogieLocalVariable(new BoogieTypedIdent(name, WasmValueBoogieType()))
                );
                indexToId.Add(new BoogieIdentifierExpr(name));
            }
            for (int i = 1; i <= m; i++)
            {
                var name = $"loc{i}";
                locals.Add(
                    new BoogieLocalVariable(new BoogieTypedIdent(name, WasmValueBoogieType()))
                );
                indexToId.Add(new BoogieIdentifierExpr(name));
            }

            // Helper locals used by translation
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("entry_sp", BoogieType.Int)));

            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("idx", BoogieType.Int)));
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("load_i", BoogieType.Int)));
            locals.Add(new BoogieLocalVariable(new BoogieTypedIdent("store_i", BoogieType.Int)));

            currentLocalMap = indexToId;

            // Prologue
            body.AddStatement(
                new BoogieAssignCmd(
                    new BoogieIdentifierExpr("entry_sp"),
                    new BoogieIdentifierExpr("$sp")
                )
            );

            if (n > 0)
            {
                EnsurePopArgsProc(n);
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
                    new BoogieCallCmd($"popArgs{n}", new(), indexToId.Take(n).ToList())
                );
            }

            if (func.LocalTypes.Count != func.LocalCount)
            {
                throw new InvalidOperationException(
                    $"Function {func.Name}: expected {func.LocalCount} local types, "
                        + $"but found {func.LocalTypes.Count}."
                );
            }

            for (int localIndex = 0; localIndex < func.LocalCount; localIndex++)
            {
                body.AddStatement(
                    new BoogieAssignCmd(
                        indexToId[func.ParamCount + localIndex],
                        ZeroValueForType(func.LocalTypes[localIndex])
                    )
                );
            }
            foreach (var node in func.Body)
                CheckNoCycles(node, new HashSet<WasmNode>());
            foreach (var node in func.Body)
                TranslateNode(node, body);

            if (!string.IsNullOrEmpty(functionExitLabel))
            {
                body.AddStatement(new BoogieSkipCmd(functionExitLabel + ":"));
                functionExitLabel = null;
            }

            var expected = new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.ADD,
                new BoogieIdentifierExpr("entry_sp"),
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.SUB,
                    new BoogieLiteralExpr(r),
                    new BoogieLiteralExpr(n)
                )
            );
            string funcName = SanitizeFunctionName(func.Name, contractName);

            var mods = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", WasmValueBoogieType())),
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$stack", WasmStackBoogieType())),
            };

            bool tableEnabled = PreludeOptions.Sections.HasFlag(PreludeSection.Table);

            if (tableEnabled)
            {
                mods.Add(
                    new BoogieGlobalVariable(
                        new BoogieTypedIdent(
                            "$table",
                            new BoogieMapType(BoogieType.Int, WasmValueBoogieType())
                        )
                    )
                );

                mods.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent("$table_size", BoogieType.Int))
                );
            }

            bool memEnabled =
                PreludeOptions.Sections.HasFlag(PreludeSection.Memory)
                && PreludeOptions.EnableMemory;

            if (memEnabled)
            {
                mods.Add(
                    new BoogieGlobalVariable(
                        new BoogieTypedIdent(
                            "$mem",
                            new BoogieMapType(BoogieType.Int, new BoogieCtorType("bv8"))
                        )
                    )
                );

                mods.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent("$mem_pages", BoogieType.Int))
                );
            }

            if (currentModifiedGlobals != null)
                foreach (var g in currentModifiedGlobals)
                    mods.Add(
                        new BoogieGlobalVariable(new BoogieTypedIdent(g, WasmValueBoogieType()))
                    );

            var pre = new List<BoogieExpr>();
            var post = new List<BoogieExpr>();

            if (moduleSpec != null && func.Name != null)
            {
                SpecToBoogieTranslator translator = GetSpecTranslator();

                if (moduleSpec.RequiresByFunc.TryGetValue(func.Name, out var requires))
                {
                    foreach (var requirement in requires)
                    {
                        pre.Add(translator.Translate(requirement));
                    }
                }

                if (moduleSpec.EnsuresByFunc.TryGetValue(func.Name, out var ensures))
                {
                    foreach (var ensure in ensures)
                    {
                        post.Add(translator.Translate(ensure));
                    }
                }
            }

            var proc = new BoogieProcedure(
                funcName,
                inParams,
                outParams,
                attributes: InlineAttrsIfNotEntry(funcName),
                modSet: mods,
                pre: pre,
                post: post
            );
            RemoveUnusedLabels(body);
            var impl = new BoogieImplementation(
                proc.Name,
                inParams,
                outParams,
                locals,
                body,
                attributes: null //InlineAttrsIfNotEntry(proc.Name)
            );

            // reset état
            currentLocalMap = null;
            currentFunction = null;
            neededLoopStartLabels = null;
            neededBlockEndLabels = null;
            labelStack.Clear();
            currentModifiedGlobals = null;

            return (proc, impl);
        }

        // ============================================================
        // Label pre-scan
        // ============================================================

        private void PrecomputeLabelNeeds(WasmFunction func)
        {
            neededLoopStartLabels = new HashSet<string>(StringComparer.Ordinal);
            neededBlockEndLabels = new HashSet<string>(StringComparer.Ordinal);

            // We also need to handle DEPTH branches, which can target unnamed constructs.
            // So we keep a parallel scope stack of "isLoop" and "watLabel" (may be null).
            var scope = new Stack<(string? watLabel, bool isLoop)>();

            void MarkTargetByDepth(string depthStr)
            {
                if (!AllDigits(depthStr))
                    return;

                int depth = int.Parse(depthStr);
                if (depth < 0 || depth >= scope.Count)
                    return;

                // scope.ToArray(): top -> bottom
                var target = scope.ToArray()[depth];

                if (target.isLoop)
                {
                    // continue target (loop start)
                    if (target.watLabel != null)
                        neededLoopStartLabels!.Add(target.watLabel);
                    // For unnamed loops, we can’t store a name; we’ll just emit labels always.
                }
                else
                {
                    // break target (block/if end)
                    if (target.watLabel != null)
                        neededBlockEndLabels!.Add(target.watLabel);
                    // For unnamed blocks, we’ll emit end label always.
                }
            }

            void MarkTargetByName(string rawLabel)
            {
                var target = NormalizeLabel(rawLabel);
                foreach (var (lab, isLoop) in scope)
                {
                    if (lab == target)
                    {
                        if (isLoop)
                            neededLoopStartLabels!.Add(lab);
                        else
                            neededBlockEndLabels!.Add(lab);
                        break;
                    }
                }
            }

            void Walk(WasmNode n)
            {
                switch (n)
                {
                    case BlockNode blk:
                    {
                        string? wat =
                            (
                                blk.Label != null
                                && blk.Label.StartsWith("$", StringComparison.Ordinal)
                            )
                                ? blk.Label.Substring(1)
                                : null;

                        scope.Push((wat, false));
                        foreach (var m in blk.Body)
                            Walk(m);
                        scope.Pop();
                        break;
                    }

                    case LoopNode lp:
                    {
                        string? wat =
                            (lp.Label != null && lp.Label.StartsWith("$", StringComparison.Ordinal))
                                ? lp.Label.Substring(1)
                                : null;

                        scope.Push((wat, true));
                        foreach (var m in lp.Body)
                            Walk(m);
                        scope.Pop();
                        break;
                    }

                    case IfNode iff:
                        Walk(iff.Condition);
                        // "if" is also a structured construct (break targets end)
                        scope.Push((null, false)); // unnamed if-scope for depth branches
                        foreach (var m in iff.ThenBody)
                            Walk(m);
                        if (iff.ElseBody != null)
                            foreach (var m in iff.ElseBody)
                                Walk(m);
                        scope.Pop();
                        break;

                    case BrNode br:
                        if (AllDigits(br.Label))
                            MarkTargetByDepth(br.Label);
                        else
                            MarkTargetByName(br.Label);
                        break;

                    case BrIfNode bri:
                        Walk(bri.Condition);
                        if (AllDigits(bri.Label))
                            MarkTargetByDepth(bri.Label);
                        else
                            MarkTargetByName(bri.Label);
                        break;

                    case BrTableNode bt:
                    {
                        if (bt.Selector != null)
                            Walk(bt.Selector);

                        foreach (var t in bt.Targets)
                        {
                            if (AllDigits(t))
                                MarkTargetByDepth(t);
                            else
                                MarkTargetByName(t);
                        }

                        if (AllDigits(bt.Default))
                            MarkTargetByDepth(bt.Default);
                        else
                            MarkTargetByName(bt.Default);

                        break;
                    }

                    case BinaryOpNode b:
                        Walk(b.Left);
                        Walk(b.Right);
                        break;

                    case UnaryOpNode u:
                        if (u.Operand != null)
                            Walk(u.Operand);
                        break;

                    case LocalSetNode ls:
                        if (ls.Value != null)
                            Walk(ls.Value);
                        break;

                    case GlobalSetNode gs:
                        if (gs.Value != null)
                            Walk(gs.Value);
                        break;

                    case CallNode c:
                        if (c.Args != null)
                            foreach (var a in c.Args)
                                Walk(a);
                        break;

                    case SelectNode s:
                        Walk(s.V1);
                        Walk(s.V2);
                        Walk(s.Cond);
                        break;

                    case MemoryOpNode m:
                        if (m.Address != null)
                            Walk(m.Address);
                        if (m.Value != null)
                            Walk(m.Value);
                        break;

                    default:
                        break;
                }
            }

            // Initialize scope with the implicit function “outermost” frame? Optional.
            // Usually not needed unless you allow br to escape function body; you already use func_exit for that.

            foreach (var n in func.Body)
                Walk(n);
        }

        // ============================================================
        // Node translation
        // ============================================================

private int ResolveFunctionReferenceIndex(
    string target
)
{
    if (string.IsNullOrWhiteSpace(target))
    {
        throw new InvalidOperationException(
            "ref.func has an empty function target."
        );
    }

    // Forme numérique : (ref.func 3)
    if (int.TryParse(target, out int numericIndex))
    {
        if (numericIndex < 0)
        {
            throw new InvalidOperationException(
                $"ref.func has a negative function index: {target}"
            );
        }

        return numericIndex;
    }

    // Forme nommée : (ref.func $foo)
    if (
        currentModule != null
        && currentModule.FunctionIndexByName.TryGetValue(
            target,
            out int namedIndex
        )
    )
    {
        return namedIndex;
    }

    throw new InvalidOperationException(
        $"Unable to resolve function reference: {target}"
    );
}

        private int ResolveLocalIndex(int? index, string? name)
        {
            if (index.HasValue)
                return index.Value;

            if (!string.IsNullOrEmpty(name))
            {
                if (
                    currentFunction != null
                    && currentFunction.LocalIndexByName.TryGetValue(name, out var idx)
                )
                    return idx;

                if (name[0] == '$' && int.TryParse(name.AsSpan(1), out var autoIdx))
                    return autoIdx;
            }

            throw new NotSupportedException($"Unknown local index/name: {name ?? "<null>"}");
        }

private static string CanonicalIndirectTypePart(
    IEnumerable<WasmValueType> types
)
{
    var names =
        types
            .Select(
                type =>
                    type switch
                    {
                        WasmValueType.I32 => "i32",
                        WasmValueType.I64 => "i64",
                        WasmValueType.F32 => "f32",
                        WasmValueType.F64 => "f64",

                        _ => throw new NotSupportedException(
                            $"Unsupported indirect-call value type: {type}"
                        ),
                    }
            )
            .ToList();

    return names.Count == 0
        ? "none"
        : string.Join("_", names);
}

private static string CanonicalIndirectTypeName(
    WasmFuncType type
)
{
    string parameters =
        CanonicalIndirectTypePart(
            type.ParamTypes
        );

    string results =
        CanonicalIndirectTypePart(
            type.ResultTypes
        );

    // Exemple :
    // params=[i32], results=[i32]
    // devient i32_=>_i32
    return $"{parameters}_=>_{results}";
}

private static bool HasIndirectSignature(
    WasmFunctionRef functionReference,
    WasmFuncType expectedType
)
{
    IReadOnlyList<WasmValueType> parameterTypes;
    IReadOnlyList<WasmValueType> resultTypes;

    if (
        functionReference.IsImport
        && functionReference.Import != null
    )
    {
        parameterTypes =
            functionReference.Import.ParamTypes;

        resultTypes =
            functionReference.Import.ResultTypes;
    }
    else if (
        functionReference.Function != null
    )
    {
        parameterTypes =
            functionReference.Function.ParamTypes;

        resultTypes =
            functionReference.Function.ResultTypes;
    }
    else
    {
        return false;
    }

    return
        parameterTypes.SequenceEqual(
            expectedType.ParamTypes
        )
        && resultTypes.SequenceEqual(
            expectedType.ResultTypes
        );
}

private WasmFuncType ResolveIndirectType(
    string? typeUse
)
{
    if (currentModule == null)
    {
        throw new InvalidOperationException(
            "No current WebAssembly module."
        );
    }

    if (string.IsNullOrWhiteSpace(typeUse))
    {
        throw new NotSupportedException(
            "call_indirect without a type use is not supported."
        );
    }

    string raw =
        typeUse.Trim();

    if (
        raw.StartsWith(
            "$",
            StringComparison.Ordinal
        )
    )
    {
        raw = raw.Substring(1);
    }

    // Cas 1 : référence numérique, par exemple (type 0).
    if (
        int.TryParse(
            raw,
            out int typeIndex
        )
    )
    {
        var indexedType =
            currentModule.Types.FirstOrDefault(
                type => type.Index == typeIndex
            );

        if (indexedType == null)
        {
            throw new InvalidOperationException(
                $"WebAssembly type {typeIndex} not found."
            );
        }

        return indexedType;
    }

    // Cas 2 : nom canonique produit par Binaryen,
    // par exemple $i32_=>_i32.
    var canonicalType =
        currentModule.Types.FirstOrDefault(
            type =>
                string.Equals(
                    CanonicalIndirectTypeName(type),
                    raw,
                    StringComparison.Ordinal
                )
        );

    if (canonicalType != null)
    {
        return canonicalType;
    }

    throw new NotSupportedException(
        $"Unsupported indirect type reference: {typeUse}. "
        + "Known canonical types: "
        + string.Join(
            ", ",
            currentModule.Types.Select(
                type =>
                    "$"
                    + CanonicalIndirectTypeName(type)
            )
        )
    );
}

        private void TranslateNode(WasmNode node, BoogieStmtList body)
        {
            translateDepth++;

            if (translateDepth > 5000)
            {
                throw new Exception(
                    $"TranslateNode recursion too deep: depth={translateDepth}, "
                        + $"node={node.GetType().Name}, func={currentFunction?.Name}"
                );
            }
            try
            {
                switch (node)
                {
                    case RefNullNode refNull:
{
    BoogieExpr value =
        refNull.Kind switch
        {
            WasmReferenceKind.Func =>
                NullFuncRefValue(),

            WasmReferenceKind.Extern =>
                NullExternRefValue(),

            _ => throw new NotSupportedException(
                $"Unsupported reference kind: {refNull.Kind}"
            ),
        };

    body.AddStatement(
        new BoogieCallCmd(
            "push",
            new List<BoogieExpr>
            {
                value
            },
            new()
        )
    );

    break;
}
case RefFuncNode refFunc:
{
    int functionIndex =
        ResolveFunctionReferenceIndex(
            refFunc.Target
        );

    body.AddStatement(
        new BoogieCallCmd(
            "push",
            new List<BoogieExpr>
            {
                FuncRefValue(
                    new BoogieLiteralExpr(
                        functionIndex
                    )
                )
            },
            new()
        )
    );

    break;
}
                    case ConstNode cn:
                    {
                        Console.WriteLine(
                            $"DEBUG CONST BEFORE TRANSLATION: type={cn.Type}, value={cn.Value}"
                        );
                        string ty = cn.Type;

                        if (ty == "i32")
                        {
                            if (System.Numerics.BigInteger.TryParse(cn.Value, out var parsedValue))
                            {
                                var normalizedValue = NormalizeUnsignedInteger(parsedValue, 32);

                                var literal = new BoogieLiteralExpr(normalizedValue);

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "push",
                                        new List<BoogieExpr> { I32(literal) },
                                        new()
                                    )
                                );
                            }
                            else
                            {
                                throw new FormatException($"Invalid i32 constant: {cn.Value}");
                            }
                        }
                        else if (ty == "i64")
                        {
                            if (System.Numerics.BigInteger.TryParse(cn.Value, out var parsedValue))
                            {
                                var normalizedValue = NormalizeUnsignedInteger(parsedValue, 64);

                                var literal = new BoogieLiteralExpr(normalizedValue);

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "push",
                                        new List<BoogieExpr> { I64(literal) },
                                        new()
                                    )
                                );
                            }
                            else
                            {
                                throw new FormatException($"Invalid i64 constant: {cn.Value}");
                            }
                        }
                        else if (ty == "f32")
                        {
                            if (
                                float.TryParse(
                                    cn.Value,
                                    System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out var v
                                )
                            )
                            {
                                var lit = new BoogieLiteralExpr(new Pfloat(v));

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "push",
                                        new List<BoogieExpr> { F32(ToF32(lit)) },
                                        new()
                                    )
                                );
                            }
                        }
                        else if (ty == "f64")
                        {
                            if (
                                double.TryParse(
                                    cn.Value,
                                    System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out var v
                                )
                            )
                            {
                                var lit = new BoogieLiteralExpr(new Pfloat(cn.Value));

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "push",
                                        new List<BoogieExpr> { F64(ToF64(lit)) },
                                        new()
                                    )
                                );
                            }
                        }
                        else
                        {
                            body.AddStatement(
                                new BoogieCommentCmd($"// unsupported const type: {ty}")
                            );
                        }

                        break;
                    }

                    case LocalGetNode lg:
                    {
                        var id = ResolveLocalBoogieId(lg.Index, lg.Name, "local.get");
                        body.AddStatement(new BoogieCallCmd("push", new() { id }, new()));
                        break;
                    }

                    case LocalSetNode ls:
                    {
                        var id = ResolveLocalBoogieId(ls.Index, ls.Name, "local.set");
                        if (ls.Value != null)
                            TranslateNode(ls.Value, body);
                        EnsurePopArgsProc(1);
                        body.AddStatement(new BoogieCallCmd("popArgs1", new(), new() { id }));
                        break;
                    }

                    case LocalTeeNode lt:
                    {
                        var id = ResolveLocalBoogieId(lt.Index, lt.Name, "local.tee");
                        EnsurePopArgsProc(1);
                        body.AddStatement(new BoogieCallCmd("popArgs1", new(), new() { id }));
                        body.AddStatement(new BoogieCallCmd("push", new() { id }, new()));
                        break;
                    }

                    // ===== NEW: global.get / global.set =====
                    case GlobalGetNode gg:
                    {
                        string gkey = ResolveGlobalKey(gg.Index, gg.Name);
                        string bname = EnsureGlobalVar(gkey);
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieIdentifierExpr(bname) },
                                new()
                            )
                        );
                        break;
                    }

                    case GlobalSetNode gs:
                    {
                        string gkey = ResolveGlobalKey(gs.Index, gs.Name);
                        string bname = EnsureGlobalVar(gkey);

                        // folded form: (global.set $g <expr>)
                        if (gs.Value != null)
                            TranslateNode(gs.Value, body);

                        EnsurePopArgsProc(1);
                        body.AddStatement(
                            new BoogieCallCmd(
                                "popArgs1",
                                new(),
                                new() { new BoogieIdentifierExpr(bname) }
                            )
                        );

                        // IMPORTANT: Boogie framing (modifies)
                        currentModifiedGlobals?.Add(bname);
                        break;
                    }

                    case CallNode call:
                    {
                        if (call.Args != null)
                            foreach (var a in call.Args)
                                TranslateNode(a, body);

                        string target = ResolveCalleeName(call.Target);
                        body.AddStatement(new BoogieCallCmd(target, new(), new()));
                        break;
                    }

                   
case CallIndirectNode ci:
{
    var type =
        ResolveIndirectType(
            ci.TypeUse
        );

    foreach (var arg in ci.Args)
    {
        TranslateNode(
            arg,
            body
        );
    }

    TranslateNode(
        ci.CalleeIndex,
        body
    );

    body.AddStatement(
        new BoogieCallCmd(
            "popToTmp1",
            new(),
            new()
        )
    );


    body.AddStatement(
        new BoogieAssertCmd(
            IsCtor(
                Tmp1(),
                "I32"
            )
        )
    );

    var calleeTableIndex =
        Field(
            Tmp1(),
            "value_i32"
        );

    body.AddStatement(
        new BoogieAssertCmd(
            IsU32(
                calleeTableIndex
            )
        )
    );

    body.AddStatement(
        new BoogieAssignCmd(
            new BoogieIdentifierExpr("idx"),
            calleeTableIndex
        )
    );

    // Cette procédure impose également :
    // 0 <= idx && idx < $table_size.
    body.AddStatement(
        new BoogieCallCmd(
            "table_get",

            new List<BoogieExpr>
            {
                new BoogieIdentifierExpr("idx"),
            },

            new List<BoogieIdentifierExpr>
            {
                new BoogieIdentifierExpr("$tmp1"),
            }
        )
    );

    // NullFuncRef doit produire un trap.
    // Une externref n’est pas non plus une fonction appelable.
    body.AddStatement(
        new BoogieAssertCmd(
            IsCtor(
                Tmp1(),
                "FuncRef"
            )
        )
    );

var functionIndex =
    Field(
        Tmp1(),
        "function_index"
    );
 if (currentModule == null)
{
    throw new InvalidOperationException(
        "No current WebAssembly module for call_indirect."
    );
}

var compatibleTargets =
    currentModule.FunctionIndexSpace
        .Where(
            functionReference =>
                HasIndirectSignature(
                    functionReference,
                    type
                )
        )
        .OrderBy(
            functionReference =>
                functionReference.Index
        )
        .ToList();

if (compatibleTargets.Count == 0)
{
    throw new InvalidOperationException(
        "No function has the signature required by "
        + $"call_indirect type {ci.TypeUse}."
    );
}

// Construit :
//
// if (function_index == 0) {
//     call target0();
// } else if (function_index == 3) {
//     call target3();
// } else {
//     assert false;
// }
//
// Le dernier assert représente le trap produit par une
// référence dont la signature ne correspond pas.
BoogieIfCmd BuildIndirectDispatch(
    int targetPosition
)
{
    var target =
        compatibleTargets[targetPosition];

    int targetIndex =
        target.Index;

    if (
        targetIndex < 0
        || targetIndex >= functionIndexToBoogieName.Count
    )
    {
        throw new InvalidOperationException(
            $"Invalid indirect target index: {targetIndex}."
        );
    }

    string targetProcedure =
        functionIndexToBoogieName[targetIndex];

    var condition =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.EQ,
            functionIndex,
            new BoogieLiteralExpr(
                targetIndex
            )
        );

    var thenBody =
        new BoogieStmtList();

    // Les arguments sont toujours sur la pile.
    // La procédure cible les récupère avec popArgsN.
    thenBody.AddStatement(
        new BoogieCallCmd(
            targetProcedure,
            new(),
            new()
        )
    );

    var elseBody =
        new BoogieStmtList();

    if (
        targetPosition + 1
        < compatibleTargets.Count
    )
    {
        elseBody.AddStatement(
            BuildIndirectDispatch(
                targetPosition + 1
            )
        );
    }
    else
    {
        // Indice de fonction inexistant ou signature incompatible.
        elseBody.AddStatement(
            new BoogieAssertCmd(
                new BoogieLiteralExpr(false)
            )
        );
    }

    return new BoogieIfCmd(
        condition,
        thenBody,
        elseBody
    );
}

body.AddStatement(
    BuildIndirectDispatch(0)
);

break; 
}

                    // ===== return_call (tail-call) =====
                    case ReturnCallNode rc:
                    {
                        if (rc.Args != null)
                            foreach (var a in rc.Args)
                                TranslateNode(a, body);

                        string target = ResolveCalleeName(rc.Target);
                        body.AddStatement(new BoogieCallCmd(target, new(), new()));

                        if (functionExitLabel == null)
                            functionExitLabel = GenerateLabel("func_exit");
                        body.AddStatement(new BoogieGotoCmd(functionExitLabel));
                        break;
                    }

case ReturnCallIndirectNode rci:
{
    var type =
        ResolveIndirectType(
            rci.TypeUse
        );

    // Évaluer les arguments dans leur ordre.
    foreach (var argument in rci.Args)
    {
        TranslateNode(
            argument,
            body
        );
    }

    // Évaluer l’indice dans la table.
    TranslateNode(
        rci.CalleeIndex,
        body
    );

    body.AddStatement(
        new BoogieCallCmd(
            "popToTmp1",
            new(),
            new()
        )
    );

    // L’indice de table doit être un i32.
    body.AddStatement(
        new BoogieAssertCmd(
            IsCtor(
                Tmp1(),
                "I32"
            )
        )
    );

    var calleeTableIndex =
        Field(
            Tmp1(),
            "value_i32"
        );

    body.AddStatement(
        new BoogieAssertCmd(
            IsU32(
                calleeTableIndex
            )
        )
    );

    // Sauvegarder l’indice avant table_get.
    body.AddStatement(
        new BoogieAssignCmd(
            new BoogieIdentifierExpr("idx"),
            calleeTableIndex
        )
    );

    // table_get impose aussi :
    // 0 <= idx && idx < $table_size.
    body.AddStatement(
        new BoogieCallCmd(
            "table_get",

            new List<BoogieExpr>
            {
                new BoogieIdentifierExpr("idx"),
            },

            new List<BoogieIdentifierExpr>
            {
                new BoogieIdentifierExpr("$tmp1"),
            }
        )
    );

    // NullFuncRef, ExternRef et NullExternRef provoquent un trap.
    body.AddStatement(
        new BoogieAssertCmd(
            IsCtor(
                Tmp1(),
                "FuncRef"
            )
        )
    );

    var functionIndex =
        Field(
            Tmp1(),
            "function_index"
        );

    if (currentModule == null)
    {
        throw new InvalidOperationException(
            "No current WebAssembly module "
            + "for return_call_indirect."
        );
    }

    var compatibleTargets =
        currentModule.FunctionIndexSpace
            .Where(
                functionReference =>
                    HasIndirectSignature(
                        functionReference,
                        type
                    )
            )
            .OrderBy(
                functionReference =>
                    functionReference.Index
            )
            .ToList();

    if (compatibleTargets.Count == 0)
    {
        throw new InvalidOperationException(
            "No function has the signature required by "
            + $"return_call_indirect type {rci.TypeUse}."
        );
    }

    BoogieIfCmd BuildReturnIndirectDispatch(
        int targetPosition
    )
    {
        var target =
            compatibleTargets[targetPosition];

        int targetIndex =
            target.Index;

        if (
            targetIndex < 0
            || targetIndex >= functionIndexToBoogieName.Count
        )
        {
            throw new InvalidOperationException(
                $"Invalid indirect target index: {targetIndex}."
            );
        }

        string targetProcedure =
            functionIndexToBoogieName[
                targetIndex
            ];

        var condition =
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.EQ,
                functionIndex,
                new BoogieLiteralExpr(
                    targetIndex
                )
            );

        var thenBody =
            new BoogieStmtList();

        // Les arguments sont encore sur la pile.
        // La fonction cible les récupère avec popArgsN.
        thenBody.AddStatement(
            new BoogieCallCmd(
                targetProcedure,
                new(),
                new()
            )
        );

        var elseBody =
            new BoogieStmtList();

        if (
            targetPosition + 1
            < compatibleTargets.Count
        )
        {
            elseBody.AddStatement(
                BuildReturnIndirectDispatch(
                    targetPosition + 1
                )
            );
        }
        else
        {
            // Indice invalide ou signature incompatible.
            elseBody.AddStatement(
                new BoogieAssertCmd(
                    new BoogieLiteralExpr(false)
                )
            );
        }

        return new BoogieIfCmd(
            condition,
            thenBody,
            elseBody
        );
    }

    body.AddStatement(
        BuildReturnIndirectDispatch(0)
    );

    // Sémantique tail-call :
    // ne pas exécuter les instructions suivantes
    // de la fonction courante.
    if (functionExitLabel == null)
    {
        functionExitLabel =
            GenerateLabel(
                "func_exit"
            );
    }

    body.AddStatement(
        new BoogieGotoCmd(
            functionExitLabel
        )
    );

    break;
}

                    case MemoryOpNode mem:
                    {
                        // Helpers communs
                        BoogieExpr Tmp1() => new BoogieIdentifierExpr("$tmp1");
                        BoogieExpr Tmp2() => new BoogieIdentifierExpr("$tmp2");

                        // --- helper: compute idx from an address already on the stack (top) ---
                        void PopAddrComputeIdx()
                        {
                            // L’adresse WebAssembly est un i32 placé au sommet de la pile.
                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I32")));

                            BoogieExpr address = I32Value(Tmp1());

                            body.AddStatement(new BoogieAssertCmd(IsU32(address)));

                            // L’offset statique est ajouté sans réduction modulo 2^32.
                            BoogieExpr effectiveAddress = new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.ADD,
                                address,
                                new BoogieLiteralExpr(new System.Numerics.BigInteger(mem.Offset))
                            );

                            body.AddStatement(
                                new BoogieAssignCmd(
                                    new BoogieIdentifierExpr("idx"),
                                    effectiveAddress
                                )
                            );
                        }

                        // --- helper: compute idx from folded address node (mem.Address) ---
                        void EvalAddrComputeIdx()
                        {
                            if (mem.Address != null)
                                TranslateNode(mem.Address, body);
                            // address is now on stack
                            PopAddrComputeIdx();
                        }
                        switch (mem.Op)
                        {
                            case "memory.size":
                            {
                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "memory_size",
                                        new(),
                                        new() { new BoogieIdentifierExpr("load_i") }
                                    )
                                );

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "push",
                                        new() { I32(ToU32(new BoogieIdentifierExpr("load_i"))) },
                                        new()
                                    )
                                );

                                break;
                            }

                            case "memory.grow":
                            {
                                if (mem.Address != null)
                                {
                                    TranslateNode(mem.Address, body);
                                }

                                body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                                body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I32")));

                                BoogieExpr delta = I32Value(Tmp1());

                                body.AddStatement(new BoogieAssertCmd(IsU32(delta)));

                                body.AddStatement(
                                    new BoogieAssignCmd(new BoogieIdentifierExpr("idx"), delta)
                                );

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "memory_grow",
                                        new() { new BoogieIdentifierExpr("idx") },
                                        new() { new BoogieIdentifierExpr("load_i") }
                                    )
                                );

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "push",
                                        new() { I32(ToU32(new BoogieIdentifierExpr("load_i"))) },
                                        new()
                                    )
                                );
                                break;
                            }
                        }

                        if (mem.Op == "memory.size" || mem.Op == "memory.grow")
                            break;
                        if (mem.Op == "memory.fill")
                        {
                            // Ordre de pile WebAssembly : dst, value, len
                            if (mem.Address != null)
                            {
                                TranslateNode(mem.Address, body);
                            }

                            if (mem.Value != null)
                            {
                                TranslateNode(mem.Value, body);
                            }

                            if (mem.Length != null)
                            {
                                TranslateNode(mem.Length, body);
                            }

                            // Retrait dans l’ordre inverse.
                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // len

                            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // value

                            body.AddStatement(new BoogieCallCmd("popToTmp3", new(), new())); // dst

                            // Les trois opérandes de memory.fill sont des i32.
                            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I32")));

                            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), "I32")));

                            body.AddStatement(
                                new BoogieAssertCmd(
                                    IsCtor(new BoogieIdentifierExpr("$tmp3"), "I32")
                                )
                            );

                            BoogieExpr len = I32Value(Tmp1());

                            BoogieExpr value = I32Value(Tmp2());

                            BoogieExpr dst = I32Value(new BoogieIdentifierExpr("$tmp3"));

                            body.AddStatement(new BoogieAssertCmd(IsU32(len)));

                            body.AddStatement(new BoogieAssertCmd(IsU32(value)));

                            body.AddStatement(new BoogieAssertCmd(IsU32(dst)));

                            body.AddStatement(
                                new BoogieCallCmd("memory_fill", new() { dst, value, len }, new())
                            );

                            break;
                        }
                        if (mem.Op == "memory.copy")
                        {
                            // Ordre de pile WebAssembly : dst, src, len
                            if (mem.Address != null)
                            {
                                TranslateNode(mem.Address, body);
                            }

                            if (mem.Value != null)
                            {
                                TranslateNode(mem.Value, body);
                            }

                            if (mem.Length != null)
                            {
                                TranslateNode(mem.Length, body);
                            }

                            // Retrait dans l’ordre inverse.
                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // len

                            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // src

                            body.AddStatement(new BoogieCallCmd("popToTmp3", new(), new())); // dst

                            // Les trois opérandes de memory.copy sont des i32.
                            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I32")));

                            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), "I32")));

                            body.AddStatement(
                                new BoogieAssertCmd(
                                    IsCtor(new BoogieIdentifierExpr("$tmp3"), "I32")
                                )
                            );

                            BoogieExpr len = I32Value(Tmp1());

                            BoogieExpr src = I32Value(Tmp2());

                            BoogieExpr dst = I32Value(new BoogieIdentifierExpr("$tmp3"));

                            body.AddStatement(new BoogieAssertCmd(IsU32(len)));

                            body.AddStatement(new BoogieAssertCmd(IsU32(src)));

                            body.AddStatement(new BoogieAssertCmd(IsU32(dst)));

                            body.AddStatement(
                                new BoogieCallCmd("memory_copy", new() { dst, src, len }, new())
                            );

                            break;
                        }
                        // --- STORE path ---
                        bool isStore =
                            mem.Op
                            is "i32.store"
                                or "i64.store"
                                or "f32.store"
                                or "f64.store"
                                or "i32.store8"
                                or "i32.store16"
                                or "i64.store8"
                                or "i64.store16"
                                or "i64.store32";

                        if (isStore)
                        {
                            // WebAssembly store expects stack order: ... addr, value
                            // If you have folded nodes, keep the same order:
                            //   evaluate address first, then value, so top-of-stack is value, below is addr.
                            if (mem.Address != null)
                                TranslateNode(mem.Address, body);
                            if (mem.Value != null)
                                TranslateNode(mem.Value, body);

                            // pop value -> $tmp2
                            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

                            // pop addr -> $tmp1 and compute idx
                            PopAddrComputeIdx();

                            if (mem.Op is "i32.store" or "i32.store8" or "i32.store16")
                            {
                                body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), "I32")));

                                BoogieExpr value = I32Value(Tmp2());

                                body.AddStatement(new BoogieAssertCmd(IsU32(value)));

                                body.AddStatement(
                                    new BoogieAssignCmd(new BoogieIdentifierExpr("store_i"), value)
                                );
                            }
                            else if (
                                mem.Op
                                is "i64.store"
                                    or "i64.store8"
                                    or "i64.store16"
                                    or "i64.store32"
                            )
                            {
                                body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), "I64")));

                                BoogieExpr value = I64Value(Tmp2());

                                body.AddStatement(new BoogieAssertCmd(IsU64(value)));

                                body.AddStatement(
                                    new BoogieAssignCmd(new BoogieIdentifierExpr("store_i"), value)
                                );
                            }
                            else if (mem.Op == "f32.store")
                            {
                                body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), "F32")));

                                BoogieExpr value = F32Value(Tmp2());

                                body.AddStatement(new BoogieAssertCmd(IsF32(value)));

                                BoogieExpr bits = new BoogieFunctionCall(
                                    "f32_real_to_bits",
                                    new List<BoogieExpr> { value }
                                );

                                body.AddStatement(new BoogieAssertCmd(IsU32(bits)));

                                body.AddStatement(
                                    new BoogieAssignCmd(new BoogieIdentifierExpr("store_i"), bits)
                                );
                            }
                            else if (mem.Op == "f64.store")
                            {
                                body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp2(), "F64")));

                                BoogieExpr value = F64Value(Tmp2());

                                body.AddStatement(new BoogieAssertCmd(IsF64(value)));

                                BoogieExpr bits = new BoogieFunctionCall(
                                    "f64_real_to_bits",
                                    new List<BoogieExpr> { value }
                                );

                                body.AddStatement(new BoogieAssertCmd(IsU64(bits)));

                                body.AddStatement(
                                    new BoogieAssignCmd(new BoogieIdentifierExpr("store_i"), bits)
                                );
                            }
                            else
                            {
                                // Chemin temporaire pour les opérations mémoire
                                // qui ne sont pas encore migrées.
                                body.AddStatement(
                                    new BoogieAssignCmd(
                                        new BoogieIdentifierExpr("store_i"),
                                        new BoogieFunctionCall(
                                            "real_to_int",
                                            new List<BoogieExpr> { Tmp2() }
                                        )
                                    )
                                );
                            }

                            // dispatch write
                            switch (mem.Op)
                            {
                                case "i32.store":
                                case "f32.store":
                                case "i64.store32":
                                    body.AddStatement(
                                        new BoogieCallCmd(
                                            "mem_write_u32",
                                            new()
                                            {
                                                new BoogieIdentifierExpr("idx"),
                                                new BoogieIdentifierExpr("store_i"),
                                            },
                                            new()
                                        )
                                    );
                                    break;

                                case "i64.store":
                                case "f64.store":
                                    body.AddStatement(
                                        new BoogieCallCmd(
                                            "mem_write_u64",
                                            new()
                                            {
                                                new BoogieIdentifierExpr("idx"),
                                                new BoogieIdentifierExpr("store_i"),
                                            },
                                            new()
                                        )
                                    );
                                    break;

                                case "i32.store8":
                                case "i64.store8":
                                    body.AddStatement(
                                        new BoogieCallCmd(
                                            "mem_write_u8",
                                            new()
                                            {
                                                new BoogieIdentifierExpr("idx"),
                                                new BoogieIdentifierExpr("store_i"),
                                            },
                                            new()
                                        )
                                    );
                                    break;

                                case "i32.store16":
                                case "i64.store16":
                                    body.AddStatement(
                                        new BoogieCallCmd(
                                            "mem_write_u16",
                                            new()
                                            {
                                                new BoogieIdentifierExpr("idx"),
                                                new BoogieIdentifierExpr("store_i"),
                                            },
                                            new()
                                        )
                                    );
                                    break;

                                default:
                                    body.AddStatement(
                                        new BoogieCommentCmd($"// unsupported store op: {mem.Op}")
                                    );
                                    break;
                            }

                            break; // done
                        }

                        // --- LOAD path ---
                        // evaluate address (folded) then pop it and compute idx
                        EvalAddrComputeIdx();

                        BoogieExpr idxExpr = new BoogieIdentifierExpr("idx");
                        var loadVar = new BoogieIdentifierExpr("load_i");

                        void CallRead(string procName)
                        {
                            body.AddStatement(
                                new BoogieCallCmd(procName, new() { idxExpr }, new() { loadVar })
                            );
                        }

                        void PushLoadedI32()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { I32(ToU32(loadVar)) },
                                    new()
                                )
                            );
                        }
                        void PushLoadedI64()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { I64(ToU64(loadVar)) },
                                    new()
                                )
                            );
                        }

                        void PushLoadedSignedI32()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { I32(ToU32(loadVar)) },
                                    new()
                                )
                            );
                        }

                        void PushLoadedSignedI64()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { I64(ToU64(loadVar)) },
                                    new()
                                )
                            );
                        }

                        void PushLoadedUnsignedI64()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { I64(loadVar) },
                                    new()
                                )
                            );
                        }
                        void PushLoadedUnsignedI32()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { I32(loadVar) },
                                    new()
                                )
                            );
                        }

                        void PushLoadedF32()
                        {
                            BoogieExpr value = new BoogieFunctionCall(
                                "f32_bits_to_real",
                                new List<BoogieExpr> { loadVar }
                            );

                            body.AddStatement(new BoogieAssertCmd(IsF32(value)));

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { F32(value) },
                                    new()
                                )
                            );
                        }

                        void PushLoadedF64()
                        {
                            BoogieExpr value = new BoogieFunctionCall(
                                "f64_bits_to_real",
                                new List<BoogieExpr> { loadVar }
                            );

                            body.AddStatement(new BoogieAssertCmd(IsF64(value)));

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { F64(value) },
                                    new()
                                )
                            );
                        }

                        void PushLoadedIntAsReal()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new()
                                    {
                                        new BoogieFunctionCall("int_to_real", new() { loadVar }),
                                    },
                                    new()
                                )
                            );
                        }

                        void PushLoadedBits32AsReal()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new()
                                    {
                                        new BoogieFunctionCall("bits32_to_real", new() { loadVar }),
                                    },
                                    new()
                                )
                            );
                        }

                        void PushLoadedBits64AsReal()
                        {
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new()
                                    {
                                        new BoogieFunctionCall("bits64_to_real", new() { loadVar }),
                                    },
                                    new()
                                )
                            );
                        }

                        switch (mem.Op)
                        {
                            case "i32.load":
                                CallRead("mem_read_s32");
                                PushLoadedI32();
                                break;

                            case "i64.load":
                                CallRead("mem_read_s64");
                                PushLoadedI64();
                                break;

                            case "f32.load":
                                CallRead("mem_read_u32");
                                PushLoadedF32();
                                break;

                            case "f64.load":
                                CallRead("mem_read_u64");
                                PushLoadedF64();
                                break;

                            case "i32.load8_s":
                                CallRead("mem_read_s8");
                                PushLoadedSignedI32();
                                break;

                            case "i32.load8_u":
                                CallRead("mem_read_u8");
                                PushLoadedUnsignedI32();
                                break;

                            case "i32.load16_s":
                                CallRead("mem_read_s16");
                                PushLoadedSignedI32();
                                break;

                            case "i32.load16_u":
                                CallRead("mem_read_u16");
                                PushLoadedUnsignedI32();
                                break;

                            case "i64.load8_s":
                                CallRead("mem_read_s8");
                                PushLoadedSignedI64();
                                break;

                            case "i64.load8_u":
                                CallRead("mem_read_u8");
                                PushLoadedUnsignedI64();
                                break;

                            case "i64.load16_s":
                                CallRead("mem_read_s16");
                                PushLoadedSignedI64();
                                break;

                            case "i64.load16_u":
                                CallRead("mem_read_u16");
                                PushLoadedUnsignedI64();
                                break;

                            case "i64.load32_s":
                                CallRead("mem_read_s32");
                                PushLoadedSignedI64();
                                break;

                            case "i64.load32_u":
                                CallRead("mem_read_u32");
                                PushLoadedUnsignedI64();
                                break;

                            default:
                                body.AddStatement(
                                    new BoogieCommentCmd($"// unsupported memory op: {mem.Op}")
                                );
                                break;
                        }

                        break;
                    }
                    case TableOpNode t:
                    {
if (t.Op == "table.get")
{
    if (t.Index != null)
    {
        TranslateNode(
            t.Index,
            body
        );
    }

    body.AddStatement(
        new BoogieCallCmd(
            "popToTmp1",
            new(),
            new()
        )
    );

    body.AddStatement(
        new BoogieAssertCmd(
            IsCtor(
                new BoogieIdentifierExpr("$tmp1"),
                "I32"
            )
        )
    );

    BoogieExpr tableIndex =
        I32Value(
            new BoogieIdentifierExpr("$tmp1")
        );

    body.AddStatement(
        new BoogieAssertCmd(
            IsU32(tableIndex)
        )
    );

    body.AddStatement(
        new BoogieAssignCmd(
            new BoogieIdentifierExpr("idx"),
            tableIndex
        )
    );

    body.AddStatement(
        new BoogieCallCmd(
            "table_get",
            new List<BoogieExpr>
            {
                new BoogieIdentifierExpr("idx")
            },
            new List<BoogieIdentifierExpr>
            {
                new BoogieIdentifierExpr("$tmp1")
            }
        )
    );

    body.AddStatement(
        new BoogieCallCmd(
            "push",
            new List<BoogieExpr>
            {
                new BoogieIdentifierExpr("$tmp1")
            },
            new()
        )
    );

    break;
}

if (t.Op == "table.set")
{
    if (t.Index != null)
    {
        TranslateNode(
            t.Index,
            body
        );
    }

    if (t.Value != null)
    {
        TranslateNode(
            t.Value,
            body
        );
    }

    // Sommet : valeur de référence.
    body.AddStatement(
        new BoogieCallCmd(
            "popToTmp1",
            new(),
            new()
        )
    );

    // En dessous : indice i32.
    body.AddStatement(
        new BoogieCallCmd(
            "popToTmp2",
            new(),
            new()
        )
    );

    body.AddStatement(
        new BoogieAssertCmd(
            IsCtor(
                new BoogieIdentifierExpr("$tmp2"),
                "I32"
            )
        )
    );

    BoogieExpr tableIndex =
        I32Value(
            new BoogieIdentifierExpr("$tmp2")
        );

    body.AddStatement(
        new BoogieAssertCmd(
            IsU32(tableIndex)
        )
    );

    /*
     * Pour l’instant, une table peut contenir une référence de
     * fonction ou une référence externe.
     */
    BoogieExpr isReference =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.OR,

            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.OR,
                IsCtor(
                    new BoogieIdentifierExpr("$tmp1"),
                    "NullFuncRef"
                ),
                IsCtor(
                    new BoogieIdentifierExpr("$tmp1"),
                    "FuncRef"
                )
            ),

            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.OR,
                IsCtor(
                    new BoogieIdentifierExpr("$tmp1"),
                    "NullExternRef"
                ),
                IsCtor(
                    new BoogieIdentifierExpr("$tmp1"),
                    "ExternRef"
                )
            )
        );

    body.AddStatement(
        new BoogieAssertCmd(
            isReference
        )
    );

    body.AddStatement(
        new BoogieAssignCmd(
            new BoogieIdentifierExpr("idx"),
            tableIndex
        )
    );

    body.AddStatement(
        new BoogieCallCmd(
            "table_set",
            new List<BoogieExpr>
            {
                new BoogieIdentifierExpr("idx"),
                new BoogieIdentifierExpr("$tmp1")
            },
            new()
        )
    );

    break;
}

if (t.Op == "table.size")
{
    body.AddStatement(
        new BoogieCallCmd(
            "table_size",
            new(),
            new()
            {
                new BoogieIdentifierExpr("idx")
            }
        )
    );

    body.AddStatement(
        new BoogieCallCmd(
            "push",
            new List<BoogieExpr>
            {
                I32(
                    ToU32(
                        new BoogieIdentifierExpr("idx")
                    )
                )
            },
            new()
        )
    );

    break;
}

if (t.Op == "table.grow")
{
    if (t.Value != null)
    {
        TranslateNode(
            t.Value,
            body
        );
    }

    if (t.Delta != null)
    {
        TranslateNode(
            t.Delta,
            body
        );
    }

    // Sommet : delta i32.
    body.AddStatement(
        new BoogieCallCmd(
            "popToTmp1",
            new(),
            new()
        )
    );

    // En dessous : valeur d’initialisation.
    body.AddStatement(
        new BoogieCallCmd(
            "popToTmp2",
            new(),
            new()
        )
    );

    body.AddStatement(
        new BoogieAssertCmd(
            IsCtor(
                new BoogieIdentifierExpr("$tmp1"),
                "I32"
            )
        )
    );

    BoogieExpr delta =
        I32Value(
            new BoogieIdentifierExpr("$tmp1")
        );

    body.AddStatement(
        new BoogieAssertCmd(
            IsU32(delta)
        )
    );

    BoogieExpr isReference =
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.OR,

            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.OR,
                IsCtor(
                    new BoogieIdentifierExpr("$tmp2"),
                    "NullFuncRef"
                ),
                IsCtor(
                    new BoogieIdentifierExpr("$tmp2"),
                    "FuncRef"
                )
            ),

            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.OR,
                IsCtor(
                    new BoogieIdentifierExpr("$tmp2"),
                    "NullExternRef"
                ),
                IsCtor(
                    new BoogieIdentifierExpr("$tmp2"),
                    "ExternRef"
                )
            )
        );

    body.AddStatement(
        new BoogieAssertCmd(
            isReference
        )
    );

    body.AddStatement(
        new BoogieAssignCmd(
            new BoogieIdentifierExpr("idx"),
            delta
        )
    );

    body.AddStatement(
        new BoogieCallCmd(
            "table_grow",
            new List<BoogieExpr>
            {
                new BoogieIdentifierExpr("$tmp2"),
                new BoogieIdentifierExpr("idx")
            },
            new()
            {
                new BoogieIdentifierExpr("load_i")
            }
        )
    );

    body.AddStatement(
        new BoogieCallCmd(
            "push",
            new List<BoogieExpr>
            {
                I32(
                    ToU32(
                        new BoogieIdentifierExpr("load_i")
                    )
                )
            },
            new()
        )
    );

    break;
}

                        body.AddStatement(new BoogieCommentCmd($"// unsupported table op: {t.Op}"));
                        break;
                    }

                    case UnaryOpNode un:
                    {
                        if (un.Operand != null)
                            TranslateNode(un.Operand, body);

                        if (un.Op == "drop")
                        {
                            body.AddStatement(new BoogieCallCmd("pop", new(), new()));
                        }else if (un.Op == "ref.is_null")
{
    EmitTypedRefIsNull(body);
}
                        else if (IsTypedEqzOp(un.Op))
                        {
                            EmitTypedEqz(un.Op, body);
                        }
                        else if (IsTypedFloatUnaryOp(un.Op))
                        {
                            EmitTypedFloatUnary(un.Op, body);
                        }
                        else if (IsTypedFloatRoundingOp(un.Op))
                        {
                            EmitTypedFloatRoundingOp(un.Op, body);
                        }
                        else if (IsTypedIntegerWidthCast(un.Op))
                        {
                            EmitTypedIntegerWidthCast(un.Op, body);
                        }
                        else if (IsTypedIntegerSignExtensionOp(un.Op))
                        {
                            EmitTypedIntegerSignExtensionOp(un.Op, body);
                        }
                        else if (IsTypedFloatWidthCast(un.Op))
                        {
                            EmitTypedFloatWidthCast(un.Op, body);
                        }
                        else if (IsTypedReinterpretCast(un.Op))
                        {
                            EmitTypedReinterpretCast(un.Op, body);
                        }
                        else if (IsTypedIntegerToFloatCast(un.Op))
                        {
                            EmitTypedIntegerToFloatCast(un.Op, body);
                        }
                        else if (IsTypedFloatToIntegerCast(un.Op))
                        {
                            EmitTypedFloatToIntegerCast(un.Op, body);
                        }
                        else if (IsTypedIntegerUnaryBitOp(un.Op))
                        {
                            EmitTypedIntegerUnaryBitOp(un.Op, body);
                        }
                        else
                        {
                            body.AddStatement(
                                new BoogieCommentCmd(
                                    $"// unsupported unary op in typed stage: {un.Op}"
                                )
                            );
                        }

                        break;
                    }
                    /*  case UnaryOpNode un:
                      {
                          if (un.Operand != null)
                              TranslateNode(un.Operand, body);
  
                          if (un.Op == "drop")
                          {
                              body.AddStatement(new BoogieCallCmd("pop", new(), new()));
                          }
                          else if (un.Op == "i32.eqz" || un.Op == "i64.eqz")
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                              var eqzExpr = new BoogieFunctionCall(
                                  "bool_to_real",
                                  new()
                                  {
                                      new BoogieBinaryOperation(
                                          BoogieBinaryOperation.Opcode.EQ,
                                          new BoogieIdentifierExpr("$tmp1"),
                                          UndefValue()
                                      ),
                                  }
                              );
                              body.AddStatement(new BoogieCallCmd("push", new() { eqzExpr }, new()));
                          }
                          else if (un.Op == "i32.wrap_i64" || un.Op == "i64.wrap_i64")
                          {
                              body.AddStatement(
                                  new BoogieCommentCmd("// wrap: no-op under real semantics")
                              );
                          }
                          else if (un.Op == "f32.abs" || un.Op == "f64.abs")
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
  
                              var absExpr = new BoogieFunctionCall(
                                  "abs_real",
                                  new() { new BoogieIdentifierExpr("$tmp1") }
                              );
  
                              body.AddStatement(new BoogieCallCmd("push", new() { absExpr }, new()));
                          }
                          else if (un.Op == "f32.neg" || un.Op == "f64.neg")
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
  
                              var negExpr = new BoogieUnaryOperation(
                                  BoogieUnaryOperation.Opcode.NEG,
                                  new BoogieIdentifierExpr("$tmp1")
                              );
  
                              body.AddStatement(new BoogieCallCmd("push", new() { negExpr }, new()));
                          }
                          else if (un.Op == "f32.sqrt" || un.Op == "f64.sqrt")
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
  
                              var sqrtExpr = new BoogieFunctionCall(
                                  "sqrt_real",
                                  new() { new BoogieIdentifierExpr("$tmp1") }
                              );
  
                              body.AddStatement(new BoogieCallCmd("push", new() { sqrtExpr }, new()));
                          }
                          else if (un.Op == "f32.nearest" || un.Op == "f64.nearest")
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                              var nearestExpr = new BoogieFunctionCall(
                                  "nearest_real",
                                  new() { new BoogieIdentifierExpr("$tmp1") }
                              );
                              body.AddStatement(
                                  new BoogieCallCmd("push", new() { nearestExpr }, new())
                              );
                          }
                          else if (un.Op == "f32.floor" || un.Op == "f64.floor")
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                              var flo = new BoogieFunctionCall(
                                  "floor_real",
                                  new() { new BoogieIdentifierExpr("$tmp1") }
                              );
                              body.AddStatement(new BoogieCallCmd("push", new() { flo }, new()));
                          }
                          else if (
                              un.Op
                              is "i32.clz"
                                  or "i64.clz"
                                  or "i32.ctz"
                                  or "i64.ctz"
                                  or "i32.popcnt"
                                  or "i64.popcnt"
                          )
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
  
                              string fun = un.Op switch
                              {
                                  "i32.clz" or "i64.clz" => "int_clz",
                                  "i32.ctz" or "i64.ctz" => "int_ctz",
                                  "i32.popcnt" or "i64.popcnt" => "int_popcnt",
                                  _ => throw new NotSupportedException(
                                      $"Unsupported integer unary op: {un.Op}"
                                  ),
                              };
  
                              body.AddStatement(
                                  new BoogieCallCmd(
                                      "push",
                                      new()
                                      {
                                          new BoogieFunctionCall(
                                              fun,
                                              new() { new BoogieIdentifierExpr("$tmp1") }
                                          ),
                                      },
                                      new()
                                  )
                              );
                          }
                          else if (un.Op == "f32.ceil" || un.Op == "f64.ceil")
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
  
                              var expr = new BoogieFunctionCall(
                                  "ceil_real",
                                  new() { new BoogieIdentifierExpr("$tmp1") }
                              );
  
                              body.AddStatement(new BoogieCallCmd("push", new() { expr }, new()));
                          }
                          else if (un.Op == "f32.trunc" || un.Op == "f64.trunc")
                          {
                              body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
  
                              var expr = new BoogieFunctionCall(
                                  "trunc_real",
                                  new() { new BoogieIdentifierExpr("$tmp1") }
                              );
  
                              body.AddStatement(new BoogieCallCmd("push", new() { expr }, new()));
                          }
                          else if (
                              un.Op
                              is "i64.extend_i32_s"
                                  or "i64.extend_i32_u"
                                  or "i32.trunc_f32_s"
                                  or "i32.trunc_f32_u"
                                  or "i32.trunc_f64_s"
                                  or "i32.trunc_f64_u"
                                  or "i64.trunc_f32_s"
                                  or "i64.trunc_f32_u"
                                  or "i64.trunc_f64_s"
                                  or "i64.trunc_f64_u"
                                  or "f32.convert_i32_s"
                                  or "f32.convert_i32_u"
                                  or "f32.convert_i64_s"
                                  or "f32.convert_i64_u"
                                  or "f64.convert_i32_s"
                                  or "f64.convert_i32_u"
                                  or "f64.convert_i64_s"
                                  or "f64.convert_i64_u"
                                  or "f32.demote_f64"
                                  or "f64.promote_f32"
                                  or "i32.reinterpret_f32"
                                  or "f32.reinterpret_i32"
                                  or "i64.reinterpret_f64"
                                  or "f64.reinterpret_i64"
                          )
                          {
                              body.AddStatement(
                                  new BoogieCommentCmd(
                                      $"// numeric cast {un.Op}: no-op under real semantics"
                                  )
                              );
                          }
                          else
                          {
                              body.AddStatement(
                                  new BoogieCommentCmd($"// unsupported unary op: {un.Op}")
                              );
                          }
                          break;
                      }*/

                    /*                    case BinaryOpNode bn:
                                        {
                                            TranslateNode(bn.Left, body);
                                            TranslateNode(bn.Right, body);
                    
                                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                                            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));
                                            var tmp1 = new BoogieIdentifierExpr("$tmp1");
                                            var tmp2 = new BoogieIdentifierExpr("$tmp2");
                    
                                            if (
                                                bn.Op
                                                is "i32.add"
                                                    or "i64.add"
                                                    or "f32.add"
                                                    or "f64.add"
                                                    or "i32.sub"
                                                    or "i64.sub"
                                                    or "f32.sub"
                                                    or "f64.sub"
                                                    or "i32.mul"
                                                    or "i64.mul"
                                                    or "f32.mul"
                                                    or "f64.mul"
                                                    or "i32.div_s"
                                                    or "i64.div_s"
                                                    or "f32.div"
                                                    or "f64.div"
                                                    or "i32.div_u"
                                                    or "i64.div_u"
                                            )
                                            {
                                                var opKind = bn.Op switch
                                                {
                                                    "i32.add" or "i64.add" or "f32.add" or "f64.add" =>
                                                        BoogieBinaryOperation.Opcode.ADD,
                                                    "i32.sub" or "i64.sub" or "f32.sub" or "f64.sub" =>
                                                        BoogieBinaryOperation.Opcode.SUB,
                                                    "i32.mul" or "i64.mul" or "f32.mul" or "f64.mul" =>
                                                        BoogieBinaryOperation.Opcode.MUL,
                                                    _ => BoogieBinaryOperation.Opcode.DIV,
                                                };
                                                var arithExpr = new BoogieBinaryOperation(opKind, tmp2, tmp1);
                                                body.AddStatement(
                                                    new BoogieCallCmd("push", new() { arithExpr }, new())
                                                );
                                            }
                                            else if (
                                                bn.Op
                                                is "i32.eq"
                                                    or "i64.eq"
                                                    or "f32.eq"
                                                    or "f64.eq"
                                                    or "i32.ne"
                                                    or "i64.ne"
                                                    or "f32.ne"
                                                    or "f64.ne"
                                                    or "i32.lt_s"
                                                    or "i64.lt_s"
                                                    or "i32.lt_u"
                                                    or "i64.lt_u"
                                                    or "f32.lt"
                                                    or "f64.lt"
                                                    or "i32.le_s"
                                                    or "i64.le_s"
                                                    or "i32.le_u"
                                                    or "i64.le_u"
                                                    or "f32.le"
                                                    or "f64.le"
                                                    or "i32.gt_s"
                                                    or "i64.gt_s"
                                                    or "i32.gt_u"
                                                    or "i64.gt_u"
                                                    or "f32.gt"
                                                    or "f64.gt"
                                                    or "i32.ge_s"
                                                    or "i64.ge_s"
                                                    or "i32.ge_u"
                                                    or "i64.ge_u"
                                                    or "f32.ge"
                                                    or "f64.ge"
                                            )
                                            {
                                                BoogieExpr cmpExpr = bn.Op switch
                                                {
                                                    "i32.eq" or "i64.eq" or "f32.eq" or "f64.eq" =>
                                                        new BoogieFunctionCall(
                                                            "bool_to_real",
                                                            new()
                                                            {
                                                                new BoogieBinaryOperation(
                                                                    BoogieBinaryOperation.Opcode.EQ,
                                                                    tmp2,
                                                                    tmp1
                                                                ),
                                                            }
                                                        ),
                                                    "i32.ne" or "i64.ne" or "f32.ne" or "f64.ne" =>
                                                        new BoogieFunctionCall(
                                                            "bool_to_real",
                                                            new()
                                                            {
                                                                new BoogieBinaryOperation(
                                                                    BoogieBinaryOperation.Opcode.NEQ,
                                                                    tmp2,
                                                                    tmp1
                                                                ),
                                                            }
                                                        ),
                                                    "i32.lt_s"
                                                    or "i64.lt_s"
                                                    or "i32.lt_u"
                                                    or "i64.lt_u"
                                                    or "f32.lt"
                                                    or "f64.lt" => new BoogieFunctionCall(
                                                        "bool_to_real",
                                                        new()
                                                        {
                                                            new BoogieBinaryOperation(
                                                                BoogieBinaryOperation.Opcode.LT,
                                                                tmp2,
                                                                tmp1
                                                            ),
                                                        }
                                                    ),
                                                    "i32.le_s"
                                                    or "i64.le_s"
                                                    or "i32.le_u"
                                                    or "i64.le_u"
                                                    or "f32.le"
                                                    or "f64.le" => new BoogieFunctionCall(
                                                        "bool_to_real",
                                                        new()
                                                        {
                                                            new BoogieBinaryOperation(
                                                                BoogieBinaryOperation.Opcode.LE,
                                                                tmp2,
                                                                tmp1
                                                            ),
                                                        }
                                                    ),
                                                    "i32.gt_s"
                                                    or "i64.gt_s"
                                                    or "i32.gt_u"
                                                    or "i64.gt_u"
                                                    or "f32.gt"
                                                    or "f64.gt" => new BoogieFunctionCall(
                                                        "bool_to_real",
                                                        new()
                                                        {
                                                            new BoogieBinaryOperation(
                                                                BoogieBinaryOperation.Opcode.GT,
                                                                tmp2,
                                                                tmp1
                                                            ),
                                                        }
                                                    ),
                                                    _ // ge
                                                    => new BoogieFunctionCall(
                                                        "bool_to_real",
                                                        new()
                                                        {
                                                            new BoogieBinaryOperation(
                                                                BoogieBinaryOperation.Opcode.GE,
                                                                tmp2,
                                                                tmp1
                                                            ),
                                                        }
                                                    ),
                                                };
                                                body.AddStatement(new BoogieCallCmd("push", new() { cmpExpr }, new()));
                                            }
                                            else if (
                                                bn.Op
                                                is "i32.and"
                                                    or "i64.and"
                                                    or "i32.or"
                                                    or "i64.or"
                                                    or "i32.xor"
                                                    or "i64.xor"
                                                    or "i32.shl"
                                                    or "i64.shl"
                                                    or "i32.shr_s"
                                                    or "i64.shr_s"
                                                    or "i32.shr_u"
                                                    or "i64.shr_u"
                                                    or "i32.rotl"
                                                    or "i64.rotl"
                                                    or "i32.rotr"
                                                    or "i64.rotr"
                                            )
                                            {
                                                string fun = bn.Op switch
                                                {
                                                    "i32.and" or "i64.and" => "bv_and",
                                                    "i32.or" or "i64.or" => "bv_or",
                                                    "i32.xor" or "i64.xor" => "bv_xor",
                                                    "i32.shl" or "i64.shl" => "bv_shl",
                                                    "i32.shr_s" or "i64.shr_s" => "bv_shr_s",
                                                    "i32.shr_u" or "i64.shr_u" => "bv_shr_u",
                                                    "i32.rotl" or "i64.rotl" => "bv_rotl",
                                                    "i32.rotr" or "i64.rotr" => "bv_rotr",
                                                    _ => throw new NotSupportedException(
                                                        $"Unsupported bitwise op: {bn.Op}"
                                                    ),
                                                };
                    
                                                var bitwiseExpr = new BoogieFunctionCall(
                                                    fun,
                                                    new()
                                                    {
                                                        new BoogieIdentifierExpr("$tmp2"),
                                                        new BoogieIdentifierExpr("$tmp1"),
                                                    }
                                                );
                    
                                                body.AddStatement(
                                                    new BoogieCallCmd("push", new() { bitwiseExpr }, new())
                                                );
                                            }
                                            else if (bn.Op is "i32.rem_s" or "i64.rem_s" or "i32.rem_u" or "i64.rem_u")
                                            {
                                                string fun = bn.Op switch
                                                {
                                                    "i32.rem_s" or "i64.rem_s" or "i32.rem_u" or "i64.rem_u" =>
                                                        "int_rem",
                    
                                                    _ => throw new NotSupportedException(
                                                        $"Unsupported remainder op: {bn.Op}"
                                                    ),
                                                };
                    
                                                var remExpr = new BoogieFunctionCall(
                                                    fun,
                                                    new()
                                                    {
                                                        new BoogieIdentifierExpr("$tmp2"),
                                                        new BoogieIdentifierExpr("$tmp1"),
                                                    }
                                                );
                    
                                                body.AddStatement(new BoogieCallCmd("push", new() { remExpr }, new()));
                                            }
                                            else if (bn.Op == "f32.copysign" || bn.Op == "f64.copysign")
                                            {
                                                var expr = new BoogieFunctionCall(
                                                    "copysign_real",
                                                    new()
                                                    {
                                                        new BoogieIdentifierExpr("$tmp2"),
                                                        new BoogieIdentifierExpr("$tmp1"),
                                                    }
                                                );
                    
                                                body.AddStatement(new BoogieCallCmd("push", new() { expr }, new()));
                                            }
                                            else if (bn.Op == "f32.min" || bn.Op == "f64.min")
                                            {
                                                var minExpr = new BoogieFunctionCall(
                                                    "min_real",
                                                    new()
                                                    {
                                                        new BoogieIdentifierExpr("$tmp2"),
                                                        new BoogieIdentifierExpr("$tmp1"),
                                                    }
                                                );
                    
                                                body.AddStatement(new BoogieCallCmd("push", new() { minExpr }, new()));
                                            }
                                            else if (bn.Op == "f32.max" || bn.Op == "f64.max")
                                            {
                                                var maxExpr = new BoogieFunctionCall(
                                                    "max_real",
                                                    new()
                                                    {
                                                        new BoogieIdentifierExpr("$tmp2"),
                                                        new BoogieIdentifierExpr("$tmp1"),
                                                    }
                                                );
                    
                                                body.AddStatement(new BoogieCallCmd("push", new() { maxExpr }, new()));
                                            }
                                            else
                                            {
                                                body.AddStatement(
                                                    new BoogieCommentCmd($"// unsupported binary op: {bn.Op}")
                                                );
                                            }
                                            break;
                                        }*/

                    case BinaryOpNode bn:
                    {
                        TranslateNode(bn.Left, body);
                        TranslateNode(bn.Right, body);

                        if (IsTypedArithmeticOp(bn.Op))
                        {
                            EmitTypedBinaryArithmetic(bn.Op, body);
                        }
                        else if (IsTypedIntegerDivRemOp(bn.Op))
                        {
                            EmitTypedIntegerDivRem(bn.Op, body);
                        }
                        else if (IsTypedComparisonOp(bn.Op))
                        {
                            EmitTypedComparison(bn.Op, body);
                        }
                        else if (IsTypedFloatBinaryMathOp(bn.Op))
                        {
                            EmitTypedFloatBinaryMathOp(bn.Op, body);
                        }
                        else if (IsTypedIntegerLogicalBitwiseOp(bn.Op))
                        {
                            EmitTypedIntegerLogicalBitwiseOp(bn.Op, body);
                        }
                        else if (IsTypedIntegerShiftOp(bn.Op))
                        {
                            EmitTypedIntegerShiftOp(bn.Op, body);
                        }
                        else if (IsTypedIntegerRotateOp(bn.Op))
                        {
                            EmitTypedIntegerRotateOp(bn.Op, body);
                        }
                        else
                        {
                            body.AddStatement(
                                new BoogieCommentCmd(
                                    $"// unsupported binary op in typed stage: {bn.Op}"
                                )
                            );
                        }

                        break;
                    }

                    case BlockNode blk:
                    {
                        if (blk.Label == null || blk.Label == "module" || blk.Label == "func")
                        {
                            foreach (var child in blk.Body)
                                TranslateNode(child, body);
                            break;
                        }

                        if (blk.Label == "type")
                            break;

                        string? wat =
                            blk.Label != null && blk.Label.StartsWith("$")
                                ? blk.Label.Substring(1)
                                : null;

                        var ctx = new LabelContext
                        {
                            WatLabel = wat,
                            IsLoop = false,
                            StartLabel = null,
                            EndLabel = GenerateLabel(wat != null ? $"{wat}_end" : "block_end"),
                        };

                        labelStack.Push(ctx);

                        foreach (var child in blk.Body)
                            TranslateNode(child, body);

                        body.AddStatement(new BoogieSkipCmd(ctx.EndLabel + ":"));
                        labelStack.Pop();
                        break;
                    }

                    case LoopNode loop:
                    {
                        string? wat =
                            loop.Label != null && loop.Label.StartsWith("$")
                                ? loop.Label.Substring(1)
                                : null;

                        var ctx = new LabelContext
                        {
                            WatLabel = wat,
                            IsLoop = true,
                            StartLabel = GenerateLabel(wat != null ? $"{wat}_start" : "loop_start"),
                            EndLabel = GenerateLabel(wat != null ? $"{wat}_end" : "loop_end"),
                        };
                        labelStack.Push(ctx);

                        body.AddStatement(new BoogieSkipCmd(ctx.StartLabel + ":")); // continue
                        foreach (var child in loop.Body)
                            TranslateNode(child, body);
                        body.AddStatement(new BoogieSkipCmd(ctx.EndLabel + ":")); // break

                        labelStack.Pop();
                        break;
                    }

                    case BrNode br:
                    {
                        var target = ResolveBranchTargetLabel(br.Label);
                        body.AddStatement(new BoogieGotoCmd(target));
                        break;
                    }

                    case BrIfNode brIf:
                    {
                        // Evaluate condition
                        TranslateNode(brIf.Condition, body);

                        // condition -> tmp1
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        // Typed WebAssembly i32 condition
                        var cond = ExtractI32Condition(body, Tmp1());

                        var target = ResolveBranchTargetLabel(brIf.Label);

                        var thenBlk = new BoogieStmtList();

                        thenBlk.AddStatement(new BoogieGotoCmd(target));

                        body.AddStatement(new BoogieIfCmd(cond, thenBlk, null));

                        break;
                    }

                    case BrTableNode bt:
                    {
                        // ============================================================
                        // Evaluate br_table selector
                        // ============================================================

                        if (bt.Selector != null)
                        {
                            TranslateNode(bt.Selector, body);
                        }

                        // selector -> tmp1
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        // WebAssembly br_table selector must be i32
                        body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I32")));

                        var selector = Field(Tmp1(), "value_i32");

                        body.AddStatement(new BoogieAssertCmd(IsU32(selector)));

                        // idx := unsigned i32 payload
                        body.AddStatement(
                            new BoogieAssignCmd(new BoogieIdentifierExpr("idx"), selector)
                        );

                        // ============================================================
                        // Existing br_table dispatch
                        // ============================================================

                        int k = bt.Targets.Count;

                        var idx = new BoogieIdentifierExpr("idx");

                        var outCond = new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.OR,
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.LT,
                                idx,
                                new BoogieLiteralExpr(0)
                            ),
                            new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.GE,
                                idx,
                                new BoogieLiteralExpr(k)
                            )
                        );

                        var outBlk = new BoogieStmtList();

                        outBlk.AddStatement(
                            new BoogieGotoCmd(ResolveBranchTargetLabel(bt.Default))
                        );

                        var inBlk = new BoogieStmtList();

                        for (int i = 0; i < k; i++)
                        {
                            var condEq = new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.EQ,
                                idx,
                                new BoogieLiteralExpr(i)
                            );

                            var thenBlk = new BoogieStmtList();

                            thenBlk.AddStatement(
                                new BoogieGotoCmd(ResolveBranchTargetLabel(bt.Targets[i]))
                            );

                            inBlk.AddStatement(new BoogieIfCmd(condEq, thenBlk, null));
                        }

                        inBlk.AddStatement(new BoogieGotoCmd(ResolveBranchTargetLabel(bt.Default)));

                        body.AddStatement(new BoogieIfCmd(outCond, outBlk, inBlk));

                        break;
                    }

                    case UnreachableNode:
                    {
                        body.AddStatement(new BoogieAssumeCmd(new BoogieLiteralExpr(false)));
                        break;
                    }

                    case SelectNode sel:
                    {
                        // Stack:
                        //
                        // V1
                        // V2
                        // cond
                        //
                        TranslateNode(sel.V1, body);
                        TranslateNode(sel.V2, body);
                        TranslateNode(sel.Cond, body);

                        // cond
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        // V2
                        body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));

                        // V1
                        body.AddStatement(new BoogieCallCmd("popToTmp3", new(), new()));

                        var cond = ExtractI32Condition(body, Tmp1());

                        var thenBlk = new BoogieStmtList();

                        // cond != 0 -> V1
                        thenBlk.AddStatement(
                            new BoogieCallCmd("push", new List<BoogieExpr> { Tmp3() }, new())
                        );

                        var elseBlk = new BoogieStmtList();

                        // cond == 0 -> V2
                        elseBlk.AddStatement(
                            new BoogieCallCmd("push", new List<BoogieExpr> { Tmp2() }, new())
                        );

                        body.AddStatement(new BoogieIfCmd(cond, thenBlk, elseBlk));

                        break;
                    }

                    case IfNode ifn:
                    {
                        // 1) Evaluate condition, then pop it into $tmp1
                        TranslateNode(ifn.Condition, body);

                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        var cond = ExtractI32Condition(body, Tmp1());

                        // --------- CASE A: if-statement (no result) ----------
                        if (ifn.ResultType == null)
                        {
                            var thenBlock = new BoogieStmtList();
                            foreach (var stmt in ifn.ThenBody)
                                TranslateNode(stmt, thenBlock);

                            BoogieStmtList? elseBlock = null;
                            if (ifn.ElseBody != null)
                            {
                                elseBlock = new BoogieStmtList();
                                foreach (var stmt in ifn.ElseBody)
                                    TranslateNode(stmt, elseBlock);
                            }

                            body.AddStatement(new BoogieIfCmd(cond, thenBlock, elseBlock));
                            break;
                        }

                        // If this is an if-expression but one branch returns (does not join),
                        // do NOT build a phi-merge. Just emit it as a normal if-statement.
                        // The return path already pushes the return value and jumps to func_exit.
                        bool thenReturns = ContainsReturn(ifn.ThenBody);
                        bool elseReturns = ifn.ElseBody != null && ContainsReturn(ifn.ElseBody);

                        if (thenReturns || elseReturns)
                        {
                            var thenBlock = new BoogieStmtList();
                            foreach (var stmt in ifn.ThenBody)
                                TranslateNode(stmt, thenBlock);

                            BoogieStmtList? elseBlock = null;
                            if (ifn.ElseBody != null)
                            {
                                elseBlock = new BoogieStmtList();
                                foreach (var stmt in ifn.ElseBody)
                                    TranslateNode(stmt, elseBlock);
                            }

                            body.AddStatement(new BoogieIfCmd(cond, thenBlock, elseBlock));
                            break;
                        }

                        // --------- CASE B: if-expression (produces a value on stack) ----------
                        // We must ensure: both branches leave exactly ONE value on the stack,
                        // then we merge it into a variable and push it back once.

                        // We'll use $tmp2 as the merge value holder (or create a fresh local).
                        // Important: each branch must end with a value on the stack.

                        var thenExpr = new BoogieStmtList();
                        foreach (var stmt in ifn.ThenBody)
                            TranslateNode(stmt, thenExpr);
                        // capture branch result
                        thenExpr.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));
                        // store into $tmp3 as "phi" (use tmp3 to avoid clobbering tmp2 later)
                        thenExpr.AddStatement(
                            new BoogieAssignCmd(
                                new BoogieIdentifierExpr("$tmp3"),
                                new BoogieIdentifierExpr("$tmp2")
                            )
                        );

                        var elseExpr = new BoogieStmtList();
                        if (ifn.ElseBody != null)
                        {
                            foreach (var stmt in ifn.ElseBody)
                                TranslateNode(stmt, elseExpr);
                        }
                        else
                        {
                            elseExpr.AddStatement(
                                new BoogieCallCmd("push", new() { UndefValue() }, new())
                            );
                        }
                        elseExpr.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));
                        elseExpr.AddStatement(
                            new BoogieAssignCmd(
                                new BoogieIdentifierExpr("$tmp3"),
                                new BoogieIdentifierExpr("$tmp2")
                            )
                        );

                        body.AddStatement(new BoogieIfCmd(cond, thenExpr, elseExpr));

                        // After the if, push the merged value once
                        body.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieIdentifierExpr("$tmp3") },
                                new()
                            )
                        );

                        break;
                    }

                    case ReturnNode:
                    {
                        if (functionExitLabel == null)
                            functionExitLabel = GenerateLabel("func_exit");
                        body.AddStatement(new BoogieGotoCmd(functionExitLabel));
                        break;
                    }

                    case NopNode:
                    {
                        body.AddStatement(new BoogieSkipCmd());
                        break;
                    }

                    case RawInstructionNode raw:
                    {
                        var s = raw.Instruction;

                        // ignore noise from parsing non-executable syntax
                        if (
                            s.StartsWith("$", StringComparison.Ordinal)
                            || s.Contains("=>", StringComparison.Ordinal)
                            || s
                                is "module"
                                    or "type"
                                    or "func"
                                    or "param"
                                    or "result"
                                    or "mut"
                                    or "global"
                                    or "table"
                                    or "elem"
                        )
                        {
                            // ignore
                        }
                        else
                        {
                            body.AddStatement(
                                new BoogieCommentCmd($"// unhandled raw instruction: {s}")
                            );
                        }
                        break;
                    }
                }
            }
            finally
            {
                translateDepth--;
            }
        }
    }
}
