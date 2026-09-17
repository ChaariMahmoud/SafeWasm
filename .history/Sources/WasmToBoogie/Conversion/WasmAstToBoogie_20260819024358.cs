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
        private static BoogieExpr Tmp1() => Id("$tmp1");

        private static BoogieExpr Tmp2() => Id("$tmp2");

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

        private static bool IsTypedEqzOp(string op) => op is "i32.eqz" or "i64.eqz";

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
                            new BoogieMapType(BoogieType.Int, BoogieType.Int)
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
                mods.Add(new BoogieGlobalVariable(new BoogieTypedIdent(bname,WasmValueBoogieType())));
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

        private static BoogieExpr IsCtor(BoogieExpr e, string ctor) =>
            new BoogieIsConstructorExpr(e, ctor);

        private static BoogieExpr Field(BoogieExpr e, string field) =>
            new BoogieDatatypeFieldAccessExpr(e, field);

        //private static BoogieIdentifierExpr Id(string x) => new BoogieIdentifierExpr(x);
        private static BoogieLiteralExpr IntLit(int v) =>
            new BoogieLiteralExpr(new System.Numerics.BigInteger(v));

        private void EmitHavocPushArgs(WasmFunction f, BoogieStmtList blk)
        {
            for (int k = 0; k < f.ParamCount; k++)
            {
                blk.AddStatement(new BoogieHavocCmd(Id("argTmp")));

                if (k < f.ParamTypes.Count)
                {
                    var ty = f.ParamTypes[k];

                    if (ty == WasmValueType.I32 || ty == WasmValueType.I64)
                    {
                        blk.AddStatement(
                            new BoogieAssumeCmd(
                                new BoogieBinaryOperation(
                                    BoogieBinaryOperation.Opcode.EQ,
                                    Id("argTmp"),
                                    new BoogieFunctionCall(
                                        "real",
                                        new List<BoogieExpr>
                                        {
                                            new BoogieFunctionCall(
                                                "int",
                                                new List<BoogieExpr> { Id("argTmp") }
                                            ),
                                        }
                                    )
                                )
                            )
                        );
                    }
                }

                blk.AddStatement(
                    new BoogieCallCmd("push", new List<BoogieExpr> { Id("argTmp") }, new())
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

            bool needArgTmp = m.Functions.Any(f => f.ParamCount > 0);
            if (needArgTmp)
                locals.Add(
                    new BoogieLocalVariable(new BoogieTypedIdent("argTmp", WasmValueBoogieType()))
                );

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

            bool needArgTmp = m.Functions.Any(f => f.ParamCount > 0);
            if (needArgTmp)
                locals.Add(
                    new BoogieLocalVariable(new BoogieTypedIdent("argTmp", WasmValueBoogieType()))
                );

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
                return I32(
                    ToU32(
                        new BoogieLiteralExpr(v)
                    )
                );

            break;
        }

        case "i64":
        {
            if (System.Numerics.BigInteger.TryParse(s, out var v))
                return I64(
                    ToU64(
                        new BoogieLiteralExpr(v)
                    )
                );

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
                return F32(
                    ToF32(
                        new BoogieLiteralExpr(new Pfloat(v))
                    )
                );
            }

            break;
        }

case "f64":
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
        return F64(
            ToF64(
                new BoogieLiteralExpr(new Pfloat(v))
            )
        );
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
            new BoogieTypedIdent(
                boogieName,
                WasmValueBoogieType()
            )
        )
    );
}
else
{
    program.Declarations.Add(
        new BoogieConstant(
            new BoogieTypedIdent(
                boogieName,
                WasmValueBoogieType()
            )
        )
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

            foreach (var g in wasmModule.Globals)
            {
                var key = ResolveGlobalKey(g.Index, g.Name);
                string bname = EnsureGlobalDecl(g, key);

                // uniquement mutables
                if (!g.IsMutable)
                    continue;

                mods.Add(new BoogieGlobalVariable(new BoogieTypedIdent(bname, WasmValueBoogieType())));

var init = BuildTypedGlobalInit(g);

if (init != null)
{
    body.AddStatement(
        new BoogieAssignCmd(
            new BoogieIdentifierExpr(bname),
            init
        )
    );

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
        new BoogieAssignCmd(
            new BoogieIdentifierExpr(bname),
            UndefValue()
        )
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
            var p = new BoogieProgram();
            program = p;
moduleSpec = wasmModule.Spec;

specTranslator = moduleSpec is null
    ? null
    : new SpecToBoogieTranslator(
        ResolveSpecIdentifier
    );

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
            foreach (var import in wasmModule.Imports.Where(i => i.Kind == WasmImportKind.Func))
            {
                functionIndexToBoogieName.Add(
                    SanitizeFunctionName(import.InternalName, contractName)
                );

                var (proc, impl) = TranslateImportedFunction(import);
                p.Declarations.Add(proc);
                p.Declarations.Add(impl);
            }
            foreach (var func in wasmModule.Functions)
            {
                functionIndexToBoogieName.Add(SanitizeFunctionName(func.Name, contractName));
                var (proc, impl) = TranslateFunction(func);
                p.Declarations.Add(proc);
                p.Declarations.Add(impl);
            }

            var (choiceP, choiceI) = BuildCorralChoice(wasmModule);
            p.Declarations.Add(choiceP);
            p.Declarations.Add(choiceI);

            var (beP, beI) = BuildBoogieEntry(wasmModule);
            p.Declarations.Add(beP);
            p.Declarations.Add(beI);

            var (ceP, ceI) = BuildCorralEntry(wasmModule);
            p.Declarations.Add(ceP);
            p.Declarations.Add(ceI);

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
                body.AddStatement(new BoogieHavocCmd(Id("$tmp1")));

                if (i < import.ResultTypes.Count)
                {
                    var ty = import.ResultTypes[i];

                    if (ty == WasmValueType.I32 || ty == WasmValueType.I64)
                    {
                        body.AddStatement(
                            new BoogieAssumeCmd(
                                new BoogieBinaryOperation(
                                    BoogieBinaryOperation.Opcode.EQ,
                                    Id("$tmp1"),
                                    new BoogieFunctionCall(
                                        "real",
                                        new List<BoogieExpr>
                                        {
                                            new BoogieFunctionCall(
                                                "int",
                                                new List<BoogieExpr> { Id("$tmp1") }
                                            ),
                                        }
                                    )
                                )
                            )
                        );
                    }
                }

                body.AddStatement(
                    new BoogieCallCmd("push", new List<BoogieExpr> { Id("$tmp1") }, new())
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

            // Locals init 0
            for (int i = n; i < n + m; i++)
                body.AddStatement(new BoogieAssignCmd(indexToId[i], UndefValue()));
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
                            new BoogieMapType(BoogieType.Int, BoogieType.Int)
                        )
                    )
                );

                mods.Add(
                    new BoogieGlobalVariable(new BoogieTypedIdent("$mem_pages", BoogieType.Int))
                );
            }

            if (currentModifiedGlobals != null)
                foreach (var g in currentModifiedGlobals)
                    mods.Add(new BoogieGlobalVariable(new BoogieTypedIdent(g, WasmValueBoogieType())));

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
                        // selector expression is already on stack in your AST model,
                        // but if bt has an expression node, walk it here (depends on your AST)
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

        private int ResolveIndirectResultCount(string? typeUse) => 0;

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
                    case ConstNode cn:
                    {
                        string ty = cn.Type;

                        if (ty == "i32")
                        {
                            if (System.Numerics.BigInteger.TryParse(cn.Value, out var v))
                            {
                                var lit = new BoogieLiteralExpr(v);

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "push",
                                        new List<BoogieExpr> { I32(ToU32(lit)) },
                                        new()
                                    )
                                );
                            }
                        }
                        else if (ty == "i64")
                        {
                            if (System.Numerics.BigInteger.TryParse(cn.Value, out var v))
                            {
                                var lit = new BoogieLiteralExpr(v);

                                body.AddStatement(
                                    new BoogieCallCmd(
                                        "push",
                                        new List<BoogieExpr> { I64(ToU64(lit)) },
                                        new()
                                    )
                                );
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

                    // ===== call_indirect (conservative model) =====
                    case CallIndirectNode ci:
                    {
                        foreach (var a in ci.Args)
                            TranslateNode(a, body);
                        TranslateNode(ci.CalleeIndex, body);

                        int nArgs = ci.Args.Count;
                        EnsurePopDiscardProc(nArgs + 1); // args + idx
                        body.AddStatement(
                            new BoogieCallCmd($"popDiscard{nArgs + 1}", new(), new())
                        );

                        int r = ResolveIndirectResultCount(ci.TypeUse);
                        for (int i = 0; i < r; i++)
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new() { new BoogieFunctionCall("nd_real", new()) },
                                    new()
                                )
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

                    // ===== return_call_indirect (tail-call indirect) =====
                    case ReturnCallIndirectNode rci:
                    {
                        foreach (var a in rci.Args)
                            TranslateNode(a, body);
                        TranslateNode(rci.CalleeIndex, body);

                        int nArgs = rci.Args.Count;
                        EnsurePopDiscardProc(nArgs + 1);
                        body.AddStatement(
                            new BoogieCallCmd($"popDiscard{nArgs + 1}", new(), new())
                        );

                        int r = ResolveIndirectResultCount(rci.TypeUse);
                        for (int i = 0; i < r; i++)
                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new() { new BoogieFunctionCall("nd_real", new()) },
                                    new()
                                )
                            );

                        if (functionExitLabel == null)
                            functionExitLabel = GenerateLabel("func_exit");
                        body.AddStatement(new BoogieGotoCmd(functionExitLabel));
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
                            // pop address into $tmp1
                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                            // idx := real_to_int($tmp1) + offset
                            var addrInt = new BoogieBinaryOperation(
                                BoogieBinaryOperation.Opcode.ADD,
                                new BoogieFunctionCall("real_to_int", new() { (BoogieExpr)Tmp1() }),
                                new BoogieLiteralExpr(mem.Offset)
                            );

                            body.AddStatement(
                                new BoogieAssignCmd(new BoogieIdentifierExpr("idx"), addrInt)
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
                                        new()
                                        {
                                            new BoogieFunctionCall(
                                                "int_to_real",
                                                new() { new BoogieIdentifierExpr("load_i") }
                                            ),
                                        },
                                        new()
                                    )
                                );

                                break;
                            }

                            case "memory.grow":
                            {
                                if (mem.Address != null)
                                    TranslateNode(mem.Address, body);

                                body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                                body.AddStatement(
                                    new BoogieAssignCmd(
                                        new BoogieIdentifierExpr("idx"),
                                        new BoogieFunctionCall(
                                            "real_to_int",
                                            new() { new BoogieIdentifierExpr("$tmp1") }
                                        )
                                    )
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
                                        new()
                                        {
                                            new BoogieFunctionCall(
                                                "int_to_real",
                                                new() { new BoogieIdentifierExpr("load_i") }
                                            ),
                                        },
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
                            // WAT stack order: dst, value, len
                            if (mem.Address != null)
                                TranslateNode(mem.Address, body);

                            if (mem.Value != null)
                                TranslateNode(mem.Value, body);

                            if (mem.Length != null)
                                TranslateNode(mem.Length, body);

                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // len
                            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // value
                            body.AddStatement(new BoogieCallCmd("popToTmp3", new(), new())); // dst

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "memory_fill",
                                    new()
                                    {
                                        new BoogieFunctionCall(
                                            "real_to_int",
                                            new() { new BoogieIdentifierExpr("$tmp3") }
                                        ),
                                        new BoogieFunctionCall(
                                            "real_to_int",
                                            new() { new BoogieIdentifierExpr("$tmp2") }
                                        ),
                                        new BoogieFunctionCall(
                                            "real_to_int",
                                            new() { new BoogieIdentifierExpr("$tmp1") }
                                        ),
                                    },
                                    new()
                                )
                            );

                            break;
                        }
                        if (mem.Op == "memory.copy")
                        {
                            // WAT stack order: dst, src, len
                            if (mem.Address != null)
                                TranslateNode(mem.Address, body);

                            if (mem.Value != null)
                                TranslateNode(mem.Value, body);

                            if (mem.Length != null)
                                TranslateNode(mem.Length, body);

                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // len
                            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // src
                            body.AddStatement(new BoogieCallCmd("popToTmp3", new(), new())); // dst

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "memory_copy",
                                    new()
                                    {
                                        new BoogieFunctionCall(
                                            "real_to_int",
                                            new() { new BoogieIdentifierExpr("$tmp3") }
                                        ),
                                        new BoogieFunctionCall(
                                            "real_to_int",
                                            new() { new BoogieIdentifierExpr("$tmp2") }
                                        ),
                                        new BoogieFunctionCall(
                                            "real_to_int",
                                            new() { new BoogieIdentifierExpr("$tmp1") }
                                        ),
                                    },
                                    new()
                                )
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

                            // store_i := real_to_int($tmp2)
                            body.AddStatement(
                                new BoogieAssignCmd(
                                    new BoogieIdentifierExpr("store_i"),
                                    new BoogieFunctionCall(
                                        "real_to_int",
                                        new() { (BoogieExpr)Tmp2() }
                                    )
                                )
                            );

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
                                PushLoadedIntAsReal();
                                break;

                            case "i64.load":
                                CallRead("mem_read_s64");
                                PushLoadedIntAsReal();
                                break;

                            case "f32.load":
                                CallRead("mem_read_u32");
                                PushLoadedBits32AsReal();
                                break;

                            case "f64.load":
                                CallRead("mem_read_u64");
                                PushLoadedBits64AsReal();
                                break;

                            case "i32.load8_s":
                                CallRead("mem_read_s8");
                                PushLoadedIntAsReal();
                                break;

                            case "i32.load8_u":
                                CallRead("mem_read_u8");
                                PushLoadedIntAsReal();
                                break;

                            case "i32.load16_s":
                                CallRead("mem_read_s16");
                                PushLoadedIntAsReal();
                                break;

                            case "i32.load16_u":
                                CallRead("mem_read_u16");
                                PushLoadedIntAsReal();
                                break;

                            case "i64.load8_s":
                                CallRead("mem_read_s8");
                                PushLoadedIntAsReal();
                                break;

                            case "i64.load8_u":
                                CallRead("mem_read_u8");
                                PushLoadedIntAsReal();
                                break;

                            case "i64.load16_s":
                                CallRead("mem_read_s16");
                                PushLoadedIntAsReal();
                                break;

                            case "i64.load16_u":
                                CallRead("mem_read_u16");
                                PushLoadedIntAsReal();
                                break;

                            case "i64.load32_s":
                                CallRead("mem_read_s32");
                                PushLoadedIntAsReal();
                                break;

                            case "i64.load32_u":
                                CallRead("mem_read_u32");
                                PushLoadedIntAsReal();
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
                                TranslateNode(t.Index, body);

                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                            body.AddStatement(
                                new BoogieAssignCmd(
                                    new BoogieIdentifierExpr("idx"),
                                    new BoogieFunctionCall(
                                        "real_to_int",
                                        new() { new BoogieIdentifierExpr("$tmp1") }
                                    )
                                )
                            );

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "table_get",
                                    new() { new BoogieIdentifierExpr("idx") },
                                    new() { new BoogieIdentifierExpr("$tmp1") }
                                )
                            );

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new() { new BoogieIdentifierExpr("$tmp1") },
                                    new()
                                )
                            );

                            break;
                        }

                        if (t.Op == "table.set")
                        {
                            if (t.Index != null)
                                TranslateNode(t.Index, body);

                            if (t.Value != null)
                                TranslateNode(t.Value, body);

                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // value
                            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // index

                            body.AddStatement(
                                new BoogieAssignCmd(
                                    new BoogieIdentifierExpr("idx"),
                                    new BoogieFunctionCall(
                                        "real_to_int",
                                        new() { new BoogieIdentifierExpr("$tmp2") }
                                    )
                                )
                            );

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "table_set",
                                    new()
                                    {
                                        new BoogieIdentifierExpr("idx"),
                                        new BoogieIdentifierExpr("$tmp1"),
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
                                    new() { new BoogieIdentifierExpr("idx") }
                                )
                            );

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new()
                                    {
                                        new BoogieFunctionCall(
                                            "int_to_real",
                                            new() { new BoogieIdentifierExpr("idx") }
                                        ),
                                    },
                                    new()
                                )
                            );

                            break;
                        }

                        if (t.Op == "table.grow")
                        {
                            if (t.Value != null)
                                TranslateNode(t.Value, body);

                            if (t.Delta != null)
                                TranslateNode(t.Delta, body);

                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new())); // delta
                            body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new())); // value

                            body.AddStatement(
                                new BoogieAssignCmd(
                                    new BoogieIdentifierExpr("idx"),
                                    new BoogieFunctionCall(
                                        "real_to_int",
                                        new() { new BoogieIdentifierExpr("$tmp1") }
                                    )
                                )
                            );

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "table_grow",
                                    new()
                                    {
                                        new BoogieIdentifierExpr("$tmp2"),
                                        new BoogieIdentifierExpr("idx"),
                                    },
                                    new() { new BoogieIdentifierExpr("load_i") }
                                )
                            );

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new()
                                    {
                                        new BoogieFunctionCall(
                                            "int_to_real",
                                            new() { new BoogieIdentifierExpr("load_i") }
                                        ),
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
                        }
                        else if (IsTypedEqzOp(un.Op))
                        {
                            EmitTypedEqz(un.Op, body);
                        }
                        else if (IsTypedFloatUnaryOp(un.Op))
                        {
                            EmitTypedFloatUnary(un.Op, body);
                        }
                        else if (un.Op == "i32.wrap_i64")
                        {
                            body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                            body.AddStatement(new BoogieAssertCmd(IsCtor(Tmp1(), "I64")));

                            var value = Field(Tmp1(), "value_i64");

                            body.AddStatement(new BoogieAssertCmd(IsU64(value)));

                            body.AddStatement(
                                new BoogieCallCmd(
                                    "push",
                                    new List<BoogieExpr> { I32(ToU32(value)) },
                                    new()
                                )
                            );
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
                        else if (IsTypedComparisonOp(bn.Op))
                        {
                            EmitTypedComparison(bn.Op, body);
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
                        TranslateNode(brIf.Condition, body);
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        var target = ResolveBranchTargetLabel(brIf.Label);
                        var thenBlk = new BoogieStmtList();
                        thenBlk.AddStatement(new BoogieGotoCmd(target));
                        var cond = new BoogieFunctionCall(
                            "real_to_bool",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );
                        body.AddStatement(new BoogieIfCmd(cond, thenBlk, null));
                        break;
                    }

                    case BrTableNode bt:
                    {
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        body.AddStatement(
                            new BoogieAssignCmd(
                                new BoogieIdentifierExpr("idx"),
                                new BoogieFunctionCall(
                                    "real_to_int",
                                    new() { new BoogieIdentifierExpr("$tmp1") }
                                )
                            )
                        );

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
                        TranslateNode(sel.V1, body);
                        TranslateNode(sel.V2, body);
                        TranslateNode(sel.Cond, body);

                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));
                        body.AddStatement(new BoogieCallCmd("popToTmp2", new(), new()));
                        body.AddStatement(new BoogieCallCmd("popToTmp3", new(), new()));

                        var cond = new BoogieFunctionCall(
                            "real_to_bool",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );

                        var thenBlk = new BoogieStmtList();
                        thenBlk.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieIdentifierExpr("$tmp3") },
                                new()
                            )
                        );

                        var elseBlk = new BoogieStmtList();
                        elseBlk.AddStatement(
                            new BoogieCallCmd(
                                "push",
                                new() { new BoogieIdentifierExpr("$tmp2") },
                                new()
                            )
                        );

                        body.AddStatement(new BoogieIfCmd(cond, thenBlk, elseBlk));
                        break;
                    }

                    case IfNode ifn:
                    {
                        // 1) Evaluate condition, then pop it into $tmp1
                        TranslateNode(ifn.Condition, body);
                        body.AddStatement(new BoogieCallCmd("popToTmp1", new(), new()));

                        var cond = new BoogieFunctionCall(
                            "real_to_bool",
                            new() { new BoogieIdentifierExpr("$tmp1") }
                        );

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
