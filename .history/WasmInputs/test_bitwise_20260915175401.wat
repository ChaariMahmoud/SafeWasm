(module
  (func $test_i32_and (result i32)
    (i32.and
      (i32.const 6)
      (i32.const 3)
    )
  )

  (func $test_i32_or (result i32)
    (i32.or
      (i32.const 6)
      (i32.const 3)
    )
  )

  (func $test_i32_xor (result i32)
    (i32.xor
      (i32.const 6)
      (i32.const 3)
    )
  )

  (func $test_i64_and (result i64)
    (i64.and
      (i64.const 15)
      (i64.const 6)
    )
  )

  (export "test_i32_and" (func $test_i32_and))
  (export "test_i32_or"  (func $test_i32_or))
  (export "test_i32_xor" (func $test_i32_xor))
  (export "test_i64_and" (func $test_i64_and))
)