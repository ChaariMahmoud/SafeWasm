(module

  (type (;0;) (func (result i32)))
  (;@requires
      $sp >= 0
  ;)
  
  (;@ensures
      $sp == old($sp) + 1
  ;)

  (;@ensures
      $stack[old($sp)].value_i32 == 129
  ;)

  ;; La fonction ne modifie pas le premier octet de la mémoire.
  (;@ensures
      $mem[0] == old($mem[0])
  ;)

  (func (;0;) (type 0) (result i32)

    (local i32 i32)
    i32.const 1
    i32.const 2
    i32.add
    i32.const 3
    i32.mul
    local.set 0
    i32.const 2
    i32.const 3
    local.get 0
    i32.mul
    i32.add
    local.set 1
    local.get 1
    i32.const 100
    i32.add
    local.tee 0
  )

  (table (;0;) 0 funcref)

  (memory (;0;) 1)

  (export "memory" (memory 0))
  (export "calc" (func 0))
)