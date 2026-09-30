(module

  ;; ============================================================
  ;; Types
  ;; ============================================================

  (type (;0;)
    (func
      (param i32 i32 i32 i32 i32 i64 i64 i32 i32)
      (result i32)
    )
  )

  (type (;1;)
    (func
      (param i32 i64 i32)
      (result i32)
    )
  )

  (type (;2;)
    (func
      (param i32 i32 i32 i32)
      (result i32)
    )
  )

  (type (;3;)
    (func)
  )

  (type (;4;)
    (func
      (param i32 i32 i32)
      (result i32)
    )
  )

  (type (;5;)
    (func
      (param i32 i32 i32 i32 i32 i32 i32 i32 i32)
      (result i32)
    )
  )


  ;; ============================================================
  ;; Imports WASI
  ;;
  ;; Les codes de retour sont modélisés comme des i32
  ;; non déterministes.
  ;; ============================================================

  (import "wasi_unstable" "clock_time_get"
    (func $wasi_clock_time_get (type 4))
  )

  (import "wasi_unstable" "path_open"
    (func $wasi_path_open (type 5))
  )

  (import "wasi_unstable" "fd_write"
    (func $wasi_fd_write (type 2))
  )


  ;; ============================================================
  ;; Point d'entrée
  ;;
  ;; La fonction :
  ;;   1. appelle clock_time_get ;
  ;;   2. appelle path_open ;
  ;;   3. construit un iovec en mémoire ;
  ;;   4. appelle fd_write ;
  ;;   5. élimine tous les codes de retour.
  ;;
  ;; Elle ne doit laisser aucune valeur supplémentaire sur la pile.
  ;; ============================================================

  (;@ensures
      $sp == old($sp)
  ;)

  (func $start
    (type 3)

    ;; clock_time_get(0, 1000, 100)
    i32.const 0
    i64.const 1000
    i32.const 100
    call $clock_time_get_wrapper
    drop

    ;; path_open(...)
    i32.const 12
    i32.const 12
    i32.const 12
    i32.const 12
    i32.const 12
    i64.const 12
    i64.const 12
    i32.const 12
    i32.const 12
    call $path_open_wrapper
    drop

    ;; Construction d'un iovec :
    ;;
    ;; memory[0] = 8
    ;; memory[4] = 6
    ;;
    ;; Le buffer commence à l'adresse 8
    ;; et contient 6 octets : "Done!\n".

    i32.const 0
    i32.const 8
    i32.store

    i32.const 4
    i32.const 6
    i32.store

    ;; fd_write(
    ;;     fd       = 1,
    ;;     iovs     = 0,
    ;;     iovs_len = 1,
    ;;     nwritten = 20
    ;; )

    i32.const 1
    i32.const 0
    i32.const 1
    i32.const 20
    call $wasi_fd_write
    drop
  )


  ;; ============================================================
  ;; Wrapper de clock_time_get
  ;;
  ;; Convertit le paramètre i64 en i32 avec i32.wrap_i64.
  ;; La fonction consomme 3 paramètres et produit 1 résultat.
  ;; ============================================================

  (;@requires
      $sp >= 3
  ;)

  (;@ensures
      $sp == old($sp) - 2
  ;)

  (;@ensures
      $stack[old($sp) - 3].value_i32 >= 0
      &&
      $stack[old($sp) - 3].value_i32 < 4294967296
  ;)

  (func $clock_time_get_wrapper
    (type 1)
    (param $clock_id i32)
    (param $precision i64)
    (param $time_address i32)
    (result i32)

    local.get $clock_id

    local.get $precision
    i32.wrap_i64

    local.get $time_address

    call $wasi_clock_time_get
  )


  ;; ============================================================
  ;; Wrapper de path_open
  ;;
  ;; Les deux paramètres i64 sont convertis en i32.
  ;; La fonction consomme 9 paramètres et produit 1 résultat.
  ;; ============================================================

  (;@requires
      $sp >= 9
  ;)

  (;@ensures
      $sp == old($sp) - 8
  ;)

  (;@ensures
      $stack[old($sp) - 9].value_i32 >= 0
      &&
      $stack[old($sp) - 9].value_i32 < 4294967296
  ;)

  (func $path_open_wrapper
    (type 0)

    (param $fd i32)
    (param $dirflags i32)
    (param $path_address i32)
    (param $path_length i32)
    (param $oflags i32)
    (param $rights_base i64)
    (param $rights_inheriting i64)
    (param $fdflags i32)
    (param $opened_fd_address i32)

    (result i32)

    local.get $fd
    local.get $dirflags
    local.get $path_address
    local.get $path_length
    local.get $oflags

    local.get $rights_base
    i32.wrap_i64

    local.get $rights_inheriting
    i32.wrap_i64

    local.get $fdflags
    local.get $opened_fd_address

    call $wasi_path_open
  )


  ;; ============================================================
  ;; Test du point d'entrée
  ;;
  ;; Si $start termine correctement et restaure la pile,
  ;; cette fonction retourne la constante 1.
  ;; ============================================================

  (;@ensures
      $stack[old($sp)].value_i32 == 1
  ;)

  (func $test_start
    (export "test_start")
    (result i32)

    call $start
    i32.const 1
  )


  ;; ============================================================
  ;; Mémoire et exports
  ;; ============================================================

  (memory (;0;) 1)

  (export "memory" (memory 0))
  (export "_start" (func $start))


  ;; ============================================================
  ;; Segment de données
  ;;
  ;; Adresse 8 :
  ;;
  ;;   D o n e ! \n
  ;;
  ;; Six octets au total.
  ;; ============================================================

  (data (;0;)
    (i32.const 8)
    "Done!\0a"
  )
)