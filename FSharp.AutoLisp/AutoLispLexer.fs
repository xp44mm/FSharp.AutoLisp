module FSharp.AutoLisp.AutoLispLexer
open System
open FSharp.Idioms
open FSharp.Idioms.Literal
open FSharp.LexYacc
open FSharp.LexYacc.Bootstrap
open FSharp.AutoLisp
let anal:Analyzer = {nextStates=Map [0u,set [[-7,8;11,12;14,31;33,33;35,38;42,42;44,44;47,47;58,58;60,1048575],28u;[9,10;13,13;32,32],1u;[34,34],10u;[39,39],9u;[40,40],7u;[41,41],8u;[43,43;45,45],14u;[46,46],24u;[48,57],15u;[59,59],2u];1u,set [[9,10;13,13;32,32],1u];2u,set [[-7,9;11,12;14,123;125,1048575],6u;[124,124],3u];3u,set [[-7,123;125,1048575],3u;[124,124],4u];4u,set [[-7,58;60,1048575],3u;[59,59],5u];6u,set [[-7,9;11,12;14,1048575],6u];10u,set [[-7,33;35,91;93,1048575],11u;[34,34],13u;[92,92],12u];11u,set [[-7,33;35,91;93,1048575],11u;[34,34],13u;[92,92],12u];12u,set [[-7,1048575],11u];14u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,1048575],28u;[48,57],15u];15u,set [[-7,8;11,12;14,31;33,33;35,38;42,45;47,47;58,58;60,68;70,100;102,1048575],28u;[46,46],16u;[48,57],15u;[69,69;101,101],20u];16u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,68;70,100;102,1048575],28u;[48,57],16u;[69,69;101,101],17u];17u,set [[-7,8;11,12;14,31;33,33;35,38;42,42;44,44;46,47;58,58;60,1048575],28u;[43,43;45,45],18u;[48,57],19u];18u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,1048575],28u;[48,57],19u];19u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,1048575],28u;[48,57],19u];20u,set [[-7,8;11,12;14,31;33,33;35,38;42,42;44,44;46,47;58,58;60,1048575],28u;[43,43;45,45],21u;[48,57],22u];21u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,1048575],28u;[48,57],22u];22u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,1048575],28u;[48,57],22u];23u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,68;70,100;102,1048575],28u;[48,57],23u;[69,69;101,101],25u];24u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,1048575],28u;[48,57],23u];25u,set [[-7,8;11,12;14,31;33,33;35,38;42,42;44,44;46,47;58,58;60,1048575],28u;[43,43;45,45],26u;[48,57],27u];26u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,1048575],28u;[48,57],27u];27u,set [[-7,8;11,12;14,31;33,33;35,38;42,47;58,58;60,1048575],28u;[48,57],27u];28u,set [[-7,8;11,12;14,31;33,33;35,38;42,58;60,1048575],28u]];flags=Map [1u,set [0uy,Accept];2u,set [3uy,Accept];5u,set [1uy,Accept];6u,set [2uy,Accept];7u,set [4uy,Accept];8u,set [5uy,Accept];9u,set [6uy,Accept];10u,set [13uy,Accept];13u,set [7uy,Accept];14u,set [12uy,Accept];15u,set [11uy,Accept];16u,set [8uy,Accept];17u,set [12uy,Accept];18u,set [12uy,Accept];19u,set [8uy,Accept];20u,set [12uy,Accept];21u,set [12uy,Accept];22u,set [9uy,Accept];23u,set [10uy,Accept];24u,set [12uy,Accept];25u,set [12uy,Accept];26u,set [12uy,Accept];27u,set [10uy,Accept];28u,set [12uy,Accept]];lookbehinds=Map []}
let actions : Map<byte,  char list -> AutoLispToken list > = Map [
    0uy, (fun buff -> [])
    1uy, (fun buff -> [])
    2uy, (fun buff -> [])
    3uy, (fun buff -> [])
    4uy, (fun buff -> [LPAREN])
    5uy, (fun buff -> [RPAREN])
    6uy, (fun buff -> [QUOTE])
    7uy, (fun buff -> [STRING(AutoLispString.unquote buff)])
    8uy, (fun buff -> [REAL(String.build buff)])
    9uy, (fun buff -> [REAL(String.build buff)])
    10uy, (fun buff -> [REAL(String.build buff)])
    11uy, (fun buff -> [INT(String.build buff)])
    12uy, (fun buff -> [SYMBOL(String.build buff)])
    13uy, (fun buff -> [INVALID(String.build buff)])
    ]