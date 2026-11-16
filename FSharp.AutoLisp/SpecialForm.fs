namespace FSharp.AutoLisp

/// AutoLISP 的特殊形式（求值规则特殊，不是普通函数调用）。
/// 这部分不能通用：Common Lisp 的 flet/labels、Scheme 的 define、Clojure 的 def 都不在这里。
module SpecialForm =

    let names =
        set [
            "defun"
            "defun-q"
            "defmacro"
            "lambda"
            "setq"
            "if"
            "progn"
            "prog1"
            "prog2"
            "cond"
            "while"
            "repeat"
            "foreach"
            "quote"
            "function"
            "and"
            "or"
        ]

    let isSpecial (name: string) = names.Contains(name.ToLowerInvariant())

    /// 定义函数的形式，用于收集用户自定义的函数名
    let isDefinition (name: string) =
        match name.ToLowerInvariant() with
        | "defun"
        | "defun-q"
        | "defmacro" -> true
        | _ -> false
