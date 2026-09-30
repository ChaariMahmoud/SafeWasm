(module

  (type $malloc_type
    (func (param i32) (result i32))
  )

  (type $init_stu_type
    (func (param i32 i32 i32) (result i32))
  )

  ;; malloc retourne une adresse représentée par un i32.
  (import "env" "malloc"
    (func $malloc (type $malloc_type))
  )


  ;; ============================================================
  ;; Initialise une structure de 12 octets :
  ;;
  ;;   address + 0 : field0
  ;;   address + 4 : field1
  ;;   address + 8 : field2
  ;;
  ;; La fonction retourne l'adresse de la structure.
  ;; ============================================================

  (;@requires
      $sp >= 3
  ;)

  (;@ensures
      $sp == old($sp) - 2
  ;)

  (;@ensures
      $mem_pages == old($mem_pages)
  ;)

  (func $initStu
    (type $init_stu_type)
    (param $field0 i32)
    (param $field1 i32)
    (param $field2 i32)
    (result i32)

    (local $address i32)

    i32.const 12
    call $malloc

    local.tee $address

    local.get $field1
    i32.store offset=4

    local.get $address
    local.get $field0
    i32.store

    local.get $address
    local.get $field2
    i32.store offset=8

    local.get $address
  )


  ;; ============================================================
  ;; Test sémantique :
  ;;
  ;;   field0 = 11
  ;;   field1 = 22
  ;;   field2 = 33
  ;;
  ;; Retourne 1 si les trois valeurs relues sont correctes.
  ;; ============================================================

  (;@ensures
      $stack[old($sp)].value_i32 == 1
  ;)

  (;@ensures
      $mem_pages == old($mem_pages)
  ;)

  (func $test_initStu
    (export "test_initStu")
    (result i32)

    (local $address i32)

    i32.const 11
    i32.const 22
    i32.const 33
    call $initStu

    local.set $address

    ;; Champ 0 : memory[address] == 11
    local.get $address
    i32.load
    i32.const 11
    i32.eq

    ;; Champ 1 : memory[address + 4] == 22
    local.get $address
    i32.load offset=4
    i32.const 22
    i32.eq

    i32.and

    ;; Champ 2 : memory[address + 8] == 33
    local.get $address
    i32.load offset=8
    i32.const 33
    i32.eq

    i32.and
  )


  (table 0 funcref)
  (memory 1)

  (export "memory" (memory 0))
  (export "initStu" (func $initStu))
)