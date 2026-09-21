(module
  (func $target)

  (func $null_ref
    (result i32)

    (ref.is_null
      (ref.null func)
    )
  )

  (func $non_null_ref
    (result i32)

    (ref.is_null
      (ref.func $target)
    )
  )

  (elem declare func $target)

  (export "null_ref" (func $null_ref))
  (export "non_null_ref" (func $non_null_ref))
)