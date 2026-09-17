(module
  ;; 4294967297 mod 2^32 = 1
  (func $test_wrap (result i32)
    (i32.wrap_i64
      (i64.const 4294967297)
    )
  )

  ;; -1 en i32 possède le payload non signé 2^32 - 1.
  (func $test_extend_u (result i64)
    (i64.extend_i32_u
      (i32.const -1)
    )
  )

  ;; Extension signée de -1 vers i64.
  (func $test_extend_s (result i64)
    (i64.extend_i32_s
      (i32.const -1)
    )
  )

  ;; Extension signée de la plus petite valeur i32.
  (func $test_extend_s_min (result i64)
    (i64.extend_i32_s
      (i32.const -2147483648)
    )
  )

  (export "test_wrap" (func $test_wrap))
  (export "test_extend_u" (func $test_extend_u))
  (export "test_extend_s" (func $test_extend_s))
  (export "test_extend_s_min" (func $test_extend_s_min))
)