(set-option :print-success false)
(set-info :smt-lib-version 2.6)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
; done setting options


(declare-fun tickleBool (Bool) Bool)
(assert (and (tickleBool true) (tickleBool false)))
(push 1)
(declare-fun ControlFlow (Int Int) Int)
(declare-fun $stack_max@0 () Int)
(declare-fun $sp@0 () Int)
(declare-fun $sp@1 () Int)
(declare-fun $stack@0 () (Array Int Real))
(declare-fun $stack () (Array Int Real))
(declare-fun $sp@2 () Int)
(declare-fun $stack@1 () (Array Int Real))
(declare-fun $sp@3 () Int)
(declare-fun $tmp1@1 () Real)
(declare-fun $sp@4 () Int)
(declare-fun $tmp2@1 () Real)
(declare-fun call0formal@val@0 () Real)
(declare-fun $sp@5 () Int)
(declare-fun $stack@2 () (Array Int Real))
(set-info :boogie-vc-id func_0)
(set-option :timeout 0)
(set-option :rlimit 0)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
(assert (not
 (=> (= (ControlFlow 0 0) 22) (let ((anon0_correct  (and (=> (= (ControlFlow 0 2) (- 0 21)) (> 32 0)) (=> (> 32 0) (=> (and (= $stack_max@0 32) (= $sp@0 0)) (=> (and (and (<= 0 $sp@0) (<= $sp@0 $stack_max@0)) (and (<= 0 $sp@0) (<= $sp@0 $stack_max@0))) (and (=> (= (ControlFlow 0 2) (- 0 20)) (= $sp@0 0)) (=> (= $sp@0 0) (and (=> (= (ControlFlow 0 2) (- 0 19)) (and (<= 0 $sp@0) (<= $sp@0 $stack_max@0))) (=> (and (<= 0 $sp@0) (<= $sp@0 $stack_max@0)) (and (=> (= (ControlFlow 0 2) (- 0 18)) (< $sp@0 $stack_max@0)) (=> (< $sp@0 $stack_max@0) (=> (= $sp@1 (+ $sp@0 1)) (=> (and (and (= (select $stack@0 $sp@0) 5.0) (forall ((i Int) ) (!  (=> (not (= i $sp@0)) (= (select $stack@0 i) (select $stack i)))
 :qid |dynamicpredulebpl.57:18|
 :skolemid |0|
))) (and (<= 0 $sp@1) (<= $sp@1 $stack_max@0))) (and (=> (= (ControlFlow 0 2) (- 0 17)) (and (<= 0 $sp@1) (<= $sp@1 $stack_max@0))) (=> (and (<= 0 $sp@1) (<= $sp@1 $stack_max@0)) (and (=> (= (ControlFlow 0 2) (- 0 16)) (< $sp@1 $stack_max@0)) (=> (< $sp@1 $stack_max@0) (=> (= $sp@2 (+ $sp@1 1)) (=> (and (and (= (select $stack@1 $sp@1) 7.0) (forall ((i@@0 Int) ) (!  (=> (not (= i@@0 $sp@1)) (= (select $stack@1 i@@0) (select $stack@0 i@@0)))
 :qid |dynamicpredulebpl.57:18|
 :skolemid |0|
))) (and (<= 0 $sp@2) (<= $sp@2 $stack_max@0))) (and (=> (= (ControlFlow 0 2) (- 0 15)) (= $sp@2 (+ $sp@0 2))) (=> (= $sp@2 (+ $sp@0 2)) (and (=> (= (ControlFlow 0 2) (- 0 14)) (and (<= 0 $sp@2) (<= $sp@2 $stack_max@0))) (=> (and (<= 0 $sp@2) (<= $sp@2 $stack_max@0)) (and (=> (= (ControlFlow 0 2) (- 0 13)) (> $sp@2 0)) (=> (> $sp@2 0) (=> (= $sp@3 (- $sp@2 1)) (=> (and (and (forall ((i@@1 Int) ) (!  (=> (not (= i@@1 $sp@2)) (= (select $stack@1 i@@1) (select $stack@1 i@@1)))
 :qid |dynamicpredulebpl.76:18|
 :skolemid |1|
)) (= $tmp1@1 (select $stack@1 (- $sp@2 1)))) (and (<= 0 $sp@3) (<= $sp@3 $stack_max@0))) (and (=> (= (ControlFlow 0 2) (- 0 12)) (= $tmp1@1 7.0)) (=> (= $tmp1@1 7.0) (and (=> (= (ControlFlow 0 2) (- 0 11)) (= $sp@3 (+ $sp@0 1))) (=> (= $sp@3 (+ $sp@0 1)) (and (=> (= (ControlFlow 0 2) (- 0 10)) (and (<= 0 $sp@3) (<= $sp@3 $stack_max@0))) (=> (and (<= 0 $sp@3) (<= $sp@3 $stack_max@0)) (and (=> (= (ControlFlow 0 2) (- 0 9)) (> $sp@3 0)) (=> (> $sp@3 0) (=> (and (and (= $sp@4 (- $sp@3 1)) (= $tmp2@1 (select $stack@1 (- $sp@3 1)))) (and (<= 0 $sp@4) (<= $sp@4 $stack_max@0))) (and (=> (= (ControlFlow 0 2) (- 0 8)) (= $tmp2@1 5.0)) (=> (= $tmp2@1 5.0) (and (=> (= (ControlFlow 0 2) (- 0 7)) (= $sp@4 $sp@0)) (=> (= $sp@4 $sp@0) (=> (= call0formal@val@0 (+ $tmp2@1 $tmp1@1)) (and (=> (= (ControlFlow 0 2) (- 0 6)) (and (<= 0 $sp@4) (<= $sp@4 $stack_max@0))) (=> (and (<= 0 $sp@4) (<= $sp@4 $stack_max@0)) (and (=> (= (ControlFlow 0 2) (- 0 5)) (< $sp@4 $stack_max@0)) (=> (< $sp@4 $stack_max@0) (=> (= $sp@5 (+ $sp@4 1)) (=> (and (and (= (select $stack@2 $sp@4) call0formal@val@0) (forall ((i@@2 Int) ) (!  (=> (not (= i@@2 $sp@4)) (= (select $stack@2 i@@2) (select $stack@1 i@@2)))
 :qid |dynamicpredulebpl.57:18|
 :skolemid |0|
))) (and (<= 0 $sp@5) (<= $sp@5 $stack_max@0))) (and (=> (= (ControlFlow 0 2) (- 0 4)) (= $sp@5 (+ $sp@0 1))) (=> (= $sp@5 (+ $sp@0 1)) (and (=> (= (ControlFlow 0 2) (- 0 3)) (= (select $stack@2 (- $sp@5 1)) 12.0)) (=> (= (select $stack@2 (- $sp@5 1)) 12.0) (=> (= (ControlFlow 0 2) (- 0 1)) (and (<= 0 $sp@5) (<= $sp@5 $stack_max@0)))))))))))))))))))))))))))))))))))))))))))))))))))))))
(let ((PreconditionGeneratedEntry_correct  (=> (= (ControlFlow 0 22) 2) anon0_correct)))
PreconditionGeneratedEntry_correct)))
))
(check-sat)
(get-info :rlimit)
(pop 1)
; Valid
