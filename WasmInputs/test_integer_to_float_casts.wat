(module
  (func $f32_i32_s (result f32)
    (f32.convert_i32_s
      (i32.const -1)
    )
  )

  (func $f32_i32_u (result f32)
    (f32.convert_i32_u
      (i32.const -1)
    )
  )

  (func $f32_i64_s (result f32)
    (f32.convert_i64_s
      (i64.const -2)
    )
  )

  (func $f32_i64_u (result f32)
    (f32.convert_i64_u
      (i64.const 42)
    )
  )

  (func $f64_i32_s (result f64)
    (f64.convert_i32_s
      (i32.const -3)
    )
  )

  (func $f64_i32_u (result f64)
    (f64.convert_i32_u
      (i32.const -1)
    )
  )

  (func $f64_i64_s (result f64)
    (f64.convert_i64_s
      (i64.const -4)
    )
  )

  (func $f64_i64_u (result f64)
    (f64.convert_i64_u
      (i64.const 100)
    )
  )

  (export "f32_i32_s" (func $f32_i32_s))
  (export "f32_i32_u" (func $f32_i32_u))
  (export "f32_i64_s" (func $f32_i64_s))
  (export "f32_i64_u" (func $f32_i64_u))

  (export "f64_i32_s" (func $f64_i32_s))
  (export "f64_i32_u" (func $f64_i32_u))
  (export "f64_i64_s" (func $f64_i64_s))
  (export "f64_i64_u" (func $f64_i64_u))
)