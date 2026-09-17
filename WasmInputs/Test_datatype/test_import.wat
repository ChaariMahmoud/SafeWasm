(module

  ;; ============================================================
  ;; IMPORTS
  ;; ============================================================

  (import "env" "get_i32"
    (func $get_i32 (result i32))
  )

  (import "env" "get_i64"
    (func $get_i64 (result i64))
  )

  (import "env" "get_f32"
    (func $get_f32 (result f32))
  )

  (import "env" "get_f64"
    (func $get_f64 (result f64))
  )

  (import "env" "compute_i32"
    (func $compute_i32
      (param i32 i64)
      (result i32)
    )
  )


  ;; ============================================================
  ;; TEST IMPORT I32
  ;; ============================================================

  (func $test_import_i32 (result i32)
    call $get_i32
  )


  ;; ============================================================
  ;; TEST IMPORT I64
  ;; ============================================================

  (func $test_import_i64 (result i64)
    call $get_i64
  )


  ;; ============================================================
  ;; TEST IMPORT F32
  ;; ============================================================

  (func $test_import_f32 (result f32)
    call $get_f32
  )


  ;; ============================================================
  ;; TEST IMPORT F64
  ;; ============================================================

  (func $test_import_f64 (result f64)
    call $get_f64
  )


  ;; ============================================================
  ;; TEST IMPORT WITH PARAMETERS
  ;; ============================================================

  (func $test_import_params (result i32)
    i32.const 10
    i64.const 20
    call $compute_i32
  )


  ;; ============================================================
  ;; EXPORTS
  ;; ============================================================

  (export "test_import_i32"
    (func $test_import_i32)
  )

  (export "test_import_i64"
    (func $test_import_i64)
  )

  (export "test_import_f32"
    (func $test_import_f32)
  )

  (export "test_import_f64"
    (func $test_import_f64)
  )

  (export "test_import_params"
    (func $test_import_params)
  )
)