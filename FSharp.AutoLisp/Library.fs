namespace FSharp.AutoLisp

open System.IO
open System.Text

/// 对外入口。编辑器/插件只需要调用 AutoLisp.check 或 AutoLisp.checkFile，
/// 拿到带 行:列 的诊断列表，就能在把代码送进 AutoCAD 之前提示语法错误。
module AutoLisp =

    /// 读文件：优先按 BOM 判断编码；不是 UTF-8 时按单字节映射解码，
    /// 保证括号、引号等结构字符不会因为编码问题被误判。
    let readText (path: string) =
        let bytes = File.ReadAllBytes path
        if bytes.Length >= 3 && bytes.[0] = 0xEFuy && bytes.[1] = 0xBBuy && bytes.[2] = 0xBFuy then
            UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3)
        elif bytes.Length >= 2 && bytes.[0] = 0xFFuy && bytes.[1] = 0xFEuy then
            Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2)
        elif bytes.Length >= 2 && bytes.[0] = 0xFEuy && bytes.[1] = 0xFFuy then
            Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2)
        else
            let strict = UTF8Encoding(false, true)
            try
                strict.GetString bytes
            with :? DecoderFallbackException ->
                Encoding.GetEncoding("iso-8859-1").GetString bytes

    /// 只做词法与语法分析
    let parse (text: string) = AutoLispCompiler.parseOnly text

    /// 词法 + 语法 + AutoLISP 特殊形式检查
    let check (options: Validator.CheckOptions) (text: string) =
        AutoLispCompiler.check options text

    let checkWithDefaults (text: string) =
        check Validator.CheckOptions.defaultOptions text

    let checkFile (options: Validator.CheckOptions) (path: string) =
        readText path |> check options

    let checkFileWithDefaults (path: string) =
        checkFile Validator.CheckOptions.defaultOptions path
