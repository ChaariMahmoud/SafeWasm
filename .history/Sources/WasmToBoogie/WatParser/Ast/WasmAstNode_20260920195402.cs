using System.Collections.Generic;

namespace WasmToBoogie.Parser.Ast
{
    public enum WasmValueType
    {
        I32,
        I64,
        F32,
        F64,
    }

    public abstract class WasmNode { }

public enum WasmReferenceKind
{
    Func,
    Extern,
}

public class RefNullNode : WasmNode
{
    public WasmReferenceKind Kind { get; set; }
}

public class RefFuncNode : WasmNode
{
    
    public string Target { get; set; } = "";
}    

    public class ConstNode : WasmNode
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }

    public class UnaryOpNode : WasmNode
    {
        public string Op { get; set; }
        public WasmNode Operand { get; set; }
    }

    public class BinaryOpNode : WasmNode
    {
        public string Op { get; set; }
        public WasmNode Left { get; set; }
        public WasmNode Right { get; set; }
    }

    public class IfNode : WasmNode
    {
        public WasmValueType? ResultType { get; set; } = null;
        public WasmNode Condition { get; set; }
        public List<WasmNode> ThenBody { get; set; } = new();
        public List<WasmNode>? ElseBody { get; set; } = null;
    }

    public class BlockNode : WasmNode
    {
        public WasmValueType? ResultType { get; set; }
        public string? Label { get; set; }
        public List<WasmNode> Body { get; set; } = new();
    }

    public class LoopNode : WasmNode
    {
        public WasmValueType? ResultType { get; set; }
        public string? Label { get; set; }
        public List<WasmNode> Body { get; set; } = new();
    }

    public class BrNode : WasmNode
    {
        public string Label { get; set; }
    }

    public class BrIfNode : WasmNode
    {
        public string Label { get; set; }
        public WasmNode Condition { get; set; }
    }

    public class LocalGetNode : WasmNode
    {
        public int? Index { get; set; }
        public string? Name { get; set; }
    }

    public class LocalSetNode : WasmNode
    {
        public int? Index { get; set; }
        public string? Name { get; set; }
        public WasmNode? Value { get; set; }
    }

    public class LocalTeeNode : WasmNode
    {
        public int? Index { get; set; }
        public string? Name { get; set; }
    }

    public class CallNode : WasmNode
    {
        public string Target { get; set; }
        public List<WasmNode> Args { get; set; } = new(); // e.g., $compute or function index
    }

    public class CallIndirectNode : WasmNode
    {
        public string? TypeUse { get; set; } // e.g. "0" or "$t0" if present
        public WasmNode CalleeIndex { get; set; } // expression producing the table index
        public List<WasmNode> Args { get; set; } = new();
    }

    public class ReturnCallNode : WasmNode
    {
        public string Target { get; set; }
        public List<WasmNode> Args { get; set; } = new();
    }

    public class ReturnCallIndirectNode : WasmNode
    {
        public string? TypeUse { get; set; }
        public WasmNode CalleeIndex { get; set; }
        public List<WasmNode> Args { get; set; } = new();
    }

    public class GlobalDeclNode : WasmNode
    {
        public int? Index { get; set; }
        public string? Name { get; set; }
        public bool IsMutable { get; set; }
        public WasmValueType ValType { get; set; }
        public WasmNode? Init { get; set; }
    }

    public class GlobalGetNode : WasmNode
    {
        public int? Index { get; set; }
        public string? Name { get; set; } // e.g., $g
    }

    public class GlobalSetNode : WasmNode
    {
        public int? Index { get; set; }
        public string? Name { get; set; } // e.g., $g
        public WasmNode? Value { get; set; } // folded form: (global.set $g (i32.const 5))
    }

    public class RawInstructionNode : WasmNode
    {
        public string Instruction { get; set; }
    }

    public class UnreachableNode : WasmNode { }

    public class SelectNode : WasmNode
    {
        public WasmNode V1 { get; set; } // first value
        public WasmNode V2 { get; set; } // second value
        public WasmNode Cond { get; set; } // condition (nonzero = true)
    }

public class WasmFunction
{
    public string? Name { get; set; }

    public List<WasmNode> Body { get; set; } = new();

    public int ParamCount { get; set; } = 0;
    public int LocalCount { get; set; } = 0;
    public int ResultCount { get; set; }

