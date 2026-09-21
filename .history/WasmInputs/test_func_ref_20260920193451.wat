(module
  (table $functions 4 10 funcref)

  (func $target)

  (func $test
    nop
  )

  (export "test" (func $test))
)