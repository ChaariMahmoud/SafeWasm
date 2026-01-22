(set-option :print-success false)
(set-info :smt-lib-version 2.6)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
; done setting options


(declare-fun tickleBool (Bool) Bool)
(assert (and (tickleBool true) (tickleBool false)))
(declare-fun floor_real (Real) Real)
(declare-fun sqrt_real (Real) Real)
(declare-fun to_u8 (Int) Int)
(declare-fun byte0_16 (Int) Int)
(declare-fun byte1_16 (Int) Int)
(declare-fun byte0_32 (Int) Int)
(declare-fun byte1_32 (Int) Int)
(declare-fun byte2_32 (Int) Int)
(declare-fun byte3_32 (Int) Int)
(declare-fun byte0_64 (Int) Int)
(declare-fun byte1_64 (Int) Int)
(declare-fun byte2_64 (Int) Int)
(declare-fun byte3_64 (Int) Int)
(declare-fun byte4_64 (Int) Int)
(declare-fun byte5_64 (Int) Int)
(declare-fun byte6_64 (Int) Int)
(declare-fun byte7_64 (Int) Int)
(declare-fun real_to_bool (Real) Bool)
(declare-fun min_real (Real Real) Real)
(declare-fun max_real (Real Real) Real)
(declare-fun abs_real (Real) Real)
(declare-fun nearest_real (Real) Real)
(declare-fun bool_to_real (Bool) Real)
(assert (forall ((r Real) ) (! (< r (+ (floor_real r) 1.0))
 :qid |testmaintenantbpl.230:15|
 :skolemid |25|
)))
(assert (forall ((r@@0 Real) ) (!  (=> (>= r@@0 0.0) (>= (sqrt_real r@@0) 0.0))
 :qid |testmaintenantbpl.218:15|
 :skolemid |20|
 :pattern ( (sqrt_real r@@0))
)))
(assert (forall ((x Int) ) (!  (and (<= 0 (to_u8 x)) (< (to_u8 x) 256))
 :qid |testmaintenantbpl.10:15|
 :skolemid |0|
)))
(assert (forall ((x@@0 Int) ) (!  (and (<= 0 (byte0_16 x@@0)) (< (byte0_16 x@@0) 256))
 :qid |testmaintenantbpl.112:15|
 :skolemid |1|
)))
(assert (forall ((x@@1 Int) ) (!  (and (<= 0 (byte1_16 x@@1)) (< (byte1_16 x@@1) 256))
 :qid |testmaintenantbpl.115:15|
 :skolemid |2|
)))
(assert (forall ((x@@2 Int) ) (!  (and (<= 0 (byte0_32 x@@2)) (< (byte0_32 x@@2) 256))
 :qid |testmaintenantbpl.118:15|
 :skolemid |3|
)))
(assert (forall ((x@@3 Int) ) (!  (and (<= 0 (byte1_32 x@@3)) (< (byte1_32 x@@3) 256))
 :qid |testmaintenantbpl.121:15|
 :skolemid |4|
)))
(assert (forall ((x@@4 Int) ) (!  (and (<= 0 (byte2_32 x@@4)) (< (byte2_32 x@@4) 256))
 :qid |testmaintenantbpl.124:15|
 :skolemid |5|
)))
(assert (forall ((x@@5 Int) ) (!  (and (<= 0 (byte3_32 x@@5)) (< (byte3_32 x@@5) 256))
 :qid |testmaintenantbpl.127:15|
 :skolemid |6|
)))
(assert (forall ((x@@6 Int) ) (!  (and (<= 0 (byte0_64 x@@6)) (< (byte0_64 x@@6) 256))
 :qid |testmaintenantbpl.130:15|
 :skolemid |7|
)))
(assert (forall ((x@@7 Int) ) (!  (and (<= 0 (byte1_64 x@@7)) (< (byte1_64 x@@7) 256))
 :qid |testmaintenantbpl.133:15|
 :skolemid |8|
)))
(assert (forall ((x@@8 Int) ) (!  (and (<= 0 (byte2_64 x@@8)) (< (byte2_64 x@@8) 256))
 :qid |testmaintenantbpl.136:15|
 :skolemid |9|
)))
(assert (forall ((x@@9 Int) ) (!  (and (<= 0 (byte3_64 x@@9)) (< (byte3_64 x@@9) 256))
 :qid |testmaintenantbpl.139:15|
 :skolemid |10|
)))
(assert (forall ((x@@10 Int) ) (!  (and (<= 0 (byte4_64 x@@10)) (< (byte4_64 x@@10) 256))
 :qid |testmaintenantbpl.142:15|
 :skolemid |11|
)))
(assert (forall ((x@@11 Int) ) (!  (and (<= 0 (byte5_64 x@@11)) (< (byte5_64 x@@11) 256))
 :qid |testmaintenantbpl.145:15|
 :skolemid |12|
)))
(assert (forall ((x@@12 Int) ) (!  (and (<= 0 (byte6_64 x@@12)) (< (byte6_64 x@@12) 256))
 :qid |testmaintenantbpl.148:15|
 :skolemid |13|
)))
(assert (forall ((x@@13 Int) ) (!  (and (<= 0 (byte7_64 x@@13)) (< (byte7_64 x@@13) 256))
 :qid |testmaintenantbpl.151:15|
 :skolemid |14|
)))
(assert (forall ((r@@1 Real) ) (! (= (real_to_bool r@@1) (ite (= r@@1 0.0) false true))
 :qid |testmaintenantbpl.196:23|
 :skolemid |16|
 :pattern ( (real_to_bool r@@1))
)))
(assert (forall ((x@@14 Real) (y Real) ) (! (= (min_real x@@14 y) (ite (<= x@@14 y) x@@14 y))
 :qid |testmaintenantbpl.204:19|
 :skolemid |17|
 :pattern ( (min_real x@@14 y))
)))
(assert (forall ((x@@15 Real) (y@@0 Real) ) (! (= (max_real x@@15 y@@0) (ite (>= x@@15 y@@0) x@@15 y@@0))
 :qid |testmaintenantbpl.208:19|
 :skolemid |18|
 :pattern ( (max_real x@@15 y@@0))
)))
(assert (forall ((x@@16 Real) ) (! (= (abs_real x@@16) (ite (>= x@@16 0.0) x@@16 (- 0.0 x@@16)))
 :qid |testmaintenantbpl.212:19|
 :skolemid |19|
 :pattern ( (abs_real x@@16))
)))
(assert (forall ((r@@2 Real) ) (!  (=> (>= r@@2 0.0) (= (* (sqrt_real r@@2) (sqrt_real r@@2)) r@@2))
 :qid |testmaintenantbpl.220:15|
 :skolemid |21|
 :pattern ( (sqrt_real r@@2))
)))
(assert (forall ((r@@3 Real) ) (! (<= (floor_real r@@3) r@@3)
 :qid |testmaintenantbpl.228:15|
 :skolemid |24|
)))
(assert (forall ((r@@4 Real) ) (!  (and (<= (- (nearest_real r@@4) 0.5) r@@4) (<= r@@4 (+ (nearest_real r@@4) 0.5)))
 :qid |testmaintenantbpl.223:15|
 :skolemid |22|
 :pattern ( (nearest_real r@@4))
)))
(assert (forall ((b Bool) ) (! (= (bool_to_real b) (ite b 1.0 0.0))
 :qid |testmaintenantbpl.192:23|
 :skolemid |15|
 :pattern ( (bool_to_real b))
)))
(assert (forall ((r@@5 Real) ) (! (= (nearest_real (nearest_real r@@5)) (nearest_real r@@5))
 :qid |testmaintenantbpl.225:15|
 :skolemid |23|
 :pattern ( (nearest_real r@@5))
)))
(push 1)
(declare-fun ControlFlow (Int Int) Int)
(set-info :boogie-vc-id popToTmp1)
(set-option :timeout 0)
(set-option :rlimit 0)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
(assert (not
 (=> (= (ControlFlow 0 0) 2) true)
))
(check-sat)
(get-info :rlimit)
(pop 1)
; Valid
(reset)
(set-option :print-success false)
(set-info :smt-lib-version 2.6)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
; done setting options


