(module
  ;; 1 << (33 mod 32) = 2
  (func $test_i32_shl_mask (result i32)
    (i32.shl
      (i32.const 1)
      (i32.const 33)
    )
  )

  ;; 0xffffffff >>u 1 = 0x7fffffff
  (func $test_i32_shr_u (result i32)
    (i32.shr_u
      (i32.const -1)
      (i32.const 1)
    )
  )

  ;; -4 >>s 1 = -2, représenté par 4294967294
  (func $test_i32_shr_s (result i32)
    (i32.shr_s
      (i32.const -4)
      (i32.const 1)
    )
  )

  ;; 1 << (65 mod 64) = 2
  (func $test_i64_shl_mask (result i64)
    (i64.shl
      (i64.const 1)
      (i64.const 65)
    )
  )

  ;; 0xffffffffffffffff >>u 1 = 2^63 - 1
  (func $test_i64_shr_u (result i64)
    (i64.shr_u
      (i64.const -1)
      (i64.const 1)
    )
  )

  ;; -4 >>s 1 = -2, représenté modulo 2^64
  (func $test_i64_shr_s (result i64)
    (i64.shr_s
      (i64.const -4)
      (i64.const 1)
    )
  )

  (export "test_i32_shl_mask" (func $test_i32_shl_mask))
  (export "test_i32_shr_u" (func $test_i32_shr_u))
  (export "test_i32_shr_s" (func $test_i32_shr_s))
  (export "test_i64_shl_mask" (func $test_i64_shl_mask))
  (export "test_i64_shr_u" (func $test_i64_shr_u))
  (export "test_i64_shr_s" (func $test_i64_shr_s))
)