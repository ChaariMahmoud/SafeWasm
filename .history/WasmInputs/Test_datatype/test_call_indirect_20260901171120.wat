(module

  ;; ============================================================
  ;; TYPES
  ;; ============================================================

  (type $t_i32
    (func
      (param i32)
      (result i32)
    )
  )

  (type $t_i64
    (func
      (param i64)
      (result i64)
    )
  )

  (type $t_f32
    (func
      (param f32)
      (result f32)
    )
  )

  (type $t_f64
    (func
      (param f64)
      (result f64)
    )
  )


  ;; ============================================================
  ;; FUNCTIONS PUT IN TABLE
  ;; ============================================================

  (func $fi32
    (type $t_i32)
    (param i32)
    (result i32)

    local.get 0
  )

  (func $fi64
    (type $t_i64)
    (param i64)
    (result i64)

    local.get 0
  )

  (func $ff32
    (type $t_f32)
    (param f32)
    (result f32)

    local.get 0
  )

  (func $ff64
    (type $t_f64)
    (param f64)
    (result f64)

    local.get 0
  )


  ;; ============================================================
  ;; TABLE
  ;; ============================================================

  (table 4 funcref)

  (elem
    (i32.const 0)
    $fi32
    $fi64
    $ff32
    $ff64
  )


  ;; ============================================================
  ;; CALL_INDIRECT I32
  ;; ============================================================

  (func $test_call_indirect_i32
    (result i32)

    i32.const 123
    i32.const 0

    call_indirect
      (type $t_i32)
  )


  ;; ============================================================
  ;; CALL_INDIRECT I64
  ;; ============================================================

  (func $test_call_indirect_i64
    (result i64)

    i64.const 456
    i32.const 1

    call_indirect
      (type $t_i64)
  )


  ;; ============================================================
  ;; CALL_INDIRECT F32
  ;; ============================================================

  (func $test_call_indirect_f32
    (result f32)

    f32.const 1.5
    i32.const 2

    call_indirect
      (type $t_f32)
  )


  ;; ============================================================
  ;; CALL_INDIRECT F64
  ;; ============================================================

  (func $test_call_indirect_f64
    (result f64)

    f64.const 2.5
    i32.const 3

    call_indirect
      (type $t_f64)
  )


  ;; ============================================================
  ;; EXPORTS
  ;; ============================================================

  (export "test_call_indirect_i32"
    (func $test_call_indirect_i32)
  )

  (export "test_call_indirect_i64"
    (func $test_call_indirect_i64)
  )

  (export "test_call_indirect_f32"
    (func $test_call_indirect_f32)
  )

  (export "test_call_indirect_f64"
    (func $test_call_indirect_f64)
  )
)