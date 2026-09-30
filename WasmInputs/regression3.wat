(module

  ;; ============================================================
  ;; Type de la fonction principale
  ;; ============================================================

  (type $i32_to_i32
    (func (param i32) (result i32))
  )


  ;; ============================================================
  ;; Fonction testée
  ;;
  ;; br_table possède ici :
  ;;   index 0  -> profondeur 2 -> sortie du bloc externe -> 6
  ;;   index 1  -> profondeur 1 -> sortie du bloc intermédiaire -> 5
  ;;   défaut   -> profondeur 0 -> sortie du bloc interne -> 4
  ;;
  ;; Donc :
  ;;   brTable(0) = 6
  ;;   brTable(1) = 5
  ;;   brTable(n) = 4, pour n >= 2
  ;; ============================================================

  (;@requires
      $sp >= 1
  ;)

  (;@ensures
      $sp == old($sp)
  ;)

  (;@ensures
      $stack[old($sp) - 1].value_i32 == 4
      ||
      $stack[old($sp) - 1].value_i32 == 5
      ||
      $stack[old($sp) - 1].value_i32 == 6
  ;)

  (func $brTable
    (type $i32_to_i32)
    (param $index i32)
    (result i32)

    block $outer
      block $middle
        block $inner

          local.get $index

          br_table
            $outer
            $middle
            $inner

        end

        ;; Branche par défaut : index >= 2
        i32.const 4
        return
      end

      ;; Index 1
      i32.const 5
      return
    end

    ;; Index 0
    i32.const 6
  )


  ;; ============================================================
  ;; Test 1 : index 0
  ;;
  ;; La cible sélectionnée est $outer.
  ;; Le résultat attendu est 6.
  ;; ============================================================

  (;@ensures
      $stack[old($sp)].value_i32 == 6
  ;)

  (func $test_br_table_index_0
    (export "test_br_table_index_0")
    (result i32)

    i32.const 0
    call $brTable
  )


  ;; ============================================================
  ;; Test 2 : index 1
  ;;
  ;; La cible sélectionnée est $middle.
  ;; Le résultat attendu est 5.
  ;; ============================================================

  (;@ensures
      $stack[old($sp)].value_i32 == 5
  ;)

  (func $test_br_table_index_1
    (export "test_br_table_index_1")
    (result i32)

    i32.const 1
    call $brTable
  )


  ;; ============================================================
  ;; Test 3 : index hors de la liste explicite
  ;;
  ;; La cible par défaut est $inner.
  ;; Le résultat attendu est 4.
  ;; ============================================================

  (;@ensures
      $stack[old($sp)].value_i32 == 4
  ;)

  (func $test_br_table_default
    (export "test_br_table_default")
    (result i32)

    i32.const 10
    call $brTable
  )
  (export "brTable" (func $brTable))
)