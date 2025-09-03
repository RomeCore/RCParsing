# RCParsing

[![NuGet](https://img.shields.io/nuget/v/RCParsing.svg?style=flat&label=NuGet:%20RCParsing)](https://www.nuget.org/packages/RCParsing/)
[![License](https://img.shields.io/github/license/RomeCore/RCParsing.svg)](https://github.com/RomeCore/RCParsing/blob/main/LICENSE)
[![Star me on GitHub](https://img.shields.io/github/stars/RomeCore/RCParsing.svg?style=social&label=Star%20Me)](https://github.com/RomeCore/RCParsing)

**A Fluent, Lexerless Parser Builder for .NET — Define ANY grammars with the elegance of BNF and the power of C#.**

This library focuses on **Delevoper-experience (DX)** first, providing best toolkit for creating your programming languages or file formats with declarative API, debugging tools, and more. This allows you to design your parser directly in code and easily fix it using *rule stack traces* and detailed error messages.

It provides a Fluent API to fully construct your own parser from grammar to usable object creation. Since this parsing library is lexerless, you don't need to prioritize your tokens and you can mix code with text that contains keywords, just using this library!

It has a brand new feature for parser combinators, the **barrier tokens** that cannot be missed and **must** be parsed, this allows to parse indent-sensitive languages like Python.

## Why RCParsing?

- 🐍 **Hybrid**: Unique support for **barrier tokens** to parse indent-sensitive languages like Python and YAML flawlessly.
- 🚫 **Lexerless**: No token priority headaches. Parse directly from raw text, even with keywords embedded in sources using tokens just as primitives.
- 🎯 **Fluent API**: Write parsers in C# that read like clean BNF grammars, boosting readability and maintainability without that imperative functional approach.
- 🐛 **Debug-Friendly**: Get detailed, actionable error messages with stack traces and precise source locations.
- ⚡ **Fast**: Performance is now on par with the fastest .NET parsing libraries (see benchmarks below).
- 🌳 **Rich AST**: Parser makes an AST (Abstract Syntax Tree) from raw text, with ability to optimize, fully analyze and calculate the result value entirely lazy, reducing unneccesary allocations.
- 🔧 **Configurable Skipping**: Advanced strategies for whitespace and comments, allowing you to use conflicting tokens in your main rules.
- 📦 **Batteries Included**: Useful built-in tokens and rules (regex, identifiers, numbers, escaped strings, separated lists, custom tokens, and more...).
- 🖥️ **Broad Compatibility**: Targets `.NET Standard 2.0` (runs on `.NET Framework 4.6.1+`), `.NET 6.0`, and `.NET 8.0`.

# Table of contents

- [Installation](#installation)
- [Tutorials, docs and examples](#tutorials-docs-and-examples)
- [Simple examples](#simple-examples) - The examples that you can copy, paste and run!
	- [A + B](#a--b) - Basic arithmetic expression parser.
	- [JSON](#json) - A complete JSON parser with comments and skipping.
	- [Python-like](#python-like) - Demonstrating barrier tokens for indentation.
- [Comparison with other parsing libraries](#comparison-with-other-parsing-libraries)
- [Benchmarks](#benchmarks)
	- [JSON](#json-1)
- [Projects using RCParsing](#projects-using-rcparsing)
- [Roadmap](#roadmap)
- [Contributing](#contributing)

# Installation

You can install the package via NuGet Package Manager or console window, using one of these commands:

```shell
dotnet add package RCParsing
```
```powershell
Install-Package RCParsing
```

Or do it manually by cloning this repository.

# Tutorials, docs and examples

- [Tutorials](./docs/tutorials.md) - detailed tutorials, explaining features and mechanics of this library, highly recommended to read!

# Simple examples

### A + B

Here is simple example how to make simple parser that parses "a + b" string with numbers and transforms the result:

```csharp
using RCParsing;
using RCParsing.Building;

// First, you need to create a builder
var builder = new ParserBuilder();

// Enable and configure the auto-skip (you can replace `Whitespaces` with any parser rule)
builder.Settings
    .Skip(b => b.Whitespaces().ConfigureForSkip());

// Create the number token that transforms to double
builder.CreateToken("number")
	.Number<double>();

// Create a main sequential expression rule
builder.CreateMainRule("expression")
    .Token("number")
    .LiteralChoice("+", "-")
    .Token("number")
    .Transform(v => {
        var value1 = v.GetValue<double>(0);
        var op = v.Children[1].Text;
        var value2 = v.GetValue<double>(2);
        return op == "+" ? value1 + value2 : value1 - value2;
    });

// Build the parser
var parser = builder.Build();

// Parse a string using 'expression' rule and get the raw AST (value will be calculated lazily)
var parsedRule = parser.Parse("10 + 15");

// We can now get the value from our 'Transform' functions (value calculates now)
var transformedValue = parsedRule.GetValue<double>();
Console.WriteLine(transformedValue); // 25
```

### JSON

And here is JSON example:

```csharp
using RCParsing;
using RCParsing.Building;

var builder = new ParserBuilder();

// Configure whitespace and comment skip-rule
builder.Settings
	.Skip(r => r.Rule("skip"), ParserSkippingStrategy.SkipBeforeParsingGreedy);

builder.CreateRule("skip")
	.Choice(
		b => b.Whitespaces(),
		b => b.Literal("//").TextUntil('\n', '\r'))
	.ConfigureForSkip(); // Prevents from error recording

builder.CreateToken("string")
	.Literal("\"")
	.EscapedTextPrefix(prefix: '\\', '\\', '\"') // This sub-token automaticaly escapes the source string and puts it into intermediate value
	.Literal("\"")
	.Pass(index: 1); // Pass the EscapedTextPrefix's intermediate value up (it will be used as token's result value)

builder.CreateToken("number")
	.Number<double>();

builder.CreateToken("boolean")
	.LiteralChoice(["true", "false"], v => v.Text == "true");

builder.CreateToken("null")
	.Literal("null", _ => null);

builder.CreateRule("value")
	.Choice(
		c => c.Token("string"),
		c => c.Token("number"),
		c => c.Token("boolean"),
		c => c.Token("null"),
		c => c.Rule("array"),
		c => c.Rule("object")
	); // Choice rule propogates child's value by default

builder.CreateRule("array")
	.Literal("[")
	.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","),
		allowTrailingSeparator: true, includeSeparatorsInResult: false,
		factory: v => v.SelectArray())
	.Literal("]")
	.TransformSelect(1); // Selects the Children[1]'s value

builder.CreateRule("object")
	.Literal("{")
	.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","),
		allowTrailingSeparator: true, includeSeparatorsInResult: false,
		factory: v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary(k => k.Key, v => v.Value))
	.Literal("}")
	.TransformSelect(1);

builder.CreateRule("pair")
	.Token("string")
	.Literal(":")
	.Rule("value")
	.Transform(v => KeyValuePair.Create(v.GetValue<string>(0), v.GetValue(2)));

builder.CreateMainRule("content")
	.Rule("value")
	.EOF() // Sure that we captured all the input
	.TransformSelect(0);

var jsonParser = builder.Build();

var json =
"""
{
	"id": 1,
	"name": "Sample Data",
	"created": "2023-01-01T00:00:00", // This is a comment
	"tags": ["tag1", "tag2", "tag3"],
	"isActive": true,
	"nested": {
		"value": 123.456,
		"description": "Nested description"
	}
}
""";

// Get the result!
var result = jsonParser.Parse<Dictionary<string, object>>(json);
Console.WriteLine(result["name"]); // Output: Sample Data
```

## Python-like

This example involves our killer-feature, **barrier tokens** that allows to parse indentations without missing them:

```csharp
using RCParsing;
using RCParsing.Building;

var builder = new ParserBuilder();

builder.Settings
	.Skip(b => b.Whitespaces().ConfigureForSkip());

// Add the 'INDENT' and 'DEDENT' barrier tokenizer
// 'INDENT' is emmited when indentation grows
// And 'DEDENT' is emmited when indentation cuts
// They are indentation delta tokens
builder.BarrierTokenizers
	.AddIndent(indentSize: 4, "INDENT", "DEDENT");

// Create the statement rule
builder.CreateRule("statement")
	.Choice(
	b => b
		.Literal("def")
		.Identifier()
		.Literal("():")
		.Rule("block"),
	b => b
		.Literal("if")
		.Identifier()
		.Literal(":")
		.Rule("block"),
	b => b
		.Identifier()
		.Literal("=")
		.Identifier()
		.Literal(";"));

// Create the 'block' rule that matches our 'INDENT' and 'DEDENT' barrier tokens
builder.CreateRule("block")
	.Token("INDENT")
	.OneOrMore(b => b.Rule("statement"))
	.Token("DEDENT");

builder.CreateMainRule("program")
	.ZeroOrMore(b => b.Rule("statement"))
	.EOF();

var parser = builder.Build();

string inputStr =
"""
def a():
    b = c;
    c = a;
a = p;
if c:
    h = i;
    if b:
        a = aa;
""";

// Get the optimized AST...
var ast = parser.Parse(inputStr).Optimized();

// And print it!
foreach (var statement in ast.Children)
{
	Console.WriteLine(statement.Text);
	Console.Write("\n\n");
}

// Outputs:

/*
def a():
    b = c;
    c = a;

a = p;

if c:
    h = i;
    if b:
        a = aa;
*/
```

# Comparison with Other Parsing Libraries

`RCParsing` is designed to outstand with unique features, and **easy** developer experience, speed is not the target, but it is good enough to compete with other fastest parsers. The benchmarks show that it competes directly with the fastest libraries, while the feature comparison reveals why it stands apart.

### Performance at a Glance (based on JSON benchmark)

| Library        | Speed (Relative to RCParsing) | Memory Efficiency |
| :------------- | :---------------------------- | :---------------- |
| **RCParsing**  | 1.00x (baseline)              | High              |
| **Pidgin**     | **~1.10x faster**             | **Excellent**     |
| **Superpower** | ~5.10x slower                 | Medium            |

### Feature Comparison

This table highlights the unique architectural and usability features of each library.

| Feature                    | **RCParsing**                 | Pidgin              | Parlot                 | Superpower          | ANTLR4             |
| :------------------------- | :---------------------------- | :------------------ | :--------------------- | :------------------ | :----------------- |
| **Architecture**           | **Scannerless hybrid**        | Scannerless         | Scannerless            | Lexer-based         | Lexer-based        |
| **API**                    | **High-readable Fluent**      | Functional          | Fluent/functional      | Fluent/functional   | Grammar Files      |
| **Barrier/complex Tokens** | **Yes, built-in or manual**   | None                | None                   | Yes, manual         | Yes, manual        |
| **Skipping**               | **6 strategies, globally**    | Manual              | Global or manual       | Tokenizer-based     | Tokenizer-based    |
| **Error Messages**         | **Extremely Detailed**        | Position/expected   | Manual messages        | Position/expected   | Position/expected  |
| **Minimum .NET Target**    | **.NET Standard 2.0**         | .NET 7.0            | .NET Standard 2.0      | .NET Standard 2.0   | .NET Framework 4.5 |

### The Verdict: Why RCParsing?

The performance gap has been nearly closed in `v2.0.0`. The choice now comes down to what you value most:

- **Choose `RCParsing` when you need:**
  - **Rapid Development:** A fluent API that reads like a grammar definition
  - **Maximum Flexibility:** To parse complex syntax (Python-like indentation, mixed data/code formats) with **barrier tokens**
  - **Superior Debugging:** Detailed errors with stack traces to quickly pinpoint problems
  - **Modern Features:** Built-in ruleset (`EscapedText`, `SeparatedRepeat`, `Number`) for common patterns

- **Consider other libraries only for:**
  - **Specialized ultra-low-memory scenarios** where every byte counts (Pidgin, Parlot)
  - **When already invested** in a different ecosystem (ANTLR)

The performance is now near-optimal, but the developer experience advantage is **significant and enduring**.

# Benchmarks

All benchmarks are done via `BenchmarkDotNet`.

Here is machine and runtime information:
```
BenchmarkDotNet v0.15.2, Windows 10 (10.0.19045.3448/22H2/2022Update)
AMD Ryzen 5 5600 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-KTXINV : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2

Runtime=.NET 8.0  IterationCount=3  WarmupCount=2
```

## JSON

The JSON value calculation with the typeset `Dictionary<string, object>`, `object[]`, `string`, `int` and `null`.

| Method               | Mean        | Error     | StdDev   | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|--------------------- |------------:|----------:|---------:|------:|--------:|--------:|--------:|----------:|------------:|
| JsonBig_RCParsing    |   230.06 us | 24.871 us | 1.363 us |  1.00 |    0.01 | 25.3906 | 12.2070 | 418.27 KB |        1.00 |
| JsonBig_Pidgin       |   211.99 us |  4.125 us | 0.226 us |  0.92 |    0.00 |  3.9063 |  0.2441 |  65.25 KB |        0.16 |
| JsonBig_Superpower   | 1,184.04 us | 46.592 us | 2.554 us |  5.15 |    0.03 | 39.0625 |  5.8594 | 638.31 KB |        1.53 |
|                      |             |           |          |       |         |         |         |           |             |
| JsonShort_RCParsing  |    12.56 us |  1.769 us | 0.097 us |  1.00 |    0.01 |  1.4954 |  0.0610 |  24.51 KB |        1.00 |
| JsonShort_Pidgin     |    10.73 us |  1.896 us | 0.104 us |  0.85 |    0.01 |  0.2136 |       - |   3.58 KB |        0.15 |
| JsonShort_Superpower |    63.92 us |  2.153 us | 0.118 us |  5.09 |    0.04 |  1.9531 |       - |  33.32 KB |        1.36 |

Notes:

- `RCParsing` uses `UseInlining()` and `IgnoreErrors()` settings.
- `JsonShort` methods uses ~20 lines of hardcoded (not generated) JSON with simple content.
- `JsonBig` methods uses ~180 lines of hardcoded (not generated) JSON with various content (deep, long objects/arrays).
- `Parlot` was temporarily excluded from this benchmark.

## Expressions

The `int` value calculation from expression with parentheses `()`, spaces and operators `+-/*` with priorities.

| Method                    | Mean         | Error        | StdDev      | Ratio | Gen0    | Gen1    | Allocated | Alloc Ratio |
|-------------------------- |-------------:|-------------:|------------:|------:|--------:|--------:|----------:|------------:|
| ExpressionBig_RCParsing   | 345,740.9 ns | 41,384.23 ns | 2,268.41 ns |  1.00 | 47.8516 | 34.6680 |  807240 B |        1.00 |
| ExpressionBig_Pidgin      | 690,574.8 ns | 64,348.54 ns | 3,527.16 ns |  2.00 |  0.9766 |       - |   23536 B |        0.03 |
| ExpressionBig_Parlot      |  64,181.6 ns |  1,089.82 ns |    59.74 ns |  0.19 |  3.2959 |       - |   56608 B |        0.07 |
|                           |              |              |             |       |         |         |           |             |
| ExpressionShort_RCParsing |   2,871.4 ns |    242.95 ns |    13.32 ns |  1.00 |  0.4730 |  0.0076 |    7960 B |        1.00 |
| ExpressionShort_Pidgin    |   6,628.0 ns |     84.15 ns |     4.61 ns |  2.31 |  0.0153 |       - |     344 B |        0.04 |
| ExpressionShort_Parlot    |     599.3 ns |     16.96 ns |     0.93 ns |  0.21 |  0.0534 |       - |     896 B |        0.11 |

Notes:

- `RCParsing` uses `UseInlining()` and `IgnoreErrors()` settings.
- `Parlot` uses `Compiled()` version of parser.
- `ExpressionShort` methods uses single line with 4 operators of hardcoded (not generated) expression.
- `ExpressionBig` methods uses single line with ~400 operators of hardcoded (not generated) expression.

*More benchmarks will be later here...*

# Projects using RCParsing

- [RCLargeLangugeModels](https://github.com/RomeCore/RCLargeLanguageModels): My project, used for `LLT`, the template Razor-like language with VERY *specific* syntax.

*Using RCParsing in your project? We'd love to feature it here! Submit a pull request to add your project to the list.*

# Roadmap

The future development of `RCParsing` is focused on:
- **Performance:** Continued profiling and optimization, especially for large files with deep structures.
- **API Ergonomics:** Introducing even more expressive and concise fluent methods (such as expression builder).
- **New Built-in Rules:** Adding common patterns (e.g., number with wide range of notations) out of the box.
- **Visualization Tooling:** Exploring tools for debugging and visualizing grammar rules.
- **API for analyzing errors:** The API that will allow users to analyze errors more effectively.
- **Error recovery:** Ability to re-parse the content when encountering an error using the anchor token. Applicable to `Repeat` and `SeparatedRepeat` rules.
- **Transformation sugars:** Ignorance flags of AST childs, more automatic transformation factories.
- ***Incremental parsing:*** Parsing only changed parts in the middle of text that will be good for IDE and LSP (Language Server Protocol).
- ***Streaming incremental parsing:*** The stateful approach for parsing chunked streaming content. For example, *Markdown* or structured *JSON* output from LLM.
- ***Cosmic levels of debug:*** Very detailed parse walk traces, showing the order of what was parsed with success/fail status.

# Contributing

### Contributions are welcome!

If you have an idea about this project, you can report it to `Issues`.  
For contributing code, please fork the repository and make your changes in a new branch. Once you're ready, create a pull request to merge your changes into the main branch. Pull requests should include a clear description of what was changed and why.