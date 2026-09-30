using System;
using System.Collections.Generic;
using System.Linq;

namespace WasmToBoogie.Parser.Ast
{
    public record WasmCoverageResult(
        int Total,
        int Supported,
        int Unsupported,
        Dictionary<string, int> UnsupportedOps
    );

    public static class WasmCoverageAnalyzer
    {
        public static WasmCoverageResult Analyze(WasmModule module)
        {
            int total = 0;
            int supported = 0;
            int unsupported = 0;

            var unsupportedOps = new Dictionary<string, int>();

            foreach (var g in module.Globals)
            {
                if (g.InitConst != null)
                {
                    total++;
                    supported++;
                }
            }

            foreach (var f in module.Functions)
            {
                foreach (var n in f.Body)
                    Visit(n);
            }

            return new WasmCoverageResult(total, supported, unsupported, unsupportedOps);

            void CountSupported()
            {
                total++;
                supported++;
            }

            void CountUnsupported(string op)
            {
                total++;
                unsupported++;

                if (string.IsNullOrWhiteSpace(op))
                    op = "<unknown>";

                unsupportedOps[op] = unsupportedOps.GetValueOrDefault(op) + 1;
            }

            void Visit(WasmNode node)
            {
                switch (node)
                {
                    case ConstNode:
                    case LocalGetNode:
                    case LocalTeeNode:
                    case GlobalGetNode:
                    case BrNode:
                    case ReturnNode:
                    case NopNode:
                    case UnreachableNode:
                        CountSupported();
                        break;

case UnaryOpNode u:
    if (WasmInstructionSupport.IsUnarySupported(u.Op))
        CountSupported();
    else
        CountUnsupported(u.Op);

    if (u.Operand != null)
        Visit(u.Operand);

    break;

                    case BinaryOpNode b:
                        CountSupported();
                        if (b.Left != null)
                            Visit(b.Left);
                        if (b.Right != null)
                            Visit(b.Right);
                        break;

                    case IfNode i:
                        CountSupported();
                        if (i.Condition != null)
                            Visit(i.Condition);
                        foreach (var x in i.ThenBody)
                            Visit(x);
                        if (i.ElseBody != null)
                            foreach (var x in i.ElseBody)
                                Visit(x);
                        break;

                    case BlockNode b:
                        CountSupported();
                        foreach (var x in b.Body)
                            Visit(x);
                        break;

                    case LoopNode l:
                        CountSupported();
                        foreach (var x in l.Body)
                            Visit(x);
                        break;

                    case BrIfNode b:
                        CountSupported();
                        if (b.Condition != null)
                            Visit(b.Condition);
                        break;

                    case BrTableNode b:
                        CountSupported();
                        if (b.Selector != null)
                            Visit(b.Selector);
                        break;

                    case LocalSetNode s:
                        CountSupported();
                        if (s.Value != null)
                            Visit(s.Value);
                        break;

                    case GlobalSetNode s:
                        CountSupported();
                        if (s.Value != null)
                            Visit(s.Value);
                        break;

                    case CallNode c:
                        CountSupported();
                        foreach (var a in c.Args)
                            Visit(a);
                        break;

                    case CallIndirectNode c:
                        CountSupported();
                        foreach (var a in c.Args)
                            Visit(a);
                        if (c.CalleeIndex != null)
                            Visit(c.CalleeIndex);
                        break;

                    case ReturnCallNode c:
                        CountSupported();
                        foreach (var a in c.Args)
                            Visit(a);
                        break;

                    case ReturnCallIndirectNode c:
                        CountSupported();
                        foreach (var a in c.Args)
                            Visit(a);
                        if (c.CalleeIndex != null)
                            Visit(c.CalleeIndex);
                        break;

                    case SelectNode s:
                        CountSupported();
                        if (s.V1 != null)
                            Visit(s.V1);
                        if (s.V2 != null)
                            Visit(s.V2);
                        if (s.Cond != null)
                            Visit(s.Cond);
                        break;

                    case MemoryOpNode m:
                        CountSupported();
                        if (m.Address != null)
                            Visit(m.Address);
                        if (m.Value != null)
                            Visit(m.Value);
                        if (m.Length != null)
                            Visit(m.Length);
                        break;

                    case TableOpNode t:
                        CountSupported();
                        if (t.Index != null)
                            Visit(t.Index);
                        if (t.Value != null)
                            Visit(t.Value);
                        if (t.Delta != null)
                            Visit(t.Delta);
                        break;

                    case RawInstructionNode r:
                        CountUnsupported(r.Instruction);
                        break;

                    default:
                        CountUnsupported(node.GetType().Name);
                        break;
                }
            }
        }

        public static void Print(WasmCoverageResult r)
        {
            Console.WriteLine($"COVERAGE_TOTAL={r.Total}");
            Console.WriteLine($"COVERAGE_SUPPORTED={r.Supported}");
            Console.WriteLine($"COVERAGE_UNSUPPORTED={r.Unsupported}");

            string ops = string.Join(
                "; ",
                r.UnsupportedOps.OrderByDescending(x => x.Value)
                    .ThenBy(x => x.Key)
                    .Select(x => $"{x.Key}:{x.Value}")
            );

            Console.WriteLine($"COVERAGE_UNSUPPORTED_OPS={ops}");
        }
    }
}