    public Dictionary<string, int> LocalIndexByName { get; set; } = new();

    public List<string?> ParamNames { get; set; } = new();

    public List<WasmValueType> ParamTypes { get; set; } = new();

    // Types des variables locales, sans inclure les paramètres.
    public List<WasmValueType> LocalTypes { get; set; } = new();

    public List<WasmValueType> ResultTypes { get; set; } = new();
}

    public class WasmGlobal
    {
        public int Index { get; set; }
        public string? Name { get; set; }
        public bool IsMutable { get; set; }
        public string ValType { get; set; }
        public string? InitConst { get; set; }
    }

    public class WasmModule
    {
        public bool HasTable { get; set; } = false;

public int InitialTableSize { get; set; } = 0;

public int? MaximumTableSize { get; set; } = null;

public WasmReferenceKind TableReferenceKind { get; set; }
    = WasmReferenceKind.Func;
        public List<WasmFuncType> Types { get; set; } = new();
        public int InitialMemoryPages { get; set; } = 0;
        public List<WasmGlobal> Globals { get; } = new();
        public Dictionary<string, int> GlobalIndexByName { get; } = new();

        public List<WasmFunction> Functions { get; set; } = new();

        public List<WasmFunctionRef> FunctionIndexSpace { get; set; } = new();
        public Dictionary<string, int> FunctionIndexByName { get; set; } = new();

        public ModuleSpec? Spec { get; set; }

        public List<WasmImport> Imports { get; set; } = new();
        public List<WasmExport> Exports { get; set; } = new();

        public LinkingMode LinkingMode { get; set; } = LinkingMode.Off;
    }

    public class ReturnNode : WasmNode { }

    public class NopNode : WasmNode { }

    public class BrTableNode : WasmNode
    {
        public List<string> Targets { get; set; } = new();
        public string Default { get; set; } = "";
        public WasmNode? Selector { get; set; }
    }

    public class MemoryOpNode : WasmNode
    {
        public string Op { get; set; } // "i32.load", "i32.store8", ...
        public int Offset { get; set; } = 0; // from "offset=..."
        public int Align { get; set; } = 0; // from "align=..."
        public WasmNode? Address { get; set; } // folded form: (i32.load (i32.const ...))
        public WasmNode? Value { get; set; }
        public WasmNode? Length { get; set; } // folded store form: (i32.store (addr) (val))
        public int MemoryIndex { get; set; } = 0;
    }

public class TableOpNode : WasmNode
{
    public string Op { get; set; } = "";

    // table.get / table.set
    public WasmNode? Index { get; set; }

    // table.set / table.grow / table.fill
    public WasmNode? Value { get; set; }

    // table.grow
    public WasmNode? Delta { get; set; }

    // table.fill / table.copy
    public WasmNode? Destination { get; set; }

    // table.copy
    public WasmNode? Source { get; set; }

    // table.fill / table.copy
    public WasmNode? Length { get; set; }
}

    public enum LinkingMode
    {
        On,
        Off,
    }

    public enum WasmImportKind
    {
        Func,
        Global,
        Memory,
        Table,
    }

    public class WasmFuncType
    {
        public int Index { get; set; }
        public List<WasmValueType> ParamTypes { get; set; } = new();
        public List<WasmValueType> ResultTypes { get; set; } = new();
    }

    public class WasmFunctionRef
    {
        public int Index { get; set; }
        public string? Name { get; set; }
        public bool IsImport { get; set; }
        public WasmImport? Import { get; set; }
        public WasmFunction? Function { get; set; }
    }

    public class WasmImport
    {
        public string ModuleName { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string? InternalName { get; set; }
        public WasmImportKind Kind { get; set; }

        public int ParamCount { get; set; }
        public int ResultCount { get; set; }
        public List<WasmValueType> ParamTypes { get; set; } = new();
        public List<WasmValueType> ResultTypes { get; set; } = new();

        public bool IsResolved { get; set; } = false;
        public WasmFunction? LinkedFunction { get; set; }
    }

    public enum WasmExportKind
    {
        Func,
        Global,
        Memory,
        Table,
    }

    public class WasmExport
    {
        public string ExportName { get; set; } = "";
        public WasmExportKind Kind { get; set; }
        public string? InternalName { get; set; }
        public int? Index { get; set; }
    }
}
