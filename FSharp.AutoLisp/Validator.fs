namespace FSharp.AutoLisp

open System
open System.Collections.Generic

/// 在语法树之上做 AutoLISP 自己的语义检查：
/// 特殊形式的形状、参数表、setq 成对、cond 子句、内置函数参数个数等。
/// 这些规则是 AutoLISP 独有的，不能照搬其他 LISP 方言。
module Validator =

    type CheckOptions = {
        /// 检查 defun/if/cond/setq 等特殊形式的形状
        CheckSpecialForms: bool
        /// 检查常用内置函数的参数个数
        CheckBuiltinArity: bool
    } with
        static member defaultOptions = {
            CheckSpecialForms = true
            CheckBuiltinArity = true
        }

    let private maxArgs = Int32.MaxValue

    let private lower (text: string) = text.ToLowerInvariant()

    let private arityText (min: int) (max: int) =
        if max = maxArgs then sprintf "至少 %d 个参数" min
        elif min = max then sprintf "正好 %d 个参数" min
        // 数字开头时留一个空格，拼出来是“需要 2 到 3 个参数”
        else sprintf " %d 到 %d 个参数" min max

    /// 收集用户自己定义的函数名，避免把重定义的内置函数当成内置函数检查
    let collectDefined (exprs: SExpr list) =
        let names = ResizeArray<string>()

        let rec walk (expr: SExpr) =
            match expr with
            | SList(items, _) ->
                (match items with
                 | SSymbol(head, _) :: SSymbol(name, _) :: _ when SpecialForm.isDefinition head ->
                     names.Add(lower name)
                 | _ -> ())
                items |> List.iter walk
            | _ -> ()

        exprs |> List.iter walk
        names |> Seq.map lower |> Set.ofSeq

    /// 返回按位置排序的诊断
    let validate (options: CheckOptions) (exprs: SExpr list) : Diagnostic list =
        let diagnostics = ResizeArray<Diagnostic>()
        let defined = collectDefined exprs

        let error code (range: SourceRange) message =
            diagnostics.Add(Diagnostic.error code range message)

        let warning code (range: SourceRange) message =
            diagnostics.Add(Diagnostic.warning code range message)

        let checkArity (name: string) (min: int) (max: int) (range: SourceRange) (count: int) =
            if count < min || count > max then
                error
                    "AL0010"
                    range
                    (sprintf "\"%s\"需要%s，实际给了 %d 个" name (arityText min max) count)

        let checkVarTarget (expr: SExpr) =
            match expr with
            | SSymbol(name, range) ->
                let lname = lower name
                if lname = "nil" || lname = "t" then
                    error "AL0021" range (sprintf "\"%s\" 不能作为变量名" name)
                elif SpecialForm.isSpecial lname then
                    error "AL0021" range (sprintf "\"%s\" 是特殊形式，不能作为变量名" name)
            | _ -> error "AL0011" expr.Range "这里应该是一个符号（变量名）"

        /// 参数表：(a b / c d)
        let checkParams (parameters: SExpr list) (range: SourceRange) =
            let seen = HashSet<string>()
            let mutable slashCount = 0
            let mutable slashIndex = -1

            parameters
            |> List.iteri(fun index parameter ->
                match parameter with
                | SSymbol(name, nameRange) ->
                    let lname = lower name
                    if lname = "/" then
                        slashCount <- slashCount + 1
                        slashIndex <- index
                        if slashCount > 1 then
                            error "AL0012" nameRange "参数表里只能有一个 / 分隔符"
                    elif lname.StartsWith "&" || lname.StartsWith "#!" then
                        error
                            "AL0020"
                            nameRange
                            (sprintf "AutoLISP 不支持 \"%s\"，局部变量要写在 / 后面" name)
                    elif not (seen.Add lname) then
                        error "AL0012" nameRange (sprintf "参数 \"%s\" 重复定义" name)
                | _ -> error "AL0011" parameter.Range "参数表里只能出现符号")

            if slashCount = 1 && slashIndex = parameters.Length - 1 then
                warning "AL0014" range "参数表以 / 结尾，后面没有局部变量"

        let checkSpecial (name: string) (args: SExpr list) (range: SourceRange) =
            match name with
            | "defun"
            | "defun-q"
            | "defmacro" ->
                match args with
                | functionName :: parameters :: body ->
                    (match functionName with
                     | SSymbol(fn, fnRange) ->
                         let lfn = lower fn
                         if lfn = "nil" || lfn = "t" then
                             error "AL0011" fnRange "函数名不能是 nil 或 t"
                     | _ -> error "AL0011" functionName.Range "函数名应该是一个符号")

                    (match parameters with
                     | SList(parameters, parametersRange) -> checkParams parameters parametersRange
                     | _ ->
                         error
                             "AL0013"
                             parameters.Range
                             "函数参数表应该是列表，例如 (defun f (a b) ...)")

                    if body.IsEmpty then
                        warning "AL0017" range (sprintf "\"%s\" 没有函数体" name)
                | _ -> error "AL0010" range (sprintf "\"%s\" 至少需要 函数名 和 参数表" name)

            | "lambda" ->
                match args with
                | parameters :: body ->
                    (match parameters with
                     | SList(parameters, parametersRange) -> checkParams parameters parametersRange
                     | _ ->
                         error "AL0016" parameters.Range "lambda 的参数表应该是列表，例如 (lambda (x) ...)")

                    if body.IsEmpty then
                        warning "AL0017" range "lambda 没有函数体"
                | [] -> error "AL0016" range "lambda 缺少参数表"

            | "setq" ->
                if args.IsEmpty then
                    warning "AL0014" range "\"setq\" 没有变量赋值"
                else
                    if args.Length % 2 <> 0 then
                        error
                            "AL0014"
                            range
                            (sprintf "\"setq\" 的变量和值必须成对出现，实际有 %d 个参数" args.Length)
                    args
                    |> List.indexed
                    |> List.iter(fun (index, argument) ->
                        if index % 2 = 0 then checkVarTarget argument)

            | "if" -> checkArity "if" 2 3 range args.Length
            // AutoLISP 的 while/repeat 允许跟多个表达式组成循环体
            | "while" -> checkArity "while" 2 maxArgs range args.Length
            | "repeat" -> checkArity "repeat" 2 maxArgs range args.Length
            | "prog1" -> if args.IsEmpty then error "AL0010" range "\"prog1\" 至少需要 1 个参数"
            | "prog2" ->
                if args.Length < 2 then
                    error "AL0010" range "\"prog2\" 至少需要 2 个参数"
            | "quote" -> checkArity "quote" 1 1 range args.Length
            | "function" -> checkArity "function" 1 1 range args.Length

            | "foreach" ->
                match args with
                | variable :: _ ->
                    checkVarTarget variable
                    if args.Length < 2 then
                        error "AL0010" range "\"foreach\" 需要 迭代变量 和 列表"
                    elif args.Length = 2 then
                        warning "AL0018" range "\"foreach\" 没有循环体"
                | [] -> error "AL0010" range "\"foreach\" 需要 迭代变量 和 列表"

            | "cond" ->
                args
                |> List.iter(fun clause ->
                    match clause with
                    | SList([], clauseRange) ->
                        error "AL0015" clauseRange "\"cond\" 的子句不能是空表"
                    | SList _ -> ()
                    | _ ->
                        error
                            "AL0015"
                            clause.Range
                            "\"cond\" 的子句应该是列表，例如 ((= a 1) (foo))")

            | "progn"
            | "and"
            | "or" -> ()
            | _ -> ()

        let checkBuiltin (name: string) (args: SExpr list) (range: SourceRange) =
            if options.CheckBuiltinArity && not (defined.Contains name) then
                match Builtin.tryArity name with
                | Some(min, max) -> checkArity name min max range args.Length
                | None -> ()

        /// 点对检查要在引用的数据里也做，所以单独走一遍整棵树
        let rec checkDots (expr: SExpr) =
            match expr with
            | SSymbol(".", range) ->
                error "AL0005" range "AutoLISP 不支持点对（dotted pair），这里不能写 ."
            | SList(items, _) -> items |> List.iter checkDots
            | _ -> ()

        let rec walk (expr: SExpr) =
            match expr with
            | SList([], _) -> ()
            | SList(head :: rest, range) ->
                (match head with
                 | SSymbol(name, _) ->
                     let lname = lower name
                     if options.CheckSpecialForms && SpecialForm.isSpecial lname then
                         checkSpecial lname rest range
                     elif lname = "t" || lname = "nil" then
                         error "AL0022" head.Range (sprintf "\"%s\" 不是函数" name)
                     else
                         checkBuiltin lname rest range
                 | SInt _
                 | SReal _
                 | SString _ -> error "AL0022" head.Range "调用头不是函数"
                 | _ -> ())

                // quote/function 的内容是数据，不做求值层面的检查
                let isQuoting =
                    match head with
                    | SSymbol(name, _) ->
                        let lname = lower name
                        lname = "quote" || lname = "function"
                    | _ -> false

                if not isQuoting then rest |> List.iter walk

            | SSymbol _ -> ()
            | _ -> ()

        exprs |> List.iter checkDots
        exprs |> List.iter walk
        diagnostics |> Seq.sortBy(fun d -> d.Range.Start.Offset) |> List.ofSeq
