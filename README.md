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

`RCParsing` is designed to outstand with unique features, and **easy** developer experience, but it is good enough to compete with other fastest parser tools.

### Performance at a Glance (based on benchmarks)

| Library        | Speed (Relative to RCParsing default mode)  | Speed (Relative to RCParsing token combination style) | Memory Efficiency                      |
| :------------- | :------------------------------------------ | :---------------------------------------------------- | :------------------------------------- |
| **RCParsing**  | 1.00x (baseline)                            | **1.00x (baseline), ~5.00x faster than default**      | **High or Excellent** (based on style) |
| **Parlot**     | **~3.50x-3.70x faster**                     | ~1.20x-1.45x slower                                   | **Excellent**                          |
| **Pidgin**     | ~1.45x-3.00x slower                         | ~6.75x-13.55x slower                                  | **Excellent**                          |
| **Superpower** | ~8.00x-8.10x slower                         | ~40.75x slower                                        | Medium                                 |
| **Sprache**    | ~7.50x-8.10x slower                         | ~41.00x slower                                        | Very low                               |

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

Runtime=.NET 8.0  IterationCount=5  WarmupCount=2
```

## JSON

The JSON value calculation with the typeset `Dictionary<string, object>`, `object[]`, `string`, `int` and `null`.

| Method                               | Mean         | Error      | StdDev     | Ratio | RatioSD | Gen0     | Gen1    | Allocated  | Alloc Ratio |
|------------------------------------- |-------------:|-----------:|-----------:|------:|--------:|---------:|--------:|-----------:|------------:|
| JsonBig_RCParsing                    |   150.510 us |  5.9708 us |  2.1292 us |  1.00 |    0.02 |  13.6719 |  4.3945 |  223.99 KB |        1.00 |
| JsonBig_RCParsing_Optimized          |    94.158 us |  0.9837 us |  0.3508 us |  0.63 |    0.01 |   9.7656 |  2.4414 |  159.91 KB |        0.71 |
| JsonBig_RCParsing_TokenCombination   |    30.257 us |  0.4361 us |  0.1936 us |  0.20 |    0.00 |   2.9602 |  0.2441 |   48.81 KB |        0.22 |
| JsonBig_Parlot                       |    45.456 us |  0.8677 us |  0.3853 us |  0.30 |    0.00 |   1.9531 |  0.1221 |   32.08 KB |        0.14 |
| JsonBig_Pidgin                       |   203.785 us |  0.6212 us |  0.2758 us |  1.35 |    0.02 |   3.9063 |  0.2441 |   65.25 KB |        0.29 |
| JsonBig_Superpower                   | 1,201.028 us | 19.4509 us |  6.9364 us |  7.98 |    0.11 |  39.0625 |  5.8594 |  638.31 KB |        2.85 |
| JsonBig_Sprache                      | 1,238.111 us | 33.6678 us | 14.9487 us |  8.23 |    0.14 | 232.4219 | 27.3438 | 3808.34 KB |       17.00 |
|                                      |              |            |            |       |         |          |         |            |             |
| JsonShort_RCParsing                  |     8.171 us |  0.0573 us |  0.0205 us |  1.00 |    0.00 |   0.6714 |  0.0153 |      11 KB |        1.00 |
| JsonShort_RCParsing_Optimized        |     5.152 us |  0.0326 us |  0.0116 us |  0.63 |    0.00 |   0.5493 |  0.0076 |    9.03 KB |        0.82 |
| JsonShort_RCParsing_TokenCombination |     1.504 us |  0.0144 us |  0.0064 us |  0.18 |    0.00 |   0.1583 |       - |     2.6 KB |        0.24 |
| JsonShort_Parlot                     |     2.256 us |  0.0205 us |  0.0091 us |  0.28 |    0.00 |   0.1144 |       - |    1.91 KB |        0.17 |
| JsonShort_Pidgin                     |    11.284 us |  0.5601 us |  0.1997 us |  1.38 |    0.02 |   0.2136 |       - |    3.58 KB |        0.33 |
| JsonShort_Superpower                 |    64.991 us |  0.7441 us |  0.3304 us |  7.95 |    0.04 |   1.9531 |       - |   33.32 KB |        3.03 |
| JsonShort_Sprache                    |    64.344 us |  3.0978 us |  1.3755 us |  7.87 |    0.16 |  12.6953 |  0.2441 |  208.17 KB |       18.92 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `UseFirstCharacterMatch()`, `IgnoreErrors()` and `SkipWhitespacesOptimized()` settings.
- `RCParsing_TokenCombination` uses complex manual tokens with immediate transformations instead of rules, and `UseFirstCharacterMatch()` setting.
- `Parlot` uses `Compiled()` version of parser.
- `JsonShort` methods uses ~20 lines of hardcoded (not generated) JSON with simple content.
- `JsonBig` methods uses ~180 lines of hardcoded (not generated) JSON with various content (deep, long objects/arrays).

## Expressions

The `int` value calculation from expression with parentheses `()`, spaces and operators `+-/*` with priorities.

| Method                                     | Mean         | Error       | StdDev    | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------- |-------------:|------------:|----------:|------:|--------:|--------:|--------:|----------:|------------:|
| ExpressionBig_RCParsing                    | 244,419.2 ns | 5,831.71 ns | 902.46 ns |  1.00 |    0.00 | 24.1699 | 11.9629 |  408280 B |        1.00 |
| ExpressionBig_RCParsing_Optimized          | 175,608.8 ns | 1,598.11 ns | 415.02 ns |  0.72 |    0.00 | 20.2637 |  9.0332 |  342656 B |        0.84 |
| ExpressionBig_RCParsing_TokenCombination   |  53,168.0 ns |   612.27 ns |  94.75 ns |  0.22 |    0.00 |  4.1504 |  0.0610 |   70288 B |        0.17 |
| ExpressionBig_Parlot                       |  62,379.0 ns |   460.50 ns | 119.59 ns |  0.26 |    0.00 |  3.2959 |       - |   56608 B |        0.14 |
| ExpressionBig_Pidgin                       | 735,510.6 ns | 3,462.74 ns | 899.26 ns |  3.01 |    0.01 |  0.9766 |       - |   23536 B |        0.06 |
|                                            |              |             |           |       |         |         |         |           |             |
| ExpressionShort_RCParsing                  |   2,130.1 ns |    78.86 ns |  12.20 ns |  1.00 |    0.01 |  0.2174 |       - |    3680 B |        1.00 |
| ExpressionShort_RCParsing_Optimized        |   1,627.5 ns |    52.44 ns |   8.12 ns |  0.76 |    0.01 |  0.2098 |       - |    3528 B |        0.96 |
| ExpressionShort_RCParsing_TokenCombination |     446.4 ns |    16.76 ns |   4.35 ns |  0.21 |    0.00 |  0.0391 |       - |     656 B |        0.18 |
| ExpressionShort_Parlot                     |     612.1 ns |    17.06 ns |   4.43 ns |  0.29 |    0.00 |  0.0534 |       - |     896 B |        0.24 |
| ExpressionShort_Pidgin                     |   6,282.3 ns |   120.52 ns |  18.65 ns |  2.95 |    0.02 |  0.0153 |       - |     344 B |        0.09 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `IgnoreErrors()` and `SkipWhitespacesOptimized()` settings.
- `RCParsing_TokenCombination` uses complex manual tokens with immediate transformations instead of rules, and `UseFirstCharacterMatch()` setting.
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

This framework is born recently (1.5 months ago) and some little features may not be tested.

If you have an idea about this project, you can report it to `Issues`.  
For contributing code, please fork the repository and make your changes in a new branch. Once you're ready, create a pull request to merge your changes into the main branch. Pull requests should include a clear description of what was changed and why.
