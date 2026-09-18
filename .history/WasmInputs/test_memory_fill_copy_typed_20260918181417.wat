(module
  (memory 1)

  ;; 0x1234 est tronqué sur 8 bits : 0x34 = 52.
  (func $fill_four
    i32.const 16
    i32.const 4660
    i32.const 4
    memory.fill
  )

  ;; Remplit [32..35] avec 0xAB, puis copie vers [48..51].
  (func $copy_non_overlap
    i32.const 32
    i32.const 171
    i32.const 4
    memory.fill

    i32.const 48
    i32.const 32
    i32.const 4
    memory.copy
  )

  ;; Mémoire initiale :
  ;; [64,65,66,67] = [1,2,3,4]
  ;;
  ;; memory.copy(dst=65, src=64, len=3)
  ;; doit produire [1,1,2,3], comme memmove.
  (func $copy_overlap
    i32.const 64
    i32.const 1
    i32.store8

    i32.const 65
    i32.const 2
    i32.store8

    i32.const 66
    i32.const 3
    i32.store8

    i32.const 67
    i32.const 4
    i32.store8

    i32.const 65
    i32.const 64
    i32.const 3
    memory.copy
  )

  (export "fill_four" (func $fill_four))
  (export "copy_non_overlap" (func $copy_non_overlap))
  (export "copy_overlap" (func $copy_overlap))
)