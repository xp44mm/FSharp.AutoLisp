namespace FSharp.AutoLisp.Generator

open System.IO
open System.Text

open Xunit
open FSharp.xUnit
open FSharp.Idioms.Literal
open FSharp.LexYacc
open FSharp.LexYacc.Bootstrap

open FSharp.AutoLisp

/// 用 autolisp.fsyacc 重新构造 LALR 分析表，并与 AutoLispYacc.fs 比对。
type AutoLispYaccTest(output: ITestOutputHelper) as this =
    let name =
        let x = this.GetType().Name
        x.Substring(0, x.Length - 4)

    let srcPath = Path.Combine(__SOURCE_DIRECTORY__, "autolisp.fsyacc")

    let text = File.ReadAllText(srcPath, Encoding.UTF8)

    [<Fact>]
    member _.``check bnf``() =
        let rawFsyacc: RawFsyaccFile = FsyaccCompiler.compile text
        let flatFsyacc = FlatFsyaccFile.from rawFsyacc

        let analysis = BnfAnalysisRecord.Create(flatFsyacc)

        output.WriteLine("let orphan_bnf = " + stringify analysis.OrphanBnf)
        output.WriteLine("let absence_decl = " + stringify analysis.AbsenceDecl)
        output.WriteLine("let orphan_decl = " + stringify analysis.OrphanDecl)

        let rep =
            rawFsyacc.declarationStmts
            |> DeclarationStatementUtils.duplicated_type_annotations
            |> Set.ofList
            |> Set.toList

        output.WriteLine("let duplicate typedef = " + stringify rep)

    [<Fact>]
    member _.``conflicted productions``() =
        let rawFsyacc = FsyaccCompiler.compile text
        let flatFsyacc = FlatFsyaccFile.from rawFsyacc
        let yacc = flatFsyacc.getYacc()
        let bnf: BNF = yacc.bnf

        let prods = bnf.getConflictedProductions()

        if Set.isEmpty prods then
            output.WriteLine("no conflicted production.")
        else
            output.WriteLine("conflicted productions:")
            prods |> Set.iter(stringify >> output.WriteLine)

        Assert.Empty(prods)

    [<Fact(Skip = "没有改变，不必重复生成")>]
    member _.``生成``() =
        let rawFsyacc = FsyaccCompiler.compile text
        let flatFsyacc = FlatFsyaccFile.from rawFsyacc

        let printer = FsyaccFilePrinter.from flatFsyacc
        let src = printer.printYaccModule(flatFsyacc.header)

        let path = Path.Combine(Dir.core, name + ".fs")
        File.WriteAllText(path, src, Encoding.UTF8)
        output.WriteLine($"output:\r\n{path}")

    [<Fact>]
    member _.``验证``() =
        // actual
        let stateSymbols = AutoLispYacc.stateSymbols
        let actions = AutoLispYacc.actions
        let rules = AutoLispYacc.rules

        // expect
        let rawFsyacc = FsyaccCompiler.compile text
        let flatFsyacc = FlatFsyaccFile.from rawFsyacc
        let yaccFile = FsyaccFilePrinter.from flatFsyacc

        // compare
        Should.equal yaccFile.stateSymbols stateSymbols
        Should.equal yaccFile.actions actions

        let productions1 = yaccFile.rules |> List.map fst
        let productions2 = rules |> List.map fst
        Should.equal productions1 productions2
