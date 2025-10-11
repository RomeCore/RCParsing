# RCParsing

[![Build](https://github.com/RomeCore/RCParsing/actions/workflows/build.yml/badge.svg)](https://github.com/RomeCore/RCParsing/actions/workflows/build.yml)
[![Tests](https://github.com/RomeCore/RCParsing/actions/workflows/tests.yml/badge.svg)](https://github.com/RomeCore/RCParsing/actions/workflows/tests.yml)
[![Auto Release](https://github.com/RomeCore/RCParsing/actions/workflows/auto-release.yml/badge.svg)](https://github.com/RomeCore/RCParsing/actions/workflows/auto-release.yml)
[![NuGet](https://img.shields.io/nuget/v/RCParsing.svg?style=flat&label=NuGet:%20RCParsing)](https://www.nuget.org/packages/RCParsing/)
[![License](https://img.shields.io/github/license/RomeCore/RCParsing.svg)](https://github.com/RomeCore/RCParsing/blob/main/LICENSE)
[![Star me on GitHub](https://img.shields.io/github/stars/RomeCore/RCParsing.svg?style=social&label=Star%20Me)](https://github.com/RomeCore/RCParsing)

**RCParsing - the fluent, lightweight and powerful .NET lexerless parsing library for language development (DSL) and data scraping.**

This library focuses on **Developer-experience (DX)** first, providing best toolkit for creating your **programming languages**, **file formats** or even **data extraction tools** with declarative API, debugging tools, and more. This allows you to design your parser directly in code and easily fix it using stack and walk traces with detailed error messages.

**Here is some useful links:** 

- [Home page of the project](https://romecore.github.io/RCParsing/)
- [Web demo/playground](https://romecore.github.io/RCParsing.WebDemo/)

## Why RCParsing?

- 🐍 **Hybrid Power**: Unique support for **barrier tokens** to parse indent-sensitive languages like Python and YAML.
- ☄️ **Incremental Parsing**: Edit large documents with instant feedback. Our persistent AST enables efficient re-parsing of only changed sections, perfect for LSP servers and real-time editing scenarios.
- 💪 **Regex on Steroids**: You can find all matches for target structure in the input text with detailed AST information and transformed value.
- 🌀 **Lexerless Freedom**: No token priority headaches. Parse directly from raw text, even with keywords embedded in identifiers. Tokens are used just as lightweight matching primitives.
- 🎨 **Fluent API**: Write parsers in C# that read like clean BNF grammars, boosting readability and maintainability compared to imperative, functional or code-generation approaches.
- 🧩 **Combinator Style**: Unlock maximum performance by defining complex tokens with immediate value transformation, bypassing the AST construction entirely for a direct, allocation-free result. Perfect for high-speed parsing of well-defined formats. Also can be used with AST mode.
- 🐛 **Superior Debugging**: Get detailed, actionable error messages with stack traces, walk traces and precise source locations. Richest API for manual error information included.
- 🚑 **Error Recovery**: Define custom recovery strategies per rule to handle syntax errors and go further.
- ⚡ **Blazing Fast**: Performance is now on par with the fastest .NET parsing libraries, even with most complex grammars (see benchmarks below).
- 🌳 **Rich AST**: Parser makes an AST (Abstract Syntax Tree) from raw text, with ability to optimize, fully analyze and calculate the result value entirely lazy, reducing unnecessary allocations.
- 🔧 **Configurable Skipping**: Advanced strategies for whitespace and comments, allowing you to use conflicting tokens in your main rules.
- 📦 **Batteries Included**: Useful built-in tokens and rules (regex, identifiers, numbers, escaped strings, separated lists, custom tokens, and more...).
- 🖥️ **Broad Compatibility**: Targets `.NET Standard 2.0` (runs on `.NET Framework 4.6.1+`), `.NET 6.0`, and `.NET 8.0`.

# Table of contents

- [Installation](#installation)
- [Tutorials, docs and examples](#tutorials-docs-and-examples)
- [Simple examples](#simple-examples) - The examples that you can copy, paste, run or look!
	- [A + B](#a--b) - Basic arithmetic expression parser with result calculation.
	- [JSON (with incremental parsing)](#json-with-incremental-parsing) - A complete JSON parser with comments and skipping (with incremental parsing example included).
	- [Python-like](#python-like) - Demonstrating barrier tokens for indentation.
	- [JSON token combination](#json-token-combination) - A maximum speed approach for getting values without AST or just to validate inputs with zero-overhead.
	- [Finding patterns](#finding-patterns) - How to find all occurrences of a rule in a string.
	- [Errors example](#errors-example) - Just a simple example of how errors look in default and debug modes.
- [Comparison with other parsing libraries](#comparison-with-other-parsing-libraries)
- [Benchmarks](#benchmarks)
	- [JSON AST](#json-ast) - Comparing JSON parsing with ANTLR, uses JSON parser with default rule-based style.
	- [JSON Combinators](#json-combinators) - Comparing JSON parsing across combinators, uses parser with token combination style for maximum speed.
	- [Expressions](#expressions) - Calculating expressions with '+-*/' operators with precedence rules.
	- [Regex](#regex) - Finding identifiers and emails in plain text using regex-like `FindAllMatches` feature.
	- [Python](#python) - Parsing entire Python 3.13.
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

- [Tutorials](https://romecore.github.io/RCParsing/guide/) - The tutorial website, not completely migrated from the markdown version yet.
- [Tutorials (old)](https://github.com/RomeCore/RCParsing/blob/main/docs_md/tutorials.md) - Detailed tutorials in the Markdown file, explaining features and mechanics of this library.
- [Syntax colorizer](https://github.com/RomeCore/RCParsing/tree/main/samples/SyntaxColorizer) - The syntax colorizer sample that automatically colorizes text based on provided parser.
- [Math calculator](https://github.com/RomeCore/RCParsing/tree/main/samples/MathCalculator) - Math expression evaluator with support of power, math functions and constants.
- [ANTLR to RCParsing converter](https://github.com/RomeCore/RCParsing/tree/main/samples/ANTLRToRCParsingConverter) - Simple tool for generating RCParsing API code from ANTLR rules.
- [Tests Library](https://github.com/RomeCore/RCParsing/tree/main/tests/RCParsing.Tests) - The tests directory that contains tests for various things, including C, GraphQL and Python.

# Simple examples

## A + B

Here is simple example how to make simple parser that parses "a + b" string with numbers and transforms the result:

```csharp
using RCParsing;

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
        var op = v.GetValue<string>(1);
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

## JSON (with incremental parsing)

And here is JSON example that also shows the partial re-parsing of parse tree:

```csharp
var builder = new ParserBuilder();

// Configure AST type and skip-rule for whitespace and comments 
builder.Settings
	.Skip(r => r.Rule("skip"), ParserSkippingStrategy.SkipBeforeParsingGreedy)
	.UseLazyAST(); // Use lazy AST type to store cached resuls

// The rule that will be skipped before every parsing attempt
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
	); // Choice rule propagates child's value by default

builder.CreateRule("array")
	.Literal("[")
	.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","),
		allowTrailingSeparator: true, includeSeparatorsInResult: false)
		.TransformLast(v => v.SelectArray())
	.Literal("]")
	.TransformSelect(index: 1); // Selects the Children[1]'s value

builder.CreateRule("object")
	.Literal("{")
	.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","),
		allowTrailingSeparator: true, includeSeparatorsInResult: false)
		.TransformLast(v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary(k => k.Key, v => v.Value))
	.Literal("}")
	.TransformSelect(index: 1);

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

// The same JSON, but with 'tags' value changed
var changedJson =
"""
{
	"id": 1,
	"name": "Sample Data",
	"created": "2023-01-01T00:00:00", // This is a comment
	"tags": { "nested": ["tag1", "tag2", "tag3"] },
	"isActive": true,
	"nested": {
		"value": 123.456,
		"description": "Nested description"
	}
}
""";

// Parse the input text and calculate values (them will be recorded into the cache because we're using lazy AST)
var ast = jsonParser.Parse(json);
var value = ast.Value as Dictionary<string, object>;
var tags = value!["tags"] as object[];
var nested = value!["nested"] as Dictionary<string, object>;

// Prints: Sample Data
Console.WriteLine(value["name"]);
// Prints: tag1
Console.WriteLine(tags![0]);

// Re-parse the sligtly changed input string and get the values
var changedAst = ast.Reparsed(changedJson);
var changedValue = changedAst.Value as Dictionary<string, object>;
var changedTags = changedValue!["tags"] as Dictionary<string, object>;
var nestedTags = changedTags!["nested"] as object[];
var changedNested = changedValue!["nested"] as Dictionary<string, object>;

// Prints type: System.Object[]
Console.WriteLine(changedTags["nested"]);
// Prints: tag1
Console.WriteLine(nestedTags![0]);

// And untouched values remains the same!
// Prints: True
Console.WriteLine(ReferenceEquals(nested, changedNested));
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

var invalidJson =
"""
{
	"id": 1,
	"name": "Sample Data",
	"created": "2023-01-01T00:00:00",
	"tags": ["tag1", "tag2", "tag3"],,
	"isActive": true,
	"nested": {
		"value": 123.456,
		"description": "Nested description"
	}
}
""";

// Retrieve the furthest error
var error = parser.TryMatchToken("value", invalidJson).Context.CreateErrorGroups().Last!;
Console.WriteLine(error.Column); // 35
Console.WriteLine(error.Line);   // 5

// Also you can check if the input matches token the fastest way, without value calculation:
Console.WriteLine(parser.MatchesToken("value", "[90, 60, true, null]", out int matchedLength)); // true
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

## Errors example

There is how errors are displayed in the default mode:

```
RCParsing.ParsingException : An error occurred during parsing:

The line where the error occurred (position 130):
	"tags": ["tag1", "tag2", "tag3"],,
                   line 5, column 35 ^

',' is unexpected character, expected one of:
  'string'
  literal '}'

... and more errors omitted
```

And there is errors when using the `builder.Settings.UseDebug()` setting:

```
RCParsing.ParsingException : An error occurred during parsing:

['string']: Failed to parse token.
['pair']: Failed to parse sequence rule.
[literal '}']: Failed to parse token.
['object']: Failed to parse sequence rule.

The line where the error occurred (position 130):
	"tags": ["tag1", "tag2", "tag3"],,
                   line 5, column 35 ^

',' is unexpected character, expected one of:
  'string'
  'pair'
  literal '}'
  'object'

['string'] Stack trace (top call recently):
- Sequence 'pair':
    'string' <-- here
    literal ':'
    'value'
- SeparatedRepeat[0..] (allow trailing): 'pair' <-- here
  sep literal ','
- Sequence 'object':
    literal '{'
    SeparatedRepeat[0..] (allow trailing)... <-- here
    literal '}'
- Choice 'value':
    'string'
    'number'
    'boolean'
    'null'
    'array'
    'object' <-- here
- Sequence 'content':
    'value' <-- here
    end of file

[literal '}'] Stack trace (top call recently):
- Sequence 'object':
    literal '{'
    SeparatedRepeat[0..] (allow trailing)...
    literal '}' <-- here
- Choice 'value':
    'string'
    'number'
    'boolean'
    'null'
    'array'
    'object' <-- here
- Sequence 'content':
    'value' <-- here
    end of file

... and more errors omitted

Walk Trace:

... 316 hidden parsing steps. Total: 356 ...
[ENTER]   pos:128   literal '//'
[FAIL]    pos:128   literal '//' failed to match: '],,\r\n\t"isActive...'
[FAIL]    pos:128   Sequence... failed to match: '],,\r\n\t"isActive...'
[FAIL]    pos:128   'skip' failed to match: '],,\r\n\t"isActive...'
[ENTER]   pos:128   literal ','
[FAIL]    pos:128   literal ',' failed to match: '],,\r\n\t"isActive...'
[SUCCESS] pos:106   SeparatedRepeat[0..] (allow trailing)... matched: '"tag1", "tag2", "tag3"' [22 chars]
[ENTER]   pos:128   literal ']'
[SUCCESS] pos:128   literal ']' matched: ']' [1 chars]
[SUCCESS] pos:105   'array' matched: '["tag1", "tag2", "tag3"]' [24 chars]
[SUCCESS] pos:105   'value' matched: '["tag1", "tag2", "tag3"]' [24 chars]
[SUCCESS] pos:97    'pair' matched: '"tags": ["tag1" ..... ", "tag3"]' [32 chars]
[ENTER]   pos:129   'skip'
[ENTER]   pos:129   whitespaces
[FAIL]    pos:129   whitespaces failed to match: ',,\r\n\t"isActive"...'
[ENTER]   pos:129   Sequence...
[ENTER]   pos:129   literal '//'
[FAIL]    pos:129   literal '//' failed to match: ',,\r\n\t"isActive"...'
[FAIL]    pos:129   Sequence... failed to match: ',,\r\n\t"isActive"...'
[FAIL]    pos:129   'skip' failed to match: ',,\r\n\t"isActive"...'
[ENTER]   pos:129   literal ','
[SUCCESS] pos:129   literal ',' matched: ',' [1 chars]
[ENTER]   pos:130   'skip'
[ENTER]   pos:130   whitespaces
[FAIL]    pos:130   whitespaces failed to match: ',\r\n\t"isActive":...'
[ENTER]   pos:130   Sequence...
[ENTER]   pos:130   literal '//'
[FAIL]    pos:130   literal '//' failed to match: ',\r\n\t"isActive":...'
[FAIL]    pos:130   Sequence... failed to match: ',\r\n\t"isActive":...'
[FAIL]    pos:130   'skip' failed to match: ',\r\n\t"isActive":...'
[ENTER]   pos:130   'pair'
[ENTER]   pos:130   'string'
[FAIL]    pos:130   'string' failed to match: ',\r\n\t"isActive":...'
[FAIL]    pos:130   'pair' failed to match: ',\r\n\t"isActive":...'
[SUCCESS] pos:4     SeparatedRepeat[0..] (allow trailing)... matched: '"id": 1,\r\n\t"nam ..... , "tag3"],' [126 chars]
[ENTER]   pos:130   literal '}'
[FAIL]    pos:130   literal '}' failed to match: ',\r\n\t"isActive":...'
[FAIL]    pos:0     'object' failed to match: '{\r\n\t"id": 1,\r\n\t...'
[FAIL]    pos:0     'value' failed to match: '{\r\n\t"id": 1,\r\n\t...'
[FAIL]    pos:0     'content' failed to match: '{\r\n\t"id": 1,\r\n\t...'

... End of walk trace ...
```

# Comparison with Other Parsing Libraries

`RCParsing` is designed to outstand with unique features, and **easy** developer experience, but it performance is good enough to compete with other fastest parser tools.

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

## JSON AST

The JSON value calculation with the typeset `Dictionary<string, object>`, `object[]`, `string`, `int` and `null`. It uses visitors to transform value from AST (Abstract Syntax Tree).

| Method                                       | Mean           | Error        | StdDev      | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|--------------------------------------------- |---------------:|-------------:|------------:|------:|--------:|---------:|--------:|----------:|------------:|
| JsonBig_RCParsing                            |   204,608.9 ns | 12,662.21 ns | 5,622.10 ns |  1.00 |    0.04 |  13.1836 |  3.9063 |  221880 B |        1.00 |
| JsonBig_RCParsing_NoValue                    |   159,211.6 ns |  2,989.65 ns | 1,066.14 ns |  0.78 |    0.02 |   8.0566 |  2.4414 |  138016 B |        0.62 |
| JsonBig_RCParsing_Optimized                  |    93,754.2 ns |    809.58 ns |   359.46 ns |  0.46 |    0.01 |   9.2773 |  2.0752 |  156256 B |        0.70 |
| JsonBig_RCParsing_Optimized_NoValue          |    75,500.3 ns | 17,682.37 ns | 7,851.08 ns |  0.37 |    0.04 |   4.2725 |  0.8545 |   72392 B |        0.33 |
| JsonBig_ANTLR                                |   183,579.8 ns |  2,031.71 ns |   902.09 ns |  0.90 |    0.02 |  19.5313 |  7.5684 |  330584 B |        1.49 |
| JsonBig_ANTLR_NoValue                        |   122,045.1 ns |    708.53 ns |   314.59 ns |  0.60 |    0.02 |  10.7422 |  4.1504 |  180232 B |        0.81 |
|                                              |                |              |             |       |         |          |         |           |             |
| JsonShort_RCParsing                          |    10,349.6 ns |    124.24 ns |    44.30 ns |  1.00 |    0.01 |   0.6409 |       - |   10936 B |        1.00 |
| JsonShort_RCParsing_NoValue                  |     8,377.9 ns |    112.15 ns |    49.79 ns |  0.81 |    0.01 |   0.3815 |       - |    6536 B |        0.60 |
| JsonShort_RCParsing_Optimized                |     5,349.1 ns |     51.37 ns |    18.32 ns |  0.52 |    0.00 |   0.5264 |  0.0076 |    8920 B |        0.82 |
| JsonShort_RCParsing_Optimized_NoValue        |     3,409.7 ns |     21.47 ns |     9.53 ns |  0.33 |    0.00 |   0.2670 |       - |    4520 B |        0.41 |
| JsonShort_ANTLR                              |    10,132.4 ns |    113.86 ns |    40.60 ns |  0.98 |    0.01 |   1.1444 |  0.0305 |   19360 B |        1.77 |
| JsonShort_ANTLR_NoValue                      |     6,667.6 ns |     44.08 ns |    19.57 ns |  0.64 |    0.00 |   0.6332 |  0.0229 |   10600 B |        0.97 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `UseFirstCharacterMatch()`, `IgnoreErrors()` and `SkipWhitespacesOptimized()` settings.
- `*_NoValue` methods does not calculates a value.
- `JsonShort` methods uses ~20 lines of hardcoded (not generated) JSON with simple content.
- `JsonBig` methods uses ~180 lines of hardcoded (not generated) JSON with various content (deep, long objects/arrays).

## JSON Combinators

The JSON value calculation with the typeset `Dictionary<string, object>`, `object[]`, `string`, `int` and `null`. It uses token combination style for immediate transformations without AST.

| Method                                       | Mean           | Error        | StdDev      | Ratio | RatioSD | Gen0     | Gen1    | Allocated | Alloc Ratio |
|--------------------------------------------- |---------------:|-------------:|------------:|------:|--------:|---------:|--------:|----------:|------------:|
| JsonBig_RCParsing                            |   204,608.9 ns | 12,662.21 ns | 5,622.10 ns |  1.00 |    0.04 |  13.1836 |  3.9063 |  221880 B |        1.00 |
| JsonBig_RCParsing_NoValue                    |   159,211.6 ns |  2,989.65 ns | 1,066.14 ns |  0.78 |    0.02 |   8.0566 |  2.4414 |  138016 B |        0.62 |
| JsonBig_RCParsing_TokenCombination           |    28,764.1 ns |    396.91 ns |   176.23 ns |  0.14 |    0.00 |   2.5635 |  0.1831 |   43096 B |        0.19 |
| JsonBig_RCParsing_TokenCombination_NoValue   |    17,508.8 ns |     84.55 ns |    37.54 ns |  0.09 |    0.00 |   0.4883 |       - |    8312 B |        0.04 |
| JsonBig_Parlot                               |    41,634.4 ns |    232.42 ns |   103.20 ns |  0.20 |    0.01 |   1.9531 |  0.1221 |   32848 B |        0.15 |
| JsonBig_Pidgin                               |   197,369.0 ns |    617.13 ns |   274.01 ns |  0.97 |    0.02 |   3.9063 |  0.2441 |   66816 B |        0.30 |
| JsonBig_Superpower                           | 1,184,126.8 ns |  8,091.53 ns | 3,592.69 ns |  5.79 |    0.15 |  39.0625 |  5.8594 |  653627 B |        2.95 |
| JsonBig_Sprache                              | 1,146,488.4 ns |  6,118.19 ns | 2,716.52 ns |  5.61 |    0.14 | 232.4219 | 27.3438 | 3899736 B |       17.58 |
|                                              |                |              |             |       |         |          |         |           |             |
| JsonShort_RCParsing                          |    10,349.6 ns |    124.24 ns |    44.30 ns |  1.00 |    0.01 |   0.6409 |       - |   10936 B |        1.00 |
| JsonShort_RCParsing_NoValue                  |     8,377.9 ns |    112.15 ns |    49.79 ns |  0.81 |    0.01 |   0.3815 |       - |    6536 B |        0.60 |
| JsonShort_RCParsing_TokenCombination         |     1,503.5 ns |     18.96 ns |     8.42 ns |  0.15 |    0.00 |   0.1354 |       - |    2280 B |        0.21 |
| JsonShort_RCParsing_TokenCombination_NoValue |       980.6 ns |     16.23 ns |     7.21 ns |  0.09 |    0.00 |   0.0324 |       - |     568 B |        0.05 |
| JsonShort_Parlot                             |     2,224.6 ns |      9.00 ns |     4.00 ns |  0.21 |    0.00 |   0.1144 |       - |    1960 B |        0.18 |
| JsonShort_Pidgin                             |    10,881.9 ns |    216.14 ns |    95.97 ns |  1.05 |    0.01 |   0.2136 |       - |    3664 B |        0.34 |
| JsonShort_Superpower                         |    66,092.8 ns |    241.35 ns |    86.07 ns |  6.39 |    0.03 |   1.9531 |       - |   34117 B |        3.12 |
| JsonShort_Sprache                            |    62,895.2 ns |    496.98 ns |   220.66 ns |  6.08 |    0.03 |  12.6953 |  0.2441 |  213168 B |       19.49 |

Notes:

- `RCParsing_TokenCombination` uses complex manual tokens with immediate transformations instead of rules, and `UseFirstCharacterMatch()` setting.
- `Parlot` uses `Compiled()` version of parser.
- `*_NoValue` methods does not calculates a value.
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

## GraphQL

Just GraphQL parsing without transformations from AST. GraphQL is a mid-complex language that can be described in 600 lines of ANTLR's version of BNF notation. 

| Method                                 | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1    | Gen2    | Allocated  | Alloc Ratio |
|--------------------------------------- |------------:|----------:|----------:|------:|--------:|---------:|--------:|--------:|-----------:|------------:|
| QueryBig_RCParsing_Default             | 1,645.29 us |  6.565 us |  2.915 us |  1.00 |    0.00 |  27.3438 |  9.7656 |  1.9531 |  554.86 KB |        1.00 |
| QueryBig_RCParsing_Optimized           |   453.46 us | 18.405 us |  8.172 us |  0.28 |    0.00 |  18.0664 |  4.3945 |       - |  295.82 KB |        0.53 |
| QueryBig_ANTLR                         | 1,200.23 us |  7.136 us |  2.545 us |  0.73 |    0.00 |  35.1563 | 11.7188 |       - |  590.55 KB |        1.06 |
|                                        |             |           |           |       |         |          |         |         |            |             |
| QueryShort_RCParsing_Default           |   162.92 us |  0.479 us |  0.171 us |  1.00 |    0.00 |   4.1504 |  0.4883 |       - |   69.15 KB |        1.00 |
| QueryShort_RCParsing_Optimized         |    47.14 us |  0.347 us |  0.154 us |  0.29 |    0.00 |   2.0752 |  0.1221 |       - |   34.86 KB |        0.50 |
| QueryShort_ANTLR                       |    66.79 us |  0.569 us |  0.253 us |  0.41 |    0.00 |   5.9814 |  0.7324 |       - |    99.2 KB |        1.43 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `IgnoreErrors()` and `UseFirstCharacterMatch()` settings.
- `RCParsing` grammar was ported from this [ANTLR Grammar](https://github.com/antlr/grammars-v4/blob/master/graphql/GraphQL.g4).
- `QueryShort` methods uses ~40 lines of hardcoded (not generated) GraphQL query.
- `QueryBig` methods uses ~400 lines of hardcoded (not generated) GraphQL query with various content (all syntax structures, long and deep queries).

## Python

Yes, seriously, the entire Python 3.13 parsing, without transformations from AST. Involves barrier tokens for RCParsing and custom lexer for ANTLR.

| Method                                  | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated   | Alloc Ratio |
|---------------------------------------- |------------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|------------:|------------:|
| PythonBig_RCParsing_Default             | 36,722.4 us | 638.32 us | 283.42 us |  1.00 |    0.01 | 384.6154 | 307.6923 | 153.8462 | 37397.21 KB |        1.00 |
| PythonBig_RCParsing_Optimized           |  6,133.8 us |  11.42 us |   4.07 us |  0.17 |    0.00 | 218.7500 | 109.3750 |        - |  3863.07 KB |        0.10 |
| PythonBig_RCParsing_Memoized            |          NA |        NA |        NA |     ? |       ? |       NA |       NA |       NA |          NA |           ? |
| PythonBig_RCParsing_MemoizedOptimized   |          NA |        NA |        NA |     ? |       ? |       NA |       NA |       NA |          NA |           ? |
| PythonBig_ANTLR                         |  5,673.8 us |  18.01 us |   8.00 us |  0.15 |    0.00 | 406.2500 | 281.2500 |        - |  6699.11 KB |        0.18 |
|                                         |             |           |           |       |         |          |          |          |             |             |
| PythonShort_RCParsing_Default           |  3,696.3 us |  37.07 us |  16.46 us |  1.00 |    0.01 |  42.9688 |  23.4375 |   7.8125 |  2555.13 KB |        1.00 |
| PythonShort_RCParsing_Optimized         |    710.3 us |   7.08 us |   2.52 us |  0.19 |    0.00 |  27.3438 |   5.8594 |        - |   460.98 KB |        0.18 |
| PythonShort_RCParsing_Memoized          |  1,579.9 us |  49.83 us |  22.12 us |  0.43 |    0.01 |  31.2500 |  23.4375 |   5.8594 |  1336.86 KB |        0.52 |
| PythonShort_RCParsing_MemoizedOptimized |    765.3 us |  27.49 us |   9.80 us |  0.21 |    0.00 |  19.5313 |  18.5547 |   4.8828 |   570.82 KB |        0.22 |
| PythonShort_ANTLR                       |    550.1 us |   9.09 us |   3.24 us |  0.15 |    0.00 |  46.8750 |  12.6953 |        - |   780.65 KB |        0.31 |

Notes:

- `RCParsing` uses its default configuration, without any optimizations and settings applied.
- `RCParsing_Optimized` uses `UseInlining()`, `IgnoreErrors()` and `UseFirstCharacterMatch()` settings.
- `RCParsing` grammar was ported using this [ANTLR Grammar](https://github.com/antlr/grammars-v4/blob/master/graphql/GraphQL.g4) and [Python Reference Grammar](https://docs.python.org/3.13/reference/grammar.html).
- `PythonShort` methods uses ~20 lines of hardcoded (not generated) Python code, see [source](https://github.com/python/cpython/blob/3.13/Lib/antigravity.py).
- `PythonBig` methods uses ~430 lines of hardcoded (not generated) Python code, see [source](https://github.com/python/cpython/blob/3.13/Lib/fileinput.py).
- Memoization currently works bad with barrier tokens, we're working on it!

*More benchmarks will be later here...*

# Projects using RCParsing

- [RCLargeLangugeModels](https://github.com/RomeCore/RCLargeLanguageModels): Used for `LLT`, the template Razor-like language.

*Using RCParsing in your project? We'd love to feature it here! Submit a pull request to add your project to the list.*

# Roadmap

The future development of `RCParsing` is focused on:
- **Performance:** Continued profiling and optimization, especially for large files with deep structures.
- **API Ergonomics:** Introducing even more expressive and fluent methods (such as expression builder).
- **New Built-in Rules:** Adding common patterns (e.g., number with wide range of notations).
- **Visualization Tooling:** Exploring tools for debugging and visualizing resulting AST.
- **Grammar Transformers**: Builder extensions that can be used to optimize parsers, eliminate left recursion and more.
- **Semantic analysis**: Multi-stage tools that simplifies AST semantic analysis.
- **NFA Algorithm**: Adaptive parsing algorithm, which is more powerful for parsing complex rules.

# Contributing

### Contributions are welcome!

This framework is born recently (2 months ago) and some little features may not be tested and be buggy.

If you have an idea about this project, you can report it to `Issues`.  
For contributing code, please fork the repository and make your changes in a new branch. Once you're ready, create a pull request to merge your changes into the main branch. Pull requests should include a clear description of what was changed and why.
