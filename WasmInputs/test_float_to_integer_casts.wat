(module
  (func $i32_f32_s (result i32)
    (i32.trunc_f32_s (f32.const -3.7))
  )

  (func $i32_f32_u (result i32)
    (i32.trunc_f32_u (f32.const 3.7))
  )

  (func $i32_f64_s (result i32)
    (i32.trunc_f64_s (f64.const -4.9))
  )

  (func $i32_f64_u (result i32)
    (i32.trunc_f64_u (f64.const 5.9))
  )

  (func $i64_f32_s (result i64)
    (i64.trunc_f32_s (f32.const -6.7))
  )

  (func $i64_f32_u (result i64)
    (i64.trunc_f32_u (f32.const 7.7))
  )

  (func $i64_f64_s (result i64)
    (i64.trunc_f64_s (f64.const -8.9))
  )

  (func $i64_f64_u (result i64)
    (i64.trunc_f64_u (f64.const 9.9))
  )

  (export "i32_f32_s" (func $i32_f32_s))
  (export "i32_f32_u" (func $i32_f32_u))
  (export "i32_f64_s" (func $i32_f64_s))
  (export "i32_f64_u" (func $i32_f64_u))

  (export "i64_f32_s" (func $i64_f32_s))
  (export "i64_f32_u" (func $i64_f32_u))
  (export "i64_f64_s" (func $i64_f64_s))
  (export "i64_f64_u" (func $i64_f64_u))
)