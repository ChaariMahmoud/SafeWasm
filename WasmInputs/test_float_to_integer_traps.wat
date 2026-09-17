(module
  ;; Dépasse la valeur maximale d'un i32 signé.
  (func $trap_i32_signed (result i32)
    (i32.trunc_f64_s
      (f64.const 2147483648.0)
    )
  )

  ;; Une valeur négative ne peut pas être convertie vers i32_u.
  (func $trap_i32_unsigned (result i32)
    (i32.trunc_f64_u
      (f64.const -1.0)
    )
  )

  ;; Dépasse la valeur maximale d'un i64 signé.
  (func $trap_i64_signed (result i64)
    (i64.trunc_f64_s
      (f64.const 9223372036854775808.0)
    )
  )

  ;; Une valeur négative ne peut pas être convertie vers i64_u.
  (func $trap_i64_unsigned (result i64)
    (i64.trunc_f64_u
      (f64.const -1.0)
    )
  )

  (export "trap_i32_signed" (func $trap_i32_signed))
  (export "trap_i32_unsigned" (func $trap_i32_unsigned))
  (export "trap_i64_signed" (func $trap_i64_signed))
  (export "trap_i64_unsigned" (func $trap_i64_unsigned))
)