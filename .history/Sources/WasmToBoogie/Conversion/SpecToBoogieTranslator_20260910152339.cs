using System;
using System.Numerics;
using BoogieAST;
using WasmToBoogie.Spec.Ast.Specifications;

namespace WasmToBoogie.Conversion
{
    public sealed class SpecToBoogieTranslator
    {
        private readonly Func<string, string> resolveIdentifier;

        public SpecToBoogieTranslator(
            Func<string, string> resolveIdentifier)
        {
            this.resolveIdentifier =
                resolveIdentifier
                ?? throw new ArgumentNullException(
                    nameof(resolveIdentifier)
                );
        }

        public BoogieExpr Translate(
            SpecDirective directive)
        {
            ArgumentNullException.ThrowIfNull(directive);

            return Translate(directive.Expression);
        }

        public BoogieExpr Translate(
            SpecExpression expression)
        {
            ArgumentNullException.ThrowIfNull(expression);

            return expression switch
            {
                SpecIntegerExpression integer =>
                    TranslateInteger(integer),

                SpecBooleanExpression boolean =>
                    TranslateBoolean(boolean),

                SpecIdentifierExpression identifier =>
                    TranslateIdentifier(identifier),

                SpecBinaryExpression binary =>
                    TranslateBinary(binary),

                SpecUnaryExpression unary =>
                    TranslateUnary(unary),

                SpecOldExpression old =>
                    TranslateOld(old),

                SpecResultExpression =>
                    TranslateResult(),

                SpecIndexExpression index =>
                    TranslateIndex(index),

                SpecFieldAccessExpression field =>
                    TranslateFieldAccess(field),

                _ => throw new NotSupportedException(
                    $"Unsupported specification expression: " +
                    $"{expression.GetType().Name}"
                )
            };
        }

        private static BoogieExpr TranslateInteger(
            SpecIntegerExpression expression)
        {
            return new BoogieLiteralExpr(
                new BigInteger(expression.Value)
            );
        }

        private static BoogieExpr TranslateBoolean(
            SpecBooleanExpression expression)
        {
            return new BoogieLiteralExpr(
                expression.Value
            );
        }

        private BoogieExpr TranslateIdentifier(
            SpecIdentifierExpression expression)
        {
            string boogieName =
                resolveIdentifier(expression.Name);

            return new BoogieIdentifierExpr(
                boogieName
            );
        }

        private BoogieExpr TranslateBinary(
            SpecBinaryExpression expression)
        {
            BoogieExpr left =
                Translate(expression.Left);

            BoogieExpr right =
                Translate(expression.Right);

            return new BoogieBinaryOperation(
                TranslateBinaryOperator(
                    expression.Operator
                ),
                left,
                right
            );
        }

        private static BoogieBinaryOperation.Opcode
            TranslateBinaryOperator(
                SpecBinaryOperator op)
        {
            return op switch
            {
                SpecBinaryOperator.Add =>
                    BoogieBinaryOperation.Opcode.ADD,

                SpecBinaryOperator.Subtract =>
                    BoogieBinaryOperation.Opcode.SUB,

                SpecBinaryOperator.Multiply =>
                    BoogieBinaryOperation.Opcode.MUL,

                SpecBinaryOperator.Divide =>
                    BoogieBinaryOperation.Opcode.DIV,

                SpecBinaryOperator.Modulo =>
                    BoogieBinaryOperation.Opcode.MOD,

                SpecBinaryOperator.Equal =>
                    BoogieBinaryOperation.Opcode.EQ,

                SpecBinaryOperator.NotEqual =>
                    BoogieBinaryOperation.Opcode.NEQ,

                SpecBinaryOperator.LessThan =>
                    BoogieBinaryOperation.Opcode.LT,

                SpecBinaryOperator.LessThanOrEqual =>
                    BoogieBinaryOperation.Opcode.LE,

                SpecBinaryOperator.GreaterThan =>
                    BoogieBinaryOperation.Opcode.GT,

                SpecBinaryOperator.GreaterThanOrEqual =>
                    BoogieBinaryOperation.Opcode.GE,

                SpecBinaryOperator.LogicalAnd =>
                    BoogieBinaryOperation.Opcode.AND,

                SpecBinaryOperator.LogicalOr =>
                    BoogieBinaryOperation.Opcode.OR,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(op),
                    op,
                    "Unsupported binary operator."
                )
            };
        }

        private BoogieExpr TranslateUnary(
            SpecUnaryExpression expression)
        {
            BoogieUnaryOperation.Opcode opcode =
                expression.Operator switch
                {
                    SpecUnaryOperator.Negate =>
                        BoogieUnaryOperation.Opcode.NEG,

                    SpecUnaryOperator.LogicalNot =>
                        BoogieUnaryOperation.Opcode.NOT,

                    _ => throw new ArgumentOutOfRangeException(
                        nameof(expression.Operator),
                        expression.Operator,
                        "Unsupported unary operator."
                    )
                };

            return new BoogieUnaryOperation(
                opcode,
                Translate(expression.Operand)
            );
        }

        private BoogieExpr TranslateOld(
            SpecOldExpression expression)
        {
            return new BoogieOldExpr(
                Translate(expression.Expression)
            );
        }

        private BoogieExpr TranslateResult()
        {
            return new BoogieIdentifierExpr(
                resolveIdentifier("result")
            );
        }

        private BoogieExpr TranslateIndex(
            SpecIndexExpression expression)
        {
            return new BoogieMapSelect(
                Translate(expression.Target),
                Translate(expression.Index)
            );
        }

        private BoogieExpr TranslateFieldAccess(
            SpecFieldAccessExpression expression)
        {
            return new BoogieDatatypeFieldAccessExpr(
                Translate(expression.Target),
                expression.FieldName
            );
        }
    }
}