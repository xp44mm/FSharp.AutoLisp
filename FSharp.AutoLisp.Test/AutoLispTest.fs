module FSharp.AutoLisp.Test.AutoLispTest

open Xunit
open FSharp.AutoLisp

let private check (text: string) =
    FSharp.AutoLisp.AutoLisp.checkWithDefaults text

let private codes (text: string) =
    (check text).Diagnostics |> List.map(fun d -> d.Code)

[<Fact>]
let ``合法程序没有诊断`` () =
    let text = """
(defun c:demo ( / p1 p2 n)
  (setq n 0)
  (while (< n 3)
    (setq p1 (getpoint "\n第一点: "))
    (if p1
      (progn
        (command "_.line" p1 p2 "")
        (setq n (1+ n)))
      (setq n 3)))
  (princ))
"""
    let result = check text
    Assert.Empty(result.Diagnostics)

[<Fact>]
let ``缺少右括号时报出左括号位置`` () =
    let result = check "(defun f (x)\n  (if x (princ))"
    let diagnostic = Assert.Single(result.Diagnostics)
    Assert.Equal("AL0002", diagnostic.Code)
    Assert.Equal(1, diagnostic.Range.Start.Line)

[<Fact>]
let ``多余的右括号`` () =
    Assert.Equal<string list>([ "AL0001" ], codes "(princ))")

[<Fact>]
let ``引用的点对是合法数据`` () =
    // entmake 的组码表就是这么写的：'(0 . "LINE")
    let result = check "(entmake (list '(0 . \"LINE\") (cons 10 '(0.0 0.0 0.0))))"
    Assert.DoesNotContain(result.Diagnostics, fun d -> d.Code = "AL0005")

[<Fact>]
let ``求值位置写点对报错`` () =
    let result = check "(foo 1 . 2)"
    let diagnostic = result.Diagnostics |> List.find(fun d -> d.Code = "AL0005")
    Assert.Contains("引用的数据", diagnostic.Message)

[<Fact>]
let ``cond 子句用 T 当测试不误报`` () =
    let text = "(defun f (x) (cond ((= x 1) 1) (T 2)))"
    Assert.Empty((check text).Diagnostics)

[<Fact>]
let ``setq 变量和值必须成对`` () =
    Assert.Contains("AL0014", codes "(defun f () (setq a 1 b))")

[<Fact>]
let ``if 参数个数不对`` () =
    let result = check "(defun f (x) (if x))"
    let diagnostic = result.Diagnostics |> List.find(fun d -> d.Code = "AL0010")
    Assert.Contains("if", diagnostic.Message)

[<Fact>]
let ``参数重复定义`` () =
    Assert.Contains("AL0012", codes "(defun f (x x) x)")

[<Fact>]
let ``不支持 &optional`` () =
    Assert.Contains("AL0020", codes "(defun f (&optional x) x)")

[<Fact>]
let ``内置函数参数个数`` () =
    Assert.Contains("AL0010", codes "(defun f (x) (car x 1))")

[<Fact>]
let ``引用展开成 quote`` () =
    let result = FSharp.AutoLisp.AutoLisp.parse "'(1 2)"
    match result.Expressions with
    | [ SList([ SSymbol("quote", _); SList([ SInt(1L, _); SInt(2L, _) ], _) ], _) ] -> ()
    | _ -> failwith "没有展开成 (quote (1 2))"

[<Fact>]
let ``字符串转义解码`` () =
    let result = FSharp.AutoLisp.AutoLisp.parse "(princ \"a\\tb\")"
    match result.Expressions with
    | [ SList([ _; SString(value, _) ], _) ] -> Assert.Equal("a\tb", value)
    | _ -> failwith "没有读出字符串"

[<Fact>]
let ``块注释与行注释`` () =
    let result = FSharp.AutoLisp.AutoLisp.parse ";| 块注释 |; (princ) ; 行注释\r\n"
    Assert.Single(result.Expressions) |> ignore
    Assert.Empty(result.Diagnostics)

[<Fact>]
let ``整数超出 32 位范围`` () =
    Assert.Contains("AL0007", codes "(setq x 3000000000)")

[<Fact>]
let ``中文全角括号被提示`` () =
    Assert.Contains("AL0023", codes "(defun f () （princ))")

[<Fact>]
let ``特殊形式不区分大小写`` () =
    let result = check "(DEFUN F (X) (IF X X))"
    Assert.Empty(result.Diagnostics)

[<Fact>]
let ``参数表必须是列表`` () =
    Assert.Contains("AL0013", codes "(defun f x x)")

[<Fact>]
let ``行号正确`` () =
    let result = check "(princ)\n\n(defun f () (setq a 1 b))"
    let diagnostic = result.Diagnostics |> List.find(fun d -> d.Code = "AL0014")
    Assert.Equal(3, diagnostic.Range.Start.Line)

[<Fact>]
let ``数字与符号的边界`` () =
    // 1+ 是符号，- 是符号，1.5e-3 是实数
    let result = FSharp.AutoLisp.AutoLisp.parse "(1+ 1.5e-3 - .5 1.)"
    match result.Expressions with
    | [ SList([ SSymbol("1+", _); SReal(a, _); SSymbol("-", _); SReal(b, _); SReal(c, _) ], _) ] ->
        Assert.Equal(0.0015, a, 12)
        Assert.Equal(0.5, b, 12)
        Assert.Equal(1.0, c, 12)
    | _ -> failwith "词法分类不符合预期"
    Assert.Empty(result.Diagnostics)

[<Fact>]
let ``未结束的字符串报语法错误`` () =
    // 引号不配对时括号仍然是平衡的，错误应落在那个双引号上
    let result = check "(princ \"abc)"
    let diagnostic = Assert.Single(result.Diagnostics)
    Assert.Equal("AL0024", diagnostic.Code)
    Assert.Equal(1, diagnostic.Range.Start.Line)
    Assert.Equal(8, diagnostic.Range.Start.Column)
