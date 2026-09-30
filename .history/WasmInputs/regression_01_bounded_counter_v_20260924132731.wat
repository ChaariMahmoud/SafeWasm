(module

  (memory $memory 1)

  (global $counter (mut i32)
    (i32.const 0)
  )

  ;; Sanity check négatif :
  ;; vrai initialement, mais non préservé par bounded_increment.
  (;@global_invariant
      $counter.value_i32 == 0
  ;)


  (;@requires
      $counter.value_i32 >= 0
      &&
      $counter.value_i32 <= 100
  ;)

  (;@ensures
      $counter.value_i32 >= old($counter.value_i32)
  ;)

  (;@ensures
      $counter.value_i32 <= old($counter.value_i32) + 1
  ;)

  (;@ensures
      $counter.value_i32 >= 0
      &&
      $counter.value_i32 <= 100
  ;)

  (;@ensures
      $stack[old($sp)].value_i32
      ==
      $counter.value_i32
  ;)

  (func $bounded_increment
    (export "bounded_increment")
    (result i32)

    global.get $counter
    i32.const 100
    i32.lt_u

    if
      global.get $counter
      i32.const 1
      i32.add
      global.set $counter
    end

    global.get $counter
  )


  (;@ensures
      $stack[old($sp)].value_i32 == 20
  ;)

  (func $test_arithmetic
    (export "test_arithmetic")
    (result i32)

    i32.const 10
    i32.const 6
    i32.add

    i32.const 3
    i32.mul

    i32.const 8
    i32.sub

    i32.const 2
    i32.div_u
  )


  (;@ensures
      $stack[old($sp)].value_i32 == 20
  ;)

  (func $test_bitwise
    (export "test_bitwise")
    (result i32)

    i32.const 12
    i32.const 10
    i32.and

    i32.const 3
    i32.or

    i32.const 1
    i32.xor

    i32.const 1
    i32.shl
  )


  (;@ensures
      $stack[old($sp)].value_i32 == 255
  ;)

  (func $test_integer_casts
    (export "test_integer_casts")
    (result i32)

    i32.const 255
    i64.extend_i32_u
    i32.wrap_i64
  )


  (;@ensures
      $stack[old($sp)].value_i32 == 12
  ;)

  (func $test_locals_and_if
    (export "test_locals_and_if")
    (result i32)

    (local $value i32)

    i32.const 7
    local.set $value

    i32.const 1

    if (result i32)
      local.get $value
      i32.const 5
      i32.add
    else
      i32.const 0
    end
  )


  ;; Pas de postcondition exacte ici :
  ;; on évite la preuve mémoire coûteuse dans ce sanity check.
  (func $test_memory
    (export "test_memory")
    (result i32)

    i32.const 16
    i32.const 305419896
    i32.store

    i32.const 16
    i32.load
  )


  (;@requires
      $counter.value_i32 >= 0
      &&
      $counter.value_i32 <= 100
  ;)

  (;@ensures
      $counter.value_i32 >= 0
      &&
      $counter.value_i32 <= 100
  ;)

  (;@ensures
      $stack[old($sp)].value_i32
      ==
      $counter.value_i32
  ;)

  (func $run_all
    (export "run_all")
    (result i32)

    call $test_arithmetic
    drop

    call $test_bitwise
    drop

    call $test_integer_casts
    drop

    call $test_locals_and_if
    drop

    call $test_memory
    drop

    call $bounded_increment
  )
)