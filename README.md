# RCParsing

A very flexible and readable .NET parser building library.

It provides a Fluent API to fully construct your own parser from grammar to object creation. Since this parsing library is lexerless, you don't need to prioritize your tokens and you can mix code with text that contains keywords, just using this library!

## Why RCParsing?
- Lexerless: no need to define token priorities, parse directly from raw text.
- Fluent API: build parsers in C# that look like BNF grammars.
- Configurable skipping: whitespace, comments, with strategies that allow use conflicting tokens in main rules.
- Useful built-in tokens and rules: regex, identifiers, escaped strings, separated lists, and more;
- Targets .NET Standard 2.0 and .NET 8.0.

# Installation

You can install the package via NuGet Package Manager:

```shell
dotnet add package RCParsing
```

Or via package manager console:

```powershell
Install-Package RCParsing
```

Or do it manually by cloning this repository.

# Getting started

Here is detailed example how to make simple parser that parses "a + b" string and transforms the result:

```csharp
using RCParsing;

// First, you need to create a builder
var builder = new ParserBuilder();

// Enable and configure the auto-skip (you can replace `Whitespaces` with any parser rule)
builder.Settings()
    .Skip(b => b.Whitespaces());

// Create the unicode identifier token
builder.CreateToken("identifier")
    .UnicodeIdentifier()
    .Transform(v => v.Text); // Transform the token as captured text

// Create the number token from regular expression that transforms to double
builder.CreateToken("number")
    .Regex(@"-?\d(?:\.\d+)?")
    .Transform(v => double.Parse(v.Text, CultureInfo.InvariantCulture));

// Create the choice value rule that matches either identifier or number token
builder.CreateRule("value")
    .Choice(
        b => b.Token("identifier"),
        b => b.Token("number"));

// Create a main sequential expression rule
builder.CreateRule("expression")
    .Rule("value")
    .LiteralChoice("+", "-")
    .Rule("value")
    .Transform(v => {
        var value1 = v.GetValue<object>(0);
        var op = v.Children[1].Text;
        var value2 = v.GetValue<object>(2);
        return $"value 1: {value1}; op: {op}; value 2: {value2};";
    });

// Build the parser
var parser = builder.Build();

// Parse a string using 'expression' rule and get the raw AST
var stringToParse = "abc + 123";
var parsedRule = parser.ParseRule("expression", stringToParse);

// We can now get the value from our 'Transform' functions
var transformedValue = parsedRule.GetValue<string>();

Console.WriteLine(transformedValue); // value 1: abc; op: +; value 2: 123;

{
    // Try to parse invalid expression
    parser.ParseRule("expression", "abc / 123"); 
}
catch (ParsingException ex)
{
    // '/' is unexpected character, expected literal choice '+/-'
    // The line where the error occurred:
    // ...
    Console.WriteLine(ex.Message);
}
```

# Basic tutorials

### Main concepts

`RCParsing` uses lexerless approach, but it has *tokens*, they are used as parser primitives, and they are not complex as *rules*.  
Parsers in this library is created via `ParserBuilder`s. When parser is being built, builder deduplicates rules and token patterns, and assigns IDs to them, then they are being compound into a `Parser`.

Here you can see deduplication example:
```csharp
var builder = new ParserBuilder();

// Create rule that matches literal 'abc' token
builder.CreateRule("rule1")
    .Literal("abc");

// Create another rule with same token
builder.CreateRule("rule2")
    .Literal("abc");

var parser = builder.Build();

// It's deduplicated!
Console.WriteLine(parser.GetRule("rule1").Id == parser.GetRule("rule2").Id); // true
```

### Auto-skipping

Parser does not skip any characters by default. If you want to configure auto-skipping, you can do it by configuring the parser builder:

```csharp
builder.Settings()
    .Skip(b => b.Whitespaces());
```

By default, if skip rule is set, parser will try to skip it before parsing every rule once. You can select the other skip strategy:

