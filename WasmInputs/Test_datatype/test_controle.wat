(module

  ;; ============================================================
  ;; IF
  ;; ============================================================

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
  ;; ============================================================

  ;; cond != 0 -> premier opérande
  (func $test_select_true (result i32)
    i32.const 11
    i32.const 22
    i32.const 1
    select
  )

  ;; cond == 0 -> deuxième opérande
  (func $test_select_false (result i32)
    i32.const 11
    i32.const 22
    i32.const 0
    select
  )

  ;; Les valeurs sélectionnées peuvent être flottantes.
  ;; La condition reste i32.
  (func $test_select_f64 (result f64)
    f64.const 1.5
    f64.const 2.5
    i32.const 1
    select
  )


  ;; ============================================================
  ;; BR_IF
  ;; ============================================================

  ;; condition vraie:
  ;; branche vers $done avant d'affecter 99
  ;; résultat attendu = 42
  (func $test_br_if_true (result i32)
    (local $r i32)

    i32.const 42
    local.set $r

    (block $done
      i32.const 1
      br_if $done

      i32.const 99
      local.set $r
    )

    local.get $r
  )


  ;; condition fausse:
  ;; le branchement n'est pas pris
  ;; résultat attendu = 99
  (func $test_br_if_false (result i32)
    (local $r i32)

    i32.const 42
    local.set $r

    (block $done
      i32.const 0
      br_if $done

      i32.const 99
      local.set $r
    )

    local.get $r
  )


  ;; ============================================================
  ;; BR_TABLE : selector = 0
  ;; ============================================================
  ;;
  ;; selector 0 -> $case0
  ;; selector 1 -> $case1
  ;; autre      -> $default
  ;;
  ;; résultat attendu = 10
  ;; ============================================================

  (func $test_br_table_0 (result i32)
    (local $r i32)

    i32.const 30
    local.set $r

    (block $exit

      (block $default

        (block $case1

          (block $case0

            i32.const 0
            br_table $case0 $case1 $default

          )

          ;; selector = 0
          i32.const 10
          local.set $r
          br $exit
        )

        ;; selector = 1
        i32.const 20
        local.set $r
        br $exit
      )

      ;; default
      i32.const 30
      local.set $r
    )

    local.get $r
  )


  ;; ============================================================
  ;; BR_TABLE : selector = 1
  ;; résultat attendu = 20
  ;; ============================================================

  (func $test_br_table_1 (result i32)
    (local $r i32)

    i32.const 30
    local.set $r

    (block $exit

      (block $default

        (block $case1

          (block $case0

            i32.const 1
            br_table $case0 $case1 $default

          )

          i32.const 10
          local.set $r
          br $exit
        )

        i32.const 20
        local.set $r
        br $exit
      )

      i32.const 30
      local.set $r
    )

    local.get $r
  )


  ;; ============================================================
  ;; BR_TABLE : default
  ;; selector = 5
  ;; résultat attendu = 30
  ;; ============================================================

  (func $test_br_table_default (result i32)
    (local $r i32)

    i32.const 30
    local.set $r

    (block $exit

      (block $default

        (block $case1

          (block $case0

            i32.const 5
            br_table $case0 $case1 $default

          )

          i32.const 10
          local.set $r
          br $exit
        )

        i32.const 20
        local.set $r
        br $exit
      )

      i32.const 30
      local.set $r
    )

    local.get $r
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