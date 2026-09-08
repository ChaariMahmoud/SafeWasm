// ==============================================================================
// 1. TYPES ET DATATYPES DE BASE
// ==============================================================================

datatype Option<T> { NoneOpt(), SomeOpt(val: T) }
type Ident;
datatype ValType { I32Type(), I64Type(), F32Type(), F64Type() }
datatype ValTypeList { VTNil(), VTCons(head: ValType, tail: ValTypeList) }

datatype Value {
  UndefVal(),
  I32Val(i: int),
  I64Val(i: int),
  F32Val(f: real),
  F64Val(f: real)
}

// ==============================================================================
// 2. ÉTAT DU SYSTÈME
// ==============================================================================

datatype Stack {
  NilStack(),
  ConsStack(top: Value, rest: Stack)
}

datatype State {
  State(
    stStack: Stack,
    stMemory: [int]int,
    stMemSize: int,
    stLocals: [int]Value,
    stGlobals: [int]Value,
    stTables: [int]Value
  )
}

// ==============================================================================
// 3. DATATYPES D'OPÉRATIONS ET AST
// ==============================================================================

datatype BinOp { Add(), Sub(), Mul(), DivS() }
datatype UnOp  { Neg(), Eqz() }
datatype MemOp { Load(), Store() }
datatype TableOp { TableGet(), TableSet() }

datatype Instr {
  ConstI32(val: int),
  UnaryOpNode(uOp: UnOp, uOperand: Instr),
  BinaryOpNode(bOp: BinOp, bLeft: Instr, bRight: Instr),
  MemoryOpNode(moOp: MemOp, moOffset: int, moAlign: int, moAddress: Option Instr, moValue: Option Instr, moLength: Option Instr, moMemoryIndex: int),
  TableOpNode(toOp: TableOp, toIndex: Option Instr, toValue: Option Instr, toDelta: Option Instr)
}

datatype InstrList { INil(), ICons(head: Instr, tail: InstrList) }
datatype IdentList { IdNil(), IdCons(head: Ident, tail: IdentList) }

datatype Param { Param(pName: Option Ident, pType: ValType) }
datatype ParamList { PNil(), PCons(head: Param, tail: ParamList) }

datatype Function {
  Function(
    fnName: Option Ident,
    fnParams: ParamList,
    fnResultTypes: ValTypeList,
    fnBody: InstrList
  )
}

datatype FunctionList { FNil(), FCons(head: Function, tail: FunctionList) }
datatype Module { Module(mdFunctions: FunctionList) }

