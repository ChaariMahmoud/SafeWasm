(module
  (table 8 funcref)

  (func $test
    (table.set
      (i32.const 2)
      (ref.null func)
    )
  )

  (export "test" (func $test))
)