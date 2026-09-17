(module

  ;; ============================================================
  ;; i32 signed division
  ;; -7 / 3 = -2
  ;; ============================================================
  (func $test_i32_div_s (result i32)
    i32.const -7
    i32.const 3
    i32.div_s
  )

  ;; ============================================================
  ;; i32 unsigned division
  ;; 0xFFFFFFFF / 2 = 2147483647
  ;; ============================================================
  (func $test_i32_div_u (result i32)
    i32.const -1
    i32.const 2
    i32.div_u
  )

  ;; ============================================================
  ;; i32 signed remainder
  ;; -7 rem 3 = -1
  ;; ============================================================
  (func $test_i32_rem_s (result i32)
    i32.const -7
    i32.const 3
    i32.rem_s
  )

  ;; ============================================================
  ;; i32 unsigned remainder
  ;; 0xFFFFFFFF rem 2 = 1
  ;; ============================================================
  (func $test_i32_rem_u (result i32)
    i32.const -1
    i32.const 2
    i32.rem_u
  )

  ;; ============================================================
  ;; i64 signed division
  ;; -15 / 4 = -3
  ;; ============================================================
  (func $test_i64_div_s (result i64)
    i64.const -15
    i64.const 4
    i64.div_s
  )

  ;; ============================================================
  ;; i64 unsigned division
  ;; 0xFFFFFFFFFFFFFFFF / 2
  ;; = 9223372036854775807
  ;; ============================================================
  (func $test_i64_div_u (result i64)
    i64.const -1
    i64.const 2
    i64.div_u
  )

  ;; ============================================================
  ;; i64 signed remainder
  ;; -15 rem 4 = -3
  ;; ============================================================
  (func $test_i64_rem_s (result i64)
    i64.const -15
    i64.const 4
    i64.rem_s
  )

  ;; ============================================================
  ;; i64 unsigned remainder
  ;; 0xFFFFFFFFFFFFFFFF rem 2 = 1
  ;; ============================================================
  (func $test_i64_rem_u (result i64)
    i64.const -1
    i64.const 2
    i64.rem_u
  )

  (export "test_i32_div_s" (func $test_i32_div_s))
  (export "test_i32_div_u" (func $test_i32_div_u))
  (export "test_i32_rem_s" (func $test_i32_rem_s))
  (export "test_i32_rem_u" (func $test_i32_rem_u))

  (export "test_i64_div_s" (func $test_i64_div_s))
  (export "test_i64_div_u" (func $test_i64_div_u))
  (export "test_i64_rem_s" (func $test_i64_rem_s))
  (export "test_i64_rem_u" (func $test_i64_rem_u))
)