```csharp
builder.Settings()
    .Skip(b => b.Literal("//").TextUntil("\n", "\r"), ParserSkippingStrategy.SkipBeforeParsingLazy);
```

Here is detailed skip strategies example:

- `SkipBeforeParsing`: Parser will try to skip the skip-rule once before parsing the target rule.
- `SkipBeforeParsingLazy`: Parser will repeatedly attempt to skip the skip-rule in an interleaved manner with the target rule, until the target rule succeeds or both skip and target rules fail. This allows incremental consumption of skip content.
- `SkipBeforeParsingGreedy`: Parser will greedily skip the skip-rule as many times as possible before attempting to parse the target rule.
- `TryParseThenSkip`: Parser will first attempt to parse the target rule; if parsing fails, it will skip the skip-rule once and then retry parsing the target rule.
- `TryParseThenSkipLazy`: Parser will attempt to parse the target rule; if parsing fails, it will alternately try to skip the skip-rule and parse the target rule repeatedly until the target rule succeeds or both fail.
- `TryParseThenSkipGreedy`: Parser will attempt to parse the target rule; if parsing fails, it will greedily skip the skip-rule as many times as possible and then retry parsing the target rule.

# Comparison with other parser libraries

This library is designed to be easy and flexible, not fast and memory-efficient, but it can compete the other libraries by speed. It uses some layers of abstractions and may produce lots of intermediate errors and AST objects, and it impacts on memory usage.

Benchmark summary on JSON:
- `RCParsing`: Baseline, used for comparison;
- `Superpower`: ~1,64 times slower than `RCPasing`;
- `Pidgin`: ~2,98 times faster that `RCParsing`.

But `RCParsing` provides:
- An extremely readable Fluent API, just like BNF grammars, but in C#;
- Ability to automatically skip characters when parsing (useful for whitespace and comments), with up to 6 strategies (before/after, once/lazy/greedy);
- A huge set of very useful rules and tokens, like `Identifier` with custom predicates, `Regex`, `SeparatedRepeat` with option to allow trailing separator, `EscapedText` that allows ANY escaping strategy (double characters/prefix character/your own) with escaped result string building, `CustomToken` with own logic that may depend on parser parameter (planned);
- Setting system with inheritance strategies, with this thing you can configure skip-rules for each rule, for example.
- Advanced error display: when encountering a syntax error, you may want to get lots of rule-specific errors, just expected tokens or just last error? You can choose!

So:
- Choose `RCParsing`: for easy development and creating specific parsers, where other libraries are not working;
- Choose other librarires: for best runtime speed and/or low memory usage.

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

### JSON
| Method                   | Mean        | Error     | StdDev   | Gen0    | Gen1    | Allocated  |
|------------------------- |------------:|----------:|---------:|--------:|--------:|-----------:|
| RCParseJsonShort         |    37.85 us |  1.609 us | 0.088 us |  4.2114 |  0.5493 |   69.72 KB |
| PidginParseJsonShort     |    13.36 us |  1.965 us | 0.108 us |  0.2136 |       - |    3.58 KB |
| SuperpowerParseJsonShort |    66.13 us |  4.202 us | 0.230 us |  1.9531 |       - |   33.32 KB |
| RCParseJsonBig           |   744.46 us | 42.985 us | 2.356 us | 73.2422 | 55.6641 | 1210.48 KB |
| PidginParseJsonBig       |   249.26 us | 10.624 us | 0.582 us |  3.9063 |       - |   65.25 KB |
| SuperpowerParseJsonBig   | 1,219.62 us | 27.310 us | 1.497 us | 39.0625 |  5.8594 |  638.31 KB |

More benchmarks will be created later...

# Contributing

### Contributions are welcome!

If you have an idea about this project, you can report it to Issues.
For contributing code, please fork the repository and make your changes in a new branch. Once you're ready, create a pull request to merge your changes into the main branch. Pull requests should include a clear description of what was changed and why.