namespace VeriSolRunner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using BoogieAST;
    using Microsoft.Extensions.Logging;
    using SharedConfig;
    using SolidityAST;
    using SolToBoogie;
    using VeriSolRunner.ExternalTools;

    internal class VeriSolExecutor
    {
        private string SolidityFilePath;
        private string SolidityFileDir;
        private string ContractName;
        private string CorralPath;
        private string BoogiePath;
        private string SolcPath;
        private bool TryProof;
        private bool TryRefutation;

        private ILogger Logger;
        private string outFileName = "__SolToBoogieTest_out.bpl";
        private readonly string corralTraceFileName = "corral_out_trace.txt";
        private readonly string counterexampleSummaryFileName = "counterexample_summary.txt";
        private readonly int CorralRecursionLimit;
        private readonly int CorralContextBound = 1; // always 1 for solidity
        private HashSet<Tuple<string, string>> ignoreMethods;
        private TranslatorFlags translatorFlags;
        private BoogieProgram program = null;

        public VeriSolExecutor(
            string solidityFilePath,
            string contractName,
            int corralRecursionLimit,
            HashSet<Tuple<string, string>> ignoreMethods,
            bool tryRefutation,
            bool tryProofFlag,
            ILogger logger,
            bool _printTransactionSequence,
            TranslatorFlags _translatorFlags = null
        )
        {
            this.SolidityFilePath = solidityFilePath;
            this.ContractName = contractName;
            this.SolidityFileDir = Path.GetDirectoryName(solidityFilePath);
            Console.WriteLine($"SpecFilesDir = {SolidityFileDir}");

            this.CorralPath = ToolPaths.CorralPath;
            Console.WriteLine($"🔍 Corral path: {this.CorralPath}");

            this.BoogiePath = ToolPaths.BoogiePath;
            Console.WriteLine($"🔍 Boogie path: {this.BoogiePath}");

            this.SolcPath = ToolPaths.SolcPath;
            this.CorralRecursionLimit = corralRecursionLimit;
            this.ignoreMethods = new HashSet<Tuple<string, string>>(ignoreMethods);
            this.Logger = logger;
            this.TryProof = tryProofFlag;
            this.TryRefutation = tryRefutation;
            this.translatorFlags = _translatorFlags ?? new TranslatorFlags();
        }

        // Constructor for WASM mode
        public VeriSolExecutor(
            BoogieProgram program,
            string contractName,
            int corralRecursionLimit,
            HashSet<Tuple<string, string>> ignoreMethods,
            bool tryRefutation,
            bool tryProofFlag,
            ILogger logger
        )
        {
            this.program = program;
            this.ContractName = contractName;
            this.CorralRecursionLimit = corralRecursionLimit;
            this.ignoreMethods = new HashSet<Tuple<string, string>>(ignoreMethods);
            this.Logger = logger;
            this.TryProof = tryProofFlag;
            this.TryRefutation = tryRefutation;
            this.translatorFlags = new TranslatorFlags();

            var baseName = Path.GetFileNameWithoutExtension(contractName);
            this.outFileName = $"BoogieOutputs/{baseName}.bpl";

            this.CorralPath = ToolPaths.CorralPath;
            this.BoogiePath = ToolPaths.BoogiePath;
        }

        public int Execute()
        {
            CleanupOldOutputFiles();

            if (this.program == null)
            {
                if (!ExecuteSolToBoogie())
                    return 1;
            }
            else
            {
                Exception? threadException = null;

                var t = new System.Threading.Thread(
                    () =>
                    {
                        try
                        {
                            WriteBoogieProgramToFile();
                        }
                        catch (Exception ex)
                        {
                            threadException = ex;
                        }
                    },
                    256 * 1024 * 1024
                ); // 256 MB stack

                t.Start();
                t.Join();

                if (threadException != null)
                    throw threadException;
            }

            // Translation-only mode used by the benchmark.
            // The Boogie file is generated, but Boogie and Corral are not executed.
            if (!TryProof && !TryRefutation)
            {
                Console.WriteLine(
                    "✅ Translation-only mode: Boogie file generated, verification skipped."
                );
                Console.WriteLine($"✅ Boogie output: {outFileName}");
                return 0;
            }

            if (TryProof && FindProof())
                return 0;

            if (TryRefutation)
            {
                Console.WriteLine("🔄 Moving to refutation phase with Corral...");

                bool noBugUpToBound = RunCorralForRefutation();

                if (!noBugUpToBound)
                {
                    Console.WriteLine("✅ Corral found a counterexample (refutation succeeded).\n");
                    return 0;
                }

                Console.WriteLine("❌ Corral did not find a counterexample (up to bound).\n");
                return 1;
            }

            return 0;
        }

        private void WriteBoogieProgramToFile()
        {
            Console.WriteLine($"📝 Writing Boogie program to {outFileName}");

            var sw = Stopwatch.StartNew();

            Console.WriteLine("Generating string...");
            var raw = this.program.ToString();
            Console.WriteLine($"ToString finished after {sw.Elapsed}");

            Console.WriteLine("Pretty printing...");
            var pretty = BoogiePrettyPrinter.IndentBoogie(raw);
            Console.WriteLine($"Pretty printer finished after {sw.Elapsed}");

            var dir = Path.GetDirectoryName(outFileName);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            Console.WriteLine("Writing file...");
            File.WriteAllText(outFileName, pretty, Encoding.UTF8);

            Console.WriteLine($"Done after {sw.Elapsed}");
        }

        private void DeleteFileIfExists(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Console.WriteLine($"🧹 Deleted old file: {path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Could not delete file {path}: {ex.Message}");
            }
        }

        private void CleanupOldOutputFiles()
        {
            DeleteFileIfExists("boogie.txt");
            DeleteFileIfExists("corral.txt");
            DeleteFileIfExists("corral_out.bpl");
            DeleteFileIfExists(corralTraceFileName);
            DeleteFileIfExists(counterexampleSummaryFileName);
        }

        private bool FindProof()
        {
            var boogieArgs = new List<string>
            {
                "-inline:spec",
                $"-inlineDepth:{translatorFlags.InlineDepthForBoogie}",
                "-proc:BoogieEntry_*",
                outFileName,
            };

            var boogieArgString = string.Join(" ", boogieArgs);
            Console.WriteLine($"... running {BoogiePath} {boogieArgString}");

            var boogieOut = RunBinary(BoogiePath, boogieArgString);
            Console.WriteLine("Boogie.Command = " + BoogiePath);

            const string boogieOutFile = "boogie.txt";
            using (var bFile = new StreamWriter(boogieOutFile))
            {
                bFile.Write(boogieOut);
            }

            if (CompareBoogieOutput(boogieOut))
            {
                Console.WriteLine(
                    $"\t*** Proof found! Formal Verification successful! (see {boogieOutFile})"
                );
                return true;
            }

            Console.WriteLine($"\t*** Did not find a proof (see {boogieOutFile})");
            return false;
        }

        private bool RunCorralForRefutation()
        {
            var corralArgs = new List<string>
            {
                $"/recursionBound:{CorralRecursionLimit}",
                $"/k:{CorralContextBound}",
                $"/main:CorralEntry_{ContractName}",
                "/printDataValues:1",
                "/trackAllVars",
                outFileName,
            };

            var corralArgString = string.Join(" ", corralArgs);
            Console.WriteLine($"... running {CorralPath} {corralArgString}");

            var corralOut = RunBinary(CorralPath, corralArgString);

            const string corralOutFile = "corral.txt";
            using (var bFile = new StreamWriter(corralOutFile))
            {
                bFile.Write(corralOut);
            }

            if (CompareCorralOutput("Program has no bugs", corralOut))
            {
                Console.WriteLine(
                    $"\t*** Formal Verification successful upto {CorralRecursionLimit} transactions (see {corralOutFile})"
                );
                return true;
            }

            if (corralOut.Contains("Execution trace:"))
            {
                Console.WriteLine($"\t*** Found a counterexample (see {corralOutFile})");

                var extractor = new CounterexampleExtractor(
                    corralOutputFile: corralOutFile,
                    boogieFile: outFileName,
                    summaryFile: counterexampleSummaryFileName
                );

                extractor.GenerateSummary(printToConsole: true);

                Console.WriteLine($"\n\tSee full execution trace inside {corralOutFile}");

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    DisplayTraceUsingConcurrencyExplorer();
                }

                return false;
            }

            Console.WriteLine($"\t*** Corral may have aborted abnormally (see {corralOutFile})");
            return false;
        }

        private void DisplayTraceUsingConcurrencyExplorer()
        {
            const string concExplorerName = "ConcurrencyExplorer.exe";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine(
                    "\tRun the command below to see the trace in a viewer (only supported on Windows):"
                );
                Console.WriteLine($"\t{concExplorerName} {corralTraceFileName}");
            }
        }

        private bool ExecuteSolToBoogie()
        {
            Console.WriteLine($"... running Solc on {SolidityFilePath}");

            SolidityCompiler compiler = new SolidityCompiler();
            CompilerOutput compilerOutput = compiler.Compile(SolcPath, SolidityFilePath);

            if (compilerOutput.ContainsError())
            {
                compilerOutput.PrintErrorsToConsole();
                throw new SystemException("Compilation Error");
            }

            AST solidityAST = new AST(compilerOutput, Path.GetDirectoryName(SolidityFilePath));

            try
            {
                BoogieTranslator translator = new BoogieTranslator();
                Console.WriteLine("... running SolToBoogie to translate Solidity to Boogie");

                BoogieAST boogieAST = translator.Translate(
                    solidityAST,
                    ignoreMethods,
                    translatorFlags,
                    ContractName
                );

                using (var outWriter = new StreamWriter(outFileName))
                {
                    outWriter.WriteLine(boogieAST.GetRoot());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"VeriSol translation error: {e.Message}");
                return false;
            }

            return true;
        }

        private string RunBinary(string cmdName, string arguments)
        {
            if (string.IsNullOrWhiteSpace(cmdName))
            {
                Console.WriteLine("❌ ERROR: Command path (cmdName) is empty!");
                Console.WriteLine($"Arguments provided: {arguments}");
                throw new InvalidOperationException(
                    "The path to the executable is empty. Check ExternalToolsManager."
                );
            }

            Console.WriteLine($"✅ Launching command: {cmdName} {arguments}");

            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.FileName = cmdName;
            p.StartInfo.Arguments = arguments;
            p.Start();

            string outputBinary = p.StandardOutput.ReadToEnd();
            string errorMsg = p.StandardError.ReadToEnd();

            if (!string.IsNullOrEmpty(errorMsg))
            {
                Console.WriteLine($"🔴 STDERR de {cmdName} : {errorMsg}");
            }

            p.WaitForExit();

            p.StandardOutput.Close();
            p.StandardError.Close();

            return outputBinary;
        }

        private bool CompareCorralOutput(string expected, string actual)
        {
            if (actual == null)
            {
                return false;
            }

            string[] actualList = actual.Split("Boogie verification time");
            if (actualList.Length == 2)
            {
                if (actualList[0].Contains(expected))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CompareBoogieOutput(string actual)
        {
            if (actual == null)
            {
                return false;
            }

            return actual.Contains("Boogie program verifier finished with ")
                && actual.Contains(" verified, 0 errors");
        }
    }
}
