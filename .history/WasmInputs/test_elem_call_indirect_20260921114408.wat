(module
  (type $return_i32
    (func
      (result i32)
    )
  )

  (table 3 funcref)

  (func $return_10
    (type $return_i32)
    (result i32)

    i32.const 10
  )

  (func $return_20
    (type $return_i32)
    (result i32)

    i32.const 20
  )

  (elem
    (i32.const 1)
    func
    $return_10
    $return_20
  )

  (func $test
    (result i32)

    // L’indice de table 2 contient return_20.
    i32.const 2

    call_indirect
      (type $return_i32)
  )

  (export "test" (func $test))
)