using System.Collections.Generic;
using BoogieAST;

namespace WasmToBoogie.Conversion
{
    public partial class WasmAstToBoogie
    {
        private static BoogieExpr IsReferenceValue(BoogieExpr value)
        {
            return new BoogieBinaryOperation(
                BoogieBinaryOperation.Opcode.OR,
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.OR,
                    IsCtor(value, "NullFuncRef"),
                    IsCtor(value, "FuncRef")
                ),
                new BoogieBinaryOperation(
                    BoogieBinaryOperation.Opcode.OR,
                    IsCtor(value, "NullExternRef"),
                    IsCtor(value, "ExternRef")
                )
            );
        }

        private void AddPreludeTable(BoogieProgram program)
        {
            var wasmValueType = new BoogieCtorType("WasmValue");

            var tableType = new BoogieMapType(BoogieType.Int, wasmValueType);

            // =====================================================
            // Global table state
            // =====================================================

            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$table", tableType))
            );

            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$table_size", BoogieType.Int))
            );

            program.Declarations.Add(
                new BoogieGlobalVariable(new BoogieTypedIdent("$table_max", BoogieType.Int))
            );

            // table.set modifies only the contents.
            var tableOnlyMods = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$table", tableType)),
            };

            // table.grow modifies contents and size.
            var tableAndSizeMods = new List<BoogieGlobalVariable>
            {
                new BoogieGlobalVariable(new BoogieTypedIdent("$table", tableType)),
                new BoogieGlobalVariable(new BoogieTypedIdent("$table_size", BoogieType.Int)),
            };

            // =====================================================
            // table_get
            // =====================================================

            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("idx", BoogieType.Int)),
                };

                var outs = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("result", wasmValueType)),
                };

                var getPreconditions =
    new List<BoogieExpr>
    {
        // idx >= 0
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.GE,
            new BoogieIdentifierExpr("idx"),
            new BoogieLiteralExpr(0)
        ),

        // idx < $table_size
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.LT,
            new BoogieIdentifierExpr("idx"),
            new BoogieIdentifierExpr("$table_size")
        ),
    };

                BoogieExpr result = new BoogieIdentifierExpr("result");

                BoogieExpr tableValue = new BoogieMapSelect(
                    new BoogieIdentifierExpr("$table"),
                    new BoogieIdentifierExpr("idx")
                );

                var postconditions = new List<BoogieExpr>
                {
                    // result == $table[idx]
                    new BoogieBinaryOperation(BoogieBinaryOperation.Opcode.EQ, result, tableValue),
                    // Toute valeur récupérée est une référence.
                    IsReferenceValue(result),
                };

                program.Declarations.Add(
                    new BoogieProcedure("table_get", ins, outs, null, new(), getPreconditions, postconditions)
                );

                // Pas d’implementation :
                // table_get est définie abstraitement par son contrat.
            }

            // =====================================================
            // table_set
            // =====================================================

            {
                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("idx", BoogieType.Int)),
                    new BoogieFormalParam(new BoogieTypedIdent("value", wasmValueType)),
                };


                var preconditions =
    new List<BoogieExpr>
    {
        IsReferenceValue(
           new BoogieIdentifierExpr("value")
        ),

        // idx >= 0
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.GE,
            new BoogieIdentifierExpr("idx"),
            new BoogieLiteralExpr(0)
        ),

        // idx < $table_size
        new BoogieBinaryOperation(
            BoogieBinaryOperation.Opcode.LT,
            new BoogieIdentifierExpr("idx"),
            new BoogieIdentifierExpr("$table_size")
        ),
    };

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_set",
                        ins,
                        new(),
                        new() { new BoogieAttribute("inline", 1) },
                        tableOnlyMods,
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

            // =====================================================
            // table_size
            // =====================================================

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
            //
            // Succès :
            //   oldSize == old($table_size)
            //   $table_size == old($table_size) + delta
            //   les nouvelles cases contiennent value
            //
            // Échec :
            //   oldSize == -1
            //   la taille et la table restent inchangées
            {
                BoogieExpr Id(string name) => new BoogieIdentifierExpr(name);

                BoogieExpr Int(int value) => new BoogieLiteralExpr(value);

                BoogieExpr Bin(
                    BoogieBinaryOperation.Opcode op,
                    BoogieExpr left,
                    BoogieExpr right
                ) => new BoogieBinaryOperation(op, left, right);

                BoogieExpr Not(BoogieExpr expr) =>
                    new BoogieUnaryOperation(BoogieUnaryOperation.Opcode.NOT, expr);

                BoogieExpr Old(BoogieExpr expr) => new BoogieOldExpr(expr);

                var ins = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("value", wasmValueType)),
                    new BoogieFormalParam(new BoogieTypedIdent("delta", BoogieType.Int)),
                };

                var outs = new List<BoogieVariable>
                {
                    new BoogieFormalParam(new BoogieTypedIdent("oldSize", BoogieType.Int)),
                };

                var oldTableSize = Old(Id("$table_size"));

                var newTableSize = Bin(BoogieBinaryOperation.Opcode.ADD, oldTableSize, Id("delta"));

                // Aucun maximum si $table_max == -1.
                var hasNoMaximum = Bin(BoogieBinaryOperation.Opcode.EQ, Id("$table_max"), Int(-1));

                // Sinon, la nouvelle taille doit être <= maximum.
                var respectsMaximum = Bin(
                    BoogieBinaryOperation.Opcode.LE,
                    newTableSize,
                    Id("$table_max")
                );

                var growSucceeds = Bin(
                    BoogieBinaryOperation.Opcode.OR,
                    hasNoMaximum,
                    respectsMaximum
                );

                var preconditions = new List<BoogieExpr>
                {
                    IsReferenceValue(Id("value")),
                    Bin(BoogieBinaryOperation.Opcode.GE, Id("delta"), Int(0)),
                };

                var postconditions = new List<BoogieExpr>();

                // Succès ==> oldSize == ancienne taille.
                postconditions.Add(
                    Bin(
                        BoogieBinaryOperation.Opcode.IMP,
                        growSucceeds,
                        Bin(BoogieBinaryOperation.Opcode.EQ, Id("oldSize"), oldTableSize)
                    )
                );

                // Succès ==> nouvelle taille = ancienne taille + delta.
                postconditions.Add(
                    Bin(
                        BoogieBinaryOperation.Opcode.IMP,
                        growSucceeds,
                        Bin(BoogieBinaryOperation.Opcode.EQ, Id("$table_size"), newTableSize)
                    )
                );

                // Échec ==> oldSize == -1.
                postconditions.Add(
                    Bin(
                        BoogieBinaryOperation.Opcode.IMP,
                        Not(growSucceeds),
                        Bin(BoogieBinaryOperation.Opcode.EQ, Id("oldSize"), Int(-1))
                    )
                );

                // Échec ==> taille inchangée.
                postconditions.Add(
                    Bin(
                        BoogieBinaryOperation.Opcode.IMP,
                        Not(growSucceeds),
                        Bin(BoogieBinaryOperation.Opcode.EQ, Id("$table_size"), oldTableSize)
                    )
                );

                // Initialisation des nouvelles cases en cas de succès.
                {
                    var i = new BoogieIdentifierExpr("table_grow_i");

                    var tableAtI = new BoogieMapSelect(Id("$table"), i);

                    var inNewRegion = Bin(
                        BoogieBinaryOperation.Opcode.AND,
                        Bin(BoogieBinaryOperation.Opcode.LE, oldTableSize, i),
                        Bin(BoogieBinaryOperation.Opcode.LT, i, newTableSize)
                    );

                    var cellInitialized = Bin(
                        BoogieBinaryOperation.Opcode.EQ,
                        tableAtI,
                        Id("value")
                    );

                    var successAndNewRegion = Bin(
                        BoogieBinaryOperation.Opcode.AND,
                        growSucceeds,
                        inNewRegion
                    );

                    var quantifiedBody = Bin(
                        BoogieBinaryOperation.Opcode.IMP,
                        successAndNewRegion,
                        cellInitialized
                    );

                    postconditions.Add(
                        new BoogieQuantifiedExpr(
                            isForall: true,
                            qvars: new List<BoogieIdentifierExpr> { i },
                            qvarTypes: new List<BoogieType> { BoogieType.Int },
                            bodyExpr: quantifiedBody,
                            trigger: new List<BoogieExpr> { tableAtI }
                        )
                    );
                }

                // Les anciennes cases et les cases extérieures
                // à la nouvelle région restent inchangées en cas de succès.
                {
                    var i = new BoogieIdentifierExpr("table_frame_i");

                    var tableAtI = new BoogieMapSelect(Id("$table"), i);

                    var oldTableAtI = Old(new BoogieMapSelect(Id("$table"), i));

                    var beforeOldTable = Bin(BoogieBinaryOperation.Opcode.LT, i, oldTableSize);

                    var afterNewTable = Bin(BoogieBinaryOperation.Opcode.GE, i, newTableSize);

                    var outsideNewRegion = Bin(
                        BoogieBinaryOperation.Opcode.OR,
                        beforeOldTable,
                        afterNewTable
                    );

                    var preserveCell = Bin(BoogieBinaryOperation.Opcode.EQ, tableAtI, oldTableAtI);

                    var successOutsideRegion = Bin(
                        BoogieBinaryOperation.Opcode.AND,
                        growSucceeds,
                        outsideNewRegion
                    );

                    postconditions.Add(
                        new BoogieQuantifiedExpr(
                            isForall: true,
                            qvars: new List<BoogieIdentifierExpr> { i },
                            qvarTypes: new List<BoogieType> { BoogieType.Int },
                            bodyExpr: Bin(
                                BoogieBinaryOperation.Opcode.IMP,
                                successOutsideRegion,
                                preserveCell
                            ),
                            trigger: new List<BoogieExpr> { tableAtI }
                        )
                    );
                }

                // En cas d’échec, toute la table reste inchangée.
                {
                    var i = new BoogieIdentifierExpr("table_failure_i");

                    var tableAtI = new BoogieMapSelect(Id("$table"), i);

                    var oldTableAtI = Old(new BoogieMapSelect(Id("$table"), i));

                    var preserveCell = Bin(BoogieBinaryOperation.Opcode.EQ, tableAtI, oldTableAtI);

                    postconditions.Add(
                        new BoogieQuantifiedExpr(
                            isForall: true,
                            qvars: new List<BoogieIdentifierExpr> { i },
                            qvarTypes: new List<BoogieType> { BoogieType.Int },
                            bodyExpr: Bin(
                                BoogieBinaryOperation.Opcode.IMP,
                                Not(growSucceeds),
                                preserveCell
                            ),
                            trigger: new List<BoogieExpr> { tableAtI }
                        )
                    );
                }

                program.Declarations.Add(
                    new BoogieProcedure(
                        "table_grow",
                        ins,
                        outs,
                        // Procédure abstraite : pas d’inlining.
                        new List<BoogieAttribute>(),
                        tableAndSizeMods,
                        preconditions,
                        postconditions
                    )
                );
            }
        }
    }
}
