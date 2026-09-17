(module
  (type $binary_i32
    (func (param i32 i32) (result i32))
  )

  (func $without_param_or_local
    nop
  )

  (func $with_param
    (param $x i32)

    local.get $x
    drop
  )

  (func $with_unused_local
    (local i64)
    nop
  )

  (func $with_mixed_locals
    (param $x i32)
    (local $a i32)
    (local $b f64)

    local.get $x
    drop

    local.get $a
    drop

    local.get $b
    drop
  )

  (func $add
    (type $binary_i32)
    local.get 0
    local.get 1
    i32.add
  )

  (export "add" (func $add))
)