using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SharedConfig;
using WasmToBoogie.Parser.Ast;

namespace WasmToBoogie.Parser
{
    public class WasmParser
    {
        private readonly string filePath;
        public bool Quiet { get; set; } = false;

        private void Log(string message)
        {
            if (!Quiet)
                Console.WriteLine(message);
        }

        static WasmParser()
        {
            // Load the Binaryen library from the centralized path
            try
            {
                var binaryenPath = ToolPaths.BinaryenLibraryPath;
                if (File.Exists(binaryenPath))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        LoadLibrary(binaryenPath);
                    }
                    Console.WriteLine($"✅ Binaryen library loaded from: {binaryenPath}");
                }
                else
                {
                    Console.WriteLine($"⚠️ Binaryen library not found at: {binaryenPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to load Binaryen library: {ex.Message}");
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public WasmParser(string filePath)
        {
            this.filePath = filePath;
        }

        // ---------------- helpers for locals scan ----------------

        private static int ComputeMaxLocalIndexInBody(List<WasmNode> body)
        {
            int max = -1;

            int? TryName(WasmNode n)
            {
                return n switch
                {
                    LocalGetNode g => g.Index ?? TryParseAutoName(g.Name),
                    LocalSetNode s => s.Index ?? TryParseAutoName(s.Name),
                    LocalTeeNode t => t.Index ?? TryParseAutoName(t.Name),
                    _ => null,
                };
            }

            void Walk(WasmNode n)
            {
                switch (n)
                {
                    case LocalGetNode:
                    case LocalTeeNode:
                    {
                        var k = TryName(n);
                        if (k.HasValue)
                            max = Math.Max(max, k.Value);
                        break;
                    }

                    case LocalSetNode s:
                    {
                        var k = TryName(s);
                        if (k.HasValue)
                            max = Math.Max(max, k.Value);

                        if (s.Value != null)
                            Walk(s.Value);
                        break;
                    }

                    case UnaryOpNode u:
                        if (u.Operand != null)
                            Walk(u.Operand);
                        break;

                    case BinaryOpNode b:
                        Walk(b.Left);
                        Walk(b.Right);
                        break;

                    case IfNode iff:
                        Walk(iff.Condition);
                        foreach (var m in iff.ThenBody)
                            Walk(m);
                        if (iff.ElseBody != null)
                            foreach (var m in iff.ElseBody)
                                Walk(m);
                        break;

                    case BlockNode blk:
                        foreach (var m in blk.Body)
                            Walk(m);
                        break;

                    case LoopNode lp:
                        foreach (var m in lp.Body)
                            Walk(m);
                        break;

                    case BrIfNode brIf:
                        Walk(brIf.Condition);
                        break;

                    case BrTableNode bt:
                        if (bt.Selector != null)
                            Walk(bt.Selector);
                        break;

                    case CallNode c:
                        if (c.Args != null)
                            foreach (var a in c.Args)
                                Walk(a);
                        break;

                    case ReturnCallNode rc:
                        if (rc.Args != null)
                            foreach (var a in rc.Args)
                                Walk(a);
                        break;

                    case CallIndirectNode ci:
                        foreach (var a in ci.Args)
                            Walk(a);
                        Walk(ci.CalleeIndex);
                        break;

                    case ReturnCallIndirectNode rci:
                        foreach (var a in rci.Args)
                            Walk(a);
                        Walk(rci.CalleeIndex);
                        break;

                    case SelectNode sel:
                        Walk(sel.V1);
                        Walk(sel.V2);
                        Walk(sel.Cond);
                        break;

                    case MemoryOpNode mem:
                        if (mem.Address != null)
                            Walk(mem.Address);
                        if (mem.Value != null)
                            Walk(mem.Value);
                        if (mem.Length != null)
                            Walk(mem.Length);
                        break;

                    case TableOpNode t:
                        if (t.Index != null)
                            Walk(t.Index);
                        if (t.Value != null)
                            Walk(t.Value);
                        if (t.Delta != null)
                            Walk(t.Delta);
                        break;

                    case GlobalSetNode gs:
                        if (gs.Value != null)
                            Walk(gs.Value);
                        break;

                    case UnreachableNode:
                    case ReturnNode:
                    case NopNode:
                    case BrNode:
                    case RawInstructionNode:
                    case ConstNode:
                    case GlobalGetNode:
                    case RefNullNode:
                    case RefFuncNode:
                        break;

                    default:
                        break;
                }
            }

            foreach (var n in body)
                Walk(n);

            return max;
        }

        // ---------------- main entry ----------------

        public WasmModule Parse()
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"❌ WAT file not found: {filePath}");

            Log("📖 Reading WAT file: " + filePath);
            string watText = File.ReadAllText(filePath);
            var extractedSpec = SpecExtractor.ExtractFromWat(watText, strict: false);

            Log(
                $"📌 Specs: globalInv={extractedSpec.GlobalInvariants.Count}, "
                    + $"requiresFuncs={extractedSpec.RequiresByFunc.Count}, ensuresFuncs={extractedSpec.EnsuresByFunc.Count}"
            );
            string wasmPath = ConvertWatToWasm(filePath);

            IntPtr modulePtr = LoadWasmTextFile(wasmPath);
            if (modulePtr == IntPtr.Zero)
                throw new Exception("❌ Error reading Binaryen module");

            try
            {
                if (!ValidateModule(modulePtr))
                    Log("⚠️ Binaryen validation failed, continuing anyway.");
            }
            catch (Exception ex)
            {
                Log($"⚠️ Binaryen validation ignored: {ex.Message}");
            }

            if (!Quiet)
            {
                PrintModuleAST(modulePtr);
            }
            // ✅ IMPORTANT: create ONE WasmModule (your previous code created it twice)
            var module = new WasmModule();
            module.Spec = extractedSpec;
            ParseModuleImportsExportsAndTypes(watText, module);
            AddFunctionImportsToIndexSpace(module);

            if (module.Spec != null)
            {
                module.Spec.PrettyPrint();
            }

            // ---------------- Globals (module-level) ----------------
            int gCount = 0;
            try
            {
                gCount = GetGlobalCount(modulePtr);
            }
            catch { }

            Log($"🌍 Number of globals: {gCount}");

            for (int gi = 0; gi < gCount; gi++)
            {
                string? gName = null;
                try
                {
                    IntPtr namePtr = GetGlobalNameByIndex(modulePtr, gi);
                    var nm = namePtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(namePtr) : null;
                    if (!string.IsNullOrEmpty(nm))
                        gName = nm; // often already includes '$'
                }
                catch { }

                bool mut = false;
                try
                {
                    mut = GetGlobalIsMutableByIndex(modulePtr, gi);
                }
                catch { }

                string valType = "i32";
                IntPtr typePtr = IntPtr.Zero;
                try
                {
                    typePtr = GetGlobalTypeByIndex(modulePtr, gi);
                    var ty = typePtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(typePtr) : null;
                    if (!string.IsNullOrEmpty(ty))
                        valType = ty!;
                }
                catch { }
                finally
                {
                    // ⚠️ Only FreeCString here if your C wrapper strdup()s the returned type string.
                    // If it's owned by Binaryen, DO NOT free it.
                    if (typePtr != IntPtr.Zero)
                        FreeCString(typePtr);
                }

                string? init = null;
                IntPtr initPtr = IntPtr.Zero;
                try
                {
                    initPtr = GetGlobalInitConstByIndex(modulePtr, gi);
                    init = initPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(initPtr) : null;
                }
                catch { }
                finally
                {
                    if (initPtr != IntPtr.Zero)
                        FreeCString(initPtr);
                }

                module.Globals.Add(
                    new WasmGlobal
                    {
                        Index = gi,
                        Name = gName,
                        IsMutable = mut,
                        ValType = valType,
                        InitConst = init,
                    }
                );

                if (!string.IsNullOrEmpty(gName))
                    module.GlobalIndexByName[gName!] = gi;

                Log(
                    $"   🔸 global[{gi}] {gName ?? "<noname>"} (mut={mut}) {valType} init={init ?? "<none>"}"
                );
            }

            // ---------------- Functions ----------------

            int fnCount = GetFunctionCount(modulePtr);

