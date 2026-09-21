using System.Collections.Generic;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {

        var tableOnlyMods =
    new List<BoogieGlobalVariable>
    {
        new BoogieGlobalVariable(
            new BoogieTypedIdent(
                "$table",
                tableType
            )
        ),
    };

var tableAndSizeMods =
    new List<BoogieGlobalVariable>
    {
        new BoogieGlobalVariable(
            new BoogieTypedIdent(
                "$table",
                tableType
            )
        ),

        new BoogieGlobalVariable(
            new BoogieTypedIdent(
                "$table_size",
                BoogieType.Int
            )
        ),
    };

        private static BoogieExpr IsReferenceValue(
    BoogieExpr value
)
{
    return new BoogieBinaryOperation(
        BoogieBinaryOperation.Opcode.OR,

        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.OR,

            IsCtor(
                value,
                "NullFuncRef"
            ),

            IsCtor(
                value,
                "FuncRef"
            )
        ),

        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.OR,

            IsCtor(
                value,
                "NullExternRef"
            ),

            IsCtor(
                value,
                "ExternRef"
            )
        )
    );
}
        private void AddPreludeTable(BoogieProgram program)
        {

            var wasmValueType =
    new BoogieCtorType("WasmValue");

var tableType =
    new BoogieMapType(
    BoogieType.Int,
    new BoogieCtorType("WasmValue")
);
            program.Declarations.Add(
                new BoogieGlobalVariable(
new BoogieTypedIdent(
    "$table",
new BoogieMapType(
    BoogieType.Int,
    new BoogieCtorType("WasmValue")
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
    new BoogieCtorType("WasmValue")
)
)
                ),
                new BoogieGlobalVariable(new BoogieTypedIdent("$table_size", BoogieType.Int)),
            };

// table_get(idx) returns a WebAssembly reference.
{
    var ins =
        new List<BoogieVariable>
        {
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    "idx",
                    BoogieType.Int
                )
            ),
        };

    var outs =
        new List<BoogieVariable>
        {
            new BoogieFormalParam(
                new BoogieTypedIdent(
                    "result",
                    wasmValueType
                )
            ),
        };

    BoogieExpr result =
        new BoogieIdentifierExpr("result");

    BoogieExpr tableValue =
        new BoogieMapSelect(
            new BoogieIdentifierExpr("$table"),
            new BoogieIdentifierExpr("idx")
        );

    var postconditions =
        new List<BoogieExpr>
        {
            // result == $table[idx]
            new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.EQ,
                result,
                tableValue
            ),

            // La table ne contient que des références.
            IsReferenceValue(result),
        };

    program.Declarations.Add(
        new BoogieProcedure(
            "table_get",
            ins,
            outs,
            null,
            new(),
            new(),
            postconditions
        )
    );
}

            // table_set(idx, value)
            {
                
var valueExpr =
    new BoogieIdentifierExpr("value");

var preconditions =
    new List<BoogieExpr>
    {
        IsReferenceValue(valueExpr)
    };

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
                        preconditions,
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
