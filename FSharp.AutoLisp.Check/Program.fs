module FSharp.AutoLisp.Check.Program

open System
open System.IO
open System.Text

open FSharp.AutoLisp

let private usage = """autolisp-check —— 在把 AutoLISP 代码送进 AutoCAD 之前检查语法

用法：
  autolisp-check [选项] <文件或目录>...

选项：
  --json         以 JSON 输出诊断，便于编辑器插件接入
  --strict       有警告也返回非零退出码
  --no-builtin   不检查内置函数的参数个数
  --quiet        只输出错误
  -h, --help     显示本帮助

退出码：0 通过；1 有错误（--strict 时警告也算）；2 参数或文件错误
"""

let private enumerate (path: string) =
    if Directory.Exists path then
        seq {
            yield! Directory.EnumerateFiles(path, "*.lsp", SearchOption.AllDirectories)
            yield! Directory.EnumerateFiles(path, "*.mnl", SearchOption.AllDirectories)
        }
    elif File.Exists path then
        Seq.singleton path
    else
        Seq.empty

let private jsonEscape (text: string) =
    let buffer = StringBuilder()
    for c in text do
        match c with
        | '"' -> buffer.Append("\\\"") |> ignore
        | '\\' -> buffer.Append("\\\\") |> ignore
        | '\n' -> buffer.Append("\\n") |> ignore
        | '\r' -> buffer.Append("\\r") |> ignore
        | '\t' -> buffer.Append("\\t") |> ignore
        | c when c < ' ' -> buffer.Append(sprintf "\\u%04x" (int c)) |> ignore
        | c -> buffer.Append(c) |> ignore
    buffer.ToString()

/// 在报错行下面画一个 ^^^，只看行列号很难定位
let private printSnippet (source: string) (diagnostic: Diagnostic) =
    let lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
    let line = diagnostic.Range.Start.Line
    if line >= 1 && line <= lines.Length then
        let text = lines.[line - 1].TrimEnd()
        let column = max 1 diagnostic.Range.Start.Column
        let width = max 1 (min (diagnostic.Range.End.Column - diagnostic.Range.Start.Column) (text.Length - column + 1))
        Console.Error.WriteLine(sprintf "    %s" text)
        Console.Error.WriteLine(sprintf "    %s%s" (String(' ', column - 1)) (String('^', width)))

let private printHuman (path: string) (source: string) (quiet: bool) (diagnostics: Diagnostic list) =
    for diagnostic in diagnostics do
        if not quiet || diagnostic.Severity = Severity.Error then
            eprintfn "%s" (Diagnostic.render path diagnostic)
            printSnippet source diagnostic

let private printJson (path: string) (diagnostics: Diagnostic list) =
    let items =
        diagnostics
        |> List.map(fun d ->
            sprintf
                """{"line":%d,"column":%d,"endLine":%d,"endColumn":%d,"severity":"%s","code":"%s","message":"%s"}"""
                d.Range.Start.Line
                d.Range.Start.Column
                d.Range.End.Line
                d.Range.End.Column
                d.Severity.Text
                d.Code
                (jsonEscape d.Message))
    sprintf """{"file":"%s","diagnostics":[%s]}""" (jsonEscape path) (String.Join(",", items))

[<EntryPoint>]
let main argv =
    // 中文诊断在 GBK 控制台下会乱码
    try
        Console.OutputEncoding <- Encoding.UTF8
    with _ ->
        ()

    let mutable json = false
    let mutable strict = false
    let mutable quiet = false
    let mutable builtinArity = true
    let mutable usageError = false
    let paths = ResizeArray<string>()

    for argument in argv do
        match argument with
        | "--json" -> json <- true
        | "--strict" -> strict <- true
        | "--quiet" -> quiet <- true
        | "--no-builtin" -> builtinArity <- false
        | "-h"
        | "--help" ->
            printfn "%s" usage
            exit 0
        | _ when argument.StartsWith "-" ->
            eprintfn "未知选项：%s" argument
            usageError <- true
        | _ -> paths.Add argument

    if usageError || paths.Count = 0 then
        eprintfn "%s" usage
        2
    else
        let options: Validator.CheckOptions = {
            CheckSpecialForms = true
            CheckBuiltinArity = builtinArity
        }

        let files =
            paths
            |> Seq.collect enumerate
            |> Seq.distinct
            |> List.ofSeq

        if files.IsEmpty then
            eprintfn "没有找到 .lsp / .mnl 文件"
            2
        else
            let mutable errors = 0
            let mutable warnings = 0

            for file in files do
                let source = AutoLisp.readText file
                let result = AutoLisp.check options source
                let fileErrors = result.Diagnostics |> List.filter(fun d -> d.Severity = Severity.Error) |> List.length
                let fileWarnings = result.Diagnostics |> List.filter(fun d -> d.Severity = Severity.Warning) |> List.length
                errors <- errors + fileErrors
                warnings <- warnings + fileWarnings

                if json then
                    printfn "%s" (printJson file result.Diagnostics)
                else
                    printHuman file source quiet result.Diagnostics

            if not json then
                eprintfn ""
                eprintfn "%d 个文件，%d 个错误，%d 个警告" files.Length errors warnings

            if errors > 0 || (strict && warnings > 0) then 1 else 0
