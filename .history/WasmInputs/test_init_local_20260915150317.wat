(module
  (func $test_i32_local
    (result i32)
    (local i32)

    local.get 0
  )

  (export "test_i32_local" (func $test_i32_local))
)