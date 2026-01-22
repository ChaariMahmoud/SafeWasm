(module
  (func $test
    (drop
      (i32.eq
        (i32.const 5)
        (i32.add
          (i32.const 2)
          (i32.const 3)
        )
      )
    )
  )
)