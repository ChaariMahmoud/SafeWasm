using System.Collections.Generic;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
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

        private void AddPreludeTable(
            BoogieProgram program
        )
        {
            var wasmValueType =
                new BoogieCtorType(
                    "WasmValue"
                );

            var tableType =
                new BoogieMapType(
                    BoogieType.Int,
                    wasmValueType
                );

            // =====================================================
            // Global table state
            // =====================================================

            program.Declarations.Add(
                new BoogieGlobalVariable(
                    new BoogieTypedIdent(
                        "$table",
                        tableType
                    )
                )
            );

            program.Declarations.Add(
                new BoogieGlobalVariable(
                    new BoogieTypedIdent(
                        "$table_size",
                        BoogieType.Int
                    )
                )
            );

            // table.set modifies only the contents.
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

            // table.grow modifies contents and size.
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

            // =====================================================
            // table_get
            // =====================================================

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
                    new BoogieIdentifierExpr(
                        "result"
                    );

                BoogieExpr tableValue =
                    new BoogieMapSelect(
                        new BoogieIdentifierExpr(
                            "$table"
                        ),
                        new BoogieIdentifierExpr(
                            "idx"
                        )
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

                        // Toute valeur récupérée est une référence.
                        IsReferenceValue(
                            result
                        ),
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

                // Pas d’implementation :
                // table_get est définie abstraitement par son contrat.
            }

            // =====================================================
            // table_set
            // =====================================================

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

                        new BoogieFormalParam(
                            new BoogieTypedIdent(
                                "value",
                                wasmValueType
                            )
                        ),
                    };

                var preconditions =
                    new List<BoogieExpr>
                    {
                        IsReferenceValue(
                            new BoogieIdentifierExpr(
                                "value"
                            )
                        )
                    };

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_set",
                        ins,
                        new(),
                        new()
                        {
                            new BoogieAttribute(
                                "inline",
                                1
                            )
                        },
                        tableOnlyMods,
                        preconditions,
                        new()
                    )
                );

                var body =
                    new BoogieStmtList();

                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieMapSelect(
                            new BoogieIdentifierExpr(
                                "$table"
                            ),
                            new BoogieIdentifierExpr(
                                "idx"
                            )
                        ),
                        new BoogieIdentifierExpr(
                            "value"
                        )
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation(
                        "table_set",
                        ins,
                        new(),
                        new(),
                        body
                    )
                );
            }

            // =====================================================
            // table_size
            // =====================================================

            {
                var outs =
                    new List<BoogieVariable>
                    {
                        new BoogieFormalParam(
                            new BoogieTypedIdent(
                                "result",
                                BoogieType.Int
                            )
                        ),
                    };

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_size",
                        new(),
                        outs,
                        new()
                        {
                            new BoogieAttribute(
                                "inline",
                                1
                            )
                        },
                        new(),
                        new(),
                        new()
                    )
                );

                var body =
                    new BoogieStmtList();

                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr(
                            "result"
                        ),
                        new BoogieIdentifierExpr(
                            "$table_size"
                        )
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation(
                        "table_size",
                        new(),
                        outs,
                        new(),
                        body
                    )
                );
            }

            // =====================================================
            // table_grow
            // =====================================================

            {
                var ins =
                    new List<BoogieVariable>
                    {
                        new BoogieFormalParam(
                            new BoogieTypedIdent(
                                "value",
                                wasmValueType
                            )
                        ),

                        new BoogieFormalParam(
                            new BoogieTypedIdent(
                                "delta",
                                BoogieType.Int
                            )
                        ),
                    };

                var outs =
                    new List<BoogieVariable>
                    {
                        new BoogieFormalParam(
                            new BoogieTypedIdent(
                                "oldSize",
                                BoogieType.Int
                            )
                        ),
                    };

                var preconditions =
                    new List<BoogieExpr>
                    {
                        IsReferenceValue(
                            new BoogieIdentifierExpr(
                                "value"
                            )
                        ),

                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.GE,
                            new BoogieIdentifierExpr(
                                "delta"
                            ),
                            new BoogieLiteralExpr(0)
                        ),
                    };

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_grow",
                        ins,
                        outs,
                        new()
                        {
                            new BoogieAttribute(
                                "inline",
                                1
                            )
                        },
                        tableAndSizeMods,
                        preconditions,
                        new()
                    )
                );

                var body =
                    new BoogieStmtList();

                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr(
                            "oldSize"
                        ),
                        new BoogieIdentifierExpr(
                            "$table_size"
                        )
                    )
                );

                body.AddStatement(
                    new BoogieAssignCmd(
                        new BoogieIdentifierExpr(
                            "$table_size"
                        ),
                        new BoogieBinaryOperation(
                            BoogieBinaryOperation.Opcode.ADD,
                            new BoogieIdentifierExpr(
                                "$table_size"
                            ),
                            new BoogieIdentifierExpr(
                                "delta"
                            )
                        )
                    )
                );

                program.Declarations.Add(
                    new BoogieImplementation(
                        "table_grow",
                        ins,
                        outs,
                        new(),
                        body
                    )
                );
            }
        }
    }
}