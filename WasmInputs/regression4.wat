(module

  (type (;0;)
    (func (param i32) (result i32))
  )

  (type (;1;)
    (func (param i32 i32 i32) (result i32))
  )

  (import "env" "malloc"
    (func $malloc (type 0))
  )


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
    (type 1)
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

    local.get $address
    i32.load
    i32.const 11
    i32.eq

    local.get $address
    i32.load offset=4
    i32.const 22
    i32.eq

    i32.and

    local.get $address
    i32.load offset=8
    i32.const 33
    i32.eq

    i32.and
  )


  (table (;0;) 0 funcref)
  (memory (;0;) 1)

  (export "memory" (memory 0))
  (export "initStu" (func $initStu))
)