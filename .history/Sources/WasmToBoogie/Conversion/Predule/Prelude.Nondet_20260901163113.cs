using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
private void AddPreludeNondet(
    BoogieProgram program
)
{
    AddNondetFunction(
        program,
        "nd_i32",
        BoogieType.Int
    );

    AddNondetFunction(
        program,
        "nd_i64",
        BoogieType.Int
    );

    AddNondetFunction(
        program,
        "nd_f32",
        BoogieType.Real
    );

    AddNondetFunction(
        program,
        "nd_f64",
        BoogieType.Real
    );
}

private static void AddNondetFunction(
    BoogieProgram program,
    string name,
    BoogieType resultType
)
{
    var result =
        new BoogieFormalParam(
            new BoogieTypedIdent(
                "result",
                resultType
            )
        );

    program.Declarations.Add(
        new BoogieFunction(
            name,
            new(),
            new() { result }
        )
    );
}
    }
}
