(module
  (memory 1)

  (func $store_load_i32 (result i32)
    i32.const 16
    i32.const 305419896
    i32.store

    i32.const 16
    i32.load
  )

  (export "store_load_i32"
    (func $store_load_i32)
  )
)