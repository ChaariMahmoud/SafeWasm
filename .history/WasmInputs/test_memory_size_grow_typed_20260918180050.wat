(module
  (memory 2)

  (func $get_size (result i32)
    memory.size
  )

  (func $grow_zero (result i32)
    i32.const 0
    memory.grow
  )

  (func $grow_one (result i32)
    i32.const 1
    memory.grow
  )

  (export "get_size" (func $get_size))
  (export "grow_zero" (func $grow_zero))
  (export "grow_one" (func $grow_one))
)
