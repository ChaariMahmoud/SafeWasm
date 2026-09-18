(module
  (memory 1)

  (func $store_load_i64 (result i64)
    i32.const 32
    i64.const 81985529216486895
    i64.store

    i32.const 32
    i64.load
  )

  (export "store_load_i64"
    (func $store_load_i64)
  )
)