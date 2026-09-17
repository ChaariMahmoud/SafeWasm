(module
  (func $test_param (param i32)
    local.get 0
    drop
  )

  (export "test_param" (func $test_param))
)