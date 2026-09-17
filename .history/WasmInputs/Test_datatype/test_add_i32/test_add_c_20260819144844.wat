(module

(;@ensures $stack[old($sp)].value_i32 == 3;)
  (func $test_add_result (result i32)
    i32.const 1
    i32.const 2
    i32.add
  )
   (func $test_mul (result i32)
    i32.const 1
    i32.const 2
    i32.mul
  )
)