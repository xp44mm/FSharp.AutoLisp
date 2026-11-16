namespace FSharp.AutoLisp

/// AutoLISP 的词法单元（由生成的 AutoLispLexer 产生）。
/// 与 JSON 不同，这里没有 # 读取宏，点对也不是合法语法。
type AutoLispToken =
    | LPAREN
    | RPAREN
    /// 引用符号 '，语义上等价于 (quote ...)
    | QUOTE
    /// 整数词素，例如 1024、-3
    | INT of string
    /// 实数词素，例如 1.5、1e3
    | REAL of string
    /// 已经解码的字符串值
    | STRING of string
    | SYMBOL of string
    /// 落单的双引号等无法识别的字符
    | INVALID of string

/// 带上源码位置的词法单元，供语法分析器和诊断使用。
type PositionedToken = {
    Token: AutoLispToken
    Range: SourceRange
}

module AutoLispToken =

    let tag (token: AutoLispToken) =
        match token with
        | LPAREN -> "LPAREN"
        | RPAREN -> "RPAREN"
        | QUOTE -> "QUOTE"
        | INT _ -> "INT"
        | REAL _ -> "REAL"
        | STRING _ -> "STRING"
        | SYMBOL _ -> "SYMBOL"
        | INVALID _ -> "INVALID"

    /// 语法分析器看到的语义值；位置一并装箱，语法动作里才能构造带范围的 AST
    let lexeme (token: PositionedToken) =
        match token.Token with
        | INT text
        | REAL text
        | STRING text
        | SYMBOL text -> box (text, token.Range)
        | LPAREN
        | RPAREN
        | QUOTE -> box token.Range
        | INVALID text -> box (text, token.Range)

    /// 报错时回显
    let display (token: AutoLispToken) =
        match token with
        | LPAREN -> "("
        | RPAREN -> ")"
        | QUOTE -> "'"
        | INT text -> text
        | REAL text -> text
        | STRING text -> "\"" + text + "\""
        | SYMBOL text -> text
        | INVALID text -> text
