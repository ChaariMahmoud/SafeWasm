(module
  (memory 1)

  (global $g0 (mut i32) (i32.const 7))

  ;; ------------------------------------------------------------
  ;; func0: add, eqz, if/else, select, drop
  ;; ------------------------------------------------------------
  (func $func0 (param $a i32) (param $b i32) (result i32)
    (local $x i32)

    ;; x = a + b
    (local.set $x
      (i32.add (local.get $a) (local.get $b))
    )

    ;; if (a == 0) x = x + 1 else x = x + 2
    (if (i32.eqz (local.get $a))
      (then
        (local.set $x (i32.add (local.get $x) (i32.const 1)))
      )
      (else
        (local.set $x (i32.add (local.get $x) (i32.const 2)))
      )
    )

    ;; select test then drop (just to test select)
    (drop
      (select
        (i32.const 111)
        (i32.const 222)
        (i32.lt_s (local.get $a) (local.get $b))
      )
    )

    (local.get $x)
  )

  ;; ------------------------------------------------------------
  ;; func1: globals get/set + ne + if/else WITHOUT (result ...)
  ;; ------------------------------------------------------------
  (func $func1 (param $p i32) (result i32)
    (local $ret i32)

    ;; g0 = g0 + p
    (global.set $g0
      (i32.add (global.get $g0) (local.get $p))
    )

    ;; ret = (g0 != 0) ? 1 : 0   (no if(result i32))
    (if (i32.ne (global.get $g0) (i32.const 0))
      (then (local.set $ret (i32.const 1)))
      (else (local.set $ret (i32.const 0)))
    )

    (local.get $ret)
  )

  ;; ------------------------------------------------------------
  ;; func2: memory stores/loads
  ;; ------------------------------------------------------------
  (func $func2 (param $base i32) (result i32)
    ;; i32.store at base+0
    (i32.store
      (i32.add (local.get $base) (i32.const 0))
      (i32.const 123456)
    )

    ;; i64.store at base+8
    (i64.store
      (i32.add (local.get $base) (i32.const 8))
      (i64.const 2147483647)
    )

    ;; f32.store at base+16
    (f32.store
      (i32.add (local.get $base) (i32.const 16))
      (f32.const 3.25)
    )

    ;; f64.store at base+24
    (f64.store
      (i32.add (local.get $base) (i32.const 24))
      (f64.const 6.5)
    )

    ;; store8/store16 and i64.store8/store16/store32
    (i32.store8  (i32.add (local.get $base) (i32.const 40)) (i32.const 255))
    (i32.store16 (i32.add (local.get $base) (i32.const 42)) (i32.const 65535))

    (i64.store8  (i32.add (local.get $base) (i32.const 48)) (i64.const 255))
    (i64.store16 (i32.add (local.get $base) (i32.const 50)) (i64.const 65535))
    (i64.store32 (i32.add (local.get $base) (i32.const 56)) (i64.const 2147483647))

    ;; return i32.load(base+0)
    (i32.load (i32.add (local.get $base) (i32.const 0)))
  )

  ;; ------------------------------------------------------------
  ;; func3: loop/block/br/br_if (sans br_table pour l’instant)
  ;; car ton parser br_table est ok mais souvent fragile avec blocks imbriqués.
  ;; ------------------------------------------------------------
  (func $func3 (param $n i32) (result i32)
    (local $i i32)

    (local.set $i (i32.const 0))

    (block $out
      (loop $L
        (br_if $out (i32.ge_s (local.get $i) (local.get $n)))
        (local.set $i (i32.add (local.get $i) (i32.const 1)))
        (br $L)
      )
    )

    (local.get $i)
  )

  ;; ------------------------------------------------------------
  ;; main: call everything and drop results
  ;; ------------------------------------------------------------
  (func $main
    (drop (call $func0 (i32.const 10) (i32.const 20)))
    (drop (call $func1 (i32.const 5)))
    (drop (call $func2 (i32.const 0)))
    (drop (call $func3 (i32.const 7)))
  )
)
