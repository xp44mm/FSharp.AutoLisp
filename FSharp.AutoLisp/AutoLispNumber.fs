namespace FSharp.AutoLisp

open System
open System.Globalization

/// 数字词素的转换。词素本身由 fslex 的正则识别，这里只负责取值。
module AutoLispNumber =

    let toInt64 (text: string) =
        match Int64.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) with
        | true, value -> value
        | _ -> 0L

    let toFloat (text: string) =
        match Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture) with
        | true, value -> value
        | _ -> 0.0

    /// AutoLISP 的整数是 32 位
    let maxInt32 = 2147483647L
    let minInt32 = -2147483648L

    let isInt32Range (value: int64) =
        value <= maxInt32 && value >= minInt32
