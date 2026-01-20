using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeBoolCasts(BoogieProgram program)
        {
            // bool_to_real
            {
                var b = new BoogieFormalParam(new BoogieTypedIdent("b", BoogieType.Bool));
                var body = new BoogieITE(
                    new BoogieIdentifierExpr("b"),
                    new BoogieLiteralExpr(new Pfloat(1)),
                    new BoogieLiteralExpr(new Pfloat(0))
                );
                program.Declarations.Add(
                    new BoogieFunctionDef("bool_to_real", new() { b }, BoogieType.Real, body)
                );
            }

            // real_to_bool
            {
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                var body = new BoogieITE(
                    new BoogieBinaryOperation(
                        BoogieBinaryOperation.Opcode.EQ,
                        new BoogieIdentifierExpr("r"),
                        new BoogieLiteralExpr(new Pfloat(0))
                    ),
                    new BoogieLiteralExpr(false),
                    new BoogieLiteralExpr(true)
                );
                program.Declarations.Add(
                    new BoogieFunctionDef("real_to_bool", new() { r }, BoogieType.Bool, body)
                );
            }
        }

        private void AddPreludeNumericCasts(BoogieProgram program)
        {
            // real_to_int
            {
                var r = new BoogieFormalParam(new BoogieTypedIdent("r", BoogieType.Real));
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Int));
                program.Declarations.Add(
                    new BoogieFunction("real_to_int", new() { r }, new() { res })
                );
            }

            // int_to_real
            {
                var i = new BoogieFormalParam(new BoogieTypedIdent("i", BoogieType.Int));
                var res = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("int_to_real", new() { i }, new() { res })
                );
            }

            // bits32/64_to_real
            {
                var i = new BoogieFormalParam(new BoogieTypedIdent("i", BoogieType.Int));
                var r = new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Real));
                program.Declarations.Add(
                    new BoogieFunction("bits32_to_real", new() { i }, new() { r })
                );
                program.Declarations.Add(
                    new BoogieFunction("bits64_to_real", new() { i }, new() { r })
                );
            }
        }
    }
}
