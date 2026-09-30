namespace VeriSolRunner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Logging;
    using SharedConfig;
    using SolToBoogie;
    using VeriSolRunner.ExternalTools;
    using WasmToBoogie;
    using WasmToBoogie.Parser;
    using WasmToBoogie.Parser.Ast;

    class Program
    {
        public static int Main(string[] args)
        {
            // ✅ Tool configuration and validation mode
            if (args.Length >= 1 && args[0] == "--config")
            {
                ToolPaths.PrintConfiguration();
                Console.WriteLine(ToolPaths.GetConfigurationInstructions());
                return 0;
            }

            // ✅ Tool validation mode
            if (args.Length >= 1 && args[0] == "--validate")
            {
                Console.WriteLine("🔍 Validating tool paths...");
                if (ToolPaths.ValidateTools())
                {
                    Console.WriteLine("✅ All tools are properly configured!");
                    return 0;
                }
                else
                {
                    Console.WriteLine("❌ Some tools are missing. Please check the configuration.");
                    Console.WriteLine(ToolPaths.GetConfigurationInstructions());
                    return 1;
                }
            }

  // ✅ Dataset WebAssembly coverage mode
if (
    args.Length >= 2
    && args[0] == "--coverage-dataset"
)
{
    string datasetDirectory = args[1];

    int timeoutSeconds = 120;

    // Timeout optionnel :
    // --coverage-dataset <directory> --timeout 300
    int timeoutIndex =
        Array.IndexOf(
            args,
            "--timeout"
        );

    if (
        timeoutIndex >= 0
        && timeoutIndex + 1 < args.Length
    )
    {
        if (
            !int.TryParse(
                args[timeoutIndex + 1],
                out timeoutSeconds
            )
            || timeoutSeconds <= 0
        )
        {
            Console.Error.WriteLine(
                "Invalid timeout value. "
                + "Expected a positive number of seconds."
            );

            return 1;
        }
    }

    return DatasetCoverageRunner.Run(
        datasetDirectory,
        timeoutSeconds
    );
}          

            // ✅ Mode WebAssembly
            // ✅ Mode WebAssembly
            if (args.Length >= 2 && args[0] == "--wasm")
            {
                string wasmFile = args[1];
                string contractName = Path.GetFileNameWithoutExtension(wasmFile);

                bool noVerify = args.Contains("--no-verify") || args.Contains("--no-boogie");
                bool coverageOnly = args.Contains("--coverage-only");

                bool wasmTryProofFlag = true;
                const bool wasmTryRefutation = false;

                if (coverageOnly)
                {
                    var originalOut = Console.Out;
                    var originalErr = Console.Error;

                    try
                    {
                        // Supprime les gros logs du parser pendant coverage-only
                        Console.SetOut(TextWriter.Null);
                        Console.SetError(TextWriter.Null);

                        var parser = new WasmToBoogie.Parser.WasmParser(wasmFile) { Quiet = true };

                        var wasmModule = parser.Parse();

                        var coverage = WasmCoverageAnalyzer.Analyze(wasmModule);

                        // Réactive uniquement pour le résultat final lisible par le benchmark
                        Console.SetOut(originalOut);
                        Console.SetError(originalErr);

                        WasmCoverageAnalyzer.Print(coverage);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.SetOut(originalOut);
                        Console.SetError(originalErr);

                        Console.Error.WriteLine("COVERAGE_ERROR=" + ex.Message);
                        return 1;
                    }
                }

 string? requestedEntryPoint = null;

int entryPointIndex =
    Array.IndexOf(
        args,
        "--entry-point"
    );

if (entryPointIndex >= 0)
{
    if (
        entryPointIndex + 1
        >= args.Length
        || args[entryPointIndex + 1]
            .StartsWith(
                "--",
                StringComparison.Ordinal
            )
    )
    {
        Console.Error.WriteLine(
            "Missing function name after --entry-point."
        );

        Console.Error.WriteLine(
            "Example: --wasm contract.wat --entry-point apply"
        );

        return 1;
    }

    requestedEntryPoint =
        args[entryPointIndex + 1]
            .Trim();

    if (
        requestedEntryPoint.Length == 0
    )
    {
        Console.Error.WriteLine(
            "The WebAssembly entry-point name cannot be empty."
        );

        return 1;
    }

    /*
     * L'utilisateur peut écrire "$apply" ou "apply".
     * On conserve une représentation normalisée sans "$".
     */
    if (
        requestedEntryPoint.StartsWith(
            "$",
            StringComparison.Ordinal
        )
    )
    {
        requestedEntryPoint =
            requestedEntryPoint.Substring(1);
    }
}  else
{
    Console.WriteLine(
        $"🎯 Requested WebAssembly entry point: "
        + requestedEntryPoint
    );

    Console.WriteLine(
        "⚠️ Entry-point-specific harness generation "
        + "is not implemented yet."
    );

    Console.WriteLine(
        "🧩 Falling back to the current generic harness."
    );
}             

                if (noVerify)
                {
                    Console.WriteLine("🚀 Translation-only mode enabled");
                    wasmTryProofFlag = false;
                   // wasmTryRefutation = false;
                }

                var wasmTranslator = new WasmToBoogieMain(wasmFile, contractName);
                var program = wasmTranslator.Translate();

                var executor = new VeriSolExecutor(
                    program,
                    contractName,
                    corralRecursionLimit: 10,
                    ignoreMethods: new HashSet<Tuple<string, string>>(),
                    tryRefutation: wasmTryRefutation,
                    tryProofFlag: wasmTryProofFlag,
                    logger: LoggerFactory
                        .Create(builder => builder.AddConsole())
                        .CreateLogger("WasmMode")
                );

                Console.WriteLine("✅ WasmToBoogieMain call successful!");
                return executor.Execute();
            }

            // ✅ Classic Solidity mode
            if (args.Length < 2)
            {
                ShowUsage();
                return 1;
            }

            ExternalToolsManager.EnsureAllExisted();

            string solidityFile,
                entryPointContractName;
            bool tryProofFlag,
                tryRefutation;
            int recursionBound;
            ILogger logger;
            HashSet<Tuple<string, string>> ignoredMethods;
            bool printTransactionSequence = false;
            TranslatorFlags translatorFlags = new TranslatorFlags();

            SolToBoogie.ParseUtils.ParseCommandLineArgs(
                args,
                out solidityFile,
                out entryPointContractName,
                out tryProofFlag,
                out tryRefutation,
                out recursionBound,
                out logger,
                out ignoredMethods,
                out printTransactionSequence,
                ref translatorFlags
            );

            var verisolExecuter = new VeriSolExecutor(
                Path.Combine(Directory.GetCurrentDirectory(), solidityFile),
                entryPointContractName,
                recursionBound,
                ignoredMethods,
                tryRefutation,
                tryProofFlag,
                logger,
                printTransactionSequence, // ✅ Argument added here too
                translatorFlags
            );

            return verisolExecuter.Execute();
        }

        private static void ShowUsage()
        {
            Console.WriteLine(
                "SafeWasm: Formal specification and verification tool for Solidity and WebAssembly smart contracts"
            );

            Console.WriteLine();
Console.WriteLine("WebAssembly Options:");

Console.WriteLine(
    "   --entry-point <name>    "
    + "select the function used to build the verification harness"
);

Console.WriteLine(
    "   --no-verify             "
    + "translate WAT to Boogie without running Boogie"
);

Console.WriteLine(
    "   --coverage-only         "
    + "analyze instruction coverage only"
);
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine(
                "  VeriSol <solidity-file.sol> <contract-name> [options]     # Solidity mode"
            );
            Console.WriteLine(
                "  VeriSol --wasm <wat-file.wat>                            # WebAssembly mode"
            );
            Console.WriteLine(
                "  VeriSol --config                                          # Show tool configuration"
            );
            Console.WriteLine(
                "  VeriSol --validate                                        # Validate tool paths"
            );
            Console.WriteLine();
            Console.WriteLine("Solidity Options:");
            Console.WriteLine(
                "   /noChk                  don't perform verification, default: false"
            );
            Console.WriteLine(
                "   /noPrf                  don't perform inductive verification, default: false"
            );
            Console.WriteLine("   /txBound:k              max transaction depth, default: 4");
            Console.WriteLine("   /noTxSeq                don't print transaction sequence");
            Console.WriteLine("   /contractInfer          perform module invariant inference");
            Console.WriteLine("   /inlineDepth:k          inline nested calls upto depth k");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  VeriSol contract.sol MyContract");
            Console.WriteLine("  VeriSol --wasm contract.wat");
            Console.WriteLine("  VeriSol --config");
            Console.WriteLine("  VeriSol --validate");
        }
    }
}