            Log($"🔢 Number of functions: {fnCount}");
            var originalWatTokens = Tokenize(watText);
            for (int fi = 0; fi < fnCount; fi++)
            {
                // optional: function name
                string? funcName = null;
                try
                {
                    IntPtr namePtr = GetFunctionNameByIndex(modulePtr, fi);
                    if (namePtr != IntPtr.Zero)
                    {
                        var nm = Marshal.PtrToStringAnsi(namePtr);
                        if (!string.IsNullOrEmpty(nm))
                            funcName = nm.StartsWith("$", StringComparison.Ordinal) ? nm : "$" + nm;

                        bool isImportedFunction =
                            funcName != null
                            && module.Imports.Any(i =>
                                i.Kind == WasmImportKind.Func && i.InternalName == funcName
                            );

                        if (isImportedFunction)
                        {
                            Log(
                                $"⏭️ Skipping imported function from Binaryen function list: {funcName}"
                            );
                            continue;
                        }
                    }
                }
                catch
                {
                    // wrapper may lack this symbol
                }

                // body text (temp module)
                IntPtr bodyPtr = GetFunctionBodyText(modulePtr, fi);
                string watBody =
                    bodyPtr != IntPtr.Zero ? (Marshal.PtrToStringAnsi(bodyPtr) ?? "") : "";
                if (bodyPtr != IntPtr.Zero)
                    FreeCString(bodyPtr);

                Log($"\n📄 Extracted body of function #{fi}:\n{watBody}");

                var tokens = Tokenize(watBody);

                Log("🔍 Tokens: " + string.Join(" ", tokens));

                int idx = 0;
                var body = new List<WasmNode>();
                while (idx < tokens.Count)
                {
                    if (tokens[idx] == ")")
                    {
                        Log($"↩️ Skipping stray ')' at index {idx}");
                        idx++;
                        continue;
                    }

                    Log($"\n🕽️ ParseNode call at index {idx}");
                    body.Add(ParseNode(tokens, ref idx));
                }

                body = FlattenExecutableBody(body);

                Log($"🧪 DEBUG BODY COUNT {funcName}: {body.Count}");
                foreach (var n in body)
                    Log($"   node = {n.GetType().Name}");

                var func = new WasmFunction { Body = body, Name = funcName };

                // signature info from wrapper
                int paramCount = 0;
                try
                {
                    paramCount = GetFunctionParamCount(modulePtr, fi);
                }
                catch { }
                func.ParamCount = Math.Max(0, paramCount);

                int resultCount = 0;
                try
                {
                    resultCount = GetFunctionResultCount(modulePtr, fi);
                }
                catch { }
                func.ResultCount = Math.Max(0, resultCount);

                // infer local count by scanning references
                int maxIdx = ComputeMaxLocalIndexInBody(func.Body);
                int total = (maxIdx >= 0 ? (maxIdx + 1) : 0);

                if (func.LocalCount + func.ParamCount < total)
                    func.LocalCount = Math.Max(0, total - func.ParamCount);

                // fill $0..$N mapping
                for (int k = 0; k < func.ParamCount + func.LocalCount; k++)
                    func.LocalIndexByName["$" + k] = k;

                // parse header for explicit names/slots (if present in token stream)
                PopulateFunctionSignature(originalWatTokens, func);

                bool missingParameterTypes = func.ParamTypes.Count != func.ParamCount;

                bool missingResultTypes = func.ResultTypes.Count != func.ResultCount;

                if (missingParameterTypes || missingResultTypes)
                {
                    var binaryenSignature = new WasmFunction
                    {
                        ParamCount = func.ParamCount,
                        ResultCount = func.ResultCount,
                    };

                    // Les tokens Binaryen contiennent la signature effective :
                    // les références (type ...) y sont développées en param/result.
                    PopulateFunctionSignature(tokens, binaryenSignature);

                    if (missingParameterTypes)
                    {
                        if (binaryenSignature.ParamTypes.Count != func.ParamCount)
                        {
                            throw new InvalidOperationException(
                                $"Function {func.Name ?? "<anonymous>"}: "
                                    + $"expected {func.ParamCount} parameter types, "
                                    + $"but Binaryen provided "
                                    + $"{binaryenSignature.ParamTypes.Count}."
                            );
                        }

                        func.ParamTypes = binaryenSignature.ParamTypes.ToList();

                        while (func.ParamNames.Count < func.ParamCount)
                        {
                            func.ParamNames.Add(null);
                        }
                    }

                    if (missingResultTypes)
                    {
                        if (binaryenSignature.ResultTypes.Count != func.ResultCount)
                        {
                            throw new InvalidOperationException(
                                $"Function {func.Name ?? "<anonymous>"}: "
                                    + $"expected {func.ResultCount} result types, "
                                    + $"but Binaryen provided "
                                    + $"{binaryenSignature.ResultTypes.Count}."
                            );
                        }

                        func.ResultTypes = binaryenSignature.ResultTypes.ToList();
                    }
                }
                // sécurité finale après parsing de signature
                int maxIdxAfter = ComputeMaxLocalIndexInBody(func.Body);
                int totalAfter = maxIdxAfter >= 0 ? maxIdxAfter + 1 : 0;

                if (func.ParamCount + func.LocalCount < totalAfter)
                {
                    func.LocalCount = Math.Max(0, totalAfter - func.ParamCount);
                }

                for (int k = 0; k < func.ParamCount + func.LocalCount; k++)
                {
                    func.LocalIndexByName["$" + k] = k;
                }

                Log(
                    $"🧭 Signature: name={func.Name ?? "<anonymous>"}, params={func.ParamCount}, locals={func.LocalCount}, results={func.ResultCount}"
                );

                module.Functions.Add(func);
                int globalFuncIndex = module.FunctionIndexSpace.Count;

                module.FunctionIndexSpace.Add(
                    new WasmFunctionRef
                    {
                        Index = globalFuncIndex,
                        Name = func.Name,
                        IsImport = false,
                        Function = func,
                        Import = null,
                    }
                );

                if (!string.IsNullOrEmpty(func.Name))
                    module.FunctionIndexByName[func.Name] = globalFuncIndex;

                Log($"   🔗 func index {globalFuncIndex}: defined {func.Name}");
            }

            Log($"✅ WAT AST generated with {module.Functions.Count} functions.");

            // ---------------- Resolve global names to indices (after module is filled) ----------------
            void ResolveGlobals(WasmNode n)
            {
                switch (n)
                {
                    case GlobalGetNode g:
                        if (
                            g.Index == null
                            && g.Name != null
                            && module.GlobalIndexByName.TryGetValue(g.Name, out var gi)
                        )
                            g.Index = gi;
                        break;

                    case GlobalSetNode s:
                        if (
                            s.Index == null
                            && s.Name != null
                            && module.GlobalIndexByName.TryGetValue(s.Name, out var gsi)
                        )
                            s.Index = gsi;
                        if (s.Value != null)
                            ResolveGlobals(s.Value);
                        break;

                    case UnaryOpNode u:
                        if (u.Operand != null)
                            ResolveGlobals(u.Operand);
                        break;

                    case BinaryOpNode b:
                        ResolveGlobals(b.Left);
                        ResolveGlobals(b.Right);
                        break;

                    case IfNode iff:
                        ResolveGlobals(iff.Condition);
                        foreach (var x in iff.ThenBody)
                            ResolveGlobals(x);
                        if (iff.ElseBody != null)
                            foreach (var x in iff.ElseBody)
                                ResolveGlobals(x);
                        break;

                    case BlockNode blk:
                        foreach (var x in blk.Body)
                            ResolveGlobals(x);
                        break;

                    case LoopNode lp:
                        foreach (var x in lp.Body)
                            ResolveGlobals(x);
                        break;

                    case BrIfNode brIf:
                        ResolveGlobals(brIf.Condition);
                        break;

                    case CallNode c:
                        if (c.Args != null)
                            foreach (var a in c.Args)
                                ResolveGlobals(a);
                        break;

                    case SelectNode s2:
                        ResolveGlobals(s2.V1);
                        ResolveGlobals(s2.V2);
                        ResolveGlobals(s2.Cond);
                        break;

                    case MemoryOpNode mem:
                        if (mem.Address != null)
                            ResolveGlobals(mem.Address);
                        if (mem.Value != null)
                            ResolveGlobals(mem.Value);
                        break;

                    default:
                        break;
                }
            }

            foreach (var f in module.Functions)
            foreach (var n in f.Body)
                ResolveGlobals(n);

            // Verify labels (optional)
            VerifyLabels(module);

            return module;
        }

        private void ParseModuleImportsExportsAndTypes(string wat, WasmModule module)
        {
            var tokens = Tokenize(wat);
            int index = 0;

            while (index < tokens.Count)
            {
                if (tokens[index] != "(")
                {
                    index++;
                    continue;
                }

                int start = index;
                index++;

                if (index >= tokens.Count)
                    break;

                string head = tokens[index++];

                if (head == "module")
                {
                    continue;
                }
                else if (head == "type")
                {
                    ParseTypeDecl(tokens, ref index, module);
                }
                else if (head == "import")
                {
                    ParseImportDecl(tokens, ref index, module);
                }
                else if (head == "export")
                {
                    ParseExportDecl(tokens, ref index, module);
                }
                else if (head == "memory")
                {
                    if (index < tokens.Count && tokens[index].StartsWith("$"))
                        index++;

                    if (index < tokens.Count && int.TryParse(tokens[index], out var pages))
                    {
                        module.InitialMemoryPages = pages;
                        index++;
                    }

                    while (index < tokens.Count && tokens[index] != ")")
                        index++;

                    ExpectToken(tokens, ref index, ")");
                }
                else if (head == "table")
                {
                    if (module.HasTable)
                    {
                        throw new NotSupportedException(
                            "Multiple WebAssembly tables are not supported yet."
                        );
                    }

                    // Nom optionnel :
                    // (table $functions 4 10 funcref)
                    if (
                        index < tokens.Count
                        && tokens[index].StartsWith("$", StringComparison.Ordinal)
                    )
                    {
                        index++;
                    }

                    // Taille initiale obligatoire.
                    if (index >= tokens.Count || !int.TryParse(tokens[index], out int initialSize))
                    {
                        throw new InvalidOperationException(
                            "Invalid table declaration: missing initial table size."
                        );
                    }

                    index++;

                    // Taille maximale optionnelle.
                    int? maximumSize = null;

                    if (index < tokens.Count && int.TryParse(tokens[index], out int parsedMaximum))
                    {
                        maximumSize = parsedMaximum;
                        index++;
                    }

                    // Type de référence obligatoire.
                    if (index >= tokens.Count)
                    {
                        throw new InvalidOperationException(
                            "Invalid table declaration: missing reference type."
                        );
                    }

                    WasmReferenceKind referenceKind;

                    switch (tokens[index])
                    {
                        case "funcref":
                            referenceKind = WasmReferenceKind.Func;
                            break;

                        case "externref":
                            referenceKind = WasmReferenceKind.Extern;
                            break;

                        default:
                            throw new NotSupportedException(
                                $"Unsupported table reference type: {tokens[index]}"
                            );
                    }

                    index++;

                    module.HasTable = true;
                    module.InitialTableSize = initialSize;
                    module.MaximumTableSize = maximumSize;
                    module.TableReferenceKind = referenceKind;

                    Log(
                        $"📋 Table: initial={initialSize}, "
                            + $"maximum={maximumSize?.ToString() ?? "<none>"}, "
                            + $"type={referenceKind}"
                    );

                    // Ignorer les éventuels éléments restants jusqu'à
                    // la fin de la déclaration.
                    while (index < tokens.Count && tokens[index] != ")")
                    {
                        index++;
                    }

                    ExpectToken(tokens, ref index, ")");
                }
                else
                {
                    index = start;
                    SkipSExpr(tokens, ref index);
                }
            }

            Log($"📥 Imports parsed: {module.Imports.Count}");

            Log($"📤 Exports parsed: {module.Exports.Count}");

            Log($"🔤 Types parsed: {module.Types.Count}");
        }

