(module
  (table $functions 4 10 funcref)

  (func $target)

  (func (export "test")
    ref.func $target
    drop
  )

  (elem declare func $target)
)