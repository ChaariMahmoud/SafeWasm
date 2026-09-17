(module
  (func $i32_rotl (result i32)
    (i32.rotl
      (i32.const 1)
      (i32.const 1)
    )
  )

  (func $i32_rotr (result i32)
    (i32.rotr
      (i32.const 2)
      (i32.const 1)
    )
  )

  (func $i64_rotl (result i64)
    (i64.rotl
      (i64.const 1)
      (i64.const 1)
    )
  )

  (func $i64_rotr (result i64)
    (i64.rotr
      (i64.const 2)
      (i64.const 1)
    )
  )

  ;; Vérifie le masquage 33 mod 32.
  (func $i32_rotl_mask (result i32)
    (i32.rotl
      (i32.const 1)
      (i32.const 33)
    )
  )

  ;; Vérifie le masquage 65 mod 64.
  (func $i64_rotr_mask (result i64)
    (i64.rotr
      (i64.const 2)
      (i64.const 65)
    )
  )

  (export "i32_rotl" (func $i32_rotl))
  (export "i32_rotr" (func $i32_rotr))
  (export "i64_rotl" (func $i64_rotl))
  (export "i64_rotr" (func $i64_rotr))
  (export "i32_rotl_mask" (func $i32_rotl_mask))
  (export "i64_rotr_mask" (func $i64_rotr_mask))
)