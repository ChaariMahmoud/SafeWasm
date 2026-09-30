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

(func $spmv_coo
  (type 2)
  (param i32 i32 i32 i32 i32 i32)
    (local i32 i32)
    local.get 5
    i32.const 0
    local.tee 6
    i32.le_s
    if  ;; label = @1
      return
    end
    loop  ;; label = @1
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
      br_if 0 (;@1;)
    end)
  (func (;3;) (type 3) (param i32 i32 i32 i32 i32 i32 i32)
    (local i32)
    local.get 6
    i32.const 0
    local.tee 7
    i32.le_s
    if  ;; label = @1
      return
    end
    loop  ;; label = @1
      local.get 0
      local.get 1
      local.get 2
      local.get 3
      local.get 4
      local.get 5
      call 2
      local.get 6
      local.get 7
      i32.const 1
      i32.add
      local.tee 7
      i32.ne
      br_if 0 (;@1;)
    end)
  (func (;4;) (type 2) (param i32 i32 i32 i32 i32 i32)
    (local i32 i32 f32 i32)
    local.get 5
    i32.const 0
    local.tee 6
    i32.le_s
    if  ;; label = @1
      return
    end
    loop  ;; label = @1
      local.get 0
      i32.const 4
      i32.add
      i32.load
      local.tee 9
      local.get 0
      i32.load
      local.tee 7
      i32.gt_s
      if  ;; label = @2
        local.get 4
        f32.load
        local.set 8
        loop  ;; label = @3
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
          br_if 0 (;@3;)
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
      br_if 0 (;@1;)
    end)
  (func (;5;) (type 3) (param i32 i32 i32 i32 i32 i32 i32)
    (local i32)
    i32.const 0
    local.set 7
    block  ;; label = @1
      loop  ;; label = @2
        local.get 7
        local.get 6
        i32.eq
        br_if 1 (;@1;)
        local.get 0
        local.get 1
        local.get 2
        local.get 3
        local.get 4
        local.get 5
        call 4
        local.get 7
        i32.const 1
        i32.add
        local.set 7
        br 0 (;@2;)
      end
    end)
  (func (;6;) (type 3) (param i32 i32 i32 i32 i32 i32 i32)
    (local i32 i32 i32 i32 i32 i32 i32 i32 i32)
    local.get 3
    i32.const 0
    local.tee 7
    i32.le_s
    if  ;; label = @1
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
    loop  ;; label = @1
      local.get 0
      i32.load
      local.set 8
      i32.const 0
      local.set 11
      local.get 8
      i32.const 0
      i32.lt_s
      if (result i32)  ;; label = @2
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
      if (result i32)  ;; label = @2
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
      loop  ;; label = @2
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
        br_if 0 (;@2;)
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
      br_if 0 (;@1;)
    end)
  (func (;7;) (type 4) (param i32 i32 i32 i32 i32 i32 i32 i32)
    (local i32)
    i32.const 0
    local.set 8
    block  ;; label = @1
      loop  ;; label = @2
        local.get 8
        local.get 7
        i32.eq
        br_if 1 (;@1;)
        local.get 0
        local.get 1
        local.get 2
        local.get 3
        local.get 4
        local.get 5
        local.get 6
        call 6
        local.get 8
        i32.const 1
        i32.add
        local.set 8
        br 0 (;@2;)
      end
    end)
  (func (;8;) (type 2) (param i32 i32 i32 i32 i32 i32)
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
    if  ;; label = @1
      return
    end
    i32.const 0
    local.set 8
    loop  ;; label = @1
      i32.const 0
      local.set 6
      local.get 8
      i32.const 2
      i32.shl
      local.set 9
      loop  ;; label = @2
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
        br_if 0 (;@2;)
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
      br_if 0 (;@1;)
    end)
  (func (;9;) (type 3) (param i32 i32 i32 i32 i32 i32 i32)
    (local i32)
    i32.const 0
    local.set 7
    block  ;; label = @1
      loop  ;; label = @2
        local.get 7
        local.get 6
        i32.eq
        br_if 1 (;@1;)
        local.get 0
        local.get 1
        local.get 2
        local.get 3
        local.get 4
        local.get 5
        call 8
        local.get 7
        i32.const 1
        i32.add
        local.set 7
        br 0 (;@2;)
      end
    end)
  (export "spmv_coo" (func 2))
  (export "spmv_coo_wrapper" (func 3))
  (export "spmv_csr" (func 4))
  (export "spmv_csr_wrapper" (func 5))
  (export "spmv_dia" (func 6))
  (export "spmv_dia_wrapper" (func 7))
  (export "spmv_ell" (func 8))
  (export "spmv_ell_wrapper" (func 9)))