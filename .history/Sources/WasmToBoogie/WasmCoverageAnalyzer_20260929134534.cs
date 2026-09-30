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

            var unsupportedOps = new Dictionary<string, int>(StringComparer.Ordinal);

            /*
             * Les expressions constantes d'initialisation
             * des variables globales sont supportées.
             */
            foreach (var global in module.Globals)
            {
                if (global.InitConst != null)
                {
                    CountSupported();
                }
            }

            /*
             * Analyse récursive de toutes les fonctions définies
             * dans le module.
             */
            foreach (var function in module.Functions)
            {
                foreach (var node in function.Body)
                {
                    Visit(node);
                }
            }

            return new WasmCoverageResult(total, supported, unsupported, unsupportedOps);

            void CountSupported()
            {
                total++;
                supported++;
            }

            void CountUnsupported(string? operation)
            {
                total++;
                unsupported++;

                string name = string.IsNullOrWhiteSpace(operation) ? "<unknown>" : operation;

                unsupportedOps[name] = unsupportedOps.GetValueOrDefault(name) + 1;
            }

            void Visit(WasmNode node)
            {
                switch (node)
                {
                    /*
                     * Instructions simples sans enfant.
                     */
                    case ConstNode:
                    case RefNullNode:
                    case RefFuncNode:
                    case LocalGetNode:
                    case LocalTeeNode:
                    case GlobalGetNode:
                    case BrNode:
                    case ReturnNode:
                    case NopNode:
                    case UnreachableNode:
                        CountSupported();
                        break;

                    /*
                     * Opérations unaires.
                     */
                    case UnaryOpNode unary:
                        if (WasmInstructionSupport.IsUnarySupported(unary.Op))
                        {
                            CountSupported();
                        }
                        else
                        {
                            CountUnsupported(unary.Op);
                        }

                        if (unary.Operand != null)
                        {
                            Visit(unary.Operand);
                        }

                        break;

                    /*
                     * Opérations binaires.
                     */
                    case BinaryOpNode binary:
                        if (WasmInstructionSupport.IsBinarySupported(binary.Op))
                        {
                            CountSupported();
                        }
                        else
                        {
                            CountUnsupported(binary.Op);
                        }

                        if (binary.Left != null)
                        {
                            Visit(binary.Left);
                        }

                        if (binary.Right != null)
                        {
                            Visit(binary.Right);
                        }

                        break;

                    /*
                     * Condition structurée.
                     */
                    case IfNode ifNode:
                        CountSupported();

                        if (ifNode.Condition != null)
                        {
                            Visit(ifNode.Condition);
                        }

                        foreach (var instruction in ifNode.ThenBody)
                        {
                            Visit(instruction);
                        }

                        if (ifNode.ElseBody != null)
                        {
                            foreach (var instruction in ifNode.ElseBody)
                            {
                                Visit(instruction);
                            }
                        }

                        break;

                    /*
                     * Bloc structuré.
                     */
                    case BlockNode block:
                        CountSupported();

                        foreach (var instruction in block.Body)
                        {
                            Visit(instruction);
                        }

                        break;

                    /*
                     * Boucle structurée.
                     */
                    case LoopNode loop:
                        CountSupported();

                        foreach (var instruction in loop.Body)
                        {
                            Visit(instruction);
                        }

                        break;

                    /*
                     * Branchement conditionnel.
                     */
                    case BrIfNode branch:
                        CountSupported();

                        if (branch.Condition != null)
                        {
                            Visit(branch.Condition);
                        }

                        break;

                    /*
                     * Branchement multiple.
                     */
                    case BrTableNode branchTable:
                        CountSupported();

                        if (branchTable.Selector != null)
                        {
                            Visit(branchTable.Selector);
                        }

                        break;

                    /*
                     * Écriture d'une variable locale.
                     */
                    case LocalSetNode localSet:
                        CountSupported();

                        if (localSet.Value != null)
                        {
                            Visit(localSet.Value);
                        }

                        break;

                    /*
                     * Écriture d'une variable globale.
                     */
                    case GlobalSetNode globalSet:
                        CountSupported();

                        if (globalSet.Value != null)
                        {
                            Visit(globalSet.Value);
                        }

                        break;

                    /*
                     * Appel direct.
                     */
                    case CallNode call:
                        CountSupported();

                        foreach (var argument in call.Args)
                        {
                            Visit(argument);
                        }

                        break;

                    /*
                     * Appel indirect.
                     */
                    case CallIndirectNode indirectCall:
                        CountSupported();

                        foreach (var argument in indirectCall.Args)
                        {
                            Visit(argument);
                        }

                        if (indirectCall.CalleeIndex != null)
                        {
                            Visit(indirectCall.CalleeIndex);
                        }

                        break;

                    /*
                     * Appel terminal direct.
                     */
                    case ReturnCallNode returnCall:
                        CountSupported();

                        foreach (var argument in returnCall.Args)
                        {
                            Visit(argument);
                        }

                        break;

                    /*
                     * Appel terminal indirect.
                     *
                     * Le backend possède une traduction, même si le
                     * frontend WABT/Binaryen peut refuser certains
                     * fichiers qui utilisent cette instruction.
                     */
                    case ReturnCallIndirectNode returnIndirect:
                        CountSupported();

                        foreach (var argument in returnIndirect.Args)
                        {
                            Visit(argument);
                        }

                        if (returnIndirect.CalleeIndex != null)
                        {
                            Visit(returnIndirect.CalleeIndex);
                        }

                        break;

                    /*
                     * Sélection conditionnelle.
                     */
                    case SelectNode select:
                        CountSupported();

                        if (select.V1 != null)
                        {
                            Visit(select.V1);
                        }

                        if (select.V2 != null)
                        {
                            Visit(select.V2);
                        }

                        if (select.Cond != null)
                        {
                            Visit(select.Cond);
                        }

                        break;

                    /*
                     * Opérations mémoire.
                     */
                    case MemoryOpNode memory:
                        if (WasmInstructionSupport.IsMemorySupported(memory.Op))
                        {
                            CountSupported();
                        }
                        else
                        {
                            CountUnsupported(memory.Op);
                        }

                        if (memory.Address != null)
                        {
                            Visit(memory.Address);
                        }

                        if (memory.Value != null)
                        {
                            Visit(memory.Value);
                        }

                        if (memory.Length != null)
                        {
                            Visit(memory.Length);
                        }

                        break;

                    /*
                     * Opérations sur les tables.
                     */
                    case TableOpNode table:
                        if (WasmInstructionSupport.IsTableSupported(table.Op))
                        {
                            CountSupported();
                        }
                        else
                        {
                            CountUnsupported(table.Op);
                        }

                        if (table.Index != null)
                        {
                            Visit(table.Index);
                        }

                        if (table.Value != null)
                        {
                            Visit(table.Value);
                        }

                        if (table.Delta != null)
                        {
                            Visit(table.Delta);
                        }

                        break;

                    /*
                     * Instruction restée sous forme textuelle :
                     * elle n'est pas supportée par le traducteur typé.
                     */
                    case RawInstructionNode raw:
                        CountUnsupported(raw.Instruction);
                        break;

                    /*
                     * Nouveau type de nœud non pris en compte.
                     */
                    default:
                        CountUnsupported(node.GetType().Name);
                        break;
                }
            }
        }

        public static void Print(WasmCoverageResult result)
        {
            double percentage = result.Total == 0 ? 100.0 : 100.0 * result.Supported / result.Total;

            Console.WriteLine($"COVERAGE_TOTAL={result.Total}");

            Console.WriteLine($"COVERAGE_SUPPORTED={result.Supported}");

            Console.WriteLine($"COVERAGE_UNSUPPORTED={result.Unsupported}");

            Console.WriteLine($"COVERAGE_PERCENT={percentage:F2}");

            string operations = string.Join(
                "; ",
                result
                    .UnsupportedOps.OrderByDescending(item => item.Value)
                    .ThenBy(item => item.Key, StringComparer.Ordinal)
                    .Select(item => $"{item.Key}:{item.Value}")
            );

            Console.WriteLine($"COVERAGE_UNSUPPORTED_OPS={operations}");
        }
    }
}
