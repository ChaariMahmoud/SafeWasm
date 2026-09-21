(module
  (table 8 funcref)

  (func $test_fill
    (table.fill
      (i32.const 2)
      (ref.null func)
      (i32.const 3)
    )
  )

  (export "test_fill" (func $test_fill))
)