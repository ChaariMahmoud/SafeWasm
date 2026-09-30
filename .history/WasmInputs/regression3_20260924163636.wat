(module
  (type (;0;) (func (param f32)))
  (type (;1;) (func (param i32)))
  (type (;2;) (func (param i32 i32 i32 i32 i32 i32)))
  (type (;3;) (func (param i32 i32 i32 i32 i32 i32 i32)))
  (type (;4;) (func (param i32 i32 i32 i32 i32 i32 i32 i32)))

  (import "js" "mem" (memory (;0;) 1))
  (import "console" "log" (func (;0;) (type 0)))
  (import "console" "log" (func (;1;) (type 1)))

  (;@requires $sp >= 6;)
  (;@ensures $sp == old($sp) - 6;)
  (;@ensures $mem_pages == old($mem_pages);)
  (func $spmv_coo (type 2) (param i32 i32 i32 i32 i32 i32)
    (local i32 i32)
    local.get 5
    i32.const 0
    local.tee 6
    i32.le_s
    if
      return
    end
    loop
      local.get 4
      local.get 0
      i32.load
      i32.const 2
      i32.shl
      i32.add
      local.tee 7
      local.get 2
      f32.load
      local.get 3
      local.get 1
      i32.load
      i32.const 2
      i32.shl
      i32.add
      f32.load
      f32.mul
      local.get 7
      f32.load
      f32.add
      f32.store
      local.get 0
      i32.const 4
      i32.add
      local.set 0
      local.get 1
      i32.const 4
      i32.add
      local.set 1
      local.get 2
      i32.const 4
      i32.add
      local.set 2
      local.get 6
      i32.const 1
      i32.add
      local.tee 6
      local.get 5
      i32.ne
      br_if 0
    end)

  (;@requires $sp >= 7;)
  (;@ensures $sp == old($sp) - 7;)
  (;@ensures $mem_pages == old($mem_pages);)
  (func $spmv_coo_wrapper (type 3) (param i32 i32 i32 i32 i32 i32 i32)
    (local i32)
    local.get 6
    i32.const 0
    local.tee 7
    i32.le_s
    if
      return
    end
    loop
      local.get 0
      local.get 1
      local.get 2
      local.get 3
      local.get 4
      local.get 5
      call $spmv_coo
      local.get 6
      local.get 7
      i32.const 1
      i32.add
      local.tee 7
      i32.ne
      br_if 0
    end)

  (;@requires $sp >= 6;)
  (;@ensures $sp == old($sp) - 6;)
  (;@ensures $mem_pages == old($mem_pages);)
  (func $spmv_csr (type 2) (param i32 i32 i32 i32 i32 i32)
    (local i32 i32 f32 i32)
    local.get 5
    i32.const 0
    local.tee 6
    i32.le_s
    if
      return
    end
    loop
      local.get 0
      i32.const 4
      i32.add
      i32.load
      local.tee 9
      local.get 0
      i32.load
      local.tee 7
      i32.gt_s
      if
        local.get 4
        f32.load
        local.set 8
        loop
          local.get 2
          f32.load
          local.get 3
          local.get 1
          i32.load
          i32.const 2
          i32.shl
          i32.add
          f32.load
          f32.mul
          local.get 8
          f32.add
          local.set 8
          local.get 1
          i32.const 4
          i32.add
          local.set 1
          local.get 2
          i32.const 4
          i32.add
          local.set 2
          local.get 7
          i32.const 1
          i32.add
          local.tee 7
          local.get 9
          i32.ne
          br_if 0
        end
        local.get 4
        local.get 8
        f32.store
      end
      local.get 4
      i32.const 4
      i32.add
      local.set 4
      local.get 0
      i32.const 4
      i32.add
      local.set 0
      local.get 6
      i32.const 1
      i32.add
      local.tee 6
      local.get 5
      i32.ne
      br_if 0
    end)

  (;@requires $sp >= 7;)
  (;@ensures $sp == old($sp) - 7;)
  (;@ensures $mem_pages == old($mem_pages);)
  (func $spmv_csr_wrapper (type 3) (param i32 i32 i32 i32 i32 i32 i32)
    (local i32)
    i32.const 0
    local.set 7
    block
      loop
        local.get 7
        local.get 6
        i32.eq
        br_if 1
        local.get 0
        local.get 1
        local.get 2
        local.get 3
        local.get 4
        local.get 5
        call $spmv_csr
        local.get 7
        i32.const 1
        i32.add
        local.set 7
        br 0
      end
    end)

  (;@requires $sp >= 7;)
  (;@ensures $sp == old($sp) - 7;)
  (;@ensures $mem_pages == old($mem_pages);)
  (func $spmv_dia (type 3) (param i32 i32 i32 i32 i32 i32 i32)
    (local i32 i32 i32 i32 i32 i32 i32 i32 i32)
    local.get 3
    i32.const 0
    local.tee 7
    i32.le_s
    if
      return
    end
    local.get 2
    local.get 4
    i32.sub
    local.set 12
    local.get 2
    i32.const 1
    i32.sub
    local.set 13
    i32.const 0
    local.set 14
    loop
      local.get 0
      i32.load
      local.set 8
      i32.const 0
      local.set 11
      local.get 8
      i32.const 0
      i32.lt_s
      if (result i32)
        local.get 12
        local.set 11
        i32.const 0
        local.get 8
        i32.sub
      else
        i32.const 0
      end
      local.set 9
      local.get 13
      local.get 13
      local.get 8
      i32.sub
      i32.lt_s
      if (result i32)
        local.get 13
      else
        local.get 13
        local.get 8
        i32.sub
      end
      local.set 10
      local.get 14
      local.get 11
      i32.sub
      local.set 15
      loop
        local.get 6
        local.get 9
        i32.const 2
        i32.shl
        i32.add
        local.get 1
        local.get 15
        local.get 9
        i32.add
        i32.const 2
        i32.shl
        i32.add
        f32.load
        local.get 5
        local.get 9
        local.get 8
        i32.add
        i32.const 2
        i32.shl
        i32.add
        f32.load
        f32.mul
        local.get 6
        local.get 9
        i32.const 2
        i32.shl
        i32.add
        f32.load
        f32.add
        f32.store
        local.get 9
        i32.const 1
        i32.add
        local.tee 9
        local.get 10
        i32.le_s
        br_if 0
      end
      local.get 0
      i32.const 4
      i32.add
      local.set 0
      local.get 14
      local.get 4
      i32.add
      local.set 14
      local.get 7
      i32.const 1
      i32.add
      local.tee 7
      local.get 3
      i32.ne
      br_if 0
    end)

  (;@requires $sp >= 8;)
  (;@ensures $sp == old($sp) - 8;)
  (;@ensures $mem_pages == old($mem_pages);)
  (func $spmv_dia_wrapper (type 4) (param i32 i32 i32 i32 i32 i32 i32 i32)
    (local i32)
    i32.const 0
    local.set 8
    block
      loop
        local.get 8
        local.get 7
        i32.eq
        br_if 1
        local.get 0
        local.get 1
        local.get 2
        local.get 3
        local.get 4
        local.get 5
        local.get 6
        call $spmv_dia
        local.get 8
        i32.const 1
        i32.add
        local.set 8
        br 0
      end
    end)

  (;@requires $sp >= 6;)
  (;@ensures $sp == old($sp) - 6;)
  (;@ensures $mem_pages == old($mem_pages);)
  (func $spmv_ell (type 2) (param i32 i32 i32 i32 i32 i32)
    (local i32 i32 i32 i32)
    local.get 3
    i32.const 0
    local.tee 7
    i32.gt_s
    local.get 2
    i32.const 0
    i32.gt_s
    i32.and
    i32.eqz
    if
      return
    end
    i32.const 0
    local.set 8
    loop
      i32.const 0
      local.set 6
      local.get 8
      i32.const 2
      i32.shl
      local.set 9
      loop
        local.get 5
        local.get 6
        i32.const 2
        i32.shl
        i32.add
        local.get 1
        local.get 9
        i32.add
        f32.load
        local.get 4
        local.get 0
        local.get 9
        i32.add
        i32.load
        i32.const 2
        i32.shl
        i32.add
        f32.load
        f32.mul
        local.get 5
        local.get 6
        i32.const 2
        i32.shl
        i32.add
        f32.load
        f32.add
        f32.store
        local.get 9
        i32.const 4
        i32.add
        local.set 9
        local.get 6
        i32.const 1
        i32.add
        local.tee 6
        local.get 2
        i32.ne
        br_if 0
      end
      local.get 8
      local.get 2
      i32.add
      local.set 8
      local.get 7
      i32.const 1
      i32.add
      local.tee 7
      local.get 3
      i32.ne
      br_if 0
    end)

  (;@requires $sp >= 7;)
  (;@ensures $sp == old($sp) - 7;)
  (;@ensures $mem_pages == old($mem_pages);)
  (func $spmv_ell_wrapper (type 3) (param i32 i32 i32 i32 i32 i32 i32)
    (local i32)
    i32.const 0
    local.set 7
    block
      loop
        local.get 7
        local.get 6
        i32.eq
        br_if 1
        local.get 0
        local.get 1
        local.get 2
        local.get 3
        local.get 4
        local.get 5
        call $spmv_ell
        local.get 7
        i32.const 1
        i32.add
        local.set 7
        br 0
      end
    end)

  (export "spmv_coo" (func $spmv_coo))
  (export "spmv_coo_wrapper" (func $spmv_coo_wrapper))
  (export "spmv_csr" (func $spmv_csr))
  (export "spmv_csr_wrapper" (func $spmv_csr_wrapper))
  (export "spmv_dia" (func $spmv_dia))
  (export "spmv_dia_wrapper" (func $spmv_dia_wrapper))
  (export "spmv_ell" (func $spmv_ell))
  (export "spmv_ell_wrapper" (func $spmv_ell_wrapper))
)