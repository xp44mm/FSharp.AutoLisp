module FSharp.AutoLisp.AutoLispYacc
open FSharp.LexYacc
open FSharp.AutoLisp
let stateSymbols: string[] = [|"";"program";"INT";"REAL";"STRING";"SYMBOL";"atom";"list";"quoted";"datum";"datums";"datums";"datum";"LPAREN";"RPAREN";"RPAREN";"QUOTE";"datum"|]
let actions: (string * int) list list = [["INT",2;"LPAREN",13;"QUOTE",16;"REAL",3;"STRING",4;"SYMBOL",5;"atom",6;"datum",9;"datums",11;"list",7;"program",1;"quoted",8];["",0];["",-1;"INT",-1;"LPAREN",-1;"QUOTE",-1;"REAL",-1;"RPAREN",-1;"STRING",-1;"SYMBOL",-1];["",-2;"INT",-2;"LPAREN",-2;"QUOTE",-2;"REAL",-2;"RPAREN",-2;"STRING",-2;"SYMBOL",-2];["",-3;"INT",-3;"LPAREN",-3;"QUOTE",-3;"REAL",-3;"RPAREN",-3;"STRING",-3;"SYMBOL",-3];["",-4;"INT",-4;"LPAREN",-4;"QUOTE",-4;"REAL",-4;"RPAREN",-4;"STRING",-4;"SYMBOL",-4];["",-5;"INT",-5;"LPAREN",-5;"QUOTE",-5;"REAL",-5;"RPAREN",-5;"STRING",-5;"SYMBOL",-5];["",-6;"INT",-6;"LPAREN",-6;"QUOTE",-6;"REAL",-6;"RPAREN",-6;"STRING",-6;"SYMBOL",-6];["",-7;"INT",-7;"LPAREN",-7;"QUOTE",-7;"REAL",-7;"RPAREN",-7;"STRING",-7;"SYMBOL",-7];["",-8;"INT",-8;"LPAREN",-8;"QUOTE",-8;"REAL",-8;"RPAREN",-8;"STRING",-8;"SYMBOL",-8];["INT",2;"LPAREN",13;"QUOTE",16;"REAL",3;"RPAREN",15;"STRING",4;"SYMBOL",5;"atom",6;"datum",12;"list",7;"quoted",8];["",-12;"INT",2;"LPAREN",13;"QUOTE",16;"REAL",3;"STRING",4;"SYMBOL",5;"atom",6;"datum",12;"list",7;"quoted",8];["",-9;"INT",-9;"LPAREN",-9;"QUOTE",-9;"REAL",-9;"RPAREN",-9;"STRING",-9;"SYMBOL",-9];["INT",2;"LPAREN",13;"QUOTE",16;"REAL",3;"RPAREN",14;"STRING",4;"SYMBOL",5;"atom",6;"datum",9;"datums",10;"list",7;"quoted",8];["",-10;"INT",-10;"LPAREN",-10;"QUOTE",-10;"REAL",-10;"RPAREN",-10;"STRING",-10;"SYMBOL",-10];["",-11;"INT",-11;"LPAREN",-11;"QUOTE",-11;"REAL",-11;"RPAREN",-11;"STRING",-11;"SYMBOL",-11];["INT",2;"LPAREN",13;"QUOTE",16;"REAL",3;"STRING",4;"SYMBOL",5;"atom",6;"datum",17;"list",7;"quoted",8];["",-13;"INT",-13;"LPAREN",-13;"QUOTE",-13;"REAL",-13;"RPAREN",-13;"STRING",-13;"SYMBOL",-13]]
let rules: list<string list*(obj list->obj)> = [
    ["";"program"], (fun (ss:obj list) -> ss.[0])
    ["atom";"INT"], (fun(ss:obj list)->
        let s0 = unbox<string*SourceRange> ss.[0]
        let result:SExpr =
            let v, r = s0 in SInt(AutoLispNumber.toInt64 v, r)
        box result)
    ["atom";"REAL"], (fun(ss:obj list)->
        let s0 = unbox<string*SourceRange> ss.[0]
        let result:SExpr =
            let v, r = s0 in SReal(AutoLispNumber.toFloat v, r)
        box result)
    ["atom";"STRING"], (fun(ss:obj list)->
        let s0 = unbox<string*SourceRange> ss.[0]
        let result:SExpr =
            let v, r = s0 in SString(v, r)
        box result)
    ["atom";"SYMBOL"], (fun(ss:obj list)->
        let s0 = unbox<string*SourceRange> ss.[0]
        let result:SExpr =
            let v, r = s0 in SSymbol(v, r)
        box result)
    ["datum";"atom"], (fun(ss:obj list)->
        let s0 = unbox<SExpr> ss.[0]
        let result:SExpr =
            s0
        box result)
    ["datum";"list"], (fun(ss:obj list)->
        let s0 = unbox<SExpr> ss.[0]
        let result:SExpr =
            s0
        box result)
    ["datum";"quoted"], (fun(ss:obj list)->
        let s0 = unbox<SExpr> ss.[0]
        let result:SExpr =
            s0
        box result)
    ["datums";"datum"], (fun(ss:obj list)->
        let s0 = unbox<SExpr> ss.[0]
        let result:SExpr list =
            [s0]
        box result)
    ["datums";"datums";"datum"], (fun(ss:obj list)->
        let s0 = unbox<SExpr list> ss.[0]
        let s1 = unbox<SExpr> ss.[1]
        let result:SExpr list =
            s0 @ [s1]
        box result)
    ["list";"LPAREN";"RPAREN"], (fun(ss:obj list)->
        let s0 = unbox<SourceRange> ss.[0]
        let s1 = unbox<SourceRange> ss.[1]
        let result:SExpr =
            SList([], { Start = s0.Start; End = s1.End })
        box result)
    ["list";"LPAREN";"datums";"RPAREN"], (fun(ss:obj list)->
        let s0 = unbox<SourceRange> ss.[0]
        let s1 = unbox<SExpr list> ss.[1]
        let s2 = unbox<SourceRange> ss.[2]
        let result:SExpr =
            SList(s1, { Start = s0.Start; End = s2.End })
        box result)
    ["program";"datums"], (fun(ss:obj list)->
        let s0 = unbox<SExpr list> ss.[0]
        let result:SExpr list =
            s0
        box result)
    ["quoted";"QUOTE";"datum"], (fun(ss:obj list)->
        let s0 = unbox<SourceRange> ss.[0]
        let s1 = unbox<SExpr> ss.[1]
        let result:SExpr =
            SList([SSymbol("quote", s0); s1], { Start = s0.Start; End = s1.Range.End })
        box result)
]
let unboxRoot =
    unbox<SExpr list>
let parser = ParseTable.from(rules, actions)