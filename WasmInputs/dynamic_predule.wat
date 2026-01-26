(;;(module
  (func $simple_add (result i32)
    i32.const 5
    i32.const 7
    i32.add
  )
);)

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

(module
  (memory 1)
  (func $load (result i32)
    i32.const 0
    i32.load
  )
)

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








