(module
  (table 4 funcref)

  (func $target)

  ;; Rend $target disponible pour ref.func.
  (elem declare func $target)

  (func $set_null
    i32.const 1
    ref.null func
    table.set
  )

  (func $set_func
    i32.const 2
    ref.func $target
    table.set
  )

  ;; Copie la référence de la case 2 vers la case 3
  ;; en utilisant table.get puis table.set.
  (func $copy_func_ref
    i32.const 3

    i32.const 2
    table.get

    table.set
  )

  (func $get_and_drop
    i32.const 2
    table.get
    drop
  )

  (export "set_null" (func $set_null))
  (export "set_func" (func $set_func))
  (export "copy_func_ref" (func $copy_func_ref))
  (export "get_and_drop" (func $get_and_drop))
)