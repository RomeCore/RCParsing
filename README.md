# RCParsing

[![NuGet](https://img.shields.io/nuget/v/RCParsing.svg?style=flat&label=NuGet:%20RCParsing)](https://www.nuget.org/packages/RCParsing/)
[![License](https://img.shields.io/github/license/RomeCore/RCParsing.svg)](https://github.com/RomeCore/RCParsing/blob/main/LICENSE)
[![Star me on GitHub](https://img.shields.io/github/stars/RomeCore/RCParsing.svg?style=social&label=Star%20Me)](https://github.com/RomeCore/RCParsing)

**A Fluent, Lexerless Parser Builder for .NET — Define ANY grammars with the elegance of BNF and the power of C#.**

This library focuses on **Developer-experience (DX)** first, providing best toolkit for creating your **programming languages**, **file formats** or even **data extraction tools** with declarative API, debugging tools, and more. This allows you to design your parser directly in code and easily fix it using *rule stack traces* and detailed error messages.

## Why RCParsing?

- 🐍 **Hybrid Power**: Unique support for **barrier tokens** to parse indent-sensitive languages like Python and YAML flawlessly.
- 💪 **Regex on Steroids**: You can find all matches for target structure in the input text with detailed AST information and transformed value.
- 🚫 **Lexerless Freedom**: No token priority headaches. Parse directly from raw text, even with keywords embedded in strings. Tokens are used just as lightweight matching primitives.
- 🎯 **Fluent API**: Write parsers in C# that read like clean BNF grammars, boosting readability and maintainability compared to imperative or functional approaches.
- 🐛 **Debug-Friendly**: Get detailed, actionable error messages with stack traces and precise source locations.
- ⚡ **Fast**: Performance is now on par with the fastest .NET parsing libraries (see benchmarks below).
- 🌳 **Rich AST**: Parser makes an AST (Abstract Syntax Tree) from raw text, with ability to optimize, fully analyze and calculate the result value entirely lazy, reducing unnecessary allocations.
- 🔧 **Configurable Skipping**: Advanced strategies for whitespace and comments, allowing you to use conflicting tokens in your main rules.
- 📦 **Batteries Included**: Useful built-in tokens and rules (regex, identifiers, numbers, escaped strings, separated lists, custom tokens, and more...).
- 🖥️ **Broad Compatibility**: Targets `.NET Standard 2.0` (runs on `.NET Framework 4.6.1+`), `.NET 6.0`, and `.NET 8.0`.

# Table of contents

- [Installation](#installation)
- [Tutorials, docs and examples](#tutorials-docs-and-examples)
- [Simple examples](#simple-examples) - The examples that you can copy, paste and run!
	- [A + B](#a--b) - Basic arithmetic expression parser with result calculation.
	- [JSON](#json) - A complete JSON parser with comments and skipping.
	- [Python-like](#python-like) - Demonstrating barrier tokens for indentation.
	- [Finding patterns](#finding-patterns) - How to find all occurrences of a rule in a string.
- [Comparison with other parsing libraries](#comparison-with-other-parsing-libraries)
- [Benchmarks](#benchmarks)
	- [JSON](#json-1)
	- [Expressions](#expressions)
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

// Enable and configure the auto-skip for 'Whitespaces' (you can replace it with any other rule)
builder.Settings.SkipWhitespaces();

// Create a main sequential expression rule
builder.CreateMainRule("expression")
    .Number<double>()
    .LiteralChoice("+", "-")
    .Number<double>()
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

builder.Settings.SkipWhitespaces();

// Add the 'INDENT' and 'DEDENT' barrier tokenizer
// 'INDENT' is emitted when indentation grows
// And 'DEDENT' is emitted when indentation cuts
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

## Finding patterns

The `FindAllMatches` method allows you to extract all occurrences of a pattern from a string, even in complex inputs, while handling optional transformations. Here's an example where will will find the `Price: *PRICE* (USD|EUR)` pattern:

```csharp
var builder = new ParserBuilder();

// Skip unnecessary whitespace (you can configure comments here and they will be ignored when matching)
builder.Settings.SkipWhitespaces();

// Create the rule that we will find in text
builder.CreateMainRule()
	.Literal("Price:")
	.Number<double>() // 1
	.LiteralChoice(["USD", "EUR"]) // 2
	.Transform(v =>
	{
		var number = v[1].Value; // Get the number value
		var currency = v[2].Text; // Get the 'USD' or 'EUR' text
		return new { Amount = number, Currency = currency };
	});

var input =
"""
Some log entries.
Price: 42.99 USD
Error: something happened.
Price: 99.50 EUR
Another line.
Price: 2.50 USD
""";

// Find all transformed matches
var prices = builder.Build().FindAllMatches<dynamic>(input).ToList();

foreach (var price in prices)
{
	Console.WriteLine($"Price: {price.Amount}; Currency: {price.Currency}");
}
```

# Comparison with Other Parsing Libraries

`RCParsing` is designed to outstand with unique features, and **easy** developer experience, speed is not the target, but it is good enough to compete with other fastest parsers. The benchmarks show that it competes directly with the fastest libraries, while the feature comparison reveals why it stands apart.

### Performance at a Glance (based on benchmarks)

| Library        | Speed (Relative to RCParsing) | Memory Efficiency |
| :------------- | :---------------------------- | :---------------- |
| **RCParsing**  | 1.00x (baseline)              | High              |
| **Parlot**     | **~-4.10-4.90x faster**       | **Excellent**     |
| **Pidgin**     | ~1.00x-2.70x slower           | **Excellent**     |
| **Superpower** | ~5.90x-6.10x slower           | Medium            |
| **Sprache**    | ~5.50x-6.50x slower           | Very low          |

### Feature Comparison

This table highlights the unique architectural and usability features of each library.

| Feature                    | **RCParsing**                 | Pidgin              | Parlot                 | Superpower          | ANTLR4             |
| :------------------------- | :---------------------------- | :------------------ | :--------------------- | :------------------ | :----------------- |
| **Architecture**           | **Scannerless hybrid**        | Scannerless         | Scannerless            | Lexer-based         | Lexer-based        |
| **API**                    | **Fluent**                    | Functional          | Fluent/functional      | Fluent/functional   | Grammar Files      |
| **Barrier/complex Tokens** | **Yes, built-in or manual**   | None                | None                   | Yes, manual         | Yes, manual        |
| **Skipping**               | **6 strategies, globally**    | Manual              | Global or manual       | Tokenizer-based     | Tokenizer-based    |
| **Error Messages**         | **Extremely Detailed**        | Position/expected   | Manual messages        | Position/expected   | Position/expected  |
| **Minimum .NET Target**    | **.NET Standard 2.0**         | .NET 7.0            | .NET Standard 2.0      | .NET Standard 2.0   | .NET Framework 4.5 |

### The Verdict: Why RCParsing?

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

| Method               | Mean         | Error         | StdDev     | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|--------------------- |-------------:|--------------:|-----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| JsonBig_RCParsing    |   193.732 us |    15.2783 us |  0.8375 us |  1.00 |    0.01 |  14.4043 |  3.6621 |  237.67 KB |        1.00 |
| JsonBig_Parlot       |    40.349 us |     2.1830 us |  0.1197 us |  0.21 |    0.00 |   1.9531 |  0.1221 |   32.08 KB |        0.13 |
| JsonBig_Pidgin       |   204.885 us |     7.7079 us |  0.4225 us |  1.06 |    0.00 |   3.9063 |  0.2441 |   65.25 KB |        0.27 |
| JsonBig_Superpower   | 1,184.334 us |    41.9646 us |  2.3002 us |  6.11 |    0.03 |  39.0625 |  5.8594 |  638.31 KB |        2.69 |
| JsonBig_Sprache      | 1,258.793 us | 1,016.3510 us | 55.7096 us |  6.50 |    0.25 | 232.4219 | 27.3438 | 3808.34 KB |       16.02 |
|                      |              |               |            |       |         |          |         |            |             |
| JsonShort_RCParsing  |    10.914 us |     0.5714 us |  0.0313 us |  1.00 |    0.00 |   0.8545 |  0.0153 |   14.13 KB |        1.00 |
| JsonShort_Parlot     |     2.145 us |     0.0680 us |  0.0037 us |  0.20 |    0.00 |   0.1144 |       - |    1.91 KB |        0.14 |
| JsonShort_Pidgin     |    11.003 us |     0.1905 us |  0.0104 us |  1.01 |    0.00 |   0.2136 |       - |    3.58 KB |        0.25 |
| JsonShort_Superpower |    64.511 us |     3.6988 us |  0.2027 us |  5.91 |    0.02 |   1.9531 |       - |   33.32 KB |        2.36 |
| JsonShort_Sprache    |    60.472 us |     4.1068 us |  0.2251 us |  5.54 |    0.02 |  12.6953 |  0.3052 |  208.17 KB |       14.74 |

Notes:

- `RCParsing` uses `UseInlining()` and `IgnoreErrors()` settings.
- `Parlot` uses `Compiled()` version of parser.
- `JsonShort` methods uses ~20 lines of hardcoded (not generated) JSON with simple content.
- `JsonBig` methods uses ~180 lines of hardcoded (not generated) JSON with various content (deep, long objects/arrays).

## Expressions

The `int` value calculation from expression with parentheses `()`, spaces and operators `+-/*` with priorities.

| Method                    | Mean         | Error        | StdDev      | Ratio | Gen0    | Gen1    | Allocated | Alloc Ratio |
|-------------------------- |-------------:|-------------:|------------:|------:|--------:|--------:|----------:|------------:|
| ExpressionBig_RCParsing   | 285,804.7 ns | 20,172.23 ns | 1,105.71 ns |  1.00 | 22.9492 | 10.7422 |  385720 B |        1.00 |
| ExpressionBig_Parlot      |  69,685.0 ns |  4,580.85 ns |   251.09 ns |  0.24 |  3.2959 |       - |   56608 B |        0.15 |
| ExpressionBig_Pidgin      | 666,472.5 ns | 29,955.41 ns | 1,641.96 ns |  2.33 |  0.9766 |       - |   23536 B |        0.06 |
|                           |              |              |             |       |         |         |           |             |
| ExpressionShort_RCParsing |   2,486.6 ns |    193.33 ns |    10.60 ns |  1.00 |  0.2327 |       - |    3904 B |        1.00 |
| ExpressionShort_Parlot    |     586.0 ns |     71.83 ns |     3.94 ns |  0.24 |  0.0534 |       - |     896 B |        0.23 |
| ExpressionShort_Pidgin    |   6,893.6 ns |    192.66 ns |    10.56 ns |  2.77 |  0.0153 |       - |     344 B |        0.09 |

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