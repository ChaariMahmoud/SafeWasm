using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeBitwise(BoogieProgram program)
        {
            AddBitwiseFunction(program, "bv_and");
            AddBitwiseFunction(program, "bv_or");
            AddBitwiseFunction(program, "bv_xor");
            AddBitwiseFunction(program, "bv_shl");
            AddBitwiseFunction(program, "bv_shr_s");
            AddBitwiseFunction(program, "bv_shr_u");
            AddBitwiseFunction(program, "bv_rotl");
            AddBitwiseFunction(program, "bv_rotr");
        }

        private void AddPreludeIntOps(BoogieProgram program)
        {
            AddBinaryRealFunction(program, "int_rem");
        }

        private static void AddBinaryRealFunction(BoogieProgram program, string name)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
            var y = new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Real));
            var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));

            program.Declarations.Add(new BoogieFunction(name, new() { x, y }, new() { r }));
        }

        private void AddPreludeIntUnaryOps(BoogieProgram program)
        {
            AddUnaryRealFunction(program, "int_clz");
            AddUnaryRealFunction(program, "int_ctz");
            AddUnaryRealFunction(program, "int_popcnt");
        }

        private static void AddUnaryRealFunction(BoogieProgram program, string name)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
            var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));

            program.Declarations.Add(new BoogieFunction(name, new() { x }, new() { r }));
        }

        private static void AddBitwiseFunction(BoogieProgram program, string name)
        {
            var x = new BoogieFormalParam(new BoogieTypedIdent("x", BoogieType.Real));
            var y = new BoogieFormalParam(new BoogieTypedIdent("y", BoogieType.Real));
            var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));

            program.Declarations.Add(new BoogieFunction(name, new() { x, y }, new() { r }));
        }
    }
}
