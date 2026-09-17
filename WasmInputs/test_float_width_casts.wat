(module
  (func $test_promote (result f64)
    (f64.promote_f32
      (f32.const 1.5)
    )
  )

  (func $test_demote (result f32)
    (f32.demote_f64
      (f64.const 2.25)
    )
  )

  ;; Une valeur déjà f32 doit être conservée après
  ;; promotion puis démotion.
  (func $test_roundtrip (result f32)
    (f32.demote_f64
      (f64.promote_f32
        (f32.const 1.5)
      )
    )
  )

  (export "test_promote" (func $test_promote))
  (export "test_demote" (func $test_demote))
  (export "test_roundtrip" (func $test_roundtrip))
)