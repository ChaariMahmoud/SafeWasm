(module
  ;; 0xff interprété sur 8 bits = -1
  (func $test_i32_extend8 (result i32)
    (i32.extend8_s
      (i32.const 255)
    )
  )

  ;; 0x8000 interprété sur 16 bits = -32768
  (func $test_i32_extend16 (result i32)
    (i32.extend16_s
      (i32.const 32768)
    )
  )

  ;; 0x80 interprété sur 8 bits = -128
  (func $test_i64_extend8 (result i64)
    (i64.extend8_s
      (i64.const 128)
    )
  )

  ;; 0xffff interprété sur 16 bits = -1
  (func $test_i64_extend16 (result i64)
    (i64.extend16_s
      (i64.const 65535)
    )
  )

  ;; 0x80000000 interprété sur 32 bits = -2147483648
  (func $test_i64_extend32 (result i64)
    (i64.extend32_s
      (i64.const 2147483648)
    )
  )

  ;; Cas positif : 0x7f reste 127.
  (func $test_i32_extend8_positive (result i32)
    (i32.extend8_s
      (i32.const 127)
    )
  )

  (export "test_i32_extend8" (func $test_i32_extend8))
  (export "test_i32_extend16" (func $test_i32_extend16))
  (export "test_i64_extend8" (func $test_i64_extend8))
  (export "test_i64_extend16" (func $test_i64_extend16))
  (export "test_i64_extend32" (func $test_i64_extend32))
  (export "test_i32_extend8_positive" (func $test_i32_extend8_positive))
)