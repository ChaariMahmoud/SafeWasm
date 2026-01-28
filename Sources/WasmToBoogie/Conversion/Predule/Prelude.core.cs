using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeCoreGlobals(BoogieProgram program)
        {
            // Stack + SP
            program.Declarations.Add(
                new BoogieGlobalVariable(
                    new BoogieTypedIdent(
                        "$stack",
                        new BoogieMapType(BoogieType.Int, BoogieType.Real)
                    )
                )
            );

            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$sp", BoogieType.Int))
            );

            // Temps
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp1", BoogieType.Real))
            );
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp2", BoogieType.Real))
            );
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$tmp3", BoogieType.Real))
            );

            // Globals init flag
            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$globals_inited", BoogieType.Bool))
            );
        }
    }
}
