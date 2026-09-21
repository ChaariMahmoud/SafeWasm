using System.Collections.Generic;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private void AddPreludeTable(BoogieProgram program)
        {

            var wasmValueType =
    new BoogieCtorType("WasmValue");

var tableType =
    new BoogieMapType(
        BoogieType.Int,
        wasmValueType
    );
            program.Declarations.Add(
                new BoogieGlobalVariable(
new BoogieTypedIdent(
    "$table",
    new BoogieMapType(
        BoogieType.Int,
        BoogieType.Real
    )
)
                )
            );

            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$table_size", BoogieType.Int))
            );

            var tableMods = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(
new BoogieTypedIdent(
    "$table",
    new BoogieMapType(
        BoogieType.Int,
        BoogieType.Real
    )
)
                ),
                new BoogieGlobalVariable(new BoogieTypedIdent("$table_size", BoogieType.Int)),
            };

            // table_get(idx) returns result
            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("idx", BoogieType.Int)),
                };

                var outs = new List<BoogieVariable>
                {
                   new BoogieFormalParam(
    new BoogieTypedIdent(
        "result",
        wasmValueType
    )
),
                };

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_get",
                        ins,
                        outs,
                        new() { new BoogieAttribute("inline", 1) },
                        new(),
                        new(),
                        new()
                    )
                );

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieMapSelect(
                            new BoogieIdentifierExpr("$table"),
                            new BoogieIdentifierExpr("idx")
                        )
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation("table_get", ins, outs, new(), body)
                );
            }

            // table_set(idx, value)
            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("idx", BoogieType.Int)),
                   new BoogieFormalParam(
    new BoogieTypedIdent(
        "value",
        wasmValueType
    )
),
                };

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_set",
                        ins,
                        new(),
                        new() { new BoogieAttribute("inline", 1) },
                        tableMods,
                        new(),
                        new()
                    )
                );

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieMapSelect(
                            new BoogieIdentifierExpr("$table"),
                            new BoogieIdentifierExpr("idx")
                        ),
                        new BoogieIdentifierExpr("value")
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation("table_set", ins, new(), new(), body)
                );
            }

            // table_size() returns result
            {
                var outs = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("result", BoogieType.Int)),
                };

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_size",
                        new(),
                        outs,
                        new() { new BoogieAttribute("inline", 1) },
                        new(),
                        new(),
                        new()
                    )
                );

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("result"),
                        new BoogieIdentifierExpr("$table_size")
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation("table_size", new(), outs, new(), body)
                );
            }

            // table_grow(value, delta) returns oldSize
            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(
    new BoogieTypedIdent(
        "value",
        wasmValueType
    )
),
                    new BoogieFormalParam(new BoogieTypedIdent("delta", BoogieType.Int)),
                };

                var outs = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("oldSize", BoogieType.Int)),
                };

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_grow",
                        ins,
                        outs,
                        new() { new BoogieAttribute("inline", 1) },
                        tableMods,
                        new(),
                        new()
                    )
                );

                var body = new BoogieStmtList();
                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("oldSize"),
                        new BoogieIdentifierExpr("$table_size")
                    )
                );

                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr("$table_size"),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            new BoogieIdentifierExpr("$table_size"),
                            new BoogieIdentifierExpr("delta")
                        )
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation("table_grow", ins, outs, new(), body)
                );
            }
        }
    }
}