        private void ParseTypeDecl(List<string> tokens, ref int index, WasmModule module)
        {
            int typeIndex = module.Types.Count;

            if (index < tokens.Count && tokens[index].StartsWith("$", StringComparison.Ordinal))
            {
                index++;
            }
            else if (
                index < tokens.Count
                && tokens[index].StartsWith("(;", StringComparison.Ordinal)
            )
            {
                string raw = tokens[index++];
                var digits = new string(raw.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out var parsed))
                    typeIndex = parsed;
            }

            var type = new WasmFuncType { Index = typeIndex };

            while (index < tokens.Count && tokens[index] != ")")
            {
                if (tokens[index] == "(" && index + 1 < tokens.Count && tokens[index + 1] == "func")
                {
                    index += 2;

                    while (index < tokens.Count && tokens[index] != ")")
                    {
                        if (
                            tokens[index] == "("
                            && index + 1 < tokens.Count
                            && tokens[index + 1] == "param"
                        )
                        {
                            index += 2;

                            while (index < tokens.Count && tokens[index] != ")")
                            {
                                if (NumTypes.Contains(tokens[index]))
                                    type.ParamTypes.Add(ParseWasmValueType(tokens[index]));

                                index++;
                            }

                            ExpectToken(tokens, ref index, ")");
                            continue;
                        }

                        if (
                            tokens[index] == "("
                            && index + 1 < tokens.Count
                            && tokens[index + 1] == "result"
                        )
                        {
                            index += 2;

                            while (index < tokens.Count && tokens[index] != ")")
                            {
                                if (NumTypes.Contains(tokens[index]))
                                    type.ResultTypes.Add(ParseWasmValueType(tokens[index]));

                                index++;
                            }

                            ExpectToken(tokens, ref index, ")");
                            continue;
                        }

                        index++;
                    }

                    ExpectToken(tokens, ref index, ")");
                    continue;
                }

                index++;
            }

            ExpectToken(tokens, ref index, ")");

            module.Types.RemoveAll(t => t.Index == typeIndex);
            module.Types.Add(type);

