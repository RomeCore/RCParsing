# RCParsing

[![Build](https://github.com/RomeCore/RCParsing/actions/workflows/build.yml/badge.svg)](https://github.com/RomeCore/RCParsing/actions/workflows/build.yml)
[![Tests](https://github.com/RomeCore/RCParsing/actions/workflows/tests.yml/badge.svg)](https://github.com/RomeCore/RCParsing/actions/workflows/tests.yml)
[![Auto Release](https://github.com/RomeCore/RCParsing/actions/workflows/auto-release.yml/badge.svg)](https://github.com/RomeCore/RCParsing/actions/workflows/auto-release.yml)
[![NuGet](https://img.shields.io/nuget/v/RCParsing.svg?style=flat&label=NuGet:%20RCParsing)](https://www.nuget.org/packages/RCParsing/)
[![License](https://img.shields.io/github/license/RomeCore/RCParsing.svg)](https://github.com/RomeCore/RCParsing/blob/main/LICENSE)
[![Star me on GitHub](https://img.shields.io/github/stars/RomeCore/RCParsing.svg?style=social&label=Star%20Me)](https://github.com/RomeCore/RCParsing)

**A Fluent, Lexerless Parser Builder for .NET — Define ANY grammars with the elegance of BNF and the power of C#.**

This library focuses on **Developer-experience (DX)** first, providing best toolkit for creating your **programming languages**, **file formats** or even **data extraction tools** with declarative API, debugging tools, and more. This allows you to design your parser directly in code and easily fix it using *rule stack traces* and detailed error messages.

## Why RCParsing?

- 🐍 **Hybrid Power**: Unique support for **barrier tokens** to parse indent-sensitive languages like Python and YAML.
- 💪 **Regex on Steroids**: You can find all matches for target structure in the input text with detailed AST information and transformed value.
- 🌀 **Lexerless Freedom**: No token priority headaches. Parse directly from raw text, even with keywords embedded in strings. Tokens are used just as lightweight matching primitives.
- 🎨 **Fluent API**: Write parsers in C# that read like clean BNF grammars, boosting readability and maintainability compared to imperative or functional approaches.
- 🧩 **Combinator Mode**: Unlock maximum performance by defining complex tokens with immediate value transformation, bypassing the AST construction entirely for a direct, allocation-free result. Perfect for high-speed parsing of well-defined formats.
- 🐛 **Debug-Friendly**: Get detailed, actionable error messages with stack traces and precise source locations. Richest API for manual error information included.
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
	- [JSON token combination](#json-token-combination) - A maximum speed approach for getting values without AST.
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

- [Tutorials](https://github.com/RomeCore/RCParsing/blob/main/docs/tutorials.md) - detailed tutorials, explaining features and mechanics of this library, highly recommended to read!

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

## JSON token combination

Tokens in this parser can be complex enough to act like the combinators, with immediate value transformation without AST:

```csharp
var builder = new ParserBuilder();

// Use lookahead for 'Choice' tokens
builder.Settings.UseFirstCharacterMatch();

builder.CreateToken("string")
	// 'Between' token pattern matches a sequence of three elements,
	// but calculates and propagates intermediate value of second element
	.Between(
		b => b.Literal('"'),
		b => b.TextUntil('"'),
		b => b.Literal('"'));

builder.CreateToken("number")
	.Number<double>();

builder.CreateToken("boolean")
	// 'Map' token pattern applies intermediate value transformer to child's value
	.Map<string>(b => b.LiteralChoice("true", "false"), m => m == "true");

builder.CreateToken("null")
	// 'Return' does not calculates value for child element, just returns 'null' here
	.Return(b => b.Literal("null"), null);

builder.CreateToken("value")
	// Skip whitespaces before value token
	.SkipWhitespaces(b =>
		// 'Choice' token selects the matched token's value
		b.Choice(
			c => c.Token("string"),
			c => c.Token("number"),
			c => c.Token("boolean"),
			c => c.Token("null"),
			c => c.Token("array"),
			c => c.Token("object")
	));

builder.CreateToken("value_list")
	.ZeroOrMoreSeparated(
		b => b.Token("value"),
		b => b.SkipWhitespaces(b => b.Literal(',')),
		includeSeparatorsInResult: false)
	// You can apply passage function for tokens that
	// matches multiple and variable amount of child elements
	.Pass(v =>
	{
		return v.ToArray();
	});

builder.CreateToken("array")
	.Between(
		b => b.Literal('['),
		b => b.Token("value_list"),
		b => b.SkipWhitespaces(b => b.Literal(']')));

builder.CreateToken("pair")
	.SkipWhitespaces(b => b.Token("string"))
	.SkipWhitespaces(b => b.Literal(':'))
	.Token("value")
	.Pass(v =>
	{
		return KeyValuePair.Create((string)v[0]!, v[2]);
	});

builder.CreateToken("pair_list")
	.ZeroOrMoreSeparated(
		b => b.Token("pair"),
		b => b.SkipWhitespaces(b => b.Literal(',')))
	.Pass(v =>
	{
		return v.Cast<KeyValuePair<string, object>>().ToDictionary();
	});

builder.CreateToken("object")
	.Between(
		b => b.Literal('{'),
		b => b.Token("pair_list"),
		b => b.SkipWhitespaces(b => b.Literal('}')));

var parser = builder.Build();

var json =
"""
{
	"id": 1,
	"name": "Sample Data",
	"created": "2023-01-01T00:00:00",
	"tags": ["tag1", "tag2", "tag3"],
	"isActive": true,
	"nested": {
		"value": 123.456,
		"description": "Nested description"
	}
}
""";

// Match the token directly and produce intermediate value
// Note that it will fail silently if there is error in the input string
var result = parser.MatchToken<Dictionary<string, object>>("value", json);
Console.WriteLine(result["name"]); // Outputs: Sample data
```

## Finding patterns

The `FindAllMatches` method allows you to extract all occurrences of a pattern from a string, even in complex inputs, while handling optional transformations. Here's an example where will find the `Price: *PRICE* (USD|EUR)` pattern:

```csharp
var builder = new ParserBuilder();

// Skip unnecessary whitespace (you can configure comments here and they will be ignored when matching)
builder.Settings.SkipWhitespaces();

// Create the rule that we will find in text
builder.CreateMainRule()
	.Literal("Price:")
	.Number<double>() // 1
	.LiteralChoice("USD", "EUR") // 2
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

`RCParsing` is designed to outstand with unique features, and **easy** developer experience, speed is not the target, but it is good enough to compete with other fastest parser tools.

### Performance at a Glance (based on benchmarks)

| Library        | Speed (Relative to RCParsing default mode)  | Speed (Relative to RCParsing token combination mode)  | Memory Efficiency |
| :------------- | :------------------------------------------ | :---------------------------------------------------- | :---------------- |
| **RCParsing**  | 1.00x (baseline)                            | **1.00x (baseline), ~3.00-5.00x faster than default** | High              |
| **Parlot**     | **~3.55x-4.55x faster**                     | **~1.20x slower - ~1.55x faster**                     | **Excellent**     |
| **Pidgin**     | ~1.05x-3.00x slower                         | ~3.20x-13.55x slower                                  | **Excellent**     |
| **Superpower** | ~6.30x-6.50x slower                         | ~19.00x slower                                        | Medium            |
| **Sprache**    | ~6.10x-6.50x slower                         | ~19.15x slower                                        | Very low          |

### Feature Comparison

This table highlights the unique architectural and usability features of each library.

| Feature                    | RCParsing                                   | Pidgin              | Parlot                 | Superpower          | ANTLR4                        |
| :------------------------- | :------------------------------------------ | :------------------ | :--------------------- | :------------------ | :---------------------------- |
| **Architecture**           | **Scannerless hybrid**                      | Scannerless         | Scannerless            | Lexer-based         | **Lexer-based with modes**    |
| **API**                    | **Fluent, lambda-based**                    | Functional          | Fluent/functional      | Fluent/functional   | **Grammar Files**             |
| **Barrier/complex Tokens** | **Yes, built-in or manual**                 | None                | None                   | Yes, manual         | Yes, manual                   |
| **Skipping**               | **6 strategies, globally**                  | Manual              | Global or manual       | Lexer-based         | Lexer-based                   |
| **Error Messages**         | **Extremely Detailed, extendable with API** | Simple              | Manual messages        | Simple              | Simple by default, extendable |
| **Minimum .NET Target**    | **.NET Standard 2.0**                       | .NET 7.0            | .NET Standard 2.0      | .NET Standard 2.0   | **.NET Framework 4.5**        |

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

| Method                             | Mean         | Error      | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|----------------------------------- |-------------:|-----------:|----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| JsonBig_RCParsing                  |   184.187 us |  1.0224 us | 0.4539 us |  1.00 |    0.00 |  15.1367 |  4.8828 |  247.69 KB |        1.00 |
| JsonBig_RCParsing_Optimized        |   126.629 us |  0.5114 us | 0.2271 us |  0.69 |    0.00 |  11.2305 |  2.9297 |   183.6 KB |        0.74 |
| JsonBig_RCParsing_CombinatorMode   |    62.054 us |  1.0960 us | 0.4866 us |  0.34 |    0.00 |   4.3945 |  0.2441 |   72.51 KB |        0.29 |
| JsonBig_Parlot                     |    40.535 us |  0.1462 us | 0.0649 us |  0.22 |    0.00 |   1.9531 |  0.1221 |   32.08 KB |        0.13 |
| JsonBig_Pidgin                     |   202.380 us |  1.2544 us | 0.5570 us |  1.10 |    0.00 |   3.9063 |  0.2441 |   65.25 KB |        0.26 |
| JsonBig_Superpower                 | 1,191.205 us |  3.5113 us | 1.5591 us |  6.47 |    0.02 |  39.0625 |  5.8594 |  638.31 KB |        2.58 |
| JsonBig_Sprache                    | 1,199.835 us | 11.2303 us | 4.9863 us |  6.51 |    0.03 | 232.4219 | 27.3438 | 3808.34 KB |       15.38 |
|                                    |              |            |           |       |         |          |         |            |             |
| JsonShort_RCParsing                |    10.389 us |  0.0487 us | 0.0216 us |  1.00 |    0.00 |   0.7629 |  0.0153 |   12.63 KB |        1.00 |
| JsonShort_RCParsing_Optimized      |     7.533 us |  0.2876 us | 0.1277 us |  0.73 |    0.01 |   0.6485 |  0.0076 |   10.66 KB |        0.84 |
| JsonShort_RCParsing_CombinatorMode |     3.753 us |  0.0767 us | 0.0340 us |  0.36 |    0.00 |   0.2518 |       - |    4.23 KB |        0.33 |
| JsonShort_Parlot                   |     2.243 us |  0.0218 us | 0.0078 us |  0.22 |    0.00 |   0.1144 |       - |    1.91 KB |        0.15 |
| JsonShort_Pidgin                   |    10.875 us |  0.0246 us | 0.0088 us |  1.05 |    0.00 |   0.2136 |       - |    3.58 KB |        0.28 |
| JsonShort_Superpower               |    65.126 us |  1.2002 us | 0.5329 us |  6.27 |    0.05 |   1.9531 |       - |   33.32 KB |        2.64 |
| JsonShort_Sprache                  |    63.559 us |  1.1455 us | 0.5086 us |  6.12 |    0.05 |  12.6953 |  0.2441 |  208.17 KB |       16.49 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `UseFirstCharacterMatch()`, `IgnoreErrors()` and `SkipWhitespacesOptimized()` settings.
- `RCParsing_CombinatorMode` uses complex manual tokens with immediate transformations instead of rules, and `UseFirstCharacterMatch()` setting.
- `Parlot` uses `Compiled()` version of parser.
- `JsonShort` methods uses ~20 lines of hardcoded (not generated) JSON with simple content.
- `JsonBig` methods uses ~180 lines of hardcoded (not generated) JSON with various content (deep, long objects/arrays).

## Expressions

The `int` value calculation from expression with parentheses `()`, spaces and operators `+-/*` with priorities.

| Method                                   | Mean         | Error       | StdDev      | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------------------------------------- |-------------:|------------:|------------:|------:|--------:|--------:|--------:|----------:|------------:|
| ExpressionBig_RCParsing                  | 244,257.0 ns | 3,449.39 ns |   895.80 ns |  1.00 |    0.00 | 24.1699 | 11.9629 |  408280 B |        1.00 |
| ExpressionBig_RCParsing_Optimized        | 181,343.5 ns | 3,541.27 ns |   919.66 ns |  0.74 |    0.00 | 20.2637 |  9.0332 |  342656 B |        0.84 |
| ExpressionBig_RCParsing_CombinatorMode   |  52,275.4 ns |   332.40 ns |    86.32 ns |  0.21 |    0.00 |  4.1504 |  0.0610 |   70288 B |        0.17 |
| ExpressionBig_Parlot                     |  62,843.3 ns |   721.05 ns |   111.58 ns |  0.26 |    0.00 |  3.2959 |       - |   56608 B |        0.14 |
| ExpressionBig_Pidgin                     | 695,858.1 ns | 5,250.39 ns | 1,363.51 ns |  2.85 |    0.01 |  0.9766 |       - |   23536 B |        0.06 |
|                                          |              |             |             |       |         |         |         |           |             |
| ExpressionShort_RCParsing                |   2,133.7 ns |    29.49 ns |     7.66 ns |  1.00 |    0.00 |  0.2174 |       - |    3680 B |        1.00 |
| ExpressionShort_RCParsing_Optimized      |   1,620.9 ns |    42.12 ns |    10.94 ns |  0.76 |    0.01 |  0.2098 |       - |    3528 B |        0.96 |
| ExpressionShort_RCParsing_CombinatorMode |     435.2 ns |    11.42 ns |     2.96 ns |  0.20 |    0.00 |  0.0391 |       - |     656 B |        0.18 |
| ExpressionShort_Parlot                   |     596.8 ns |     5.76 ns |     0.89 ns |  0.28 |    0.00 |  0.0534 |       - |     896 B |        0.24 |
| ExpressionShort_Pidgin                   |   6,418.5 ns |   111.36 ns |    28.92 ns |  3.01 |    0.02 |  0.0153 |       - |     344 B |        0.09 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `IgnoreErrors()` and `SkipWhitespacesOptimized()` settings.
- `RCParsing_CombinatorMode` uses complex manual tokens with immediate transformations instead of rules, and `UseFirstCharacterMatch()` setting.
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
- **API Ergonomics:** Introducing even more expressive and fluent methods (such as expression builder).
- **New Built-in Rules:** Adding common patterns (e.g., number with wide range of notations).
- **Visualization Tooling:** Exploring tools for debugging and visualizing resulting AST.
- **Error recovery:** Ability to re-parse the content when encountering an error using the anchor token.
- ***Incremental parsing:*** Parsing only changed parts in the middle of text that will be good for IDE and LSP (Language Server Protocol).
- ***Streaming incremental parsing:*** The stateful approach for parsing chunked streaming content. For example, *Markdown* or structured *JSON* output from LLM.
- ***Cosmic levels of debug:*** Very detailed parse walk traces, showing the order of what was parsed with success/fail status.

# Contributing

### Contributions are welcome!

This framework is born recently (1.5 months ago) and not all features are tested

If you have an idea about this project, you can report it to `Issues`.  
For contributing code, please fork the repository and make your changes in a new branch. Once you're ready, create a pull request to merge your changes into the main branch. Pull requests should include a clear description of what was changed and why.
