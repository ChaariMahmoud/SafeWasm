(module
  (func $target)

  (func $same_null
    (result i32)

    (ref.eq
      (ref.null func)
      (ref.null func)
    )
  )

  (func $same_function
    (result i32)

    (ref.eq
      (ref.func $target)
      (ref.func $target)
    )
  )

  (elem declare func $target)

  (export "same_null" (func $same_null))
  (export "same_function" (func $same_function))
)