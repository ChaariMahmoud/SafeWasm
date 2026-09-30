using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace VeriSolRunner
{
    public sealed record DatasetFileCoverage(
        string File,
        string Status,
        int Total,
        int Supported,
        int Unsupported,
        double Percentage,
        string UnsupportedOps,
        string? Error
    );

    public static class DatasetCoverageRunner
    {
        public static int Run(
            string datasetDirectory,
            int timeoutSeconds = 120
        )
        {
            if (
                string.IsNullOrWhiteSpace(datasetDirectory)
                || !Directory.Exists(datasetDirectory)
            )
            {
                Console.Error.WriteLine(
                    $"Dataset directory not found: {datasetDirectory}"
                );

                return 1;
            }

            /*
             * On récupère tous les fichiers, puis on filtre explicitement
             * sur l'extension .wat.
             *
             * Les fichiers .wasm sont donc ignorés.
             */
            var watFiles =
                Directory
                    .EnumerateFiles(
                        datasetDirectory,
                        "*",
                        SearchOption.AllDirectories
                    )
                    .Where(
                        file =>
                            string.Equals(
                                Path.GetExtension(file),
                                ".wat",
                                StringComparison.OrdinalIgnoreCase
                            )
                    )
                    .OrderBy(
                        file => file,
                        StringComparer.Ordinal
                    )
                    .ToList();

            Console.WriteLine(
                $"DATASET_DIRECTORY={Path.GetFullPath(datasetDirectory)}"
            );

            Console.WriteLine(
                $"DATASET_WAT_FILES={watFiles.Count}"
            );

            var results =
                new List<DatasetFileCoverage>(
                    watFiles.Count
                );

            int current = 0;

            foreach (string watFile in watFiles)
            {
                current++;

                Console.WriteLine();
                Console.WriteLine(
                    $"[{current}/{watFiles.Count}] {watFile}"
                );

                DatasetFileCoverage result =
                    AnalyzeFile(
                        watFile,
                        timeoutSeconds
                    );

                results.Add(result);

                Console.WriteLine(
                    $"  STATUS={result.Status}"
                );

                if (
                    result.Status == "SUPPORTED"
                    || result.Status == "PARTIAL"
                )
                {
                    Console.WriteLine(
                        $"  COVERAGE={result.Percentage:F2}%"
                    );

                    Console.WriteLine(
                        $"  UNSUPPORTED={result.Unsupported}"
                    );

                    if (
                        !string.IsNullOrWhiteSpace(
                            result.UnsupportedOps
                        )
                    )
                    {
                        Console.WriteLine(
                            $"  OPS={result.UnsupportedOps}"
                        );
                    }
                }
                else if (
                    !string.IsNullOrWhiteSpace(result.Error)
                )
                {
                    Console.WriteLine(
                        $"  ERROR={FirstLine(result.Error)}"
                    );
                }
            }

            PrintSummary(results);
            WriteCsv(results, "coverage_dataset.csv");

            return 0;
        }

        private static DatasetFileCoverage AnalyzeFile(
            string watFile,
            int timeoutSeconds
        )
        {
            using var process =
                CreateCoverageProcess(watFile);

            try
            {
                process.Start();
            }
            catch (Exception exception)
            {
                return ErrorResult(
                    watFile,
                    "ERROR",
                    exception.ToString()
                );
            }

            var standardOutputTask =
                process.StandardOutput.ReadToEndAsync();

            var standardErrorTask =
                process.StandardError.ReadToEndAsync();

            bool finished =
                process.WaitForExit(
                    checked(timeoutSeconds * 1000)
                );

            if (!finished)
            {
                try
                {
                    process.Kill(
                        entireProcessTree: true
                    );

                    process.WaitForExit();
                }
                catch
                {
                    // Le processus est peut-être déjà terminé.
                }

                return ErrorResult(
                    watFile,
                    "TIMEOUT",
                    $"Coverage analysis exceeded {timeoutSeconds} seconds."
                );
            }

            string standardOutput =
                standardOutputTask
                    .GetAwaiter()
                    .GetResult();

            string standardError =
                standardErrorTask
                    .GetAwaiter()
                    .GetResult();

            string completeOutput =
                standardOutput
                + Environment.NewLine
                + standardError;

            if (process.ExitCode != 0)
            {
                return ErrorResult(
                    watFile,
                    "ERROR",
                    completeOutput.Trim()
                );
            }

            if (
                !TryGetInt(
                    completeOutput,
                    "COVERAGE_TOTAL",
                    out int total
                )
                || !TryGetInt(
                    completeOutput,
                    "COVERAGE_SUPPORTED",
                    out int supported
                )
                || !TryGetInt(
                    completeOutput,
                    "COVERAGE_UNSUPPORTED",
                    out int unsupported
                )
            )
            {
                return ErrorResult(
                    watFile,
                    "ERROR",
                    "The program did not produce valid coverage information."
                    + Environment.NewLine
                    + completeOutput.Trim()
                );
            }

            double percentage;

            if (
                !TryGetDouble(
                    completeOutput,
                    "COVERAGE_PERCENT",
                    out percentage
                )
            )
            {
                percentage =
                    total == 0
                        ? 100.0
                        : 100.0 * supported / total;
            }

            string unsupportedOps =
                GetValue(
                    completeOutput,
                    "COVERAGE_UNSUPPORTED_OPS"
                ) ?? string.Empty;

            string status =
                unsupported == 0
                    ? "SUPPORTED"
                    : "PARTIAL";

            return new DatasetFileCoverage(
                watFile,
                status,
                total,
                supported,
                unsupported,
                percentage,
                unsupportedOps,
                null
            );
        }

        private static Process CreateCoverageProcess(
            string watFile
        )
        {
            string assemblyPath =
                Assembly
                    .GetExecutingAssembly()
                    .Location;

            string processPath =
                Environment.ProcessPath
                ?? throw new InvalidOperationException(
                    "Unable to determine the current process."
                );

            var startInfo =
                new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

            /*
             * Cas normal :
             *
             * dotnet bin/Debug/VeriSol.dll ...
             *
             * Environment.ProcessPath correspond alors à dotnet.
             */
            if (
                string.Equals(
                    Path.GetFileNameWithoutExtension(processPath),
                    "dotnet",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                startInfo.FileName = processPath;
                startInfo.ArgumentList.Add(assemblyPath);
            }
            else
            {
                /*
                 * Cas d'une application publiée comme exécutable.
                 */
                startInfo.FileName = processPath;
            }

            startInfo.ArgumentList.Add("--wasm");
            startInfo.ArgumentList.Add(watFile);
            startInfo.ArgumentList.Add("--coverage-only");

            return new Process
            {
                StartInfo = startInfo,
            };
        }

        private static DatasetFileCoverage ErrorResult(
            string file,
            string status,
            string error
        )
        {
            return new DatasetFileCoverage(
                file,
                status,
                0,
                0,
                0,
                0.0,
                string.Empty,
                error
            );
        }

        private static void PrintSummary(
            IReadOnlyCollection<DatasetFileCoverage> results
        )
        {
            int supportedFiles =
                results.Count(
                    result =>
                        result.Status == "SUPPORTED"
                );

            int partialFiles =
                results.Count(
                    result =>
                        result.Status == "PARTIAL"
                );

            int errorFiles =
                results.Count(
                    result =>
                        result.Status == "ERROR"
                );

            int timeoutFiles =
                results.Count(
                    result =>
                        result.Status == "TIMEOUT"
                );

            int totalInstructions =
                results.Sum(result => result.Total);

            int supportedInstructions =
                results.Sum(result => result.Supported);

            int unsupportedInstructions =
                results.Sum(result => result.Unsupported);

            double instructionCoverage =
                totalInstructions == 0
                    ? 0.0
                    : 100.0
                        * supportedInstructions
                        / totalInstructions;

            double fileCoverage =
                results.Count == 0
                    ? 0.0
                    : 100.0
                        * supportedFiles
                        / results.Count;

            Console.WriteLine();
            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                "          DATASET COVERAGE"
            );

            Console.WriteLine(
                "========================================"
            );

            Console.WriteLine(
                $"WAT_FILES={results.Count}"
            );

            Console.WriteLine(
                $"FULLY_SUPPORTED_FILES={supportedFiles}"
            );

            Console.WriteLine(
                $"PARTIALLY_SUPPORTED_FILES={partialFiles}"
            );

            Console.WriteLine(
                $"ERROR_FILES={errorFiles}"
            );

            Console.WriteLine(
                $"TIMEOUT_FILES={timeoutFiles}"
            );

            Console.WriteLine(
                $"FILE_COVERAGE_PERCENT={fileCoverage:F2}"
            );

            Console.WriteLine(
                $"TOTAL_INSTRUCTIONS={totalInstructions}"
            );

            Console.WriteLine(
                $"SUPPORTED_INSTRUCTIONS={supportedInstructions}"
            );

            Console.WriteLine(
                $"UNSUPPORTED_INSTRUCTIONS={unsupportedInstructions}"
            );

            Console.WriteLine(
                $"INSTRUCTION_COVERAGE_PERCENT={instructionCoverage:F2}"
            );

            Console.WriteLine(
                "RESULT_FILE=coverage_dataset.csv"
            );
        }

        private static void WriteCsv(
            IEnumerable<DatasetFileCoverage> results,
            string outputFile
        )
        {
            using var writer =
                new StreamWriter(outputFile);

            writer.WriteLine(
                "\"file\",\"status\",\"total\",\"supported\","
                + "\"unsupported\",\"percentage\","
                + "\"unsupported_ops\",\"error\""
            );

            foreach (var result in results)
            {
                writer.WriteLine(
                    string.Join(
                        ",",
                        Csv(result.File),
                        Csv(result.Status),
                        result.Total.ToString(
                            CultureInfo.InvariantCulture
                        ),
                        result.Supported.ToString(
                            CultureInfo.InvariantCulture
                        ),
                        result.Unsupported.ToString(
                            CultureInfo.InvariantCulture
                        ),
                        result.Percentage.ToString(
                            "F2",
                            CultureInfo.InvariantCulture
                        ),
                        Csv(result.UnsupportedOps),
                        Csv(result.Error ?? string.Empty)
                    )
                );
            }
        }

        private static bool TryGetInt(
            string output,
            string key,
            out int value
        )
        {
            string? text =
                GetValue(output, key);

            return int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        private static bool TryGetDouble(
            string output,
            string key,
            out double value
        )
        {
            string? text =
                GetValue(output, key);

            return double.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value
            );
        }

        private static string? GetValue(
            string output,
            string key
        )
        {
            string prefix = key + "=";

            return output
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .Select(line => line.Trim())
                .Where(
                    line =>
                        line.StartsWith(
                            prefix,
                            StringComparison.Ordinal
                        )
                )
                .Select(
                    line => line.Substring(prefix.Length)
                )
                .LastOrDefault();
        }

        private static string Csv(
            string value
        )
        {
            return "\""
                + value.Replace("\"", "\"\"")
                + "\"";
        }

        private static string FirstLine(
            string value
        )
        {
            return value
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n')
                .FirstOrDefault(
                    line =>
                        !string.IsNullOrWhiteSpace(line)
                )
                ?.Trim()
                ?? string.Empty;
        }
    }
}
