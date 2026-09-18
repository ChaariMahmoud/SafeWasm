(module
  (memory 1)

  ;; ============================================================
  ;; i32.store8 + i32.load8_s
  ;; -1 tronqué sur 8 bits donne 0xFF
  ;; puis étendu signé vers i32 donne 0xFFFFFFFF
  ;; ============================================================

  (func $i32_load8_s (result i32)
    i32.const 0
    i32.const -1
    i32.store8

    i32.const 0
    i32.load8_s
  )

  ;; ============================================================
  ;; i32.store8 + i32.load8_u
  ;; Résultat : 255
  ;; ============================================================

  (func $i32_load8_u (result i32)
    i32.const 8
    i32.const -1
    i32.store8

    i32.const 8
    i32.load8_u
  )

  ;; ============================================================
  ;; i32.store16 + i32.load16_s
  ;; Résultat canonique i32 : 0xFFFFFFFF
  ;; ============================================================

  (func $i32_load16_s (result i32)
    i32.const 16
    i32.const -1
    i32.store16

    i32.const 16
    i32.load16_s
  )

  ;; ============================================================
  ;; i32.store16 + i32.load16_u
  ;; Résultat : 65535
  ;; ============================================================

  (func $i32_load16_u (result i32)
    i32.const 24
    i32.const -1
    i32.store16

    i32.const 24
    i32.load16_u
  )

  ;; ============================================================
  ;; i64.store8 + i64.load8_s
  ;; Résultat canonique i64 : 0xFFFFFFFFFFFFFFFF
  ;; ============================================================

  (func $i64_load8_s (result i64)
    i32.const 32
    i64.const -1
    i64.store8

    i32.const 32
    i64.load8_s
  )

  ;; ============================================================
  ;; i64.store8 + i64.load8_u
  ;; Résultat : 255
  ;; ============================================================

  (func $i64_load8_u (result i64)
    i32.const 40
    i64.const -1
    i64.store8

    i32.const 40
    i64.load8_u
  )

  ;; ============================================================
  ;; i64.store16 + i64.load16_s
  ;; Résultat canonique i64 : 0xFFFFFFFFFFFFFFFF
  ;; ============================================================

  (func $i64_load16_s (result i64)
    i32.const 48
    i64.const -1
    i64.store16

    i32.const 48
    i64.load16_s
  )

  ;; ============================================================
  ;; i64.store16 + i64.load16_u
  ;; Résultat : 65535
  ;; ============================================================

  (func $i64_load16_u (result i64)
    i32.const 56
    i64.const -1
    i64.store16

    i32.const 56
    i64.load16_u
  )

  ;; ============================================================
  ;; i64.store32 + i64.load32_s
  ;; Résultat canonique i64 : 0xFFFFFFFFFFFFFFFF
  ;; ============================================================

  (func $i64_load32_s (result i64)
    i32.const 64
    i64.const -1
    i64.store32

    i32.const 64
    i64.load32_s
  )

  ;; ============================================================
  ;; i64.store32 + i64.load32_u
  ;; Résultat : 4294967295
  ;; ============================================================

  (func $i64_load32_u (result i64)
    i32.const 72
    i64.const -1
    i64.store32

    i32.const 72
    i64.load32_u
  )

  (export "i32_load8_s"  (func $i32_load8_s))
  (export "i32_load8_u"  (func $i32_load8_u))
  (export "i32_load16_s" (func $i32_load16_s))
  (export "i32_load16_u" (func $i32_load16_u))

  (export "i64_load8_s"  (func $i64_load8_s))
  (export "i64_load8_u"  (func $i64_load8_u))
  (export "i64_load16_s" (func $i64_load16_s))
  (export "i64_load16_u" (func $i64_load16_u))
  (export "i64_load32_s" (func $i64_load32_s))
  (export "i64_load32_u" (func $i64_load32_u))
)