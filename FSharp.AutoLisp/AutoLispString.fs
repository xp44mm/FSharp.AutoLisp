namespace FSharp.AutoLisp

open System.Text

/// AutoLISP 的字符串字面量转义，规则和 JSON/C 都不完全一样：
///   \"  \\  \n  \t  \r  \e  以及 \nnn（1~3 位八进制）
/// 其余 \x 按字面字符 x 处理，反斜杠丢掉。
module AutoLispString =

    /// 把带首尾双引号的词素解码成字符串值
    let unquote (chars: char list) =
        // 去掉开头和结尾的双引号
        let body =
            chars
            |> List.skip 1
            |> List.rev
            |> List.skip 1
            |> List.rev

        let buffer = StringBuilder()

        let rec loop (rest: char list) =
            match rest with
            | [] -> ()
            | '\\' :: tail ->
                match tail with
                | [] ->
                    buffer.Append('\\') |> ignore
                    loop []
                | '"' :: tail' ->
                    buffer.Append('"') |> ignore
                    loop tail'
                | '\\' :: tail' ->
                    buffer.Append('\\') |> ignore
                    loop tail'
                | 'n' :: tail' ->
                    buffer.Append('\n') |> ignore
                    loop tail'
                | 't' :: tail' ->
                    buffer.Append('\t') |> ignore
                    loop tail'
                | 'r' :: tail' ->
                    buffer.Append('\r') |> ignore
                    loop tail'
                | 'e' :: tail' ->
                    buffer.Append(char 27) |> ignore
                    loop tail'
                | c :: tail' when c >= '0' && c <= '7' ->
                    // 最多读 3 位八进制
                    let mutable code = int c - int '0'
                    let mutable rest' = tail'
                    let mutable count = 1
                    let mutable go = true
                    while go && count < 3 do
                        match rest' with
                        | d :: t when d >= '0' && d <= '7' ->
                            code <- code * 8 + (int d - int '0')
                            rest' <- t
                            count <- count + 1
                        | _ -> go <- false
                    buffer.Append(char (code &&& 0xFF)) |> ignore
                    loop rest'
                | c :: tail' ->
                    buffer.Append(c) |> ignore
                    loop tail'
            | c :: tail ->
                buffer.Append(c) |> ignore
                loop tail

        loop body
        buffer.ToString()
