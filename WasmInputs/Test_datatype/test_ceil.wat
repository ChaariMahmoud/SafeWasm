(module

  ;; ============================================================
  ;; f32.ceil
  ;; ceil(2.3) = 3
  ;; ============================================================
  (func $test_f32_ceil_pos (result f32)
    f32.const 2.3
    f32.ceil
  )

  ;; ceil(-2.7) = -2
  (func $test_f32_ceil_neg (result f32)
    f32.const -2.7
    f32.ceil
  )


  ;; ============================================================
  ;; f32.trunc
  ;; trunc(3.9) = 3
  ;; ============================================================
  (func $test_f32_trunc_pos (result f32)
    f32.const 3.9
    f32.trunc
  )

  ;; trunc(-2.7) = -2
  (func $test_f32_trunc_neg (result f32)
    f32.const -2.7
    f32.trunc
  )


  ;; ============================================================
  ;; f64.ceil
  ;; ceil(5.1) = 6
  ;; ============================================================
  (func $test_f64_ceil_pos (result f64)
    f64.const 5.1
    f64.ceil
  )

  ;; ceil(-4.8) = -4
  (func $test_f64_ceil_neg (result f64)
    f64.const -4.8
    f64.ceil
  )


  ;; ============================================================
  ;; f64.trunc
  ;; trunc(6.9) = 6
  ;; ============================================================
  (func $test_f64_trunc_pos (result f64)
    f64.const 6.9
    f64.trunc
  )

  ;; trunc(-5.6) = -5
  (func $test_f64_trunc_neg (result f64)
    f64.const -5.6
    f64.trunc
  )


  ;; ============================================================
  ;; exports
  ;; ============================================================

  (export "test_f32_ceil_pos"   (func $test_f32_ceil_pos))
  (export "test_f32_ceil_neg"   (func $test_f32_ceil_neg))

  (export "test_f32_trunc_pos"  (func $test_f32_trunc_pos))
  (export "test_f32_trunc_neg"  (func $test_f32_trunc_neg))

  (export "test_f64_ceil_pos"   (func $test_f64_ceil_pos))
  (export "test_f64_ceil_neg"   (func $test_f64_ceil_neg))

  (export "test_f64_trunc_pos"  (func $test_f64_trunc_pos))
  (export "test_f64_trunc_neg"  (func $test_f64_trunc_neg))
)