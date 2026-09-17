(module

  ;; ============================================================
  ;; i32.clz
  ;; ============================================================

  ;; clz32(0) = 32
  (func $test_i32_clz_zero (result i32)
    i32.const 0
    i32.clz
  )

  ;; binary: 000...0001
  ;; clz32(1) = 31
  (func $test_i32_clz_one (result i32)
    i32.const 1
    i32.clz
  )

  ;; 8 = 000...1000
  ;; highest set bit = bit 3
  ;; clz32(8) = 28
  (func $test_i32_clz_eight (result i32)
    i32.const 8
    i32.clz
  )


  ;; ============================================================
  ;; i32.ctz
  ;; ============================================================

  ;; ctz32(0) = 32
  (func $test_i32_ctz_zero (result i32)
    i32.const 0
    i32.ctz
  )

  ;; 8 = ...1000
  ;; ctz32(8) = 3
  (func $test_i32_ctz_eight (result i32)
    i32.const 8
    i32.ctz
  )

  ;; 12 = ...1100
  ;; ctz32(12) = 2
  (func $test_i32_ctz_twelve (result i32)
    i32.const 12
    i32.ctz
  )


  ;; ============================================================
  ;; i32.popcnt
  ;; ============================================================

  ;; popcnt32(0) = 0
  (func $test_i32_popcnt_zero (result i32)
    i32.const 0
    i32.popcnt
  )

  ;; 15 = 1111
  ;; popcnt = 4
  (func $test_i32_popcnt_15 (result i32)
    i32.const 15
    i32.popcnt
  )

  ;; -1 => 0xFFFFFFFF
  ;; all 32 bits are 1
  ;; popcnt = 32
  (func $test_i32_popcnt_minus1 (result i32)
    i32.const -1
    i32.popcnt
  )


  ;; ============================================================
  ;; i64.clz
  ;; ============================================================

  ;; clz64(0) = 64
  (func $test_i64_clz_zero (result i64)
    i64.const 0
    i64.clz
  )

  ;; clz64(1) = 63
  (func $test_i64_clz_one (result i64)
    i64.const 1
    i64.clz
  )

  ;; clz64(8) = 60
  (func $test_i64_clz_eight (result i64)
    i64.const 8
    i64.clz
  )


  ;; ============================================================
  ;; i64.ctz
  ;; ============================================================

  ;; ctz64(0) = 64
  (func $test_i64_ctz_zero (result i64)
    i64.const 0
    i64.ctz
  )

  ;; ctz64(8) = 3
  (func $test_i64_ctz_eight (result i64)
    i64.const 8
    i64.ctz
  )

  ;; ctz64(12) = 2
  (func $test_i64_ctz_twelve (result i64)
    i64.const 12
    i64.ctz
  )


  ;; ============================================================
  ;; i64.popcnt
  ;; ============================================================

  ;; popcnt64(0) = 0
  (func $test_i64_popcnt_zero (result i64)
    i64.const 0
    i64.popcnt
  )

  ;; 15 = 1111
  ;; popcnt = 4
  (func $test_i64_popcnt_15 (result i64)
    i64.const 15
    i64.popcnt
  )

  ;; -1 => 0xFFFFFFFFFFFFFFFF
  ;; all 64 bits are 1
  ;; popcnt = 64
  (func $test_i64_popcnt_minus1 (result i64)
    i64.const -1
    i64.popcnt
  )


  ;; ============================================================
  ;; exports
  ;; ============================================================

  (export "test_i32_clz_zero" (func $test_i32_clz_zero))
  (export "test_i32_clz_one" (func $test_i32_clz_one))
  (export "test_i32_clz_eight" (func $test_i32_clz_eight))

  (export "test_i32_ctz_zero" (func $test_i32_ctz_zero))
  (export "test_i32_ctz_eight" (func $test_i32_ctz_eight))
  (export "test_i32_ctz_twelve" (func $test_i32_ctz_twelve))

  (export "test_i32_popcnt_zero" (func $test_i32_popcnt_zero))
  (export "test_i32_popcnt_15" (func $test_i32_popcnt_15))
  (export "test_i32_popcnt_minus1" (func $test_i32_popcnt_minus1))

  (export "test_i64_clz_zero" (func $test_i64_clz_zero))
  (export "test_i64_clz_one" (func $test_i64_clz_one))
  (export "test_i64_clz_eight" (func $test_i64_clz_eight))

  (export "test_i64_ctz_zero" (func $test_i64_ctz_zero))
  (export "test_i64_ctz_eight" (func $test_i64_ctz_eight))
  (export "test_i64_ctz_twelve" (func $test_i64_ctz_twelve))

  (export "test_i64_popcnt_zero" (func $test_i64_popcnt_zero))
  (export "test_i64_popcnt_15" (func $test_i64_popcnt_15))
  (export "test_i64_popcnt_minus1" (func $test_i64_popcnt_minus1))
)