(declare-fun tickleBool (Bool) Bool)
(assert (and (tickleBool true) (tickleBool false)))
(declare-fun floor_real (Real) Real)
(declare-fun sqrt_real (Real) Real)
(declare-fun to_u8 (Int) Int)
(declare-fun byte0_16 (Int) Int)
(declare-fun byte1_16 (Int) Int)
(declare-fun byte0_32 (Int) Int)
(declare-fun byte1_32 (Int) Int)
(declare-fun byte2_32 (Int) Int)
(declare-fun byte3_32 (Int) Int)
(declare-fun byte0_64 (Int) Int)
(declare-fun byte1_64 (Int) Int)
(declare-fun byte2_64 (Int) Int)
(declare-fun byte3_64 (Int) Int)
(declare-fun byte4_64 (Int) Int)
(declare-fun byte5_64 (Int) Int)
(declare-fun byte6_64 (Int) Int)
(declare-fun byte7_64 (Int) Int)
(declare-fun real_to_bool (Real) Bool)
(declare-fun min_real (Real Real) Real)
(declare-fun max_real (Real Real) Real)
(declare-fun abs_real (Real) Real)
(declare-fun nearest_real (Real) Real)
(declare-fun bool_to_real (Bool) Real)
(assert (forall ((r Real) ) (! (< r (+ (floor_real r) 1.0))
 :qid |testmaintenantbpl.230:15|
 :skolemid |25|
)))
(assert (forall ((r@@0 Real) ) (!  (=> (>= r@@0 0.0) (>= (sqrt_real r@@0) 0.0))
 :qid |testmaintenantbpl.218:15|
 :skolemid |20|
 :pattern ( (sqrt_real r@@0))
)))
(assert (forall ((x Int) ) (!  (and (<= 0 (to_u8 x)) (< (to_u8 x) 256))
 :qid |testmaintenantbpl.10:15|
 :skolemid |0|
)))
(assert (forall ((x@@0 Int) ) (!  (and (<= 0 (byte0_16 x@@0)) (< (byte0_16 x@@0) 256))
 :qid |testmaintenantbpl.112:15|
 :skolemid |1|
)))
(assert (forall ((x@@1 Int) ) (!  (and (<= 0 (byte1_16 x@@1)) (< (byte1_16 x@@1) 256))
 :qid |testmaintenantbpl.115:15|
 :skolemid |2|
)))
(assert (forall ((x@@2 Int) ) (!  (and (<= 0 (byte0_32 x@@2)) (< (byte0_32 x@@2) 256))
 :qid |testmaintenantbpl.118:15|
 :skolemid |3|
)))
(assert (forall ((x@@3 Int) ) (!  (and (<= 0 (byte1_32 x@@3)) (< (byte1_32 x@@3) 256))
 :qid |testmaintenantbpl.121:15|
 :skolemid |4|
)))
(assert (forall ((x@@4 Int) ) (!  (and (<= 0 (byte2_32 x@@4)) (< (byte2_32 x@@4) 256))
 :qid |testmaintenantbpl.124:15|
 :skolemid |5|
)))
(assert (forall ((x@@5 Int) ) (!  (and (<= 0 (byte3_32 x@@5)) (< (byte3_32 x@@5) 256))
 :qid |testmaintenantbpl.127:15|
 :skolemid |6|
)))
(assert (forall ((x@@6 Int) ) (!  (and (<= 0 (byte0_64 x@@6)) (< (byte0_64 x@@6) 256))
 :qid |testmaintenantbpl.130:15|
 :skolemid |7|
)))
(assert (forall ((x@@7 Int) ) (!  (and (<= 0 (byte1_64 x@@7)) (< (byte1_64 x@@7) 256))
 :qid |testmaintenantbpl.133:15|
 :skolemid |8|
)))
(assert (forall ((x@@8 Int) ) (!  (and (<= 0 (byte2_64 x@@8)) (< (byte2_64 x@@8) 256))
 :qid |testmaintenantbpl.136:15|
 :skolemid |9|
)))
(assert (forall ((x@@9 Int) ) (!  (and (<= 0 (byte3_64 x@@9)) (< (byte3_64 x@@9) 256))
 :qid |testmaintenantbpl.139:15|
 :skolemid |10|
)))
(assert (forall ((x@@10 Int) ) (!  (and (<= 0 (byte4_64 x@@10)) (< (byte4_64 x@@10) 256))
 :qid |testmaintenantbpl.142:15|
 :skolemid |11|
)))
(assert (forall ((x@@11 Int) ) (!  (and (<= 0 (byte5_64 x@@11)) (< (byte5_64 x@@11) 256))
 :qid |testmaintenantbpl.145:15|
 :skolemid |12|
)))
(assert (forall ((x@@12 Int) ) (!  (and (<= 0 (byte6_64 x@@12)) (< (byte6_64 x@@12) 256))
 :qid |testmaintenantbpl.148:15|
 :skolemid |13|
)))
(assert (forall ((x@@13 Int) ) (!  (and (<= 0 (byte7_64 x@@13)) (< (byte7_64 x@@13) 256))
 :qid |testmaintenantbpl.151:15|
 :skolemid |14|
)))
(assert (forall ((r@@1 Real) ) (! (= (real_to_bool r@@1) (ite (= r@@1 0.0) false true))
 :qid |testmaintenantbpl.196:23|
 :skolemid |16|
 :pattern ( (real_to_bool r@@1))
)))
(assert (forall ((x@@14 Real) (y Real) ) (! (= (min_real x@@14 y) (ite (<= x@@14 y) x@@14 y))
 :qid |testmaintenantbpl.204:19|
 :skolemid |17|
 :pattern ( (min_real x@@14 y))
)))
(assert (forall ((x@@15 Real) (y@@0 Real) ) (! (= (max_real x@@15 y@@0) (ite (>= x@@15 y@@0) x@@15 y@@0))
 :qid |testmaintenantbpl.208:19|
 :skolemid |18|
 :pattern ( (max_real x@@15 y@@0))
)))
(assert (forall ((x@@16 Real) ) (! (= (abs_real x@@16) (ite (>= x@@16 0.0) x@@16 (- 0.0 x@@16)))
 :qid |testmaintenantbpl.212:19|
 :skolemid |19|
 :pattern ( (abs_real x@@16))
)))
(assert (forall ((r@@2 Real) ) (!  (=> (>= r@@2 0.0) (= (* (sqrt_real r@@2) (sqrt_real r@@2)) r@@2))
 :qid |testmaintenantbpl.220:15|
 :skolemid |21|
 :pattern ( (sqrt_real r@@2))
)))
(assert (forall ((r@@3 Real) ) (! (<= (floor_real r@@3) r@@3)
 :qid |testmaintenantbpl.228:15|
 :skolemid |24|
)))
(assert (forall ((r@@4 Real) ) (!  (and (<= (- (nearest_real r@@4) 0.5) r@@4) (<= r@@4 (+ (nearest_real r@@4) 0.5)))
 :qid |testmaintenantbpl.223:15|
 :skolemid |22|
 :pattern ( (nearest_real r@@4))
)))
(assert (forall ((b Bool) ) (! (= (bool_to_real b) (ite b 1.0 0.0))
 :qid |testmaintenantbpl.192:23|
 :skolemid |15|
 :pattern ( (bool_to_real b))
)))
(assert (forall ((r@@5 Real) ) (! (= (nearest_real (nearest_real r@@5)) (nearest_real r@@5))
 :qid |testmaintenantbpl.225:15|
 :skolemid |23|
 :pattern ( (nearest_real r@@5))
)))
; Valid