            Log(
                $"   🔤 type {typeIndex}: params={type.ParamTypes.Count}, results={type.ResultTypes.Count}"
            );
        }

        private void ParseImportDecl(List<string> tokens, ref int index, WasmModule module)
        {
            string moduleName = Unquote(tokens[index++]);
            string fieldName = Unquote(tokens[index++]);

            if (tokens[index] != "(")
                throw new Exception("Malformed import: expected import descriptor.");

            var descriptorTokens = ExtractCurrentSExprTokens(tokens, index);

            Log("DEBUG import descriptor tokens: " + string.Join(" ", descriptorTokens));

            index++;
            string kind = tokens[index++];

            if (kind == "func")
            {
                string? internalName = null;

                if (index < tokens.Count && tokens[index].StartsWith("$", StringComparison.Ordinal))
                    internalName = tokens[index++];

                var paramTypes = new List<WasmValueType>();
                var resultTypes = new List<WasmValueType>();

                for (int i = 0; i < descriptorTokens.Count; i++)
                {
                    if (descriptorTokens[i] == "type" && i + 1 < descriptorTokens.Count)
                    {
                        int typeId = ParseTypeIndex(descriptorTokens[i + 1]);

                        var typeInfo = module.Types.FirstOrDefault(t => t.Index == typeId);

                        if (typeInfo != null)
                        {
                            paramTypes.AddRange(typeInfo.ParamTypes);
                            resultTypes.AddRange(typeInfo.ResultTypes);
                        }
                        else
                        {
                            Log($"⚠️ Type {typeId} not found for import {moduleName}.{fieldName}");
                        }
                    }

                    if (descriptorTokens[i] == "param")
                    {
                        i++;

                        while (i < descriptorTokens.Count && descriptorTokens[i] != ")")
                        {
                            if (descriptorTokens[i].StartsWith("$", StringComparison.Ordinal))
                            {
                                i++;
                                continue;
                            }

                            if (NumTypes.Contains(descriptorTokens[i]))
                                paramTypes.Add(ParseWasmValueType(descriptorTokens[i]));

                            i++;
                        }
                    }

                    if (descriptorTokens[i] == "result")
                    {
                        i++;

                        while (i < descriptorTokens.Count && descriptorTokens[i] != ")")
                        {
                            if (NumTypes.Contains(descriptorTokens[i]))
                                resultTypes.Add(ParseWasmValueType(descriptorTokens[i]));

                            i++;
                        }
                    }
                }

                SkipUntilMatchingClose(tokens, ref index);
                ExpectToken(tokens, ref index, ")");

                var import = new WasmImport
                {
                    ModuleName = moduleName,
                    FieldName = fieldName,
                    InternalName = internalName ?? "$" + fieldName,
                    Kind = WasmImportKind.Func,
                    ParamTypes = paramTypes,
                    ResultTypes = resultTypes,
                    ParamCount = paramTypes.Count,
                    ResultCount = resultTypes.Count,
                    IsResolved = false,
                };

                module.Imports.Add(import);

                Log(
                    $"   📥 import func {moduleName}.{fieldName} as {import.InternalName} "
                        + $"params={paramTypes.Count}, results={resultTypes.Count}"
                );

                return;
            }

            var importKind = kind switch
            {
                "global" => WasmImportKind.Global,
                "memory" => WasmImportKind.Memory,
                "table" => WasmImportKind.Table,
                _ => throw new Exception($"Unsupported import kind: {kind}"),
            };

            SkipUntilMatchingClose(tokens, ref index);
            ExpectToken(tokens, ref index, ")");

            module.Imports.Add(
                new WasmImport
                {
                    ModuleName = moduleName,
                    FieldName = fieldName,
                    InternalName = "$" + fieldName,
                    Kind = importKind,
                    IsResolved = false,
                }
            );

            Log($"   📥 import {kind} {moduleName}.{fieldName}");
        }

        private void AddFunctionImportsToIndexSpace(WasmModule module)
        {
            foreach (var imp in module.Imports.Where(i => i.Kind == WasmImportKind.Func))
            {
                int idx = module.FunctionIndexSpace.Count;

                module.FunctionIndexSpace.Add(
                    new WasmFunctionRef
                    {
                        Index = idx,
                        Name = imp.InternalName,
                        IsImport = true,
                        Import = imp,
                        Function = null,
                    }
                );

                if (!string.IsNullOrEmpty(imp.InternalName))
                    module.FunctionIndexByName[imp.InternalName] = idx;

                Log($"   🔗 func index {idx}: import {imp.InternalName}");
            }
        }

        private void ParseExportDecl(List<string> tokens, ref int index, WasmModule module)
        {
            // We enter after "( export"
            if (index >= tokens.Count)
                throw new Exception("Malformed export declaration.");

            string exportName = Unquote(tokens[index++]);

            if (tokens[index] != "(")
                throw new Exception("Malformed export: expected export descriptor.");

            index++;
            string kind = tokens[index++];

            string target = tokens[index++];
            ExpectToken(tokens, ref index, ")"); // close descriptor
            ExpectToken(tokens, ref index, ")"); // close export

            var exportKind = kind switch
            {
                "func" => WasmExportKind.Func,
                "global" => WasmExportKind.Global,
                "memory" => WasmExportKind.Memory,
                "table" => WasmExportKind.Table,
                _ => throw new Exception($"Unsupported export kind: {kind}"),
            };

            int? indexTarget = null;
            string? internalName = null;

            if (int.TryParse(target, out var n))
                indexTarget = n;
            else
                internalName = target;

            module.Exports.Add(
                new WasmExport
                {
                    ExportName = exportName,
                    Kind = exportKind,
                    InternalName = internalName,
                    Index = indexTarget,
                }
            );

            Log(
                $"   📤 export {kind} {exportName} -> {internalName ?? indexTarget?.ToString() ?? "?"}"
            );
        }

        private static string Unquote(string s)
        {
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
                return s.Substring(1, s.Length - 2);

            return s;
        }

        private static int ParseTypeIndex(string token)
        {
            if (int.TryParse(token, out var n))
                return n;

            var digits = new string(token.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out n))
                return n;

            throw new Exception($"Invalid type index: {token}");
        }

        private void SkipSExpr(List<string> tokens, ref int index)
        {
            if (index >= tokens.Count || tokens[index] != "(")
                return;

            int depth = 0;

            while (index < tokens.Count)
            {
                if (tokens[index] == "(")
                    depth++;
                else if (tokens[index] == ")")
                    depth--;

                index++;

                if (depth == 0)
                    break;
            }
        }

        private void SkipUntilMatchingClose(List<string> tokens, ref int index)
        {
            int depth = 1;

            while (index < tokens.Count && depth > 0)
            {
                if (tokens[index] == "(")
                    depth++;
                else if (tokens[index] == ")")
                    depth--;

                index++;
            }
        }

        // ---------------- signature helpers ----------------

        private static List<string> ExtractCurrentSExprTokens(List<string> tokens, int startIndex)
        {
            var result = new List<string>();

            if (startIndex >= tokens.Count || tokens[startIndex] != "(")
                return result;

            int depth = 0;
            int i = startIndex;

            while (i < tokens.Count)
            {
                string t = tokens[i];
                result.Add(t);

                if (t == "(")
                    depth++;
                else if (t == ")")
                    depth--;

                i++;

                if (depth == 0)
                    break;
            }

            return result;
        }

        private void InferSignatureFromBody(WasmFunction func)
        {
            int maxIdx = -1;
            var assigned = new HashSet<int>();

            void Walk(WasmNode n)
            {
                switch (n)
                {
                    case LocalGetNode g:
                    {
                        var k = g.Index ?? TryParseAutoName(g.Name);
                        if (k.HasValue)
                            maxIdx = Math.Max(maxIdx, k.Value);
                        break;
                    }
                    case LocalSetNode s:
                    {
                        var k = s.Index ?? TryParseAutoName(s.Name);
                        if (k.HasValue)
                        {
                            maxIdx = Math.Max(maxIdx, k.Value);
                            assigned.Add(k.Value);
                        }
                        if (s.Value != null)
                            Walk(s.Value);
                        break;
                    }
                    case LocalTeeNode t:
                    {
                        var k = t.Index ?? TryParseAutoName(t.Name);
                        if (k.HasValue)
                        {
                            maxIdx = Math.Max(maxIdx, k.Value);
                            assigned.Add(k.Value);
                        }
                        break;
                    }
                    case UnaryOpNode u:
                        if (u.Operand != null)
                            Walk(u.Operand);
                        break;
                    case BinaryOpNode b:
                        Walk(b.Left);
                        Walk(b.Right);
                        break;
                    case IfNode iff:
                        Walk(iff.Condition);
                        foreach (var m in iff.ThenBody)
                            Walk(m);
                        if (iff.ElseBody != null)
                            foreach (var m in iff.ElseBody)
                                Walk(m);
                        break;
                    case BlockNode blk:
                        foreach (var m in blk.Body)
                            Walk(m);
                        break;
                    case LoopNode lp:
                        foreach (var m in lp.Body)
                            Walk(m);
                        break;
                    case BrIfNode brIf:
                        Walk(brIf.Condition);
                        break;
                    case CallNode c:
                        foreach (var a in c.Args)
                            Walk(a);
                        break;
                    case MemoryOpNode mem:
                        if (mem.Address != null)
                            Walk(mem.Address);
                        if (mem.Value != null)
                            Walk(mem.Value);
                        break;
                }
            }

            foreach (var n in func.Body)
                Walk(n);

            int total = maxIdx + 1;
            if (total <= 0)
                return;

            int paramCount = assigned.Count > 0 ? assigned.Min() : total;
            if (paramCount < 0 || paramCount > total)
                paramCount = total;

            func.ParamCount = paramCount;
            func.LocalCount = total - paramCount;

            for (int k = 0; k < total; k++)
                func.LocalIndexByName["$" + k] = k;
        }

        private static readonly HashSet<string> NumTypes = new(StringComparer.Ordinal)
        {
            "i32",
            "i64",
            "f32",
            "f64",
        };

        private static int? TryParseAutoName(string? name)
        {
            if (
                !string.IsNullOrEmpty(name)
                && name![0] == '$'
                && int.TryParse(name.AsSpan(1), out var k)
            )
                return k;
            return null;
        }

        private static readonly HashSet<string> BodyStarters = new(StringComparer.Ordinal)
        {
            // instructions (début du corps)
            "local.set",
            "local.get",
            "local.tee",
            "global.get",
            "global.set",
            "if",
            "block",
            "loop",
            "br",
            "br_if",
            "br_table",
            "return",
            "call",
            "call_indirect",
            "return_call",
            "return_call_indirect",
            "unreachable",
            "nop",
            "select",
            // consts / ops (si jamais le corps commence direct)
            "i32.const",
            "i64.const",
            "f32.const",
            "f64.const",
            // operations (au cas où)
            "i32.add",
            "i32.sub",
            "i32.mul",
            "i32.div_s",
            "i32.div_u",
            "i32.eq",
            "i32.ne",
            "i32.lt_s",
            "i32.gt_s",
            "i32.le_s",
            "i32.ge_s",
            "f32.add",
            "f32.sub",
            "f32.mul",
            "f32.div",
            "f32.min",
            "f32.max",
            "f64.add",
            "f64.sub",
            "f64.mul",
            "f64.div",
            "f64.min",
            "f64.max",
        };

        private void PopulateFunctionSignature(List<string> tokens, WasmFunction func)
        {
            // reset metadata collected from textual header scan
            func.ParamNames.Clear();
            func.ParamTypes.Clear();
            func.LocalTypes.Clear();
            func.ResultTypes.Clear();

            int watModuleDepth = 0;

            for (int i = 0; i + 1 < tokens.Count; i++)
            {
                if (tokens[i] == ")")
                {
                    watModuleDepth--;

                    if (watModuleDepth < 0)
                    {
                        watModuleDepth = 0;
                    }

                    continue;
                }

                if (tokens[i] != "(")
                {
                    continue;
                }

                bool isModuleFunction = watModuleDepth == 1 && tokens[i + 1] == "func";

                watModuleDepth++;

                if (!isModuleFunction)
                {
                    continue;
                }

                int p = i + 2;

                string? parsedFunctionName = null;

                if (p < tokens.Count && tokens[p].StartsWith("$", StringComparison.Ordinal))
                {
                    parsedFunctionName = tokens[p];
                    p++;
                }

                if (!string.IsNullOrEmpty(func.Name) && parsedFunctionName != func.Name)
                {
                    continue;
                }

                func.Name ??= parsedFunctionName;
                int parsedParamCount = 0;
                int parsedLocalCount = 0;
                int parsedResultCount = 0;

                while (p < tokens.Count - 1)
                {
                    if (tokens[p] == ")")
                    {
                        p++;
                        break;
                    }

                    if (tokens[p] != "(")
                        break;

                    string head = tokens[p + 1];

                    // stop when body starts
                    if (BodyStarters.Contains(head))
                        break;

                    if (head == "param")
                    {
                        p += 2; // skip "(" "param"

                        while (p < tokens.Count && tokens[p] != ")")
                        {
                            if (tokens[p].StartsWith("$", StringComparison.Ordinal))
                            {
                                string name = tokens[p];
                                p++;

                                if (p < tokens.Count && NumTypes.Contains(tokens[p]))
                                {
                                    string ty = tokens[p];

                                    func.LocalIndexByName[name] = parsedParamCount;
                                    func.ParamNames.Add(name);
                                    func.ParamTypes.Add(ParseWasmValueType(ty));

                                    parsedParamCount++;
                                    p++;
                                }
                                else
                                {
                                    throw new Exception(
                                        $"Malformed param declaration in function {func.Name ?? "<anonymous>"}: missing type after parameter name {name}"
                                    );
                                }
                            }
                            else if (NumTypes.Contains(tokens[p]))
                            {
                                string ty = tokens[p];

                                func.ParamNames.Add(null);
                                func.ParamTypes.Add(ParseWasmValueType(ty));

                                parsedParamCount++;
                                p++;
                            }
                            else
                            {
                                p++;
                            }
                        }

                        if (p < tokens.Count && tokens[p] == ")")
                            p++;

                        continue;
                    }

                    if (head == "local")
                    {
                        p += 2; // skip "(" "local"

                        while (p < tokens.Count && tokens[p] != ")")
                        {
                            if (tokens[p].StartsWith("$", StringComparison.Ordinal))
                            {
                                string localName = tokens[p];
                                p++;

                                if (p >= tokens.Count || !NumTypes.Contains(tokens[p]))
                                {
                                    throw new Exception(
                                        $"Malformed local declaration in function "
                                            + $"{func.Name ?? "<anonymous>"}: "
                                            + $"missing type after local name {localName}"
                                    );
                                }

                                string localTypeToken = tokens[p];
                                WasmValueType localType = ParseWasmValueType(localTypeToken);

                                func.LocalIndexByName[localName] =
                                    func.ParamCount + parsedLocalCount;

                                func.LocalTypes.Add(localType);

                                parsedLocalCount++;
                                p++;
                            }
                            else if (NumTypes.Contains(tokens[p]))
                            {
                                string localTypeToken = tokens[p];
                                WasmValueType localType = ParseWasmValueType(localTypeToken);

                                func.LocalTypes.Add(localType);

                                parsedLocalCount++;
                                p++;
                            }
                            else
                            {
                                p++;
                            }
                        }

                        if (p < tokens.Count && tokens[p] == ")")
                            p++;

                        continue;
                    }

                    if (head == "result")
                    {
                        p += 2; // skip "(" "result"

                        while (p < tokens.Count && tokens[p] != ")")
                        {
                            if (NumTypes.Contains(tokens[p]))
                            {
                                string ty = tokens[p];
                                func.ResultTypes.Add(ParseWasmValueType(ty));
                                parsedResultCount++;
                            }
                            p++;
                        }

                        if (p < tokens.Count && tokens[p] == ")")
                            p++;

                        continue;
                    }

                    int nestedExpressionDepth = 0;

                    do
                    {
                        if (tokens[p] == "(")
                        {
                            nestedExpressionDepth++;
                        }
                        else if (tokens[p] == ")")
                        {
                            nestedExpressionDepth--;
                        }

                        p++;
                    } while (p < tokens.Count && nestedExpressionDepth > 0);
                }

                // trust wrapper counts if already filled; otherwise use parsed counts
                if (func.ParamCount == 0)
                    func.ParamCount = parsedParamCount;

                if (func.ResultCount == 0)
                    func.ResultCount = parsedResultCount;

                func.LocalCount = Math.Max(func.LocalCount, parsedLocalCount);

                break;
            }

            // safety: if wrapper count differs from parsed metadata, keep wrapper count
            // but ensure metadata size is consistent enough for later use
            if (func.ParamTypes.Count > func.ParamCount)
                func.ParamTypes = func.ParamTypes.Take(func.ParamCount).ToList();

            if (func.ParamNames.Count > func.ParamCount)
                func.ParamNames = func.ParamNames.Take(func.ParamCount).ToList();

            if (func.ResultTypes.Count > func.ResultCount)
                func.ResultTypes = func.ResultTypes.Take(func.ResultCount).ToList();

            if (func.LocalTypes.Count > func.LocalCount)
            {
                func.LocalTypes = func.LocalTypes.Take(func.LocalCount).ToList();
            }

            if (func.LocalTypes.Count != func.LocalCount)
            {
                throw new InvalidOperationException(
                    $"Function {func.Name ?? "<anonymous>"}: "
                        + $"LocalCount={func.LocalCount}, "
                        + $"but {func.LocalTypes.Count} local types were parsed."
                );
            }

            for (int k = 0; k < func.ParamCount + func.LocalCount; k++)
            {
                var auto = "$" + k;
                if (!func.LocalIndexByName.ContainsKey(auto))
                    func.LocalIndexByName[auto] = k;
            }

            Log(
                $"🧭 Signature: name={func.Name ?? "<anonymous>"}, params={func.ParamCount}, locals={func.LocalCount}, results={func.ResultCount}"
            );

            if (func.ParamCount > 0)
            {
                Log(
                    "   Param names: "
                        + string.Join(", ", func.ParamNames.Select(x => x ?? "<unnamed>"))
                );
                Log("   Param types: " + string.Join(", ", func.ParamTypes));
            }

            if (func.ResultCount > 0)
            {
                Log("   Result types: " + string.Join(", ", func.ResultTypes));
            }
        }

        private static WasmValueType ParseWasmValueType(string s)
        {
            return s switch
            {
                "i32" => WasmValueType.I32,
                "i64" => WasmValueType.I64,
                "f32" => WasmValueType.F32,
                "f64" => WasmValueType.F64,
                _ => throw new Exception($"Unsupported wasm value type: {s}"),
            };
        }

        // ---------------- label verification ----------------

        public void VerifyLabels(WasmModule module)
        {
            Log("🔍 Verifying labels...");
            foreach (var function in module.Functions)
            {
                var availableLabels = new HashSet<string>();
                var labelScopes = new Stack<HashSet<string>>();
                var labelDepths = new Dictionary<string, int>();
                labelScopes.Push(new HashSet<string>());
                VerifyLabelsInNode(function.Body, availableLabels, labelScopes, labelDepths, 0);
            }

            Log("✅ Label verification completed successfully.");
        }

        private void VerifyLabelsInNode(
            List<WasmNode> nodes,
            HashSet<string> availableLabels,
            Stack<HashSet<string>> labelScopes,
            Dictionary<string, int> labelDepths,
            int currentDepth
        )
        {
            foreach (var node in nodes)
                VerifyLabelsInNode(node, availableLabels, labelScopes, labelDepths, currentDepth);
        }

        private static bool IsNumericDepth(string s) => s.Length > 0 && char.IsDigit(s[0]);

        private void VerifyLabelsInNode(
            WasmNode node,
            HashSet<string> availableLabels,
            Stack<HashSet<string>> labelScopes,
            Dictionary<string, int> labelDepths,
            int currentDepth
        )
        {
            switch (node)
            {
                case BlockNode blockNode:
                    VerifyBlockNode(
                        blockNode,
                        availableLabels,
                        labelScopes,
                        labelDepths,
                        currentDepth
                    );
                    break;

                case LoopNode loopNode:
                    VerifyLoopNode(
                        loopNode,
                        availableLabels,
                        labelScopes,
                        labelDepths,
                        currentDepth
                    );
                    break;

                case BrNode brNode:
                    VerifyBranchNode(brNode, availableLabels, "br", labelDepths);
                    break;

                case BrIfNode brIfNode:
                    VerifyBranchNode(brIfNode, availableLabels, "br_if", labelDepths);
                    break;

                case BrTableNode bt:
                {
                    void CheckLab(string lab, string kind)
                    {
                        if (IsNumericDepth(lab))
                            return;
                        if (!availableLabels.Contains(lab))
                        {
                            var avail =
                                availableLabels.Count > 0
                                    ? string.Join(", ", availableLabels)
                                    : "aucun";
                            throw new Exception(
                                $"❌ Label invalide dans br_table {kind} : {lab}. Labels disponibles : {avail}"
                            );
                        }
                    }
                    foreach (var t in bt.Targets)
                        CheckLab(t, "target");
                    CheckLab(bt.Default, "default");
                    break;
                }

                case UnaryOpNode unaryNode:
                    VerifyLabelsInNode(
                        unaryNode.Operand,
                        availableLabels,
                        labelScopes,
                        labelDepths,
                        currentDepth
                    );
                    break;

                case BinaryOpNode binaryNode:
                    VerifyLabelsInNode(
                        binaryNode.Left,
                        availableLabels,
                        labelScopes,
                        labelDepths,
                        currentDepth
                    );
                    VerifyLabelsInNode(
                        binaryNode.Right,
                        availableLabels,
                        labelScopes,
                        labelDepths,
                        currentDepth
                    );
                    break;

                case IfNode ifNode:
                    VerifyLabelsInNode(
                        ifNode.Condition,
                        availableLabels,
                        labelScopes,
                        labelDepths,
                        currentDepth
                    );
                    VerifyLabelsInNode(
                        ifNode.ThenBody,
                        availableLabels,
                        labelScopes,
                        labelDepths,
                        currentDepth
                    );
                    if (ifNode.ElseBody != null)
                        VerifyLabelsInNode(
                            ifNode.ElseBody,
                            availableLabels,
                            labelScopes,
                            labelDepths,
                            currentDepth
                        );
                    break;

                // benign nodes
                case ReturnNode:
                case NopNode:
                case SelectNode:
                case UnreachableNode:
                case LocalGetNode:
                case LocalSetNode:
                case LocalTeeNode:
                case CallNode:
                case ConstNode:
                case RawInstructionNode:
                case GlobalGetNode:
                case GlobalSetNode:
                case MemoryOpNode:
                    break;
            }
        }

        private void VerifyBlockNode(
            BlockNode blockNode,
            HashSet<string> availableLabels,
            Stack<HashSet<string>> labelScopes,
            Dictionary<string, int> labelDepths,
            int currentDepth
        )
        {
            var newScope = new HashSet<string>();
            labelScopes.Push(newScope);
            if (!string.IsNullOrEmpty(blockNode.Label))
            {
                if (availableLabels.Contains(blockNode.Label))
                    throw new Exception(
                        $"❌ Duplicate label found: {blockNode.Label} (depth {currentDepth})"
                    );

                availableLabels.Add(blockNode.Label);
                newScope.Add(blockNode.Label);
                labelDepths[blockNode.Label] = currentDepth;

                Log($"🔹 Block label added: {blockNode.Label} (depth {currentDepth})");
            }

            VerifyLabelsInNode(
                blockNode.Body,
                availableLabels,
                labelScopes,
                labelDepths,
                currentDepth + 1
            );

            labelScopes.Pop();
            if (!string.IsNullOrEmpty(blockNode.Label))
            {
                availableLabels.Remove(blockNode.Label);
                labelDepths.Remove(blockNode.Label);
            }
        }

        private void VerifyLoopNode(
            LoopNode loopNode,
            HashSet<string> availableLabels,
            Stack<HashSet<string>> labelScopes,
            Dictionary<string, int> labelDepths,
            int currentDepth
        )
        {
            var newScope = new HashSet<string>();
            labelScopes.Push(newScope);

            if (!string.IsNullOrEmpty(loopNode.Label))
            {
                if (availableLabels.Contains(loopNode.Label))
                    throw new Exception(
                        $"❌ Label dupliqué trouvé : {loopNode.Label} (profondeur {currentDepth})"
                    );

                availableLabels.Add(loopNode.Label);
                newScope.Add(loopNode.Label);
                labelDepths[loopNode.Label] = currentDepth;

                Log($"🔹 Loop label added: {loopNode.Label} (depth {currentDepth})");
            }

            VerifyLabelsInNode(
                loopNode.Body,
                availableLabels,
                labelScopes,
                labelDepths,
                currentDepth + 1
            );

            labelScopes.Pop();
            if (!string.IsNullOrEmpty(loopNode.Label))
            {
                availableLabels.Remove(loopNode.Label);
                labelDepths.Remove(loopNode.Label);
            }
        }

        private void VerifyBranchNode(
            WasmNode branchNode,
            HashSet<string> availableLabels,
            string branchType,
            Dictionary<string, int> labelDepths
        )
        {
            static bool IsNumericDepthLocal(string s) => s.Length > 0 && char.IsDigit(s[0]);

            string label = branchNode switch
            {
                BrNode brNode => brNode.Label,
                BrIfNode brIfNode => brIfNode.Label,
                _ => throw new ArgumentException(
                    $"Type de nœud de branchement non supporté : {branchNode.GetType()}"
                ),
            };

            if (!IsNumericDepthLocal(label) && !availableLabels.Contains(label))
            {
                var availableLabelsList =
                    availableLabels.Count > 0 ? string.Join(", ", availableLabels) : "aucun";
                throw new Exception(
                    $"❌ Label invalide dans {branchType} : {label}. Labels disponibles : {availableLabelsList}"
                );
            }

            var labelDepth = labelDepths.GetValueOrDefault(label, -1);

            Log($"🔹 {branchType} to valid label: {label} (depth {labelDepth})");
        }

        // ---------------- tokenizer & parser ----------------

        private List<string> Tokenize(string wat)
        {
            return new List<string>(
                wat.Replace("(", " ( ")
                    .Replace(")", " ) ")
                    .Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            );
        }

        private static List<WasmNode> FlattenExecutableBody(List<WasmNode> nodes)
        {
            var result = new List<WasmNode>();

            foreach (var n in nodes)
            {
                if (n is NopNode)
                    continue;

                if (n is BlockNode b && b.Label == "type")
                    continue;

                // Blocs artificiels créés par le parser
                if (
                    n is BlockNode b2
                    && (b2.Label == null || b2.Label == "module" || b2.Label == "func")
                )
                {
                    result.AddRange(FlattenExecutableBody(b2.Body));
                    continue;
                }

                result.Add(n);
            }

            return result;
        }

        private WasmNode ParseNode(List<string> tokens, ref int index)
        {
            if (tokens[index] == "(")
            {
                index++;
                string op = tokens[index++];

                Log($"🔸 Block start: ({op}");

                if (op.EndsWith(".const", StringComparison.Ordinal))
                {
                    string value = tokens[index++];
                    ExpectToken(tokens, ref index, ")");

                    Log($"  🔹 ConstNode({op}, {value})");
                    return new ConstNode { Type = op.Split('.')[0], Value = value };
                }
                else if (op == "ref.null")
                {
                    if (index >= tokens.Count || tokens[index] == ")")
                    {
                        throw new InvalidOperationException("ref.null requires a reference type.");
                    }

                    string referenceType = tokens[index++];

                    WasmReferenceKind kind = referenceType switch
                    {
                        "func" => WasmReferenceKind.Func,
                        "funcref" => WasmReferenceKind.Func,

                        "extern" => WasmReferenceKind.Extern,
                        "externref" => WasmReferenceKind.Extern,

                        _ => throw new NotSupportedException(
                            $"Unsupported reference type in ref.null: {referenceType}"
                        ),
                    };

                    ExpectToken(tokens, ref index, ")");

                    return new RefNullNode { Kind = kind };
                }
                else if (op == "ref.func")
                {
                    if (index >= tokens.Count || tokens[index] == ")")
                    {
                        throw new InvalidOperationException(
                            "ref.func requires a function index or name."
                        );
                    }

                    string target = tokens[index++];

                    ExpectToken(tokens, ref index, ")");

                    return new RefFuncNode { Target = target };
                }
                else if (op == "local.get")
                {
                    string tok = tokens[index++];
                    var node = new LocalGetNode();
                    if (tok.StartsWith("$", StringComparison.Ordinal))
                        node.Name = tok;
                    else if (int.TryParse(tok, out var n))
                        node.Index = n;
                    ExpectToken(tokens, ref index, ")");
                    return node;
                }
                else if (op == "local.set")
                {
                    string tok = tokens[index++];
                    var node = new LocalSetNode();
                    if (tok.StartsWith("$", StringComparison.Ordinal))
                        node.Name = tok;
                    else if (int.TryParse(tok, out var n))
                        node.Index = n;

                    if (index < tokens.Count && tokens[index] != ")")
                        node.Value = ParseNode(tokens, ref index);

                    ExpectToken(tokens, ref index, ")");
                    return node;
                }
                else if (op == "local.tee")
                {
                    string tok = tokens[index++];
                    var tee = new LocalTeeNode();
                    if (tok.StartsWith("$", StringComparison.Ordinal))
                        tee.Name = tok;
                    else if (int.TryParse(tok, out var n))
                        tee.Index = n;

                    if (index < tokens.Count && tokens[index] != ")")
                    {
                        var valueExpr = ParseNode(tokens, ref index);
                        ExpectToken(tokens, ref index, ")");
                        var block = new BlockNode { Label = null };
                        block.Body.Add(valueExpr);
                        block.Body.Add(tee);
                        return block;
                    }
                    else
                    {
                        ExpectToken(tokens, ref index, ")");
                        return tee;
                    }
                }
                else if (op == "global.get")
                {
                    string tok = tokens[index++];
                    var node = new GlobalGetNode();
                    if (tok.StartsWith("$", StringComparison.Ordinal))
                        node.Name = tok;
                    else if (int.TryParse(tok, out var n))
                        node.Index = n;
                    ExpectToken(tokens, ref index, ")");
                    return node;
                }
                else if (op == "global.set")
                {
                    string tok = tokens[index++];
                    var node = new GlobalSetNode();
                    if (tok.StartsWith("$", StringComparison.Ordinal))
                        node.Name = tok;
                    else if (int.TryParse(tok, out var n))
                        node.Index = n;

                    if (index < tokens.Count && tokens[index] != ")")
                        node.Value = ParseNode(tokens, ref index);

                    ExpectToken(tokens, ref index, ")");
                    return node;
                }
                else if (op == "call")
                {
                    string tgt = tokens[index++];

                    var args = new List<WasmNode>();
                    while (index < tokens.Count && tokens[index] != ")")
                        args.Add(ParseNode(tokens, ref index));

                    ExpectToken(tokens, ref index, ")");
                    return new CallNode { Target = tgt, Args = args };
                }
                else if (op == "return_call")
                {
                    string tgt = tokens[index++];

                    var args = new List<WasmNode>();
                    while (index < tokens.Count && tokens[index] != ")")
                        args.Add(ParseNode(tokens, ref index));

                    ExpectToken(tokens, ref index, ")");
                    return new ReturnCallNode { Target = tgt, Args = args };
                }
                else if (op == "call_indirect")
                {
                    string? typeUse = null;

                    if (
                        index < tokens.Count
                        && tokens[index] == "("
                        && index + 2 < tokens.Count
                        && tokens[index + 1] == "type"
                    )
                    {
                        index += 2;
                        typeUse = tokens[index++];
                        ExpectToken(tokens, ref index, ")");
                    }

                    var exprs = new List<WasmNode>();
                    while (index < tokens.Count && tokens[index] != ")")
                        exprs.Add(ParseNode(tokens, ref index));

                    ExpectToken(tokens, ref index, ")");

                    if (exprs.Count < 1)
                        throw new Exception("call_indirect: missing callee index expression.");

                    var calleeIndex = exprs[^1];
                    exprs.RemoveAt(exprs.Count - 1);

                    return new CallIndirectNode
                    {
                        TypeUse = typeUse,
                        Args = exprs,
                        CalleeIndex = calleeIndex,
                    };
                }
                else if (op == "return_call_indirect")
                {
                    string? typeUse = null;

                    if (
                        index < tokens.Count
                        && tokens[index] == "("
                        && index + 2 < tokens.Count
                        && tokens[index + 1] == "type"
                    )
                    {
                        index += 2;
                        typeUse = tokens[index++];
                        ExpectToken(tokens, ref index, ")");
                    }

                    var exprs = new List<WasmNode>();
                    while (index < tokens.Count && tokens[index] != ")")
                        exprs.Add(ParseNode(tokens, ref index));

                    ExpectToken(tokens, ref index, ")");

                    if (exprs.Count < 1)
                        throw new Exception(
                            "return_call_indirect: missing callee index expression."
                        );

                    var calleeIndex = exprs[^1];
                    exprs.RemoveAt(exprs.Count - 1);

                    return new ReturnCallIndirectNode
                    {
                        TypeUse = typeUse,
                        Args = exprs,
                        CalleeIndex = calleeIndex,
                    };
                }
                else if (op == "return")
                {
                    var values = new List<WasmNode>();

                    while (index < tokens.Count && tokens[index] != ")")
                    {
                        values.Add(ParseNode(tokens, ref index));
                    }

                    ExpectToken(tokens, ref index, ")");

                    if (values.Count == 0)
                        return new ReturnNode();

                    var block = new BlockNode { Label = null };

                    foreach (var v in values)
                        block.Body.Add(v);

                    block.Body.Add(new ReturnNode());

                    return block;
                }
                else if (op == "nop")
                {
                    ExpectToken(tokens, ref index, ")");
                    return new NopNode();
                }
                else if (op == "br_table")
                {
                    var targets = new List<string>();
                    var exprs = new List<WasmNode>();

                    while (index < tokens.Count)
                    {
                        string tk = tokens[index];

                        if (tk == ")")
                        {
                            index++;
                            break;
                        }
                        else if (tk == "(")
                        {
                            exprs.Add(ParseNode(tokens, ref index));
                        }
                        else
                        {
                            targets.Add(tk);
                            index++;
                        }
                    }

                    if (exprs.Count == 0)
                        throw new Exception("br_table: expression sélecteur absente.");

                    if (targets.Count == 0)
                        throw new Exception(
                            "br_table: au moins une cible + un default sont requis."
                        );

                    var def = targets[^1];
                    targets.RemoveAt(targets.Count - 1);

                    var selector = exprs[^1];
                    exprs.RemoveAt(exprs.Count - 1);

                    var brTable = new BrTableNode
                    {
                        Targets = targets,
                        Default = def,
                        Selector = selector,
                    };

                    if (exprs.Count == 0)
                        return brTable;

                    var block = new BlockNode { Label = null };

                    foreach (var value in exprs)
                        block.Body.Add(value);

                    block.Body.Add(brTable);

                    return block;
                }
                else if (IsUnaryOp(op))
                {
                    Log($"  🔹 UnaryOpNode : {op}");
                    var operand = ParseNode(tokens, ref index);
                    ExpectToken(tokens, ref index, ")");
                    return new UnaryOpNode { Op = op, Operand = operand };
                }
                else if (IsBinaryOp(op))
                {
                    Log($"  🔹 BinaryOpNode : {op}");
                    var left = ParseNode(tokens, ref index);
                    var right = ParseNode(tokens, ref index);
                    ExpectToken(tokens, ref index, ")");
                    return new BinaryOpNode
                    {
                        Op = op,
                        Left = left,
                        Right = right,
                    };
                }
                else if (op == "block")
                {
                    Log("  🔹 Block block");

                    string? label =
                        (index < tokens.Count && tokens[index].StartsWith("$"))
                            ? tokens[index++]
                            : null;

                    WasmValueType? resultType = null;

                    if (
                        index < tokens.Count
                        && tokens[index] == "("
                        && index + 3 < tokens.Count
                        && tokens[index + 1] == "result"
                    )
                    {
                        index += 2;
                        string tyTok = tokens[index++];

                        resultType = tyTok switch
                        {
                            "i32" => WasmValueType.I32,
                            "i64" => WasmValueType.I64,
                            "f32" => WasmValueType.F32,
                            "f64" => WasmValueType.F64,
                            _ => throw new Exception($"block: unsupported result type {tyTok}"),
                        };

                        ExpectToken(tokens, ref index, ")");
                    }

                    var body = new List<WasmNode>();

                    while (index < tokens.Count && tokens[index] != ")")
                        body.Add(ParseNode(tokens, ref index));

                    ExpectToken(tokens, ref index, ")");

                    return new BlockNode
                    {
                        Label = label,
                        Body = body,
                        ResultType = resultType,
                    };
                }
                else if (op == "loop")
                {
                    Log("  🔹 Block loop");

                    string? label =
                        (index < tokens.Count && tokens[index].StartsWith("$"))
                            ? tokens[index++]
                            : null;

                    if (
                        index < tokens.Count
                        && tokens[index] == "("
                        && index + 3 < tokens.Count
                        && tokens[index + 1] == "result"
                    )
                    {
                        index += 2;
                        _ = tokens[index++];
                        ExpectToken(tokens, ref index, ")");
                    }

                    var body = new List<WasmNode>();
                    while (index < tokens.Count && tokens[index] != ")")
                        body.Add(ParseNode(tokens, ref index));

                    ExpectToken(tokens, ref index, ")");
                    return new LoopNode { Label = label, Body = body };
                }
                else if (op == "if")
                {
                    Log("  🔹 Block if");

                    Log(
                        $"    [if] next tokens: {tokens[index]} {tokens[index + 1]} {tokens[index + 2]} {tokens[index + 3]}"
                    );

                    // optional: (result <type>) right after "if"
                    WasmValueType? resTy = null;

                    Log($"    [if] consumed result type = {resTy}");

                    if (
                        index < tokens.Count
                        && tokens[index] == "("
                        && index + 3 < tokens.Count
                        && tokens[index + 1] == "result"
                    )
                    {
                        index += 2;
                        string tyTok = tokens[index++]; // i32/i64/f32/f64
                        resTy = tyTok switch
                        {
                            "i32" => WasmValueType.I32,
                            "i64" => WasmValueType.I64,
                            "f32" => WasmValueType.F32,
                            "f64" => WasmValueType.F64,
                            _ => throw new Exception($"if: unsupported result type {tyTok}"),
                        };
                        ExpectToken(tokens, ref index, ")");
                    }

                    // now parse condition
                    var condition = ParseNode(tokens, ref index);

                    // Binaryen prints branches as naked expressions (not (then)/(else)),
                    // so we parse the next expression as "then", and the next as "else" if present.
                    var thenBody = new List<WasmNode>();
                    var elseBody = (List<WasmNode>?)null;

                    if (index < tokens.Count && tokens[index] != ")")
                        thenBody.Add(ParseNode(tokens, ref index));

                    if (index < tokens.Count && tokens[index] != ")")
                        elseBody = new List<WasmNode> { ParseNode(tokens, ref index) };

                    ExpectToken(tokens, ref index, ")");
                    return new IfNode
                    {
                        Condition = condition,
                        ThenBody = thenBody,
                        ElseBody = elseBody,
                        ResultType = resTy,
                    };
                }
                else if (op == "br")
                {
                    string label = tokens[index++];

                    var values = new List<WasmNode>();

                    while (index < tokens.Count && tokens[index] != ")")
                    {
                        values.Add(ParseNode(tokens, ref index));
                    }

                    ExpectToken(tokens, ref index, ")");

                    if (values.Count == 0)
                        return new BrNode { Label = label };

                    var block = new BlockNode { Label = null };

                    foreach (var v in values)
                        block.Body.Add(v);

                    block.Body.Add(new BrNode { Label = label });

                    return block;
                }
                else if (op == "br_if")
                {
                    string label = tokens[index++];

                    var exprs = new List<WasmNode>();

                    while (index < tokens.Count && tokens[index] != ")")
                    {
                        exprs.Add(ParseNode(tokens, ref index));
                    }

                    ExpectToken(tokens, ref index, ")");

                    if (exprs.Count == 0)
                        throw new Exception("br_if: missing condition.");

                    var condition = exprs[^1];
                    exprs.RemoveAt(exprs.Count - 1);

                    if (exprs.Count == 0)
                    {
                        return new BrIfNode { Label = label, Condition = condition };
                    }

                    var block = new BlockNode { Label = null };

                    foreach (var v in exprs)
                        block.Body.Add(v);

                    block.Body.Add(new BrIfNode { Label = label, Condition = condition });

                    return block;
                }
                else if (op == "module" || op == "type" || op == "func")
                {
                    var inner = new List<WasmNode>();

                    while (index < tokens.Count && tokens[index] != ")")
                    {
                        if (tokens[index].StartsWith("$", StringComparison.Ordinal))
                        {
                            index++;
                            continue;
                        }

                        if (tokens[index] == "(" && index + 1 < tokens.Count)
                        {
                            string head = tokens[index + 1];

                            if (head == "param" || head == "result" || head == "local")
                            {
                                SkipSExpr(tokens, ref index);
                                continue;
                            }
                        }

                        inner.Add(ParseNode(tokens, ref index));
                    }

                    ExpectToken(tokens, ref index, ")");
                    return new BlockNode { Label = op, Body = inner };
                }
                else if (op == "unreachable")
                {
                    ExpectToken(tokens, ref index, ")");
                    return new UnreachableNode();
                }
                else if (op == "select")
                {
                    // (select [t] v1 v2 cond)
                    string? maybeType = (index < tokens.Count ? tokens[index] : null);
                    if (
                        maybeType == "i32"
                        || maybeType == "i64"
                        || maybeType == "f32"
                        || maybeType == "f64"
                    )
                        index++; // ignore type tag

                    var v1 = ParseNode(tokens, ref index);
                    var v2 = ParseNode(tokens, ref index);
                    var cond = ParseNode(tokens, ref index);
                    ExpectToken(tokens, ref index, ")");
                    return new SelectNode
                    {
                        V1 = v1,
                        V2 = v2,
                        Cond = cond,
                    };
                }
                else if (IsMemoryOp(op))
                {
                    // 1) parse immediates offset/align
                    ParseOffsetAlign(tokens, ref index, out int offset, out int align);

                    // 2) formes possibles :
                    // - stack form: (i32.load offset=.. align=..)  -> Address=null, Value=null
                    // - folded load: (i32.load (i32.const ...))   -> Address=expr
                    // - folded store: (i32.store (addr) (val))    -> Address + Value

                    WasmNode? addr = null;
                    WasmNode? val = null;
                    WasmNode? len = null;

                    if (index < tokens.Count && tokens[index] != ")")
                    {
                        // On peut avoir 1 ou 2 sous-expressions, ou des atomes (rare)
                        // On parse les sous-expressions tant qu'on n'est pas à ')'
                        var exprs = new List<WasmNode>();
                        while (index < tokens.Count && tokens[index] != ")")
                        {
                            if (tokens[index] == "(")
                                exprs.Add(ParseNode(tokens, ref index));
                            else
                                break; // atome inattendu -> on laisse au generic/Raw si besoin
                        }

                        if (exprs.Count >= 1)
                            addr = exprs[0];
                        if (exprs.Count >= 2)
                            val = exprs[1];
                        if (exprs.Count >= 3)
                            len = exprs[2];
                    }

                    ExpectToken(tokens, ref index, ")");

                    return new MemoryOpNode
                    {
                        Op = op,
                        Offset = offset,
                        Align = align,
                        Address = addr,
                        Value = val,
                        Length = len,
                    };
                }
                else if (IsTableOp(op))
                {
                    var exprs = new List<WasmNode>();

                    while (index < tokens.Count && tokens[index] != ")")
                    {
                        if (tokens[index] == "(")
                            exprs.Add(ParseNode(tokens, ref index));
                        else
                            index++; // ignore table index like 0 or $t for now
                    }

                    ExpectToken(tokens, ref index, ")");

                    if (op == "table.get")
                    {
                        return new TableOpNode
                        {
                            Op = op,
                            Index = exprs.Count >= 1 ? exprs[0] : null,
                        };
                    }

                    if (op == "table.set")
                    {
                        return new TableOpNode
                        {
                            Op = op,
                            Index = exprs.Count >= 1 ? exprs[0] : null,
                            Value = exprs.Count >= 2 ? exprs[1] : null,
                        };
                    }

                    if (op == "table.size")
                    {
                        return new TableOpNode { Op = op };
                    }

                    if (op == "table.grow")
                    {
                        return new TableOpNode
                        {
                            Op = op,
                            Value = exprs.Count >= 1 ? exprs[0] : null,
                            Delta = exprs.Count >= 2 ? exprs[1] : null,
                        };
                    }
                    if (op == "table.fill")
{
    return new TableOpNode
    {
        Op = op,

        // table.fill reçoit :
        // destination, valeur, longueur
        Destination =
            exprs.Count >= 1
                ? exprs[0]
                : null,

        Value =
            exprs.Count >= 2
                ? exprs[1]
                : null,

        Length =
            exprs.Count >= 3
                ? exprs[2]
                : null,
    };
}

if (op == "table.copy")
{
    return new TableOpNode
    {
        Op = op,

        // table.copy reçoit :
        // destination, source, longueur
        Destination =
            exprs.Count >= 1
                ? exprs[0]
                : null,

        Source =
            exprs.Count >= 2
                ? exprs[1]
                : null,

        Length =
            exprs.Count >= 3
                ? exprs[2]
                : null,
    };
}

                    return new RawInstructionNode { Instruction = op };
                }
                else
                {
                    // Generic: skip atoms until ')', recurse only on nested lists

                    Log($"  📦 Generic instruction: {op}");
                    while (index < tokens.Count && tokens[index] != ")")
                    {
                        if (tokens[index] == "(")
                        {
                            var _ = ParseNode(tokens, ref index);
                        }
                        else
                        {
                            index++; // plain atom
                        }
                    }
                    ExpectToken(tokens, ref index, ")");
                    return new RawInstructionNode { Instruction = op };
                }
            }
            else if (tokens[index] == ")")
            {
                // Better context around the error
                int from = Math.Max(0, index - 5);
                int to = Math.Min(tokens.Count - 1, index + 5);
                var window = string.Join(" ", tokens.GetRange(from, to - from + 1));
                throw new Exception(
                    $"❌ Parenthèse fermante inattendue. Contexte: ... {window} ..."
                );
            }
            else
            {
                Log($"📌 Isolated instruction: {tokens[index]}");
                return new RawInstructionNode { Instruction = tokens[index++] };
            }
        }

        private static bool IsMemoryOp(string op)
        {
            // loads
            if (
                op.EndsWith(".load")
                || op.Contains(".load8_")
                || op.Contains(".load16_")
                || op.Contains(".load32_")
            )
                return true;

            // stores (pour plus tard, mais on le détecte déjà)
            if (
                op.EndsWith(".store")
                || op.Contains(".store8")
                || op.Contains(".store16")
                || op.Contains(".store32")
            )
                return true;
            if (
                op == "memory.size"
                || op == "memory.grow"
                || op == "memory.fill"
                || op == "memory.copy"
            )
                return true;

            return false;
        }

