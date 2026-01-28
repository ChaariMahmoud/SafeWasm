
var $stack: [int]real;
var $sp: int;
var $tmp1: real;
var $tmp2: real;
var $tmp3: real;


procedure {:inline true} InitRuntime();
modifies $sp,  $tmp1, $tmp2, $tmp3;

ensures $sp == 0;
ensures 0 <= $sp;
implementation InitRuntime()
{

  $sp := 0;
  $tmp1 := 0.0;
  $tmp2 := 0.0;
  $tmp3 := 0.0;
}




procedure {:inline true} push(val: real);
modifies $sp;
modifies $stack;
requires 0 <= $sp ;
ensures  $sp == old($sp) + 1;
ensures  $stack[old($sp)] == val;
ensures  (forall i:int :: i != old($sp) ==> $stack[i] == old($stack)[i]);
ensures  0 <= $sp;
implementation push(val: real)
{


    $stack[$sp] := val;
    $sp := $sp + 1;


}


procedure {:inline true} popToTmp1();
modifies $sp;
modifies $tmp1;
requires $sp > 0;
ensures  $sp == old($sp) - 1;
ensures  (forall i:int :: i != old($sp) ==> $stack[i] == old($stack)[i]);
ensures  $tmp1 == old($stack)[old($sp) - 1];
ensures  0 <= $sp;
implementation popToTmp1()
{
    


    $sp := $sp - 1;
    $tmp1 := $stack[$sp];

}


procedure {:inline true} popToTmp2();
modifies $sp, $tmp2;
requires 0 <= $sp;
requires $sp > 0;
ensures  $sp == old($sp) - 1;
ensures  $tmp2 == old($stack)[old($sp) - 1];
ensures  0 <= $sp ;
implementation popToTmp2()
{
  

    $sp := $sp - 1;
    $tmp2 := $stack[$sp];

   
}

procedure  {:inline true} popToTmp3();
modifies $sp, $tmp3;
requires 0 <= $sp;
requires $sp > 0;
ensures  $sp == old($sp) - 1;
ensures  $tmp3 == old($stack)[old($sp) - 1];
ensures  0 <= $sp;
implementation popToTmp3()
{


    $sp := $sp - 1;
    $tmp3 := $stack[$sp];

}

procedure {:inline true} pop();
modifies $sp;
implementation pop()
{
 

    $sp := $sp - 1;

}

procedure func_0();
modifies $tmp1, $tmp2, $tmp3, $sp, $stack;
ensures 0 <= $sp;

implementation func_0()
{
    var entry_sp: int;

    call InitRuntime();


    entry_sp := $sp;
    
    assert entry_sp == 0;  // manual edit 

    call push(5.0);
    call push(7.0);

    assert $sp == entry_sp + 2; // manual edit 

    call popToTmp1();
    assert $tmp1 == 7.0;   // manual edit 
    assert $sp == entry_sp + 1;  // manual edit 

    call popToTmp2();
    assert $tmp2 == 5.0;  // manual edit 
    assert $sp == entry_sp;  // manual edit 

    call push($tmp2 + $tmp1);

    assert $sp == entry_sp + 1;  // manual edit 
    assert $stack[$sp - 1] == 12.0;  // manual edit 
}
