(module

  ;; ============================================================
  ;; IF
  ;; ============================================================

  ;; condition != 0 -> then -> 10
  (func $test_if_true (result i32)
    i32.const 1
    (if (result i32)
      (then
        i32.const 10
      )
      (else
        i32.const 20
      )
    )
  )

  ;; condition == 0 -> else -> 20
  (func $test_if_false (result i32)
    i32.const 0
    (if (result i32)
      (then
        i32.const 10
      )
      (else
        i32.const 20
      )
    )
  )


  ;; ============================================================
  ;; SELECT
  ;; select v1 v2 cond
  ;;
  ;; cond != 0 -> v1
  ;; cond == 0 -> v2
  ;; ============================================================

  (func $test_select_true (result i32)
    i32.const 11
    i32.const 22
    i32.const 1
    select
  )

  (func $test_select_false (result i32)
    i32.const 11
    i32.const 22
    i32.const 0
    select
  )

  ;; Important: values can also be f64.
  ;; Only the condition must be i32.
  (func $test_select_f64 (result f64)
    f64.const 1.5
    f64.const 2.5
    i32.const 1
    select
  )


  ;; ============================================================
  ;; BR_IF
  ;; ============================================================

  ;; br_if condition true:
  ;; branch to $done and skip i32.const 99
  ;;
  ;; Expected result = 42
  (func $test_br_if_true (result i32)
    (block $done
      i32.const 42

      i32.const 1
      br_if $done

      drop
      i32.const 99
      return
    )
  )

  ;; condition false:
  ;; no branch
  ;;
  ;; Expected result = 99
  (func $test_br_if_false (result i32)
    (block $done
      i32.const 42

      i32.const 0
      br_if $done

      drop
      i32.const 99
      return
    )

    i32.const 42
  )


  ;; ============================================================
  ;; BR_TABLE
  ;; ============================================================

  ;; selector = 0 -> $case0
  ;; selector = 1 -> $case1
  ;; otherwise    -> $default
  ;;
  ;; Nested blocks are needed because br_table targets labels.
  ;;
  ;; Expected:
  ;; selector 0 -> 10
  ;; selector 1 -> 20
  ;; default    -> 30
  ;; ============================================================

  (func $test_br_table_0 (result i32)
    (block $exit
      (block $default
        (block $case1
          (block $case0

            i32.const 0
            br_table $case0 $case1 $default

          )

          ;; case 0
          i32.const 10
          br $exit
        )

        ;; case 1
        i32.const 20
        br $exit
      )

      ;; default
      i32.const 30
    )
  )


  (func $test_br_table_1 (result i32)
    (block $exit
      (block $default
        (block $case1
          (block $case0

            i32.const 1
            br_table $case0 $case1 $default

          )

          i32.const 10
          br $exit
        )

        i32.const 20
        br $exit
      )

      i32.const 30
    )
  )


  (func $test_br_table_default (result i32)
    (block $exit
      (block $default
        (block $case1
          (block $case0

            i32.const 5
            br_table $case0 $case1 $default

          )

          i32.const 10
          br $exit
        )

        i32.const 20
        br $exit
      )

      i32.const 30
    )
  )


  ;; ============================================================
  ;; EXPORTS
  ;; ============================================================

  (export "test_if_true" (func $test_if_true))
  (export "test_if_false" (func $test_if_false))

  (export "test_select_true" (func $test_select_true))
  (export "test_select_false" (func $test_select_false))
  (export "test_select_f64" (func $test_select_f64))

  (export "test_br_if_true" (func $test_br_if_true))
  (export "test_br_if_false" (func $test_br_if_false))

  (export "test_br_table_0" (func $test_br_table_0))
  (export "test_br_table_1" (func $test_br_table_1))
  (export "test_br_table_default" (func $test_br_table_default))
)