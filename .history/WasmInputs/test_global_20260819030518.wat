(module
  (global $g (mut i32) (i32.const 5))

  (func $test (result i32)
    global.get $g
    i32.const 3
    i32.add
    global.set $g

    global.get $g
  )

  (export "test" (func $test))
)