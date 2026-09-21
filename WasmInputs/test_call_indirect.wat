(module
  (type $unary_i32
    (func
      (param i32)
      (result i32)
    )
  )

  (table 1 funcref)

  (func $identity
    (type $unary_i32)
    (param $x i32)
    (result i32)

    local.get $x
  )

  (func $test
    (result i32)

    i32.const 0
    ref.func $identity
    table.set

    i32.const 42
    i32.const 0

    call_indirect
      (type $unary_i32)
  )

  (elem declare func $identity)

  (export "test" (func $test))
)