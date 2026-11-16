namespace FSharp.AutoLisp.Generator

open System.IO
open System.Text

open Xunit
open FSharp.xUnit
open FSharp.Idioms.Literal

open FSharp.LexYacc.Bootstrap
open FSharp.AutoLisp

/// 用 autolisp.fslex 重新构造 DFA，并与 AutoLispLexer.fs 里保存的分析表比对。
/// 生成动作平时是 Skip 的，改了 .fslex 之后去掉 Skip 跑一次即可。
type AutoLispLexerTest(output: ITestOutputHelper) as this =
    let name =
        let x = this.GetType().Name
        x.Substring(0, x.Length - 4)

    let srcPath = Path.Combine(__SOURCE_DIRECTORY__, "autolisp.fslex")

    let text = File.ReadAllText(srcPath, Encoding.UTF8)

    [<Fact(Skip = "没有改变，不必重复生成")>]
    member _.``生成``() =
        let printer = FslexFilePrinter.forChar text
        let src = printer.print()
        output.WriteLine(src)

        let path = Path.Combine(Dir.core, name + ".fs")
        File.WriteAllText(path, src, Encoding.UTF8)
        output.WriteLine($"output:\r\n{path}")

    [<Fact>]
    member _.``验证``() =
        // actual
        let anal = AutoLispLexer.anal
        let actions = AutoLispLexer.actions

        // expect
        let printer = FslexFilePrinter.forChar text
        Should.equal printer.anal anal

        let ruleIds1 = printer.ruler.actions |> List.map fst
        let ruleIds2 = actions.Keys |> List.ofSeq
        Should.equal ruleIds1 ruleIds2
