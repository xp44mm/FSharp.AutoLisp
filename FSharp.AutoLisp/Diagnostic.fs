namespace FSharp.AutoLisp

type Severity =
    | Info
    | Warning
    | Error

    member this.Text =
        match this with
        | Info -> "info"
        | Warning -> "warning"
        | Error -> "error"

/// 一条语法/语义诊断。
type Diagnostic = {
    /// 诊断编号，便于在编辑器里做快速修复，例如 AL0002。
    Code: string
    Severity: Severity
    Message: string
    /// 出问题的代码范围
    Range: SourceRange
    /// 关联位置，例如没有闭合的左括号所在处。
    Related: SourceRange option
}

module Diagnostic =
    let private make severity code range message = {
        Code = code
        Severity = severity
        Message = message
        Range = range
        Related = None
    }

    let error code range message = make Error code range message
    let warning code range message = make Warning code range message
    let info code range message = make Info code range message

    let withRelated (related: SourceRange) (d: Diagnostic) =
        { d with Related = Some related }

    /// 按出现位置排序（同一位置保持产生顺序）
    let sort (diagnostics: Diagnostic list) =
        diagnostics
        |> List.sortBy(fun d -> d.Range.Start.Offset)

    /// path:line:col: error AL0002: 缺少右括号
    let render (path: string) (d: Diagnostic) =
        let pos = d.Range.Start
        sprintf "%s:%d:%d: %s %s: %s" path pos.Line pos.Column d.Severity.Text d.Code d.Message
