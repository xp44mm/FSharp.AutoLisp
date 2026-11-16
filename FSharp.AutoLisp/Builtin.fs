namespace FSharp.AutoLisp

open System

/// AutoLISP 常用函数的参数个数。
/// 只收录参数个数没有歧义的函数；拿不准的宁可不收，避免误报。
/// (最小, 最大)，最大为 Int32.MaxValue 表示不限。
module Builtin =

    let private table =
        [
            // 表操作
            "car", (1, 1); "cdr", (1, 1)
            "cons", (2, 2); "list", (0, Int32.MaxValue)
            "append", (0, Int32.MaxValue); "nth", (2, 2)
            "member", (2, 2); "assoc", (2, 2); "subst", (3, 3)
            "length", (1, 1); "last", (1, 1); "reverse", (1, 1)
            "apply", (2, 2); "mapcar", (2, Int32.MaxValue)
            "atom", (1, 1); "listp", (1, 1)
            // 判断与类型
            "null", (1, 1); "not", (1, 1); "numberp", (1, 1)
            "boundp", (1, 1); "type", (1, 1); "eq", (2, 2); "equal", (2, 3)
            "zerop", (1, 1); "minusp", (1, 1)
            // 数值
            "+", (0, Int32.MaxValue); "-", (1, Int32.MaxValue)
            "*", (0, Int32.MaxValue); "/", (1, Int32.MaxValue)
            "1+", (1, 1); "1-", (1, 1); "rem", (2, 2); "gcd", (2, 2)
            "abs", (1, 1); "sqrt", (1, 1); "expt", (2, 2); "exp", (1, 1)
            "log", (1, 1); "sin", (1, 1); "cos", (1, 1); "atan", (1, 2)
            "fix", (1, 1); "float", (1, 1); "max", (1, Int32.MaxValue)
            "min", (1, Int32.MaxValue)
            "=", (1, Int32.MaxValue); "/=", (1, Int32.MaxValue)
            "<", (1, Int32.MaxValue); ">", (1, Int32.MaxValue)
            "<=", (1, Int32.MaxValue); ">=", (1, Int32.MaxValue)
            // 字符串与转换
            "itoa", (1, 1); "atoi", (1, 1); "atof", (1, 1)
            "strlen", (1, 1); "strcat", (0, Int32.MaxValue)
            "substr", (2, 3); "strcase", (1, 2)
            "rtos", (1, 3); "angtos", (1, 3); "distof", (1, 2); "angtof", (1, 2)
            "chr", (1, 1); "ascii", (1, 1); "princ", (0, 2); "prin1", (0, 2)
            "print", (0, 2); "prompt", (1, 1); "terpri", (0, 1)
            // 几何
            "polar", (3, 3); "angle", (2, 2); "distance", (2, 2)
            "trans", (3, 4); "osnap", (2, 2); "textbox", (1, 2)
            // 用户交互
            "getpoint", (0, 2); "getcorner", (1, 2); "getdist", (0, 2)
            "getangle", (0, 2); "getreal", (0, 1); "getint", (0, 1)
            "getstring", (0, 1); "getkword", (0, 1); "initget", (1, 2)
            // 实体与符号表
            "entget", (1, 2); "entmod", (1, 1); "entdel", (1, 1)
            "entmake", (1, 1); "entmakex", (1, 1); "entupd", (1, 1)
            "entnext", (0, 1); "entlast", (0, 0); "handent", (1, 1)
            "entsel", (0, 1); "ssget", (0, 3); "ssname", (2, 2)
            "ssadd", (0, 2); "ssdel", (2, 2); "sslength", (1, 1)
            "tblsearch", (2, 3); "tblnext", (2, 2); "tblobjname", (2, 2)
            "namedobjdict", (0, 0)
            "dictsearch", (2, 2); "dictadd", (3, 3)
            "dictremove", (2, 2); "dictnext", (2, 2)
            // 变量与文件
            "getvar", (1, 1); "setvar", (2, 2)
            "open", (2, 2); "close", (1, 1); "read-line", (0, 1)
            "read-char", (0, 1); "write-char", (1, 2)
            "load", (1, 2); "arxload", (1, 2); "findfile", (1, 1)
            "getfiled", (3, 4)
            // Visual LISP 常用
            "vl-load-com", (0, 0)
            "vl-position", (2, 2); "vl-remove", (2, 2)
            "vl-remove-if", (2, 2); "vl-remove-if-not", (2, 2)
            "vl-sort", (2, 2); "vl-some", (2, 2); "vl-every", (2, 2)
            "vl-member-if", (2, 2); "vl-member-if-not", (2, 2)
            "vl-list-length", (1, 1); "vl-consp", (1, 1)
            "vl-string-length", (1, 1); "vl-string-search", (2, 3)
            "vl-string-subst", (3, 4); "vl-string-translate", (3, 3)
            "vl-string-trim", (2, 2); "vl-string-left-trim", (2, 2)
            "vl-string-right-trim", (2, 2)
            "vl-princ-to-string", (1, 1); "vl-prin1-to-string", (1, 1)
            // acad 允许省掉参数表：(vl-catch-all-apply 'f) 也跑
            "vl-catch-all-apply", (1, 2)
            "vl-catch-all-error-p", (1, 1); "vl-catch-all-error-message", (1, 1)
            "vl-file-delete", (1, 1); "vl-file-size", (1, 1)
            "vl-file-rename", (2, 2); "vl-file-copy", (2, 3)
            "vl-filename-base", (1, 1); "vl-filename-directory", (1, 1)
            "vl-filename-extension", (1, 1)
            // 命令与对话框
            "command", (0, Int32.MaxValue); "command-s", (0, Int32.MaxValue)
            "vl-cmdf", (0, Int32.MaxValue)
            "start_dialog", (0, 0); "done_dialog", (0, 1)
            "new_dialog", (2, 3); "action_tile", (2, 2)
            "set_tile", (2, 2); "get_tile", (1, 1); "mode_tile", (2, 2)
            "load_dialog", (1, 1); "unload_dialog", (1, 1)
        ]
        |> Map.ofList

    /// car/cdr 家族：c[ad]{1,4}r 都只接受一个参数
    let private carCdrPattern = System.Text.RegularExpressions.Regex(@"^c[ad]{1,4}r$")

    let tryArity (name: string) =
        if carCdrPattern.IsMatch name then
            Some(1, 1)
        else
            Map.tryFind name table
