(module
  (type $void
    (func)
  )

  (table 3 funcref)

  (func $f0
    (type $void)
  )

  (func $f1
    (type $void)
  )

  (elem
    (i32.const 1)
    func
    $f0
    $f1
  )

  (func $test
    nop
  )

  (export "test" (func $test))
)