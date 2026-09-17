(module
  (func $test_named
    (param $p1 i32)
    (param $p2 f64)
    (local $x i64)
    (local $y f32)

    local.get $p1
    drop

    local.get $p2
    drop

    local.get $x
    drop

    local.get $y
    drop
  )

  (export "test_named" (func $test_named))
)