private static bool IsTableOp(string op) =>
    op
        is "table.get"
        or "table.set"
        or "table.size"
        or "table.grow"
        or "table.fill"
        or "table.copy";

        private static void ParseOffsetAlign(
            List<string> tokens,
            ref int index,
            out int offset,
            out int align
        )
        {
            offset = 0;
            align = 0;

            // Les immediates arrivent comme des atomes: "offset=16", "align=4"
            while (index < tokens.Count)
            {
                string t = tokens[index];
                if (t == ")" || t == "(")
                    break;

                if (t.StartsWith("offset=", StringComparison.Ordinal))
                {
                    int.TryParse(t.AsSpan("offset=".Length), out offset);
                    index++;
                    continue;
                }
                if (t.StartsWith("align=", StringComparison.Ordinal))
                {
                    int.TryParse(t.AsSpan("align=".Length), out align);
                    index++;
                    continue;
                }

                // dès qu’on voit autre chose, on s’arrête (ça peut être une expr)
                break;
            }
        }

        private bool IsUnaryOp(string op) =>
            op == "drop"
            || op.EndsWith(".eqz")
            || op.EndsWith(".wrap_i64")
            || op.EndsWith(".abs")
            || op.EndsWith(".neg")
            || op.EndsWith(".sqrt")
            || op.EndsWith(".nearest")
            || op.EndsWith(".floor")
            || op.EndsWith(".clz")
            || op.EndsWith(".ctz")
            || op.EndsWith(".popcnt")
            || op.EndsWith(".ceil")
            || op.EndsWith(".trunc")
            || op.Contains(".extend", StringComparison.Ordinal)
            || op.Contains(".trunc_")
            || op.Contains(".convert_")
            || op == "f32.demote_f64"
            || op == "f64.promote_f32"
            || op == "i32.reinterpret_f32"
            || op == "f32.reinterpret_i32"
            || op == "i64.reinterpret_f64"
            || op == "f64.reinterpret_i64";

        private bool IsBinaryOp(string op) =>
            op.EndsWith(".add")
            || op.EndsWith(".sub")
            || op.EndsWith(".mul")
            || op.EndsWith(".div_s")
            || op.EndsWith(".div_u")
            || op.EndsWith(".div")
            || op.EndsWith(".eq")
            || op.EndsWith(".ne")
            || op.EndsWith(".lt_s")
            || op.EndsWith(".lt_u")
            || op.EndsWith(".le_s")
            || op.EndsWith(".le_u")
            || op.EndsWith(".le")
            || op.EndsWith(".lt")
            || op.EndsWith(".gt_s")
            || op.EndsWith(".gt_u")
            || op.EndsWith(".ge_s")
            || op.EndsWith(".ge_u")
            || op.EndsWith(".ge")
            || op.EndsWith(".gt")
            || op.EndsWith(".min")
            || op.EndsWith(".max")
            // bitwise
            || op.EndsWith(".and")
            || op.EndsWith(".or")
            || op.EndsWith(".xor")
            || op.EndsWith(".shl")
            || op.EndsWith(".shr_s")
            || op.EndsWith(".shr_u")
            || op.EndsWith(".rotl")
            || op.EndsWith(".rotr")
            || op.EndsWith(".rem_s")
            || op.EndsWith(".rem_u")
            || op.EndsWith(".copysign");

        private void ExpectToken(List<string> tokens, ref int index, string expected)
        {
            if (index >= tokens.Count || tokens[index] != expected)
            {
                int from = Math.Max(0, index - 5);
                int to = Math.Min(tokens.Count - 1, index + 5);
                var window = string.Join(" ", tokens.GetRange(from, to - from + 1));
                throw new Exception(
                    $"❌ Parenthèse '{expected}' attendue. Contexte: ... {window} ..."
                );
            }
            index++;
        }

        private string ConvertWatToWasm(string watPath)
        {
            string wasmPath = Path.ChangeExtension(watPath, ".wasm");
            var psi = new ProcessStartInfo
            {
                FileName = "wat2wasm",
                Arguments = $"--debug-names --enable-annotations {watPath} -o {wasmPath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi)!;
            proc.WaitForExit();
            if (proc.ExitCode != 0)
                throw new Exception("wat2wasm failed: " + proc.StandardError.ReadToEnd());

            return wasmPath;
        }

        // ===== Binaryen wrapper imports =====

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr LoadWasmTextFile(string filename);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetFunctionCount(IntPtr module);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetFunctionNameByIndex(IntPtr module, int index);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetFunctionBodyText(IntPtr module, int index); // strdup'ed; must FreeCString

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreeCString(IntPtr s);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool ValidateModule(IntPtr module);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern void PrintModuleAST(IntPtr module);

        [DllImport(
            "libbinaryenwrapper",
            EntryPoint = "GetFunctionParamCount",
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern int GetFunctionParamCount(IntPtr module, int index);

        [DllImport(
            "libbinaryenwrapper",
            EntryPoint = "GetFunctionResultCount",
            CallingConvention = CallingConvention.Cdecl
        )]
        private static extern int GetFunctionResultCount(IntPtr module, int index);

        // ===== Globals =====

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern int GetGlobalCount(IntPtr module);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetGlobalNameByIndex(IntPtr module, int index); // owned by Binaryen (do not free)

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool GetGlobalIsMutableByIndex(IntPtr module, int index);

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetGlobalTypeByIndex(IntPtr module, int index); // returns "i32"/"i64"/"f32"/"f64" as strdup'ed (or owned: depends on your wrapper)

        [DllImport("libbinaryenwrapper", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetGlobalInitConstByIndex(IntPtr module, int index); // strdup'ed string, use FreeCString
    }
}
