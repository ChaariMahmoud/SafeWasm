(;;(module
  (func $simple_add (result i32)
    i32.const 5
    i32.const 7
    i32.add
  )
);)

(;;(module
  (func $simple_add_with_locals (result i32)
    (local $a i32)
    (local $b i32)

    i32.const 5
    local.set $a

    i32.const 7
    local.set $b

    local.get $a
    local.get $b
    i32.add
  )
);)

(module
  ;; Global mutable nommée
  (global $Counter (mut i32) (i32.const 41))

  ;; Global immutable nommée
  (global $Max i32 (i32.const 100))

  (func $inc_and_check (result i32)
    ;; gCounter = gCounter + 1
    global.get $Counter
    i32.const 1
    i32.add
    global.set $Counter

    ;; return (gCounter < gMax) ? 1 : 0
    global.get $Counter
    global.get $Max
    i32.lt_s
  )
)


(;;(module
  (func $cmp (param i32 i32) (result i32)
    local.get 0
    local.get 1
    i32.lt_s
  )
);)
(;;(module
  (func $math (param f64) (result f64)
    local.get 0
    f64.sqrt
    f64.floor
    f64.abs
  )
);)

(;;(module
  (func $sel (param i32 i32 i32) (result i32)
    local.get 0
    local.get 1
    local.get 2
    select
  )
);)

(;;(module
  ;; Test f32.min
  (func (result f32)
    f32.const 5
    f32.const 3
    f32.min
  ));)



(;;(module
  (memory 1)
  (func $load (result i32)
    i32.const 0
    i32.load
  )
);)

(;;(module
  (memory 1)
  (func $load8 (result i32)
    i32.const 10
    i32.load8_s
  )
);;)

(;;(module
  (func $loop_test (param i32) (result i32)
    (local i32)
    loop $L
      local.get 0
      i32.const 1
      i32.sub
      local.tee 0
      br_if $L
    end
    local.get 0
  )
);;)

(;;(module
  (type $t0 (func (result i32)))
  (table 1 funcref)

  (func $f (result i32)
    i32.const 0
  )

  (elem (i32.const 0) $f)

  (func $indirect (result i32)
    i32.const 0
    call_indirect (type $t0)
  )
);)