(push 1)
(declare-fun ControlFlow (Int Int) Int)
(set-info :boogie-vc-id popToTmp2)
(set-option :timeout 0)
(set-option :rlimit 0)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
(assert (not
 (=> (= (ControlFlow 0 0) 2) true)
))
(check-sat)
(get-info :rlimit)
(pop 1)
; Valid
(reset)
(set-option :print-success false)
(set-info :smt-lib-version 2.6)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
; done setting options


(declare-fun tickleBool (Bool) Bool)
(assert (and (tickleBool true) (tickleBool false)))
(declare-fun floor_real (Real) Real)
(declare-fun sqrt_real (Real) Real)
(declare-fun to_u8 (Int) Int)
(declare-fun byte0_16 (Int) Int)
(declare-fun byte1_16 (Int) Int)
(declare-fun byte0_32 (Int) Int)
(declare-fun byte1_32 (Int) Int)
(declare-fun byte2_32 (Int) Int)
(declare-fun byte3_32 (Int) Int)
(declare-fun byte0_64 (Int) Int)
(declare-fun byte1_64 (Int) Int)
(declare-fun byte2_64 (Int) Int)
(declare-fun byte3_64 (Int) Int)
(declare-fun byte4_64 (Int) Int)
(declare-fun byte5_64 (Int) Int)
(declare-fun byte6_64 (Int) Int)
(declare-fun byte7_64 (Int) Int)
(declare-fun real_to_bool (Real) Bool)
(declare-fun min_real (Real Real) Real)
(declare-fun max_real (Real Real) Real)
(declare-fun abs_real (Real) Real)
(declare-fun nearest_real (Real) Real)
(declare-fun bool_to_real (Bool) Real)
(assert (forall ((r Real) ) (! (< r (+ (floor_real r) 1.0))
 :qid |testmaintenantbpl.230:15|
 :skolemid |25|
)))
(assert (forall ((r@@0 Real) ) (!  (=> (>= r@@0 0.0) (>= (sqrt_real r@@0) 0.0))
 :qid |testmaintenantbpl.218:15|
 :skolemid |20|
 :pattern ( (sqrt_real r@@0))
)))
(assert (forall ((x Int) ) (!  (and (<= 0 (to_u8 x)) (< (to_u8 x) 256))
 :qid |testmaintenantbpl.10:15|
 :skolemid |0|
)))
(assert (forall ((x@@0 Int) ) (!  (and (<= 0 (byte0_16 x@@0)) (< (byte0_16 x@@0) 256))
 :qid |testmaintenantbpl.112:15|
 :skolemid |1|
)))
(assert (forall ((x@@1 Int) ) (!  (and (<= 0 (byte1_16 x@@1)) (< (byte1_16 x@@1) 256))
 :qid |testmaintenantbpl.115:15|
 :skolemid |2|
)))
(assert (forall ((x@@2 Int) ) (!  (and (<= 0 (byte0_32 x@@2)) (< (byte0_32 x@@2) 256))
 :qid |testmaintenantbpl.118:15|
 :skolemid |3|
)))
(assert (forall ((x@@3 Int) ) (!  (and (<= 0 (byte1_32 x@@3)) (< (byte1_32 x@@3) 256))
 :qid |testmaintenantbpl.121:15|
 :skolemid |4|
)))
(assert (forall ((x@@4 Int) ) (!  (and (<= 0 (byte2_32 x@@4)) (< (byte2_32 x@@4) 256))
 :qid |testmaintenantbpl.124:15|
 :skolemid |5|
)))
(assert (forall ((x@@5 Int) ) (!  (and (<= 0 (byte3_32 x@@5)) (< (byte3_32 x@@5) 256))
 :qid |testmaintenantbpl.127:15|
 :skolemid |6|
)))
(assert (forall ((x@@6 Int) ) (!  (and (<= 0 (byte0_64 x@@6)) (< (byte0_64 x@@6) 256))
 :qid |testmaintenantbpl.130:15|
 :skolemid |7|
)))
(assert (forall ((x@@7 Int) ) (!  (and (<= 0 (byte1_64 x@@7)) (< (byte1_64 x@@7) 256))
 :qid |testmaintenantbpl.133:15|
 :skolemid |8|
)))
(assert (forall ((x@@8 Int) ) (!  (and (<= 0 (byte2_64 x@@8)) (< (byte2_64 x@@8) 256))
 :qid |testmaintenantbpl.136:15|
 :skolemid |9|
)))
(assert (forall ((x@@9 Int) ) (!  (and (<= 0 (byte3_64 x@@9)) (< (byte3_64 x@@9) 256))
 :qid |testmaintenantbpl.139:15|
 :skolemid |10|
)))
(assert (forall ((x@@10 Int) ) (!  (and (<= 0 (byte4_64 x@@10)) (< (byte4_64 x@@10) 256))
 :qid |testmaintenantbpl.142:15|
 :skolemid |11|
)))
(assert (forall ((x@@11 Int) ) (!  (and (<= 0 (byte5_64 x@@11)) (< (byte5_64 x@@11) 256))
 :qid |testmaintenantbpl.145:15|
 :skolemid |12|
)))
(assert (forall ((x@@12 Int) ) (!  (and (<= 0 (byte6_64 x@@12)) (< (byte6_64 x@@12) 256))
 :qid |testmaintenantbpl.148:15|
 :skolemid |13|
)))
(assert (forall ((x@@13 Int) ) (!  (and (<= 0 (byte7_64 x@@13)) (< (byte7_64 x@@13) 256))
 :qid |testmaintenantbpl.151:15|
 :skolemid |14|
)))
(assert (forall ((r@@1 Real) ) (! (= (real_to_bool r@@1) (ite (= r@@1 0.0) false true))
 :qid |testmaintenantbpl.196:23|
 :skolemid |16|
 :pattern ( (real_to_bool r@@1))
)))
(assert (forall ((x@@14 Real) (y Real) ) (! (= (min_real x@@14 y) (ite (<= x@@14 y) x@@14 y))
 :qid |testmaintenantbpl.204:19|
 :skolemid |17|
 :pattern ( (min_real x@@14 y))
)))
(assert (forall ((x@@15 Real) (y@@0 Real) ) (! (= (max_real x@@15 y@@0) (ite (>= x@@15 y@@0) x@@15 y@@0))
 :qid |testmaintenantbpl.208:19|
 :skolemid |18|
 :pattern ( (max_real x@@15 y@@0))
)))
(assert (forall ((x@@16 Real) ) (! (= (abs_real x@@16) (ite (>= x@@16 0.0) x@@16 (- 0.0 x@@16)))
 :qid |testmaintenantbpl.212:19|
 :skolemid |19|
 :pattern ( (abs_real x@@16))
)))
(assert (forall ((r@@2 Real) ) (!  (=> (>= r@@2 0.0) (= (* (sqrt_real r@@2) (sqrt_real r@@2)) r@@2))
 :qid |testmaintenantbpl.220:15|
 :skolemid |21|
 :pattern ( (sqrt_real r@@2))
)))
(assert (forall ((r@@3 Real) ) (! (<= (floor_real r@@3) r@@3)
 :qid |testmaintenantbpl.228:15|
 :skolemid |24|
)))
(assert (forall ((r@@4 Real) ) (!  (and (<= (- (nearest_real r@@4) 0.5) r@@4) (<= r@@4 (+ (nearest_real r@@4) 0.5)))
 :qid |testmaintenantbpl.223:15|
 :skolemid |22|
 :pattern ( (nearest_real r@@4))
)))
(assert (forall ((b Bool) ) (! (= (bool_to_real b) (ite b 1.0 0.0))
 :qid |testmaintenantbpl.192:23|
 :skolemid |15|
 :pattern ( (bool_to_real b))
)))
(assert (forall ((r@@5 Real) ) (! (= (nearest_real (nearest_real r@@5)) (nearest_real r@@5))
 :qid |testmaintenantbpl.225:15|
 :skolemid |23|
 :pattern ( (nearest_real r@@5))
)))
; Valid

