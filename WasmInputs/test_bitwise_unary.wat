(module
  ;; clz(0) = largeur
  (func $i32_clz_zero (result i32)
    (i32.clz
      (i32.const 0)
    )
  )

  ;; 0x40000000 commence par un seul zéro
  (func $i32_clz_one (result i32)
    (i32.clz
      (i32.const 1073741824)
    )
  )

  ;; 8 = 1000₂, donc 3 zéros finaux
  (func $i32_ctz (result i32)
    (i32.ctz
      (i32.const 8)
    )
  )

  ;; 11 = 1011₂, donc 3 bits à 1
  (func $i32_popcnt (result i32)
    (i32.popcnt
      (i32.const 11)
    )
  )

  ;; clz(0) = 64
  (func $i64_clz_zero (result i64)
    (i64.clz
      (i64.const 0)
    )
  )

  ;; Le bit de poids fort est positionné, donc clz = 0.
  (func $i64_clz_high_bit (result i64)
    (i64.clz
      (i64.const -9223372036854775808)
    )
  )

  ;; 8 = 1000₂, donc 3 zéros finaux
  (func $i64_ctz (result i64)
    (i64.ctz
      (i64.const 8)
    )
  )

  ;; 15 = 1111₂, donc 4 bits à 1
  (func $i64_popcnt (result i64)
    (i64.popcnt
      (i64.const 15)
    )
  )

  (export "i32_clz_zero" (func $i32_clz_zero))
  (export "i32_clz_one" (func $i32_clz_one))
  (export "i32_ctz" (func $i32_ctz))
  (export "i32_popcnt" (func $i32_popcnt))

  (export "i64_clz_zero" (func $i64_clz_zero))
  (export "i64_clz_high_bit" (func $i64_clz_high_bit))
  (export "i64_ctz" (func $i64_ctz))
  (export "i64_popcnt" (func $i64_popcnt))
)