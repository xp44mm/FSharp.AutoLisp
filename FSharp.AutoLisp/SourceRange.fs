namespace FSharp.AutoLisp

/// 源文件中的位置：行号、列号从 1 开始，Offset 从 0 开始。
type SourcePos = {
    Line: int
    Column: int
    Offset: int
}

/// 半开区间 [Start, End)，End 指向最后一个字符之后。
type SourceRange = {
    Start: SourcePos
    End: SourcePos
}

module SourcePos =
    let zero = { Line = 1; Column = 1; Offset = 0 }

module SourceRange =
    /// 只覆盖一个位置的空范围
    let point (pos: SourcePos) = { Start = pos; End = pos }

    /// 覆盖两个范围的最小范围
    let merge (a: SourceRange) (b: SourceRange) =
        { Start = if a.Start.Offset <= b.Start.Offset then a.Start else b.Start
          End = if a.End.Offset >= b.End.Offset then a.End else b.End }

    let length (range: SourceRange) = range.End.Offset - range.Start.Offset

    let isEmpty (range: SourceRange) = range.End.Offset <= range.Start.Offset
