(module
  (memory 1)

  (func (export "test_all_loads")
    ;; base address = 100
    (local $a i32)
    i32.const 100
    local.set $a

    ;; --- 0x28..0x2B: i32/i64/f32/f64.load ---
    local.get $a
    i32.load offset=0
    drop

    local.get $a
    i64.load offset=0
    drop

    local.get $a
    f32.load offset=0
    drop

    local.get $a
    f64.load offset=0
    drop

    ;; --- 0x2C..0x2F: i32.load8/16 (s/u) ---
    local.get $a
    i32.load8_s offset=1
    drop

    local.get $a
    i32.load8_u offset=2
    drop

    local.get $a
    i32.load16_s offset=3
    drop

    local.get $a
    i32.load16_u offset=4
    drop

    ;; --- 0x30..0x35: i64.load8/16/32 (s/u) ---
    local.get $a
    i64.load8_s offset=5
    drop

    local.get $a
    i64.load8_u offset=6
    drop

    local.get $a
    i64.load16_s offset=7
    drop

    local.get $a
    i64.load16_u offset=8
    drop

    local.get $a
    i64.load32_s offset=9
    drop

    local.get $a
    i64.load32_u offset=10
    drop
  )
)
