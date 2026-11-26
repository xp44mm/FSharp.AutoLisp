# FSharp.AutoLisp

AutoLISP 静态检查器：在代码送进 AutoCAD 之前发现语法与常见语义错误。

核心库用 F# 编写，词法分析器与 LALR 语法分析器由 [FSharp.LexYacc](https://www.nuget.org/packages/FSharp.LexYacc) 从 `.fslex` / `.fsyacc` 文法生成。提供命令行工具和库 API，便于编辑器插件接入。只做静态检查，不执行代码，不需要 AutoCAD。

## 功能

- 词法/语法：括号配对、S 表达式、`quote` 展开、`;` 行注释与 `;| |;` 块注释、字符串转义、整数字面量范围、全角字符提示
- 语义：`defun` / `if` / `cond` / `setq` / `foreach` / `lambda` 等特殊形式的形状、参数表、内置函数参数个数、非法调用头
- 诊断：`文件:行:列` 定位、稳定编号 `ALxxxx`、源码片段指示、JSON 输出、退出码
- 编码：按 BOM 判断编码，非 UTF-8 时回退单字节解码，结构字符不会因编码问题被误判
- 误报控制：可单独关闭内置函数参数检查

## 项目结构

| 路径 | 用途 |
| --- | --- |
| `FSharp.AutoLisp/` | 核心库（netstandard2.0），对外入口为 `Library.fs` 的 `AutoLisp` 模块 |
| `FSharp.AutoLisp.Generator/` | `.fslex` / `.fsyacc` 文法、分析表生成与一致性验证 |
| `FSharp.AutoLisp.Test/` | 运行时 xUnit 测试（21 条） |
| `FSharp.AutoLisp.Check/` | 命令行程序 `autolisp-check` |
| `samples/` | 语法错误与语义错误示例 |

## 环境要求

.NET SDK 10.0 或更高版本（本机实测 11.0.100）。库目标框架 `netstandard2.0`，命令行与测试项目目标框架 `net10.0`。

## 构建与测试

```bash
dotnet build FSharp.AutoLisp.slnx
dotnet run --project FSharp.AutoLisp.Test --no-launch-profile
dotnet run --project FSharp.AutoLisp.Generator --no-launch-profile
```

测试项目使用 xunit.v3 + Microsoft.Testing.Platform：用 `dotnet run` 执行。在 .NET 10+ SDK 下 `dotnet test` 的旧 VSTest 目标已不受支持。

## 命令行

```bash
dotnet run --project FSharp.AutoLisp.Check -- --help
dotnet run --project FSharp.AutoLisp.Check -- samples
dotnet run --project FSharp.AutoLisp.Check -- --json path/to/file.lsp
```

发布后等价用法：`autolisp-check [选项] <文件或目录>...`。目录参数递归收集 `*.lsp` 与 `*.mnl`。

| 选项 | 说明 |
| --- | --- |
| `--json` | 每个文件输出一行 JSON，便于编辑器插件解析 |
| `--strict` | 有警告也返回非零退出码 |
| `--no-builtin` | 不检查内置函数的参数个数 |
| `--quiet` | 只输出错误 |
| `-h`, `--help` | 显示帮助 |

退出码：`0` 通过；`1` 有错误（`--strict` 时警告也算）；`2` 参数或文件错误。

人类可读的诊断写到 stderr，JSON 写到 stdout。示例输出：

```text
samples/bad-brackets.lsp:2:1: error AL0002: 这个左括号没有闭合，整个文件还缺 1 个 ')'
    (defun c:bad ()
    ^
samples/bad-semantic.lsp:2:17: error AL0012: 参数 "x" 重复定义
    (defun c:bad (x x)
                    ^
...
2 个文件，6 个错误，0 个警告
```

JSON 字段：`file`、`line`、`column`、`endLine`、`endColumn`、`severity`、`code`、`message`。行列号从 1 开始。

## 库 API

