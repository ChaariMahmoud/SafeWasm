using System;
using WasmToBoogie.Parser.Ast;

namespace WasmToBoogie.Conversion
{
    internal static class PreludeAutoDetector
    {
        internal sealed class Usage
        {
            public bool UsesMemory;
            public bool UsesNondet;
            public bool UsesBoolCasts;
            public bool UsesNumericCasts;
            public bool UsesMath;
            public bool UsesStackOps;
            public bool UsesCoreGlobals;
        }

        public static (PreludeSection sections, bool enableMemory) ComputeSections(WasmModule m)
        {
            var u = Analyze(m);

            // Chez toi: CoreGlobals + StackOps quasi obligatoires
            var s = PreludeSection.None;

            if (u.UsesCoreGlobals)
                s |= PreludeSection.CoreGlobals;
            if (u.UsesStackOps)
                s |= PreludeSection.StackOps;

            if (u.UsesMemory)
                s |= PreludeSection.Memory;
            if (u.UsesNondet)
                s |= PreludeSection.Nondet;
            if (u.UsesBoolCasts)
                s |= PreludeSection.BoolCasts;
            if (u.UsesNumericCasts)
                s |= PreludeSection.NumericCasts;
            if (u.UsesMath)
                s |= PreludeSection.Math;

            // EnableMemory doit être cohérent avec la présence de la section
            bool enableMemory = u.UsesMemory;

            return (s, enableMemory);
        }

        private static Usage Analyze(WasmModule m)
        {
            var u = new Usage
            {
                UsesCoreGlobals = true,
                UsesStackOps = true, // ta traduction est stack-based partout (push/pop/tmp/sp)
            };

            foreach (var f in m.Functions)
            {
                foreach (var n in f.Body)
                    Walk(n, u);
            }

            return u;
        }

        private static void Walk(WasmNode n, Usage u)
        {
            switch (n)
            {
                case null:
                    return;

                case ConstNode:
                case LocalGetNode:
                case LocalSetNode:
                case LocalTeeNode:
                case GlobalGetNode:
                case GlobalSetNode:
                case ReturnNode:
                case NopNode:
                case UnreachableNode:
                case BrNode:
                    // rien de spécial ici (Core+Stack déjà vrais)
                    break;

                case MemoryOpNode:
                    u.UsesMemory = true;
                    // tes loads/stores utilisent real_to_int / int_to_real / bits32_to_real...
                    u.UsesNumericCasts = true;
                    break;

                case UnaryOpNode un:
                    // math ops
                    if (IsMathUnary(un.Op))
                        u.UsesMath = true;

                    // eqz -> bool_to_real
                    if (un.Op is "i32.eqz" or "i64.eqz")
                        u.UsesBoolCasts = true;

                    // wrap/trunc/convert etc. (à étendre si tu ajoutes)
                    if (LooksLikeNumericCast(un.Op))
                        u.UsesNumericCasts = true;

                    if (un.Operand != null)
                        Walk(un.Operand, u);
                    break;

                case BinaryOpNode bn:
                    // comparaisons -> bool_to_real
                    if (IsComparison(bn.Op))
                        u.UsesBoolCasts = true;

                    // arith -> rien (si tes helpers ne sont pas dans Math)
                    // min/max si tu les mets dans Math
                    if (bn.Op is "f32.min" or "f64.min" or "f32.max" or "f64.max")
                        u.UsesMath = true;

                    Walk(bn.Left, u);
                    Walk(bn.Right, u);
                    break;

                case IfNode iff:
                u.UsesBoolCasts = true;
                    Walk(iff.Condition, u);
                    foreach (var x in iff.ThenBody)
                        Walk(x, u);
                    if (iff.ElseBody != null)
                        foreach (var x in iff.ElseBody)
                            Walk(x, u);
                    break;

                case BlockNode blk:
                    foreach (var x in blk.Body)
                        Walk(x, u);
                    break;

                case LoopNode lp:
                    foreach (var x in lp.Body)
                        Walk(x, u);
                    break;

                case BrIfNode brIf:
                u.UsesBoolCasts = true;
                    Walk(brIf.Condition, u);
                    break;

                case BrTableNode:
                u.UsesBoolCasts = true;
                    // tu utilises idx := real_to_int(...) + ifs
                    u.UsesNumericCasts = true;
                    break;

                case SelectNode sel:
                    // select utilise real_to_bool (donc casts bool)
                    u.UsesBoolCasts = true;
                    Walk(sel.V1, u);
                    Walk(sel.V2, u);
                    Walk(sel.Cond, u);
                    break;

                case CallNode call:
                    foreach (var a in call.Args)
                        Walk(a, u);
                    break;

                case CallIndirectNode ci:
                    // tu utilises nd_real pour resultats indirects
                    u.UsesNondet = true;
                    foreach (var a in ci.Args)
                        Walk(a, u);
                    Walk(ci.CalleeIndex, u);
                    break;

                case ReturnCallNode rc:
                    foreach (var a in rc.Args)
                        Walk(a, u);
                    break;

                case ReturnCallIndirectNode rci:
                    u.UsesNondet = true;
                    foreach (var a in rci.Args)
                        Walk(a, u);
                    Walk(rci.CalleeIndex, u);
                    break;

                case RawInstructionNode:
                    // ignore
                    break;

                default:
                    // safe fallback: si on ne connait pas le noeud, on garde core+stack
                    break;
            }
        }

        private static bool IsMathUnary(string op) =>
            op
                is "f32.abs"
                    or "f64.abs"
                    or "f32.neg"
                    or "f64.neg"
                    or "f32.sqrt"
                    or "f64.sqrt"
                    or "f32.nearest"
                    or "f64.nearest"
                    or "f32.floor"
                    or "f64.floor";

        private static bool IsComparison(string op) =>
            op.Contains(".eq", StringComparison.Ordinal)
            || op.Contains(".ne", StringComparison.Ordinal)
            || op.Contains(".lt", StringComparison.Ordinal)
            || op.Contains(".le", StringComparison.Ordinal)
            || op.Contains(".gt", StringComparison.Ordinal)
            || op.Contains(".ge", StringComparison.Ordinal);

        private static bool LooksLikeNumericCast(string op) =>
            op.Contains("wrap", StringComparison.Ordinal)
            || op.Contains("trunc", StringComparison.Ordinal)
            || op.Contains("convert", StringComparison.Ordinal)
            || op.Contains("reinterpret", StringComparison.Ordinal)
            || op.Contains("extend", StringComparison.Ordinal);
    }
}
