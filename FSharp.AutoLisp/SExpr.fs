namespace FSharp.AutoLisp

open System.Globalization

/// 读出来的 S-表达式。
/// 与词法层一样，只保留 AutoLISP 有的东西：整数、实数、字符串、符号、表。
/// 引用 'x 在读取阶段就展开成 (quote x)。
type SExpr =
    | SInt of value: int64 * range: SourceRange
    | SReal of value: float * range: SourceRange
    | SString of value: string * range: SourceRange
    | SSymbol of name: string * range: SourceRange
    | SList of items: SExpr list * range: SourceRange

    member this.Range =
        match this with
        | SInt(_, range)
        | SReal(_, range)
        | SString(_, range)
        | SSymbol(_, range)
        | SList(_, range) -> range

module SExpr =
    /// AutoLISP 符号不区分大小写，比较时统一取小写
    let lower (name: string) = name.ToLowerInvariant()

    let trySymbol (expr: SExpr) =
        match expr with
        | SSymbol(name, _) -> Some name
        | _ -> None

    let tryItems (expr: SExpr) =
        match expr with
        | SList(items, _) -> Some items
        | _ -> None

    let isNil (expr: SExpr) =
        match expr with
        | SSymbol(name, _) -> lower name = "nil"
        | _ -> false

    /// 还原成可读的源码片段，用于报错信息
    let rec format (expr: SExpr) =
        match expr with
        | SInt(value, _) -> value.ToString(CultureInfo.InvariantCulture)
        | SReal(value, _) -> value.ToString("R", CultureInfo.InvariantCulture)
        | SString(value, _) ->
            "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
        | SSymbol(name, _) -> name
        | SList(items, _) -> "(" + String.concat " " (items |> List.map format) + ")"

    /// 超长片段截断，避免诊断信息刷屏
    let formatShort (expr: SExpr) =
        let text = format expr
        if text.Length <= 48 then text else text.Substring(0, 48) + " ..."
