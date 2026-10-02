using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using BoogieAST;
using WasmToBoogie.Conversion;
using WasmToBoogie.Parser;
using WasmToBoogie.Parser.Ast;

namespace WasmToBoogie
{
    public class WasmToBoogieMain
    {
        private readonly string wasmPath;
        private readonly string contractName;
        private readonly string? requestedEntryPoint;
private readonly bool generateHarness;

        public WasmToBoogieMain(string wasmPath, string contractName)
        {
            this.wasmPath = wasmPath;
            this.contractName = contractName;
        }

        public BoogieProgram Translate()
        {
            Console.WriteLine($"\uD83D\uDCD6 Reading WAT file: {wasmPath}");

            var parser = new WasmParser(wasmPath);
            var wasmAst = parser.Parse();
            Console.WriteLine($"✅ WAT AST generated with {wasmAst.Functions.Count} functions.");

            var converter = new WasmAstToBoogie(contractName);
            BoogieProgram? boogieProgram = null;
            Exception? threadException = null;

            var t = new Thread(
                () =>
                {
                    try
                    {
                        boogieProgram = converter.Convert(wasmAst);
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

            if (boogieProgram == null)
                throw new Exception("Boogie conversion failed.");

            Console.WriteLine("✅ WAT → Boogie conversion completed.");
            return boogieProgram;
        }

        // NEW: format + write without touching BoogieAST
        public void TranslateAndWrite(string outPath)
        {
            var program = Translate(); // build program
            var bpl = program.ToString(); // serialize
            bpl = BoogiePrettyPrinter.IndentBoogie(bpl); // format
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            File.WriteAllText(outPath, bpl, Encoding.UTF8);
            Console.WriteLine($"📝 Boogie written: {outPath}");
        }
    }
}
