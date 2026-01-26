(module
  (global $MAX_REQUESTS i32 (i32.const 10))

  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
  ;; INVARIANT: 0 <= active_requests <= MAX_REQUESTS ;;
  ;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

  (global $active_requests (mut i32) (i32.const 0))

  ;; Acquire a request slot
  ;; Returns 1 if successful, 0 if limit reached
  (func $acquire_request (result i32)
  (local $current i32)

  (local.set $current (global.get $active_requests))

  (if
    (i32.ge_s (local.get $current) (global.get $MAX_REQUESTS))
    (then
      (return (i32.const 0))
    )
    (else
      (global.set $active_requests
        (i32.add (local.get $current) (i32.const 1))
      )
      (return (i32.const 1))
    )
  )

  ;; unreachable, but optional fallback:
  (i32.const 0)
)


  ;; Release a request slot
  (func $release_request
    (local $current i32)
      (local.set $current (global.get $active_requests))
      (if (i32.gt_s (local.get $current) (i32.const 0))
      (then
        (global.set $active_requests
          (i32.sub (local.get $current) (i32.const 1))
        )
      )
    )
  )
)