namespace VeriSolRunner
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    internal class CounterexampleExtractor
    {
        private readonly string corralOutputFile;
        private readonly string boogieFile;
        private readonly string summaryFile;

        public CounterexampleExtractor(
            string corralOutputFile = "corral.txt",
            string boogieFile = "",
            string summaryFile = "counterexample_summary.txt"
        )
        {
            this.corralOutputFile = corralOutputFile;
            this.boogieFile = boogieFile;
            this.summaryFile = summaryFile;
        }

        public void GenerateSummary(bool printToConsole = true)
        {
            if (!File.Exists(corralOutputFile))
            {
                Console.WriteLine("⚠️ corral.txt not found, cannot build counterexample summary.");
                return;
            }

            var lines = File.ReadAllLines(corralOutputFile);

            string file = string.IsNullOrWhiteSpace(boogieFile) ? "unknown" : boogieFile;
            string lineNo = "unknown";
            string failingProcedure = "unknown";
            string failingCall = "unknown";
            string reason = "assertion failure";
            string failureKind = "assertion failure";
            string violatedProperty = "unknown";
            string explanation = "No detailed explanation could be reconstructed.";

            var transactionSequence = new List<string>();

            foreach (var line in lines)
            {
                var errMatch = Regex.Match(
                    line,
                    @"(?<file>BoogieOutputs/[^\s]+\.bpl)\((?<line>\d+),\d+\): error PF5001: This assertion can fail"
                );

                if (errMatch.Success)
                {
                    file = errMatch.Groups["file"].Value;
                    lineNo = errMatch.Groups["line"].Value;
                    break;
                }
            }

            foreach (var line in lines)
            {
                var callMatch = Regex.Match(line, @"\(CALL\s+(?<func>[A-Za-z0-9_]+)\)");
                if (callMatch.Success)
                {
                    string func = callMatch.Groups["func"].Value.Trim();

                    if (ShouldKeepInTransactionSequence(func))
                    {
                        transactionSequence.Add(func);
                    }
                }
            }

            if (transactionSequence.Count > 0)
            {
                failingProcedure = transactionSequence[transactionSequence.Count - 1];
            }

            foreach (var line in lines)
            {
                if (line.Contains("ASSERTION FAILS"))
                {
                    reason = "assertion failure";

                    var callFailMatch = Regex.Match(
                        line,
                        @"ASSERTION FAILS\s+call\s+(?<call>.+?)\s*: Ensures"
                    );
                    if (callFailMatch.Success)
                    {
                        failingCall = callFailMatch.Groups["call"].Value.Trim();
                        failureKind = "postcondition violation";
                        break;
                    }

                    var callFailMatch2 = Regex.Match(
                        line,
                        @"ASSERTION FAILS\s+call\s+(?<call>.+)$"
                    );
                    if (callFailMatch2.Success)
                    {
                        failingCall = callFailMatch2.Groups["call"].Value.Trim();
                        break;
                    }
                }
            }

            if (lineNo != "unknown" && File.Exists(file))
            {
                try
                {
                    var boogieLines = File.ReadAllLines(file);

                    if (int.TryParse(lineNo, out int ln))
                    {
                        int index = ln - 1;

                        if (failingCall == "unknown" && index >= 0 && index < boogieLines.Length)
                        {
                            failingCall = boogieLines[index].Trim();
                        }

                        violatedProperty = InferViolatedProperty(boogieLines, index);
                        failureKind = InferFailureKind(failureKind, violatedProperty);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to read Boogie information from {file}: {ex.Message}");
                }
            }

            explanation = BuildExplanation(
                failingProcedure,
                failingCall,
                failureKind,
                violatedProperty,
                transactionSequence
            );

            var shortSummaryLines = new List<string>
            {
                "=== Counterexample Summary ===",
                $"File: {file}",
                $"Line: {lineNo}",
                $"Failing procedure: {failingProcedure}",
                $"Failing call: {failingCall}",
                $"Reason: {reason}",
                $"Transaction sequence: {(transactionSequence.Count > 0 ? string.Join(" -> ", transactionSequence) : "unknown")}",
                "",
                "        See raw Corral output inside corral.txt"
            };

            var detailedSummaryLines = new List<string>
            {
                "=== Counterexample Summary ===",
                $"File: {file}",
                $"Line: {lineNo}",
                $"Failing procedure: {failingProcedure}",
                $"Failing call: {failingCall}",
                $"Reason: {reason}",
                $"Failure kind: {failureKind}",
                $"Violated property: {violatedProperty}",
                $"Transaction sequence: {(transactionSequence.Count > 0 ? string.Join(" -> ", transactionSequence) : "unknown")}",
                "",
                "Explanation:",
                explanation,
                "",
                "        See raw Corral output inside corral.txt"
            };

            File.WriteAllLines(summaryFile, detailedSummaryLines);

            if (printToConsole)
            {
                Console.WriteLine();
                Console.WriteLine("========== COUNTEREXAMPLE SUMMARY ==========");
                foreach (var line in shortSummaryLines)
                {
                    Console.WriteLine(line);
                }
                Console.WriteLine("============================================");
                Console.WriteLine();
            }

            Console.WriteLine($"📝 Detailed counterexample summary written to {summaryFile}");
        }

        private string InferViolatedProperty(string[] boogieLines, int index)
        {
            if (index < 0 || index >= boogieLines.Length)
                return "unknown";

            for (int i = index; i >= Math.Max(0, index - 12); i--)
            {
                string line = boogieLines[i].Trim();

                if (line.StartsWith("assert "))
                    return line;

                if (line.StartsWith("ensures "))
                    return line;

                if (line.Contains("active_requests") && line.Contains("MAX_REQUESTS"))
                    return line;
            }

            for (int i = index; i <= Math.Min(boogieLines.Length - 1, index + 12); i++)
            {
                string line = boogieLines[i].Trim();

                if (line.StartsWith("assert "))
                    return line;

                if (line.StartsWith("ensures "))
                    return line;

                if (line.Contains("active_requests") && line.Contains("MAX_REQUESTS"))
                    return line;
            }

            return "unknown";
        }

        private string InferFailureKind(string currentFailureKind, string violatedProperty)
        {
            if (currentFailureKind != "assertion failure")
                return currentFailureKind;

            if (violatedProperty.StartsWith("ensures "))
                return "postcondition violation";

            if (violatedProperty.StartsWith("requires "))
                return "precondition violation";

            if (violatedProperty.StartsWith("assert "))
                return "assert violation";

            return currentFailureKind;
        }

        private string BuildExplanation(
            string failingProcedure,
            string failingCall,
            string failureKind,
            string violatedProperty,
            List<string> transactionSequence
        )
        {
            string sequenceText = transactionSequence.Count > 0
                ? string.Join(" -> ", transactionSequence)
                : "an unknown transaction sequence";

            if (failureKind == "postcondition violation")
            {
                return
                    $"The execution reaches {failingProcedure} after the transaction sequence {sequenceText}. "
                    + $"The failure occurs while checking the postcondition of this execution path. "
                    + $"The failing call is `{failingCall}`, and the violated property appears to be `{violatedProperty}`. "
                    + $"This means that after the last transaction, the final state no longer satisfies the expected ensures condition.";
            }

            if (failureKind == "precondition violation")
            {
                return
                    $"The execution reaches {failingProcedure} after the transaction sequence {sequenceText}. "
                    + $"The failure is related to a precondition check. "
                    + $"The failing call is `{failingCall}`, and the violated property appears to be `{violatedProperty}`. "
                    + $"This suggests that the state before the operation does not satisfy the assumptions required by the generated Boogie code.";
            }

            if (failureKind == "assert violation")
            {
                return
                    $"The execution reaches {failingProcedure} after the transaction sequence {sequenceText}. "
                    + $"The failing call is `{failingCall}`. "
                    + $"The violated assertion appears to be `{violatedProperty}`. "
                    + $"This means the program reaches a state that contradicts an explicit assertion in the generated Boogie verification conditions.";
            }

            return
                $"The execution reaches {failingProcedure} after the transaction sequence {sequenceText}. "
                + $"The failing call is `{failingCall}`. "
                + $"The violated property appears to be `{violatedProperty}`. "
                + $"This indicates that the generated Boogie execution reaches a state that does not satisfy the expected safety condition.";
        }

        private bool ShouldKeepInTransactionSequence(string func)
        {
            if (string.IsNullOrWhiteSpace(func))
                return false;

            return !func.StartsWith("CorralChoice_")
                && !func.StartsWith("CorralEntry_")
                && !func.StartsWith("BoogieEntry_")
                && !func.StartsWith("InitRuntime")
                && !func.StartsWith("initGlobals")
                && !func.StartsWith("push")
                && !func.StartsWith("pop")
                && !func.StartsWith("popToTmp")
                && !func.StartsWith("popArgs")
                && !func.StartsWith("popDiscard")
                && !func.StartsWith("boogie_si_");
        }
    }
}