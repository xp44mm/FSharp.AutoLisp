module FSharp.AutoLisp.AutoLispCompiler

open FSharp.LexYacc

type CheckResult = {
    /// 读出来的顶层 S-表达式
    Expressions: SExpr list
    /// 按位置排序的诊断
    Diagnostics: Diagnostic list
} with
    member this.HasErrors =
        this.Diagnostics |> List.exists(fun d -> d.Severity = Severity.Error)

/// 语法分析的两种结局。这里不用 Result，避免和 Severity.Error 撞名。
type private ParseOutcome =
    | ParseOk of SExpr list
    | ParseFailed of Diagnostic

type private TokenizeOutcome =
    | TokenizeOk of PositionedToken list
    | TokenizeFailed of string

let private tokenRow (token: PositionedToken) = {
    token = token
    tag = AutoLispToken.tag token.Token
    lexeme = AutoLispToken.lexeme token
}

/// 括号配对检查。
/// 括号不平衡时语法分析器只报第一个错，而且往往离真正的问题很远；
/// 先在词法单元层面查出未闭合/多余的括号。
let private checkBrackets (tokens: PositionedToken list) =
    let diagnostics = ResizeArray<Diagnostic>()
    let opens = ResizeArray<PositionedToken>()

    for token in tokens do
        match token.Token with
        | LPAREN -> opens.Add token
        | RPAREN ->
            if opens.Count = 0 then
                diagnostics.Add(
                    Diagnostic.error "AL0001" token.Range "多余的右括号 ')'，没有与之匹配的左括号"
                )
            else
                opens.RemoveAt(opens.Count - 1)
        | _ -> ()

    if opens.Count > 0 then
        let innermost = opens.[opens.Count - 1]
        diagnostics.Add(
            Diagnostic.error
                "AL0002"
                innermost.Range
                (sprintf "这个左括号没有闭合，整个文件还缺 %d 个 ')'" opens.Count)
        )

    List.ofSeq diagnostics

/// 整数字面量超出 AutoLISP 的 32 位范围时给出提示
let private checkIntegers (tokens: PositionedToken list) =
    tokens
    |> List.choose(fun token ->
        match token.Token with
        | INT text ->
            match System.Int64.TryParse text with
            | true, value when not (AutoLispNumber.isInt32Range value) ->
                Some(
                    Diagnostic.warning
                        "AL0007"
                        token.Range
                        (sprintf "整数 %s 超出 AutoLISP 的 32 位整数范围 -2147483648 到 2147483647" text)
                )
            | true, _ -> None
            | _ -> Some(Diagnostic.error "AL0007" token.Range (sprintf "整数 %s 太长" text))
        | _ -> None)

let private describeUnexpected
    (eof: SourcePos)
    (tag: string)
    (row: TokenRow<PositionedToken> option)
    =
    let range, message =
        match row with
        | None -> SourceRange.point eof, "代码在这里意外结束：可能缺少右括号或表达式不完整"
        | Some row ->
            match tag with
            | "" -> row.token.Range, "代码在这里意外结束：可能缺少右括号或表达式不完整"
            | "RPAREN" -> row.token.Range, "多余的右括号 ')'"
            | "INVALID" -> row.token.Range, "无法识别的字符，或者字符串缺少结束的双引号"
            | _ -> row.token.Range, (sprintf "这里不该出现 %s" (AutoLispToken.display row.token.Token))

    Diagnostic.error "AL0024" range message

/// 用生成的 LALR 分析表逐步推进，出错时能拿到当前词法单元的位置
let private parseTokens (eof: SourcePos) (tokens: PositionedToken list) =
    let rows = tokens |> List.map tokenRow

    let rec loop (states: YaccState list) (rest: TokenRow<PositionedToken> list) =
        // 注意：FSharp.LexYacc 里另有 NextState 定义了同名分支，这里必须限定到 NextParseAction
        match AutoLispYacc.parser.nextAction states (List.tryHead rest) with
        | NextParseAction.Accepted -> ParseOk(unbox<SExpr list> (List.head states).lexeme)
        | NextParseAction.Reduced nextStates -> loop nextStates rest
        | NextParseAction.Shifted nextStates ->
            match rest with
            | _ :: tail -> loop nextStates tail
            | [] -> ParseFailed(describeUnexpected eof "" None)
        | NextParseAction.NoSymbol tag
        | NextParseAction.Rejected tag -> ParseFailed(describeUnexpected eof tag (List.tryHead rest))
        | NextParseAction.NoSource _ -> ParseFailed(describeUnexpected eof "" None)

    loop [ YaccState.just(0, null) ] rows

let private tokenize (input: string) =
    try
        TokenizeOk(LexicalIterator.forChar input |> AutoLispLexerTailer.tokenize)
    with ex ->
        TokenizeFailed ex.Message

/// 中文全角字符在 AutoLISP 里只是普通符号字符，报错信息很难看懂，
/// 所以单独扫一遍源码。放在词法层面，括号不平衡时也能提示。
let private checkFullWidth (input: string) =
    let suspicious = set [ '“'; '”'; '‘'; '’'; '（'; '）'; '；'; '，'; '：'; '＇'; '　' ]
    let diagnostics = ResizeArray<Diagnostic>()
    let mutable line = 1
    let mutable column = 1
    let mutable i = 0

    while i < input.Length do
        let c = input.[i]
        if suspicious.Contains c then
            diagnostics.Add(
                Diagnostic.warning
                    "AL0023"
                    (SourceRange.point { Line = line; Column = column; Offset = i })
                    (sprintf "出现了全角字符「%c」，AutoLISP 只认半角字符" c)
            )

        if c = '\r' && i + 1 < input.Length && input.[i + 1] = '\n' then
            line <- line + 1
            column <- 1
            i <- i + 2
        elif c = '\r' || c = '\n' then
            line <- line + 1
            column <- 1
            i <- i + 1
        else
            column <- column + 1
            i <- i + 1

    List.ofSeq diagnostics

/// 只做词法与语法分析，不做特殊形式检查
let parseOnly (input: string) : CheckResult =
    let lexical = checkFullWidth input

    match tokenize input with
    | TokenizeFailed message ->
        { Expressions = []
          Diagnostics =
            Diagnostic.sort (
                lexical
                @ [ Diagnostic.error "AL0003" (SourceRange.point SourcePos.zero) ("词法分析失败：" + message) ]
            ) }
    | TokenizeOk [] -> { Expressions = []; Diagnostics = Diagnostic.sort lexical }
    | TokenizeOk tokens ->
        let eof = tokens.[tokens.Length - 1].Range.End
        let bracketDiagnostics = checkBrackets tokens

        if not bracketDiagnostics.IsEmpty then
            // 括号不平衡时不再往下分析，避免连带的一堆错误
            { Expressions = []
              Diagnostics = Diagnostic.sort (lexical @ bracketDiagnostics) }
        else
            match parseTokens eof tokens with
            | ParseFailed diagnostic ->
                { Expressions = []
                  Diagnostics = Diagnostic.sort (lexical @ checkIntegers tokens @ [ diagnostic ]) }
            | ParseOk expressions ->
                { Expressions = expressions
                  Diagnostics = Diagnostic.sort (lexical @ checkIntegers tokens) }

/// 语法分析 + AutoLISP 语义检查
let check (options: Validator.CheckOptions) (input: string) : CheckResult =
    let parsed = parseOnly input

    let semantic =
        if parsed.Expressions.IsEmpty then []
        else Validator.validate options parsed.Expressions

    { parsed with
        Diagnostics = Diagnostic.sort (parsed.Diagnostics @ semantic) }
