(module
  (func (export "reinterpret_i32_roundtrip") (result i32)
    (i32.reinterpret_f32
      (f32.reinterpret_i32
        (i32.const 1065353216)
      )
    )
  )

  (func (export "reinterpret_i64_roundtrip") (result i64)
    (i64.reinterpret_f64
      (f64.reinterpret_i64
        (i64.const 4607182418800017408)
      )
    )
  )
)