(push 1)
(declare-fun ControlFlow (Int Int) Int)
(set-info :boogie-vc-id popToTmp3)
(set-option :timeout 0)
(set-option :rlimit 0)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
(assert (not
 (=> (= (ControlFlow 0 0) 2) true)
))
(check-sat)
(get-info :rlimit)
(pop 1)
; Valid
(reset)
(set-option :print-success false)
(set-info :smt-lib-version 2.6)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
; done setting options


(declare-fun tickleBool (Bool) Bool)
(assert (and (tickleBool true) (tickleBool false)))
(declare-fun floor_real (Real) Real)
(declare-fun sqrt_real (Real) Real)
(declare-fun to_u8 (Int) Int)
(declare-fun byte0_16 (Int) Int)
(declare-fun byte1_16 (Int) Int)
(declare-fun byte0_32 (Int) Int)
(declare-fun byte1_32 (Int) Int)
(declare-fun byte2_32 (Int) Int)
(declare-fun byte3_32 (Int) Int)
(declare-fun byte0_64 (Int) Int)
(declare-fun byte1_64 (Int) Int)
(declare-fun byte2_64 (Int) Int)
(declare-fun byte3_64 (Int) Int)
(declare-fun byte4_64 (Int) Int)
(declare-fun byte5_64 (Int) Int)
(declare-fun byte6_64 (Int) Int)
(declare-fun byte7_64 (Int) Int)
(declare-fun real_to_bool (Real) Bool)
(declare-fun min_real (Real Real) Real)
(declare-fun max_real (Real Real) Real)
(declare-fun abs_real (Real) Real)
(declare-fun nearest_real (Real) Real)
(declare-fun bool_to_real (Bool) Real)
(assert (forall ((r Real) ) (! (< r (+ (floor_real r) 1.0))
 :qid |testmaintenantbpl.230:15|
 :skolemid |25|
)))
(assert (forall ((r@@0 Real) ) (!  (=> (>= r@@0 0.0) (>= (sqrt_real r@@0) 0.0))
 :qid |testmaintenantbpl.218:15|
 :skolemid |20|
 :pattern ( (sqrt_real r@@0))
)))
(assert (forall ((x Int) ) (!  (and (<= 0 (to_u8 x)) (< (to_u8 x) 256))
 :qid |testmaintenantbpl.10:15|
 :skolemid |0|
)))
(assert (forall ((x@@0 Int) ) (!  (and (<= 0 (byte0_16 x@@0)) (< (byte0_16 x@@0) 256))
 :qid |testmaintenantbpl.112:15|
 :skolemid |1|
)))
(assert (forall ((x@@1 Int) ) (!  (and (<= 0 (byte1_16 x@@1)) (< (byte1_16 x@@1) 256))
 :qid |testmaintenantbpl.115:15|
 :skolemid |2|
)))
(assert (forall ((x@@2 Int) ) (!  (and (<= 0 (byte0_32 x@@2)) (< (byte0_32 x@@2) 256))
 :qid |testmaintenantbpl.118:15|
 :skolemid |3|
)))
(assert (forall ((x@@3 Int) ) (!  (and (<= 0 (byte1_32 x@@3)) (< (byte1_32 x@@3) 256))
 :qid |testmaintenantbpl.121:15|
 :skolemid |4|
)))
(assert (forall ((x@@4 Int) ) (!  (and (<= 0 (byte2_32 x@@4)) (< (byte2_32 x@@4) 256))
 :qid |testmaintenantbpl.124:15|
 :skolemid |5|
)))
(assert (forall ((x@@5 Int) ) (!  (and (<= 0 (byte3_32 x@@5)) (< (byte3_32 x@@5) 256))
 :qid |testmaintenantbpl.127:15|
 :skolemid |6|
)))
(assert (forall ((x@@6 Int) ) (!  (and (<= 0 (byte0_64 x@@6)) (< (byte0_64 x@@6) 256))
 :qid |testmaintenantbpl.130:15|
 :skolemid |7|
)))
(assert (forall ((x@@7 Int) ) (!  (and (<= 0 (byte1_64 x@@7)) (< (byte1_64 x@@7) 256))
 :qid |testmaintenantbpl.133:15|
 :skolemid |8|
)))
(assert (forall ((x@@8 Int) ) (!  (and (<= 0 (byte2_64 x@@8)) (< (byte2_64 x@@8) 256))
 :qid |testmaintenantbpl.136:15|
 :skolemid |9|
)))
(assert (forall ((x@@9 Int) ) (!  (and (<= 0 (byte3_64 x@@9)) (< (byte3_64 x@@9) 256))
 :qid |testmaintenantbpl.139:15|
 :skolemid |10|
)))
(assert (forall ((x@@10 Int) ) (!  (and (<= 0 (byte4_64 x@@10)) (< (byte4_64 x@@10) 256))
 :qid |testmaintenantbpl.142:15|
 :skolemid |11|
)))
(assert (forall ((x@@11 Int) ) (!  (and (<= 0 (byte5_64 x@@11)) (< (byte5_64 x@@11) 256))
 :qid |testmaintenantbpl.145:15|
 :skolemid |12|
)))
(assert (forall ((x@@12 Int) ) (!  (and (<= 0 (byte6_64 x@@12)) (< (byte6_64 x@@12) 256))
 :qid |testmaintenantbpl.148:15|
 :skolemid |13|
)))
(assert (forall ((x@@13 Int) ) (!  (and (<= 0 (byte7_64 x@@13)) (< (byte7_64 x@@13) 256))
 :qid |testmaintenantbpl.151:15|
 :skolemid |14|
)))
(assert (forall ((r@@1 Real) ) (! (= (real_to_bool r@@1) (ite (= r@@1 0.0) false true))
 :qid |testmaintenantbpl.196:23|
 :skolemid |16|
 :pattern ( (real_to_bool r@@1))
)))
(assert (forall ((x@@14 Real) (y Real) ) (! (= (min_real x@@14 y) (ite (<= x@@14 y) x@@14 y))
 :qid |testmaintenantbpl.204:19|
 :skolemid |17|
 :pattern ( (min_real x@@14 y))
)))
(assert (forall ((x@@15 Real) (y@@0 Real) ) (! (= (max_real x@@15 y@@0) (ite (>= x@@15 y@@0) x@@15 y@@0))
 :qid |testmaintenantbpl.208:19|
 :skolemid |18|
 :pattern ( (max_real x@@15 y@@0))
)))
(assert (forall ((x@@16 Real) ) (! (= (abs_real x@@16) (ite (>= x@@16 0.0) x@@16 (- 0.0 x@@16)))
 :qid |testmaintenantbpl.212:19|
 :skolemid |19|
 :pattern ( (abs_real x@@16))
)))
(assert (forall ((r@@2 Real) ) (!  (=> (>= r@@2 0.0) (= (* (sqrt_real r@@2) (sqrt_real r@@2)) r@@2))
 :qid |testmaintenantbpl.220:15|
 :skolemid |21|
 :pattern ( (sqrt_real r@@2))
)))
(assert (forall ((r@@3 Real) ) (! (<= (floor_real r@@3) r@@3)
 :qid |testmaintenantbpl.228:15|
 :skolemid |24|
)))
(assert (forall ((r@@4 Real) ) (!  (and (<= (- (nearest_real r@@4) 0.5) r@@4) (<= r@@4 (+ (nearest_real r@@4) 0.5)))
 :qid |testmaintenantbpl.223:15|
 :skolemid |22|
 :pattern ( (nearest_real r@@4))
)))
(assert (forall ((b Bool) ) (! (= (bool_to_real b) (ite b 1.0 0.0))
 :qid |testmaintenantbpl.192:23|
 :skolemid |15|
 :pattern ( (bool_to_real b))
)))
(assert (forall ((r@@5 Real) ) (! (= (nearest_real (nearest_real r@@5)) (nearest_real r@@5))
 :qid |testmaintenantbpl.225:15|
 :skolemid |23|
 :pattern ( (nearest_real r@@5))
)))
; Valid

