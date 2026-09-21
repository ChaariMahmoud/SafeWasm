(module
  (func $target)

  (elem declare func $target)

  (func $make_null
    (drop
      (ref.null func)
    )
  )

  (func $make_ref
    (drop
      (ref.func $target)
    )
  )

  (export "make_null" (func $make_null))
  (export "make_ref" (func $make_ref))
)