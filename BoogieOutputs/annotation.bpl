var $stack: [int]real;
var $sp: int;
var $tmp1: real;
var $tmp2: real;
var $tmp3: real;
procedure {:inline true} push(val: real);
modifies $sp;
modifies $stack;
implementation push(val: real)
{
    $stack[$sp] := val;
    $sp := ($sp) + (1);
}

procedure popToTmp1();
modifies $sp;
modifies $stack;
modifies $tmp1;
implementation popToTmp1()
{
    assume (($sp) > (0));
    $sp := ($sp) - (1);
    $tmp1 := $stack[$sp];
}

procedure popToTmp2();
modifies $sp;
modifies $stack;
modifies $tmp2;
implementation popToTmp2()
{
    assume (($sp) > (0));
    $sp := ($sp) - (1);
    $tmp2 := $stack[$sp];
}

procedure popToTmp3();
modifies $sp;
modifies $stack;
modifies $tmp3;
implementation popToTmp3()
{
    assume (($sp) > (0));
    $sp := ($sp) - (1);
    $tmp3 := $stack[$sp];
}

procedure pop();
modifies $sp;
implementation pop()
{
    assume (($sp) > (0));
    $sp := ($sp) - (1);
}

procedure {:inline true} popArgs6() returns (a1: real, a2: real, a3: real, a4: real, a5: real, a6: real);
modifies $sp;
modifies $stack;
implementation popArgs6() returns (a1: real, a2: real, a3: real, a4: real, a5: real, a6: real)
{
    assume (($sp) >= (6));
    $sp := ($sp) - (1);
    a6 := $stack[$sp];
    $sp := ($sp) - (1);
    a5 := $stack[$sp];
    $sp := ($sp) - (1);
    a4 := $stack[$sp];
    $sp := ($sp) - (1);
    a3 := $stack[$sp];
    $sp := ($sp) - (1);
    a2 := $stack[$sp];
    $sp := ($sp) - (1);
    a1 := $stack[$sp];
}

procedure func_0();
modifies $tmp1;
modifies $tmp2;
modifies $tmp3;
modifies $sp;
modifies $stack;
implementation func_0()
{
    var arg1: real;
    var arg2: real;
    var arg3: real;
    var arg4: real;
    var arg5: real;
    var arg6: real;
    var idx: int;
    var entry_sp: int;
    var load_i: int;
    var store_i: int;
    entry_sp := $sp;
    $tmp1 := 0.0;
    $tmp2 := 0.0;
    $tmp3 := 0.0;
    assume (($sp) >= (6));
    call arg1, arg2, arg3, arg4, arg5, arg6 := popArgs6();
    call push(arg5);
    call push(arg6);
    call popToTmp1();
    call popToTmp2();
    call push(($tmp2) + ($tmp1));
    // // footer stack assert disabled
}

