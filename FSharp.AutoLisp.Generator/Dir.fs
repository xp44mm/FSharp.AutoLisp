module FSharp.AutoLisp.Generator.Dir

open System.IO

let solutionPath = DirectoryInfo(__SOURCE_DIRECTORY__).Parent.FullName

/// 生成出来的 AutoLispLexer.fs / AutoLispYacc.fs 放在核心项目里
let core = Path.Combine(solutionPath, "FSharp.AutoLisp")
