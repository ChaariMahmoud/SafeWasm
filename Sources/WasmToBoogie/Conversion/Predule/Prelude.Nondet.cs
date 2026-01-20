using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeNondet(BoogieProgram program)
        {
            var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
            program.Declarations.Add(new BoogieFunction("nd_real", new(), new() { res }));
        }
    }
}