// ==============================================================================
// 4. FONCTIONS SÉMANTIQUES PURES (Toutes avec {:inline})
// ==============================================================================
// --- Opérations Binaires ---
function {:inline} eval_Add(st: State): State {
  if (st->stStack is ConsStack && st->stStack->rest is ConsStack) then
    var v1 := st->stStack->top;
    var v2 := st->stStack->rest->top;
    var restStack := st->stStack->rest->rest;
    if (v2 is I32Val && v1 is I32Val) then
      State(ConsStack(I32Val(v2->i + v1->i), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is I64Val && v1 is I64Val) then
      State(ConsStack(I64Val(v2->i + v1->i), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is F32Val && v1 is F32Val) then
      State(ConsStack(F32Val(v2->f + v1->f), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is F64Val && v1 is F64Val) then
      State(ConsStack(F64Val(v2->f + v1->f), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else
      State(ConsStack(UndefVal(), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_Sub(st: State): State {
  if (st->stStack is ConsStack && st->stStack->rest is ConsStack) then
    var v1 := st->stStack->top;
    var v2 := st->stStack->rest->top;
    var restStack := st->stStack->rest->rest;
    if (v2 is I32Val && v1 is I32Val) then
      State(ConsStack(I32Val(v2->i - v1->i), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is I64Val && v1 is I64Val) then
      State(ConsStack(I64Val(v2->i - v1->i), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is F32Val && v1 is F32Val) then
      State(ConsStack(F32Val(v2->f - v1->f), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is F64Val && v1 is F64Val) then
      State(ConsStack(F64Val(v2->f - v1->f), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else
      State(ConsStack(UndefVal(), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_Mul(st: State): State {
  if (st->stStack is ConsStack && st->stStack->rest is ConsStack) then
    var v1 := st->stStack->top;
    var v2 := st->stStack->rest->top;
    var restStack := st->stStack->rest->rest;
    if (v2 is I32Val && v1 is I32Val) then
      State(ConsStack(I32Val(v2->i * v1->i), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is I64Val && v1 is I64Val) then
      State(ConsStack(I64Val(v2->i * v1->i), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is F32Val && v1 is F32Val) then
      State(ConsStack(F32Val(v2->f * v1->f), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is F64Val && v1 is F64Val) then
      State(ConsStack(F64Val(v2->f * v1->f), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else
      State(ConsStack(UndefVal(), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_DivS(st: State): State {
  if (st->stStack is ConsStack && st->stStack->rest is ConsStack) then
    var v1 := st->stStack->top;
    var v2 := st->stStack->rest->top;
    var restStack := st->stStack->rest->rest;
    if (v2 is I32Val && v1 is I32Val && v1->i != 0) then
      State(ConsStack(I32Val(v2->i div v1->i), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v2 is I64Val && v1 is I64Val && v1->i != 0) then
      State(ConsStack(I64Val(v2->i div v1->i), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else
      State(ConsStack(UndefVal(), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_BinOp(op: BinOp, st: State): State {
  if (op is Add) then eval_Add(st)
  else if (op is Sub) then eval_Sub(st)
  else if (op is Mul) then eval_Mul(st)
  else if (op is DivS) then eval_DivS(st)
  else st
}

// --- Opérations Unaires ---
function {:inline} eval_Neg(st: State): State {
  if (st->stStack is ConsStack) then
    var v := st->stStack->top;
    var restStack := st->stStack->rest;
    if (v is I32Val) then
      State(ConsStack(I32Val(-(v->i)), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v is I64Val) then
      State(ConsStack(I64Val(-(v->i)), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v is F32Val) then
      State(ConsStack(F32Val(-(v->f)), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v is F64Val) then
      State(ConsStack(F64Val(-(v->f)), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else
      State(ConsStack(UndefVal(), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_Eqz(st: State): State {
  if (st->stStack is ConsStack) then
    var v := st->stStack->top;
    var restStack := st->stStack->rest;
    // En WebAssembly, eqz prend un i32 ou i64 et renvoie TOUJOURS un i32 (1 si zéro, 0 sinon)
    if (v is I32Val) then
      State(ConsStack(I32Val(if v->i == 0 then 1 else 0), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else if (v is I64Val) then
      State(ConsStack(I32Val(if v->i == 0 then 1 else 0), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
    else
      State(ConsStack(UndefVal(), restStack), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_UnOp(op: UnOp, st: State): State {
  if (op is Neg) then eval_Neg(st)
  else if (op is Eqz) then eval_Eqz(st)
  else st
}

// --- Mémoire et Tables ---
// Note : Les adresses mémoire et les index de table sont TOUJOURS des i32 (ou i64 en Wasm64),
// mais la valeur transmise dans TableSet peut être de n'importe quel type Value.

function {:inline} eval_StoreI32(st: State, offset: int): State {
  if (st->stStack is ConsStack && st->stStack->rest is ConsStack && 
      st->stStack->top is I32Val && st->stStack->rest->top is I32Val &&
      st->stStack->rest->top->i + offset >= 0 && 
      st->stStack->rest->top->i + offset + 4 <= st->stMemSize) then
    State(st->stStack->rest->rest, (st->stMemory)[st->stStack->rest->top->i + offset := st->stStack->top->i], st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else if (st->stStack is ConsStack && st->stStack->rest is ConsStack) then
    State(st->stStack->rest->rest, st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_LoadI32(st: State, offset: int): State {
  if (st->stStack is ConsStack && st->stStack->top is I32Val &&
      st->stStack->top->i + offset >= 0 && 
      st->stStack->top->i + offset + 4 <= st->stMemSize) then
    State(ConsStack(I32Val((st->stMemory)[st->stStack->top->i + offset]), st->stStack->rest), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else if (st->stStack is ConsStack) then
    State(ConsStack(UndefVal(), st->stStack->rest), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_TableSet(st: State): State {
  // L'index (rest->top) doit être un i32 >= 0, la valeur (top) peut être de n'importe quel type Value
  if (st->stStack is ConsStack && st->stStack->rest is ConsStack && 
      st->stStack->rest->top is I32Val && st->stStack->rest->top->i >= 0) then
    State(st->stStack->rest->rest, st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, 
      (st->stTables)[st->stStack->rest->top->i := st->stStack->top])
  else if (st->stStack is ConsStack && st->stStack->rest is ConsStack) then
    State(st->stStack->rest->rest, st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

function {:inline} eval_TableGet(st: State): State {
  if (st->stStack is ConsStack && st->stStack->top is I32Val && st->stStack->top->i >= 0) then
    State(ConsStack((st->stTables)[st->stStack->top->i], st->stStack->rest), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else if (st->stStack is ConsStack) then
    State(ConsStack(UndefVal(), st->stStack->rest), st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables)
  else st
}

// ==============================================================================
// 5. PROCÉDURE D'ÉVALUATION (inline 4 pour tolérer des AST de profondeur 3)
// ==============================================================================

procedure {:inline 1} eval_Instr(e: Instr, st: State) returns (outSt: State)
{
  if (e is ConstI32) {
    outSt := State(
      ConsStack(I32Val(e->val), st->stStack),
      st->stMemory, st->stMemSize, st->stLocals, st->stGlobals, st->stTables
    );
  } 
  else if (e is UnaryOpNode) {
    call outSt := eval_Instr(e->uOperand, st);
    outSt := eval_UnOp(e->uOp, outSt);
  }
  else if (e is BinaryOpNode) {
    call outSt := eval_Instr(e->bLeft, st);
    call outSt := eval_Instr(e->bRight, outSt);
    outSt := eval_BinOp(e->bOp, outSt);
  } 
  else if (e is MemoryOpNode && e->moOp is Store) {
    outSt := eval_StoreI32(st, e->moOffset);
  } 
  else if (e is MemoryOpNode && e->moOp is Load) {
    outSt := eval_LoadI32(st, e->moOffset);
  }
  else if (e is TableOpNode && e->toOp is TableSet) {
    outSt := eval_TableSet(st);
  }
  else if (e is TableOpNode && e->toOp is TableGet) {
    outSt := eval_TableGet(st);
  }
  else {
    outSt := st;
  }
}

// ==============================================================================
// 6. BATTERIE DE TESTS (Utilisant le modèle Procédure/Call)
// ==============================================================================

// Helper pour éviter de recopier l'état de base
function get_InitState(): State {
  State(NilStack(), (lambda k: int :: 0), 1024, (lambda k: int :: UndefVal()), (lambda k: int :: UndefVal()), (lambda k: int :: UndefVal()))
}

// --- Test 1 : Arithmétique (Add, Mul) ---
procedure Test_Arith_Add_Mul()
{
  var outSt: State;
  var ast: Instr;
  
  // (10 + 2) * 3 = 36
  ast := BinaryOpNode(Mul(), BinaryOpNode(Add(), ConstI32(10), ConstI32(2)), ConstI32(3));
  call outSt := eval_Instr(ast, get_InitState());
  
  assert outSt->stStack == ConsStack(I32Val(36), NilStack());
}

// --- Test 2 : Arithmétique (Sub, DivS) ---
procedure Test_Arith_Sub_Div()
{
  var outSt: State;
  var ast: Instr;
  
  // (20 - 4) / 2 = 8
  ast := BinaryOpNode(DivS(), BinaryOpNode(Sub(), ConstI32(20), ConstI32(4)), ConstI32(2));
  call outSt := eval_Instr(ast, get_InitState());
  
  assert outSt->stStack == ConsStack(I32Val(8), NilStack());
}

// --- Test 3 : Opérations Unaires (Neg, Eqz) ---
procedure Test_Unary_Neg_Eqz()
{
  var outSt: State;
  var ast: Instr;
  
  // Eqz(Neg(0)) -> -0 = 0 -> 0 == 0 -> 1 (vrai)
  ast := UnaryOpNode(Eqz(), UnaryOpNode(Neg(), ConstI32(0)));
  call outSt := eval_Instr(ast, get_InitState());
  
  assert outSt->stStack == ConsStack(I32Val(1), NilStack());
}

// --- Test 4 : Accès Mémoire (Store, Load) ---
procedure Test_Memory_Store_Load()
{
  var currentSt: State;
  currentSt := get_InitState();

  // 1. Empiler l'adresse (100) puis la valeur (42) et déclencher Store
  call currentSt := eval_Instr(ConstI32(100), currentSt);
  call currentSt := eval_Instr(ConstI32(42), currentSt);
  call currentSt := eval_Instr(MemoryOpNode(Store(), 0, 0, NoneOpt(), NoneOpt(), NoneOpt(), 0), currentSt);

  assert (currentSt->stMemory)[100] == 42;
  assert currentSt->stStack == NilStack();

  // 2. Empiler l'adresse (100) et déclencher Load
  call currentSt := eval_Instr(ConstI32(100), currentSt);
  call currentSt := eval_Instr(MemoryOpNode(Load(), 0, 0, NoneOpt(), NoneOpt(), NoneOpt(), 0), currentSt);

  assert currentSt->stStack == ConsStack(I32Val(42), NilStack());
}

// --- Test 5 : Accès Table (TableSet, TableGet) ---
procedure Test_Table_Set_Get()
{
  var currentSt: State;
  currentSt := get_InitState();

  // 1. Empiler l'index (10) puis la valeur (99) et déclencher TableSet
  call currentSt := eval_Instr(ConstI32(10), currentSt);
  call currentSt := eval_Instr(ConstI32(99), currentSt);
  call currentSt := eval_Instr(TableOpNode(TableSet(), NoneOpt(), NoneOpt(), NoneOpt()), currentSt);

  assert (currentSt->stTables)[10] == I32Val(99);
  assert currentSt->stStack == NilStack();

  // 2. Empiler l'index (10) et déclencher TableGet
  call currentSt := eval_Instr(ConstI32(10), currentSt);
  call currentSt := eval_Instr(TableOpNode(TableGet(), NoneOpt(), NoneOpt(), NoneOpt()), currentSt);

  assert currentSt->stStack == ConsStack(I32Val(99), NilStack());
}