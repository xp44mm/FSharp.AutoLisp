module FSharp.AutoLisp.AutoLispLexerTailer

open FSharp.LexYacc

/// 按词素推进位置。'\r\n'、'\r'、'\n' 都算一次换行。
let private advance (position: SourcePos) (chars: char list) =
    let mutable line = position.Line
    let mutable column = position.Column
    let mutable offset = position.Offset
    let mutable afterCr = false

    for c in chars do
        if c = '\n' && afterCr then
            // CRLF 的第二个字符，只推进偏移
            afterCr <- false
            offset <- offset + 1
        elif c = '\r' then
            afterCr <- true
            offset <- offset + 1
            line <- line + 1
            column <- 1
        elif c = '\n' then
            afterCr <- false
            offset <- offset + 1
            line <- line + 1
            column <- 1
        else
            afterCr <- false
            offset <- offset + 1
            column <- column + 1

    { Line = line; Column = column; Offset = offset }

/// 把字符流切成带位置的词法单元。
/// 与 RfcJson 的 Tailer 相同：由生成的 DFA 负责识别，这里补上行列号。
let tokenize (iter: LexicalIterator<char * int>) =
    let actions = AutoLispLexer.actions
    let tryNext = AutoLispLexer.anal.getIterator iter

    let rec loop (position: SourcePos) =
        seq {
            match tryNext() with
            | None -> ()
            | Some(ruleId, buff) ->
                let start = position
                let finish = advance start buff
                let range = { Start = start; End = finish }
                for token in actions.[ruleId] buff do
                    yield { Token = token; Range = range }
                yield! loop finish
        }

    loop SourcePos.zero |> List.ofSeq
