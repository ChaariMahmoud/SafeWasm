(module
  (memory 1)

  ;; func 0: exercise ALL store instructions (and then read back a couple)
  (func $testStores (param $p i32)
    (local $x i32)

    ;; i32.store / i64.store
    (i32.store (i32.add (local.get $p) (i32.const 0))  (i32.const 123456))
    (i64.store (i32.add (local.get $p) (i32.const 8))  (i64.const 1234567890123))

    ;; f32.store / f64.store (bit-pattern will be abstract in your model)
    (f32.store (i32.add (local.get $p) (i32.const 16)) (f32.const 3.25))
    (f64.store (i32.add (local.get $p) (i32.const 24)) (f64.const 6.5))

    ;; i32.store8 / i32.store16
    (i32.store8  (i32.add (local.get $p) (i32.const 40)) (i32.const 255))
    (i32.store16 (i32.add (local.get $p) (i32.const 42)) (i32.const 65535))

    ;; i64.store8 / i64.store16 / i64.store32
    (i64.store8  (i32.add (local.get $p) (i32.const 48)) (i64.const 255))
    (i64.store16 (i32.add (local.get $p) (i32.const 50)) (i64.const 65535))
    (i64.store32 (i32.add (local.get $p) (i32.const 56)) (i64.const 4294967295))

    ;; optional: a couple of loads after stores (good sanity check)
    (local.set $x (i32.load (i32.add (local.get $p) (i32.const 0))))
  
  )

  ;; call with base address 0
  (func (export "main")
    (call $testStores (i32.const 0))
  )
)
