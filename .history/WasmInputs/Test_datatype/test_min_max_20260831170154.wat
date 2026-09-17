(module

  ;; ============================================================
  ;; f32.min
  ;; ============================================================

  ;; min(3.5, 7.25) = 3.5
  (func $test_f32_min_pos (result f32)
    f32.const 3.5
    f32.const 7.25
    f32.min
  )

  ;; min(-4.0, 2.0) = -4.0
  (func $test_f32_min_neg (result f32)
    f32.const -4.0
    f32.const 2.0
    f32.min
  )


  ;; ============================================================
  ;; f32.max
  ;; ============================================================

  ;; max(3.5, 7.25) = 7.25
  (func $test_f32_max_pos (result f32)
    f32.const 3.5
    f32.const 7.25
    f32.max
  )

  ;; max(-4.0, 2.0) = 2.0
  (func $test_f32_max_neg (result f32)
    f32.const -4.0
    f32.const 2.0
    f32.max
  )


  ;; ============================================================
  ;; f32.copysign
  ;; ============================================================

  ;; magnitude = 5.5
  ;; sign = negative
  ;; result = -5.5
  (func $test_f32_copysign_neg (result f32)
    f32.const 5.5
    f32.const -2.0
    f32.copysign
  )

  ;; magnitude = |-5.5| = 5.5
  ;; sign = positive
  ;; result = 5.5
  (func $test_f32_copysign_pos (result f32)
    f32.const -5.5
    f32.const 2.0
    f32.copysign
  )


  ;; ============================================================
  ;; f64.min
  ;; ============================================================

  ;; min(10.25, 4.5) = 4.5
  (func $test_f64_min_pos (result f64)
    f64.const 10.25
    f64.const 4.5
    f64.min
  )

  ;; min(-8.5, -3.5) = -8.5
  (func $test_f64_min_neg (result f64)
    f64.const -8.5
    f64.const -3.5
    f64.min
  )


  ;; ============================================================
  ;; f64.max
  ;; ============================================================

  ;; max(10.25, 4.5) = 10.25
  (func $test_f64_max_pos (result f64)
    f64.const 10.25
    f64.const 4.5
    f64.max
  )

  ;; max(-8.5, -3.5) = -3.5
  (func $test_f64_max_neg (result f64)
    f64.const -8.5
    f64.const -3.5
    f64.max
  )


  ;; ============================================================
  ;; f64.copysign
  ;; ============================================================

  ;; magnitude = 12.75
  ;; sign = negative
  ;; result = -12.75
  (func $test_f64_copysign_neg (result f64)
    f64.const 12.75
    f64.const -1.0
    f64.copysign
  )

  ;; magnitude = |-12.75| = 12.75
  ;; sign = positive
  ;; result = 12.75
  (func $test_f64_copysign_pos (result f64)
    f64.const -12.75
    f64.const 1.0
    f64.copysign
  )


  ;; ============================================================
  ;; exports
  ;; ============================================================

  (export "test_f32_min_pos" (func $test_f32_min_pos))
  (export "test_f32_min_neg" (func $test_f32_min_neg))

  (export "test_f32_max_pos" (func $test_f32_max_pos))
  (export "test_f32_max_neg" (func $test_f32_max_neg))

  (export "test_f32_copysign_neg" (func $test_f32_copysign_neg))
  (export "test_f32_copysign_pos" (func $test_f32_copysign_pos))

  (export "test_f64_min_pos" (func $test_f64_min_pos))
  (export "test_f64_min_neg" (func $test_f64_min_neg))

  (export "test_f64_max_pos" (func $test_f64_max_pos))
  (export "test_f64_max_neg" (func $test_f64_max_neg))

  (export "test_f64_copysign_neg" (func $test_f64_copysign_neg))
  (export "test_f64_copysign_pos" (func $test_f64_copysign_pos))
)