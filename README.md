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
- 🧩 **Combinator Style**: Unlock maximum performance by defining complex tokens with immediate value transformation, bypassing the AST construction entirely for a direct, allocation-free result. Perfect for high-speed parsing of well-defined formats. Also can be used with AST mode.
- 🐛 **Debug-Friendly**: Get detailed, actionable error messages with stack traces and precise source locations. Richest API for manual error information included.
- 🚑 **Error Recovery**: Define custom recovery strategies per rule to handle syntax errors and go further.
- ⚡ **Blazing Fast**: Performance is now on par with the fastest .NET parsing libraries (see benchmarks below).
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
	- [Regex](#regex)
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

- [Tutorials](https://github.com/RomeCore/RCParsing/blob/main/docs/tutorials.md) - detailed tutorials, explaining features and mechanics of this library, highly recommended to read!]

# Simple examples

## A + B

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

## JSON

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
	.ConfigureForSkip();

builder.CreateToken("string")
	.Literal('"')
	.EscapedTextPrefix(prefix: '\\', '\\', '\"') // This sub-token automatically escapes the source string and puts it into intermediate value
	.Literal('"')
	.Pass(index: 1); // Pass the EscapedTextPrefix's intermediate value up (it will be used as token's result value)

builder.CreateToken("number")
	.Number<double>();

builder.CreateToken("boolean")
	.LiteralChoice("true", "false").Transform(v => v.Text == "true");

builder.CreateToken("null")
	.Literal("null").Transform(v => null);

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
		allowTrailingSeparator: true, includeSeparatorsInResult: false)
		.TransformLast(v => v.SelectArray())
	.Literal("]")
	.TransformSelect(1); // Selects the Children[1]'s value

builder.CreateRule("object")
	.Literal("{")
	.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","),
		allowTrailingSeparator: true, includeSeparatorsInResult: false)
		.TransformLast(v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary(k => k.Key, v => v.Value))
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
var result = parser.MatchToken<Dictionary<string, object>>("value", json);
Console.WriteLine(result["name"]); // Outputs: Sample Data
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

| Library        | Speed (Relative to RCParsing default mode)  | Speed (Relative to RCParsing token combination style) | Memory Efficiency                      | Type              |
| :------------- | :------------------------------------------ | :---------------------------------------------------- | :------------------------------------- | :---------------- |
| **RCParsing**  | 1.00x (baseline)                            | **1.00x (baseline), ~5.00x faster than default**      | **High or Excellent** (based on style) | **Both**          |
| **Parlot**     | **~3.50x-3.70x faster**                     | ~1.20x-1.45x slower                                   | **Excellent**                          | Combinator        |
| **Pidgin**     | ~1.45x-3.00x slower                         | ~6.75x-13.55x slower                                  | **Excellent**                          | Combinator        |
| **ANTLR**      | ~1.20x-1.30x slower                         | ~6.60x-7.30x slower                                   | High                                   | AST-based         |
| **Superpower** | ~8.00x-8.10x slower                         | ~40.75x slower                                        | Medium                                 | Combinator        |
| **Sprache**    | ~7.50x-8.10x slower                         | ~41.00x slower                                        | Very low                               | Combinator        |

### Feature Comparison

This table highlights the unique architectural and usability features of each library.

| Feature                    | RCParsing                                   | Pidgin              | Parlot                 | Superpower          | ANTLR4                            |
| :------------------------- | :------------------------------------------ | :------------------ | :--------------------- | :------------------ | :-------------------------------- |
| **Architecture**           | **Scannerless hybrid**                      | Scannerless         | Scannerless            | Lexer-based         | **Lexer-based with modes**        |
| **API**                    | **Fluent, lambda-based**                    | Functional          | Fluent/functional      | Fluent/functional   | **Grammar Files**                 |
| **Barrier/complex Tokens** | **Yes, built-in or manual**                 | None                | None                   | Yes, manual         | Yes, manual                       |
| **Skipping**               | **6 strategies, global or manual**          | Manual              | Global or manual       | Lexer-based         | Lexer-based                       |
| **Error Messages**         | **Extremely Detailed, extendable with API** | Simple              | Manual messages        | Simple              | **Simple by default, extendable** |
| **Minimum .NET Target**    | **.NET Standard 2.0**                       | .NET 7.0            | .NET Standard 2.0      | .NET Standard 2.0   | **.NET Framework 4.5**            |

## Comparison with each library

### ANTLR

