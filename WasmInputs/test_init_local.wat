(module
  (func $test_all_types
    (local i32 i64 f32 f64)

    local.get 0
    drop

    local.get 1
    drop

    local.get 2
    drop

    local.get 3
    drop
  )

  (export "test_all_types" (func $test_all_types))
)