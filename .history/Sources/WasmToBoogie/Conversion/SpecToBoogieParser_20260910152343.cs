using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BoogieAST;
using WasmToBoogie.Parser.Ast;
using WasmToBoogie.Spec.Ast.Specifications;
using WasmToBoogie.Spec.Specifications;

namespace WasmToBoogie.Conversion
{
    public sealed class SpecToBoogieParser
    {
        private readonly Func<string, string> resolveGlobal;

        public SpecToBoogieParser(Func<string, string> resolveGlobal)
        {
            this.resolveGlobal = resolveGlobal;
        }

public BoogieExpr Parse(SpecDirective directive)
{
    ArgumentNullException.ThrowIfNull(directive);

    string expr = SpecAstPrinter.Print(directive.Expression);

    return Parse(expr);
}
        public BoogieExpr Parse(string expr)
        {
            expr = expr.Trim();
            return ParseAnd(expr);
        }

        private BoogieExpr ParseAnd(string expr)
        {
            var parts = SplitTopLevel(expr, "&&");

            BoogieExpr? result = null;

            foreach (var p in parts)
            {
                var sub = ParseComparison(p.Trim());

                result =
                    result == null
                        ? sub
                        : new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.AND, result, sub);
            }

            return result!;
        }

        private BoogieExpr ParseComparison(string s)
        {
            string[] ops = { "<=", ">=", "==", "<", ">" };

            foreach (var op in ops)
            {
                int idx = s.IndexOf(op);
                if (idx > 0)
                {
                    var left = s[..idx].Trim();
                    var right = s[(idx + op.Length)..].Trim();

                    var l = ParseTerm(left);
                    var r = ParseTerm(right);

                    var opcode = op switch
                    {
                        "<=" => BoogieBinaryOperation.Opcode.LE,
                        ">=" => BoogieBinaryOperation.Opcode.GE,
                        "<" => BoogieBinaryOperation.Opcode.LT,
                        ">" => BoogieBinaryOperation.Opcode.GT,
                        "==" => BoogieBinaryOperation.Opcode.EQ,
                        _ => throw new Exception("Unknown operator"),
                    };

                    return new BoogieBinaryOperation(opcode, l, r);
                }
            }

            throw new Exception($"Unsupported spec: {s}");
        }

        private BoogieExpr ParseTerm(string s)
        {
            if (
                double.TryParse(
                    s,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var dv
                )
            )
            {
                return new BoogieLiteralExpr(new Pfloat((float)dv));
            }

            // otherwise global variable
            if (s == "true")
                return new BoogieLiteralExpr(true);

            if (s == "false")
                return new BoogieLiteralExpr(false);
            string boogieName = resolveGlobal(s);
            return new BoogieIdentifierExpr(boogieName);
        }

        private static List<string> SplitTopLevel(string expr, string sep)
        {
            var parts = new List<string>();
            int depth = 0;
            int last = 0;

            for (int i = 0; i < expr.Length - sep.Length + 1; i++)
            {
                if (expr[i] == '(')
                    depth++;
                if (expr[i] == ')')
                    depth--;

                if (depth == 0 && expr.Substring(i).StartsWith(sep))
                {
                    parts.Add(expr.Substring(last, i - last));
                    last = i + sep.Length;
                }
            }

            parts.Add(expr.Substring(last));
            return parts;
        }
    }
}