This is a powerful tool for creating own DSLs with a wide ecosystem and multiple languages support (C#, Java, Python, C++ and others). But it requires a step of code generation and it can be a bit complex to set up for beginners. Also it uses a lexer-based algorithm, so you need to carefully setup a lexer for complex languages, also it barely fits for code/text mixed grammars. Therefore, RCParsing can be a bit slow for complex grammars, but it not tested yet.

### Parlot

Parlot is known as fastest parser combinator library for .NET with support of context-specific parsing, global skipping and compilation via expression trees. But it's not that friendly for debug, you required to manually place errors in parsers, otherwise you just get nothing on parsing. But RCParsing shown that it is faster than Parlot, and it handles errors automatically, but it does not have global skip-tokens in the combinator style.

### Pidgin

Pidgin is a memory-efficient and fast parser combinator library for .NET with some kind of incremental parsing support. It was created before Parlot and supports streams of any type of input, even binary. It supports LINQ-based syntax for creating parsers, but needs to manually place skip parsers in everything. 

### Sprache and Superpower

The legacy parser combinators for .NET, came out more than 10 years ago, but somewhat unefficient in performance, especially memory usage. But Sprache has the most readable API comparing than Pidgin and Parlot (in my opinion). Superpower is a more modern alternative to Sprache, but uses lexer-based approach and requires more amount of code.

## Why RCParsing outstands

It designed to be a more convenient than other libraries, and later it been optimized and now it has a better performance than other libraries. It also supports both modes: AST and immediate calculations, or them together. RCParsing can produce stack traces for errors, recover from them, and soon will support walk traces and incremental parsing (in the next update).

# Benchmarks

All benchmarks are done via `BenchmarkDotNet`.

Here is machine and runtime information:
```
BenchmarkDotNet v0.15.2, Windows 10 (10.0.19045.3448/22H2/2022Update)
AMD Ryzen 5 5600 3.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.302
  [Host]     : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
  Job-KTXINV : .NET 8.0.18 (8.0.1825.31117), X64 RyuJIT AVX2
```

## JSON

The JSON value calculation with the typeset `Dictionary<string, object>`, `object[]`, `string`, `int` and `null`.

| Method                               | Mean           | Error        | StdDev      | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|------------------------------------- |---------------:|-------------:|------------:|------:|--------:|---------:|--------:|----------:|------------:|
| JsonBig_RCParsing                    |   164,590.7 ns |    996.83 ns |   355.48 ns |  1.00 |    0.00 |  13.1836 |  4.1504 |  222264 B |        1.00 |
| JsonBig_RCParsing_Optimized          |    99,452.6 ns |    730.99 ns |   324.57 ns |  0.60 |    0.00 |   9.2773 |  2.0752 |  156640 B |        0.70 |
| JsonBig_RCParsing_TokenCombination   |    29,387.6 ns |    330.30 ns |   117.79 ns |  0.18 |    0.00 |   2.5635 |  0.1831 |   43096 B |        0.19 |
| JsonBig_SystemTextJson               |    12,100.5 ns |     38.82 ns |    13.84 ns |  0.07 |    0.00 |   0.5035 |  0.0153 |    8648 B |        0.04 |
| JsonBig_NewtonsoftJson               |    52,827.6 ns |    494.00 ns |   219.34 ns |  0.32 |    0.00 |   4.7607 |  0.9766 |   80176 B |        0.36 |
| JsonBig_ANTLR                        |   189,947.9 ns |  1,218.53 ns |   541.04 ns |  1.15 |    0.00 |  19.5313 |  7.5684 |  330584 B |        1.49 |
| JsonBig_Parlot                       |    41,206.8 ns |    105.96 ns |    47.05 ns |  0.25 |    0.00 |   1.9531 |  0.1221 |   32848 B |        0.15 |
| JsonBig_Pidgin                       |   208,721.8 ns |    418.89 ns |   185.99 ns |  1.27 |    0.00 |   3.9063 |  0.2441 |   66816 B |        0.30 |
| JsonBig_Superpower                   | 1,184,573.2 ns |  3,998.11 ns | 1,425.76 ns |  7.20 |    0.02 |  39.0625 |  5.8594 |  653627 B |        2.94 |
| JsonBig_Sprache                      | 1,213,870.0 ns | 17,241.27 ns | 7,655.23 ns |  7.38 |    0.05 | 232.4219 | 27.3438 | 3899736 B |       17.55 |
|                                      |                |              |             |       |         |          |         |           |             |
| JsonShort_RCParsing                  |     9,207.7 ns |    104.64 ns |    46.46 ns |  1.00 |    0.01 |   0.6409 |       - |   10920 B |        1.00 |
| JsonShort_RCParsing_Optimized        |     5,397.9 ns |     32.37 ns |    14.37 ns |  0.59 |    0.00 |   0.5264 |  0.0076 |    8904 B |        0.82 |
| JsonShort_RCParsing_TokenCombination |     1,515.0 ns |     14.39 ns |     6.39 ns |  0.16 |    0.00 |   0.1354 |       - |    2280 B |        0.21 |
| JsonShort_SystemTextJson             |       814.8 ns |     12.44 ns |     5.52 ns |  0.09 |    0.00 |   0.0401 |       - |     672 B |        0.06 |
| JsonShort_NewtonsoftJson             |     3,126.4 ns |     31.03 ns |    13.78 ns |  0.34 |    0.00 |   0.3891 |       - |    6552 B |        0.60 |
| JsonShort_ANTLR                      |    10,106.1 ns |     51.06 ns |    18.21 ns |  1.10 |    0.01 |   1.1444 |  0.0305 |   19360 B |        1.77 |
| JsonShort_Parlot                     |     2,271.2 ns |     10.79 ns |     4.79 ns |  0.25 |    0.00 |   0.1144 |       - |    1960 B |        0.18 |
| JsonShort_Pidgin                     |    11,160.6 ns |     22.50 ns |     9.99 ns |  1.21 |    0.01 |   0.2136 |       - |    3664 B |        0.34 |
| JsonShort_Superpower                 |    66,312.3 ns |    240.99 ns |    85.94 ns |  7.20 |    0.04 |   1.9531 |       - |   34117 B |        3.12 |
| JsonShort_Sprache                    |    63,855.1 ns |    858.85 ns |   306.27 ns |  6.94 |    0.04 |  12.6953 |  0.2441 |  213168 B |       19.52 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `UseFirstCharacterMatch()`, `IgnoreErrors()` and `SkipWhitespacesOptimized()` settings.
- `RCParsing_TokenCombination` uses complex manual tokens with immediate transformations instead of rules, and `UseFirstCharacterMatch()` setting.
- `Parlot` uses `Compiled()` version of parser.
- `JsonShort` methods uses ~20 lines of hardcoded (not generated) JSON with simple content.
- `JsonBig` methods uses ~180 lines of hardcoded (not generated) JSON with various content (deep, long objects/arrays).

## Expressions

The `int` value calculation from expression with parentheses `()`, spaces and operators `+-/*` with priorities.

| Method                                     | Mean         | Error       | StdDev      | Ratio | RatioSD | Gen0    | Gen1    | Allocated | Alloc Ratio |
|------------------------------------------- |-------------:|------------:|------------:|------:|--------:|--------:|--------:|----------:|------------:|
| ExpressionBig_RCParsing                    | 258,484.1 ns | 4,221.70 ns |   653.31 ns |  1.00 |    0.00 | 23.4375 | 11.2305 |  399704 B |        1.00 |
| ExpressionBig_RCParsing_Optimized          | 179,193.0 ns |   622.43 ns |    96.32 ns |  0.69 |    0.00 | 19.7754 |  8.3008 |  334080 B |        0.84 |
| ExpressionBig_RCParsing_TokenCombination   |  54,463.7 ns | 1,455.89 ns |   225.30 ns |  0.21 |    0.00 |  4.1504 |  0.0610 |   70288 B |        0.18 |
| ExpressionBig_Parlot                       |  63,761.8 ns |   339.88 ns |    88.27 ns |  0.25 |    0.00 |  3.2959 |       - |   56608 B |        0.14 |
| ExpressionBig_Pidgin                       | 700,906.7 ns | 7,006.51 ns | 1,819.57 ns |  2.71 |    0.01 |  0.9766 |       - |   23540 B |        0.06 |
|                                            |              |             |             |       |         |         |         |           |             |
| ExpressionShort_RCParsing                  |   2,310.8 ns |    34.09 ns |     8.85 ns |  1.00 |    0.00 |  0.2174 |       - |    3696 B |        1.00 |
| ExpressionShort_RCParsing_Optimized        |   1,638.7 ns |    22.90 ns |     5.95 ns |  0.71 |    0.00 |  0.2117 |       - |    3544 B |        0.96 |
| ExpressionShort_RCParsing_TokenCombination |     459.0 ns |     4.37 ns |     1.14 ns |  0.20 |    0.00 |  0.0391 |       - |     656 B |        0.18 |
| ExpressionShort_Parlot                     |     603.4 ns |     5.78 ns |     1.50 ns |  0.26 |    0.00 |  0.0534 |       - |     896 B |        0.24 |
| ExpressionShort_Pidgin                     |   6,655.6 ns |   256.88 ns |    66.71 ns |  2.88 |    0.03 |  0.0153 |       - |     344 B |        0.09 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `IgnoreErrors()` and `SkipWhitespacesOptimized()` settings.
- `RCParsing_TokenCombination` uses complex manual tokens with immediate transformations instead of rules, and `UseFirstCharacterMatch()` setting.
- `Parlot` uses `Compiled()` version of parser.
- `ExpressionShort` methods uses single line with 4 operators of hardcoded (not generated) expression.
- `ExpressionBig` methods uses single line with ~400 operators of hardcoded (not generated) expression.

## Regex

Matching identifiers and emails in the plain text.

| Method                               | Mean         | Error        | StdDev      | Ratio | RatioSD | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------------------------- |-------------:|-------------:|------------:|------:|--------:|--------:|-------:|----------:|------------:|
| EmailsBig_RCParsing                  | 236,175.3 ns | 26,801.07 ns | 6,960.15 ns |  1.00 |    0.04 |  0.9766 |      - |   16568 B |        1.00 |
| EmailsBig_RCParsing_Optimized        | 157,271.9 ns |  5,076.92 ns | 1,318.46 ns |  0.67 |    0.02 |  0.9766 |      - |   16568 B |        1.00 |
| EmailsBig_Regex                      |  27,638.6 ns |    711.08 ns |   184.66 ns |  0.12 |    0.00 |  1.5564 | 0.1221 |   26200 B |        1.58 |
|                                      |              |              |             |       |         |         |        |           |             |
| EmailsShort_RCParsing                |   6,658.5 ns |     78.57 ns |    20.40 ns |  1.00 |    0.00 |  0.0916 |      - |    1600 B |        1.00 |
| EmailsShort_RCParsing_Optimized      |   3,799.0 ns |     35.69 ns |     5.52 ns |  0.57 |    0.00 |  0.0954 |      - |    1600 B |        1.00 |
| EmailsShort_Regex                    |     931.5 ns |     13.52 ns |     3.51 ns |  0.14 |    0.00 |  0.0601 |      - |    1008 B |        0.63 |
|                                      |              |              |             |       |         |         |        |           |             |
| IdentifiersBig_RCParsing             | 158,034.1 ns |  4,041.56 ns |   625.44 ns |  1.00 |    0.01 |  5.8594 |      - |  101664 B |        1.00 |
| IdentifiersBig_RCParsing_Optimized   |  99,086.9 ns |  1,619.80 ns |   420.66 ns |  0.63 |    0.00 |  5.9814 |      - |  101664 B |        1.00 |
| IdentifiersBig_Regex                 |  71,439.8 ns |  4,727.93 ns |   731.65 ns |  0.45 |    0.00 | 11.1084 | 3.6621 |  187248 B |        1.84 |
|                                      |              |              |             |       |         |         |        |           |             |
| IdentifiersShort_RCParsing           |   4,041.5 ns |    172.86 ns |    44.89 ns |  1.00 |    0.01 |  0.2518 |      - |    4240 B |        1.00 |
| IdentifiersShort_RCParsing_Optimized |   2,930.9 ns |     56.37 ns |    14.64 ns |  0.73 |    0.01 |  0.2518 |      - |    4240 B |        1.00 |
| IdentifiersShort_Regex               |   2,386.2 ns |    160.57 ns |    41.70 ns |  0.59 |    0.01 |  0.3624 | 0.0076 |    6104 B |        1.44 |


Notes:

- `RCParsing` uses naive pattern for matching, without any optimization settings applied.
- `RCParsing_Optimized` uses the same pattern, but with configured skip-rule for making it faster.
- `Regex` uses `RegexOptions.Compiled` flags.
- `Identifiers` pattern is `[a-zA-Z_][a-zA-Z0-9_]*`.
- `Emails` pattern is `[a-zA-Z0-9]+@[a-zA-Z0-9]+\.[a-zA-Z0-9]+`.

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
- ***Incremental parsing:*** Parsing only changed parts in the middle of text that will be good for IDE and LSP (Language Server Protocol).
- ***Cosmic levels of debug:*** Very detailed parse walk traces, showing the order of what was parsed with success/fail status.

# Contributing

### Contributions are welcome!

This framework is born recently (2 months ago) and some little features may not be tested and be buggy.

If you have an idea about this project, you can report it to `Issues`.  
For contributing code, please fork the repository and make your changes in a new branch. Once you're ready, create a pull request to merge your changes into the main branch. Pull requests should include a clear description of what was changed and why.