(push 1)
(declare-fun ControlFlow (Int Int) Int)
(set-info :boogie-vc-id q@pop)
(set-option :timeout 0)
(set-option :rlimit 0)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
(assert (not
 (=> (= (ControlFlow 0 0) 2) true)
))
(check-sat)
(get-info :rlimit)
(pop 1)
; Valid
(reset)
(set-option :print-success false)
(set-info :smt-lib-version 2.6)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
; done setting options


(declare-fun tickleBool (Bool) Bool)
(assert (and (tickleBool true) (tickleBool false)))
(declare-fun floor_real (Real) Real)
(declare-fun sqrt_real (Real) Real)
(declare-fun to_u8 (Int) Int)
(declare-fun byte0_16 (Int) Int)
(declare-fun byte1_16 (Int) Int)
(declare-fun byte0_32 (Int) Int)
(declare-fun byte1_32 (Int) Int)
(declare-fun byte2_32 (Int) Int)
(declare-fun byte3_32 (Int) Int)
(declare-fun byte0_64 (Int) Int)
(declare-fun byte1_64 (Int) Int)
(declare-fun byte2_64 (Int) Int)
(declare-fun byte3_64 (Int) Int)
(declare-fun byte4_64 (Int) Int)
(declare-fun byte5_64 (Int) Int)
(declare-fun byte6_64 (Int) Int)
(declare-fun byte7_64 (Int) Int)
(declare-fun real_to_bool (Real) Bool)
(declare-fun min_real (Real Real) Real)
(declare-fun max_real (Real Real) Real)
(declare-fun abs_real (Real) Real)
(declare-fun nearest_real (Real) Real)
(declare-fun bool_to_real (Bool) Real)
(assert (forall ((r Real) ) (! (< r (+ (floor_real r) 1.0))
 :qid |testmaintenantbpl.230:15|
 :skolemid |25|
)))
(assert (forall ((r@@0 Real) ) (!  (=> (>= r@@0 0.0) (>= (sqrt_real r@@0) 0.0))
 :qid |testmaintenantbpl.218:15|
 :skolemid |20|
 :pattern ( (sqrt_real r@@0))
)))
(assert (forall ((x Int) ) (!  (and (<= 0 (to_u8 x)) (< (to_u8 x) 256))
 :qid |testmaintenantbpl.10:15|
 :skolemid |0|
)))
(assert (forall ((x@@0 Int) ) (!  (and (<= 0 (byte0_16 x@@0)) (< (byte0_16 x@@0) 256))
 :qid |testmaintenantbpl.112:15|
 :skolemid |1|
)))
(assert (forall ((x@@1 Int) ) (!  (and (<= 0 (byte1_16 x@@1)) (< (byte1_16 x@@1) 256))
 :qid |testmaintenantbpl.115:15|
 :skolemid |2|
)))
(assert (forall ((x@@2 Int) ) (!  (and (<= 0 (byte0_32 x@@2)) (< (byte0_32 x@@2) 256))
 :qid |testmaintenantbpl.118:15|
 :skolemid |3|
)))
(assert (forall ((x@@3 Int) ) (!  (and (<= 0 (byte1_32 x@@3)) (< (byte1_32 x@@3) 256))
 :qid |testmaintenantbpl.121:15|
 :skolemid |4|
)))
(assert (forall ((x@@4 Int) ) (!  (and (<= 0 (byte2_32 x@@4)) (< (byte2_32 x@@4) 256))
 :qid |testmaintenantbpl.124:15|
 :skolemid |5|
)))
(assert (forall ((x@@5 Int) ) (!  (and (<= 0 (byte3_32 x@@5)) (< (byte3_32 x@@5) 256))
 :qid |testmaintenantbpl.127:15|
 :skolemid |6|
)))
(assert (forall ((x@@6 Int) ) (!  (and (<= 0 (byte0_64 x@@6)) (< (byte0_64 x@@6) 256))
 :qid |testmaintenantbpl.130:15|
 :skolemid |7|
)))
(assert (forall ((x@@7 Int) ) (!  (and (<= 0 (byte1_64 x@@7)) (< (byte1_64 x@@7) 256))
 :qid |testmaintenantbpl.133:15|
 :skolemid |8|
)))
(assert (forall ((x@@8 Int) ) (!  (and (<= 0 (byte2_64 x@@8)) (< (byte2_64 x@@8) 256))
 :qid |testmaintenantbpl.136:15|
 :skolemid |9|
)))
(assert (forall ((x@@9 Int) ) (!  (and (<= 0 (byte3_64 x@@9)) (< (byte3_64 x@@9) 256))
 :qid |testmaintenantbpl.139:15|
 :skolemid |10|
)))
(assert (forall ((x@@10 Int) ) (!  (and (<= 0 (byte4_64 x@@10)) (< (byte4_64 x@@10) 256))
 :qid |testmaintenantbpl.142:15|
 :skolemid |11|
)))
(assert (forall ((x@@11 Int) ) (!  (and (<= 0 (byte5_64 x@@11)) (< (byte5_64 x@@11) 256))
 :qid |testmaintenantbpl.145:15|
 :skolemid |12|
)))
(assert (forall ((x@@12 Int) ) (!  (and (<= 0 (byte6_64 x@@12)) (< (byte6_64 x@@12) 256))
 :qid |testmaintenantbpl.148:15|
 :skolemid |13|
)))
(assert (forall ((x@@13 Int) ) (!  (and (<= 0 (byte7_64 x@@13)) (< (byte7_64 x@@13) 256))
 :qid |testmaintenantbpl.151:15|
 :skolemid |14|
)))
(assert (forall ((r@@1 Real) ) (! (= (real_to_bool r@@1) (ite (= r@@1 0.0) false true))
 :qid |testmaintenantbpl.196:23|
 :skolemid |16|
 :pattern ( (real_to_bool r@@1))
)))
(assert (forall ((x@@14 Real) (y Real) ) (! (= (min_real x@@14 y) (ite (<= x@@14 y) x@@14 y))
 :qid |testmaintenantbpl.204:19|
 :skolemid |17|
 :pattern ( (min_real x@@14 y))
)))
(assert (forall ((x@@15 Real) (y@@0 Real) ) (! (= (max_real x@@15 y@@0) (ite (>= x@@15 y@@0) x@@15 y@@0))
 :qid |testmaintenantbpl.208:19|
 :skolemid |18|
 :pattern ( (max_real x@@15 y@@0))
)))
(assert (forall ((x@@16 Real) ) (! (= (abs_real x@@16) (ite (>= x@@16 0.0) x@@16 (- 0.0 x@@16)))
 :qid |testmaintenantbpl.212:19|
 :skolemid |19|
 :pattern ( (abs_real x@@16))
)))
(assert (forall ((r@@2 Real) ) (!  (=> (>= r@@2 0.0) (= (* (sqrt_real r@@2) (sqrt_real r@@2)) r@@2))
 :qid |testmaintenantbpl.220:15|
 :skolemid |21|
 :pattern ( (sqrt_real r@@2))
)))
(assert (forall ((r@@3 Real) ) (! (<= (floor_real r@@3) r@@3)
 :qid |testmaintenantbpl.228:15|
 :skolemid |24|
)))
(assert (forall ((r@@4 Real) ) (!  (and (<= (- (nearest_real r@@4) 0.5) r@@4) (<= r@@4 (+ (nearest_real r@@4) 0.5)))
 :qid |testmaintenantbpl.223:15|
 :skolemid |22|
 :pattern ( (nearest_real r@@4))
)))
(assert (forall ((b Bool) ) (! (= (bool_to_real b) (ite b 1.0 0.0))
 :qid |testmaintenantbpl.192:23|
 :skolemid |15|
 :pattern ( (bool_to_real b))
)))
(assert (forall ((r@@5 Real) ) (! (= (nearest_real (nearest_real r@@5)) (nearest_real r@@5))
 :qid |testmaintenantbpl.225:15|
 :skolemid |23|
 :pattern ( (nearest_real r@@5))
)))
; Valid

(push 1)
(declare-fun ControlFlow (Int Int) Int)
(set-info :boogie-vc-id func_0)
(set-option :timeout 0)
(set-option :rlimit 0)
(set-option :smt.mbqi false)
(set-option :model.compact false)
(set-option :model.v2 true)
(set-option :pp.bv_literals false)
(assert (not
 (=> (= (ControlFlow 0 0) 2) true)
))
(check-sat)
(get-info :rlimit)
(pop 1)
; Valid
