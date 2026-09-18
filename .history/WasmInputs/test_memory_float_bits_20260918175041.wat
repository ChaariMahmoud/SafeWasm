(module
  (memory 1)

  ;; 1065353216 = 0x3F800000, bits IEEE-754 de f32 1.0
  (func $f32_memory_bits_roundtrip (result i32)
    i32.const 16

    i32.const 1065353216
    f32.reinterpret_i32

    f32.store

    i32.const 16
    f32.load
    i32.reinterpret_f32
  )

  ;; 4607182418800017408 = 0x3FF0000000000000,
  ;; bits IEEE-754 de f64 1.0
  (func $f64_memory_bits_roundtrip (result i64)
    i32.const 32

    i64.const 4607182418800017408
    f64.reinterpret_i64

    f64.store

    i32.const 32
    f64.load
    i64.reinterpret_f64
  )

  (export "f32_memory_bits_roundtrip"
    (func $f32_memory_bits_roundtrip)
  )

  (export "f64_memory_bits_roundtrip"
    (func $f64_memory_bits_roundtrip)
  )
)