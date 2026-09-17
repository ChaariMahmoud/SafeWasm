(module
  (global $counter (mut i32) (i32.const 0))
  (func $set_counter (param $x i32)

    local.get $x
    global.set $counter
  )


  (func $increment

    global.get $counter
    i32.const 1
    i32.add
    global.set $counter
  )


  (func $get_counter (result i32)

    global.get $counter
  )

  (func $double (param $x i32) (result i32)

    local.get $x
    i32.const 2
    i32.mul
  )

  (func $compute (result i32)

    call $get_counter
    call $double
  )

  (func $scenario (result i32)

    i32.const 5
    call $set_counter

    call $increment

    call $compute
  )

  (export "scenario" (func $scenario))
)