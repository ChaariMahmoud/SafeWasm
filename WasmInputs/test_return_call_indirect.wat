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

  (func $tail_dispatch
    (type $unary_i32)
    (param $x i32)
    (result i32)

    (return_call_indirect
      (type $unary_i32)
      (local.get $x)
      (i32.const 0)
    )
  )

  (func $test
    (result i32)

    (table.set
      (i32.const 0)
      (ref.func $identity)
    )

    (call $tail_dispatch
      (i32.const 42)
    )
  )

  (elem declare func $identity)

  (export "test" (func $test))
)