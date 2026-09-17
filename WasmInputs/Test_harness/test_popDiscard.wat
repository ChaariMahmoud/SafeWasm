(module
  (func $add_one (param $x i32) (result i32)
    local.get $x
    i32.const 1
    i32.add
  )

  (func $double (param $x i32) (result i32)
    local.get $x
    i32.const 2
    i32.mul
  )

  (func $test (result i32)

    i32.const 5
    call $add_one
    call $double
  )

  (export "test" (func $test))
)