```fsharp
open FSharp.AutoLisp

// 语法 + 语义检查
let result = AutoLisp.checkWithDefaults source

// 自定义检查项：关闭内置函数参数检查
let options = { Validator.CheckOptions.defaultOptions with CheckBuiltinArity = false }
let result2 = AutoLisp.check options source
let fromFile = AutoLisp.checkFileWithDefaults "demo.lsp"

// 只做词法与语法分析，拿到 S 表达式
let parsed = AutoLisp.parse source

for d in result.Diagnostics do
    printfn "%s" (Diagnostic.render "demo.lsp" d)
```

`check` / `checkFile` 返回 `CheckResult`：`Expressions`（顶层 S 表达式）、`Diagnostics`（按位置排序）、`HasErrors`。一条 `Diagnostic` 包含 `Code`、`Severity`、`Message`、`Range`、`Related`。`AutoLisp.readText` 可单独用于读取源码。

## 诊断编号

| 编号 | 含义 |
| --- | --- |
| AL0001 | 多余的右括号 |
| AL0002 | 左括号未闭合（报在最内层左括号处） |
| AL0003 | 词法分析失败 |
| AL0005 | 求值位置写了点对；引用数据中的点对合法 |
| AL0007 | 整数超出 32 位范围（警告）或过长（错误） |
| AL0010 | 参数个数不对（特殊形式或内置函数） |
| AL0011 | 这里应该是符号 |
| AL0012 | 参数重复定义或参数表有多个 `/` |
| AL0013 | 参数表必须是列表 |
| AL0014 | `setq` 变量与值不成对（错误）；参数表以 `/` 结尾（警告） |
| AL0015 | `cond` 子句形状不对 |
| AL0016 | `lambda` 参数表不对 |
| AL0017 | 缺少函数体（警告） |
| AL0018 | `foreach` 没有循环体（警告） |
| AL0020 | AutoLISP 不支持 `&optional` 等写法 |
| AL0021 | `nil` / `t` / 特殊形式不能作为变量名 |
| AL0022 | 调用头不是函数 |
| AL0023 | 代码里出现全角字符（警告；注释与字符串跳过） |
| AL0024 | 语法错误，例如未结束的字符串 |

## 实现结构

```text
.fslex → DFA (AutoLispLexer.fs)
.fsyacc → LALR 表 (AutoLispYacc.fs)
              ↓
AutoLispLexerTailer.fs（token + 行列）→ AutoLispCompiler.fs（括号预检 + LALR 步进）
              ↓
Validator.fs（特殊形式、参数表、内置函数、点数）→ Library.fs（CLI / 编辑器调用入口）
```

要点：

- 文法只描述 S 表达式结构；特殊形式不进文法，放在 AST 之上的 `Validator`。
- 括号不平衡时先在 token 层报错，不再往下分析，避免连带误报。
- 参数表中的符号不当函数调用检查；`cond` 子句逐项按表达式处理。
- 点对只在求值位置报错；进入 `quote` / `function` 后视为数据，`'(0 . "LINE")` 合法。
- 全角字符检查跳过注释与字符串。

### 修改文法后重新生成

1. 编辑 `FSharp.AutoLisp.Generator/autolisp.fslex` 或 `autolisp.fsyacc`。
2. 去掉 `AutoLispLexerTest.fs` / `AutoLispYaccTest.fs` 中两个 `生成` Fact 上的 `Skip`。
3. 运行 `dotnet run --project FSharp.AutoLisp.Generator --no-launch-profile`，生成结果写回 `FSharp.AutoLisp/AutoLispLexer.fs` 与 `AutoLispYacc.fs`。
4. 恢复 `Skip`，重新构建并跑测试。`验证` Fact 平时会比对文法重新构造的分析表与已生成文件，防止两者不同步。

## 已知边界

- 只做静态检查：不求值、不展开宏、不访问 AutoCAD 对象模型。
- 内置函数参数表只覆盖常用子集；误报可用 `--no-builtin` 或 `CheckBuiltinArity = false` 关闭。
- 词法不处理 `#` 读取宏；数字只分整数与实数。

## 许可

LGPL-3.0-or-later，见 [LICENSE.txt](LICENSE.txt)。
