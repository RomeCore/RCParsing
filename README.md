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

Here is somewhat complicated, but detailed example how to make simple parser that parses "a + b" string and transforms the result:

```csharp
using RCParsing;

// First, you need to create a builder
var builder = new ParserBuilder();

// Enable and configure the auto-skip (you can replace `Whitespaces` with any parser rule)
builder.Settings()
    .Skip(b => b.Whitespaces().Configure(c => c.IgnoreErrors()));

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

# Tutorials

## Main concepts

`RCParsing` uses lexerless approach, but it has *tokens*, they are used as parser primitives, and they are not complex as *rules*:  

- **Token**: A token is a pattern that matches characters directly and it tries to match as much characters as possible, some token patterns may contain child tokens;
- **Rule**: A rule is a pattern that matches child rules and tokens based on behaviour.

Parsers in this library is created via `ParserBuilder`s. When parser is being built, builder deduplicates rules and token patterns, and assigns IDs to them, then they are being compound into a `Parser`.

## Rule and token building

When you are building rules and tokens, there is some things that you may encounter.

When a second part of rule is added, it becomes a *sequence* rule:
```csharp
// Here is rule with single 'Identifier' token reference:
builder.CreateRule("rule1")
    .Identifier();

// Here is a sequence rule with two children rules, where first child have 'Identifier' token reference, and second - 'Whitespaces' token reference:
builder.CreateRule("rule2")
    .Identifier()
    .Whitespaces();
```

You cannot create empty rules and tokens, make references to unknown rules/tokens, or make direct cyclic references:
```csharp
var builder = new ParserBuilder();

// If you create an empty rule or token and don't fill it with anything, you will get an error
builder.CreateRule("rule");

// And this is prohibited too
builder.CreateRule("D")
    .Rule("F"); // F is not registered!

// This is also prohibited
builder.CreateRule("A")
    .Rule("B");
builder.CreateRule("B")
    .Rule("A");

// P.S. You will not get error for this, because reference is not direct, it's inside a sequence rule:
builder.CreateRule("C")
    .Rule("B") // If you remove this line - builder will throw an exception!
    .Rule("B");

// Here you will get a ParserBuildingException if you made one of these mistakes
builder.Build();
```

## Auto-skipping

Parser does not skip any characters by default. If you want to configure auto-skipping, you can do it by configuring the parser builder:

```csharp
builder.Settings()
    .Skip(b => b.Whitespaces());
```

By default, if skip rule is set, parser will try to skip it before parsing every rule once. You can select the other skip strategy:

```csharp
builder.Settings()
    // Skip all whitespaces and C#-like comments
    .Skip(b => b.Choice(
        b => b.Whitespaces(), // Skip whitespaces
        b => b.Literal("//").TextUntil("\n", "\r"), // Skip single-line comments, newline characters will be skipped by upper choice (Whitespaces)
        b => b.Literal("/*").TextUntil("*/").Literal("*/")
    ), ParserSkippingStrategy.SkipBeforeParsingGreedy);
```

Here is detailed skip strategies description:

- `SkipBeforeParsing`: Parser will try to skip the skip-rule once before parsing the target rule.
- `SkipBeforeParsingLazy`: Parser will repeatedly attempt to skip the skip-rule in an interleaved manner with the target rule, until the target rule succeeds or both skip and target rules fail. This allows incremental consumption of skip content.
- `SkipBeforeParsingGreedy`: Parser will greedily skip the skip-rule as many times as possible before attempting to parse the target rule.
- `TryParseThenSkip`: Parser will first attempt to parse the target rule; if parsing fails, it will skip the skip-rule once and then retry parsing the target rule.
- `TryParseThenSkipLazy`: Parser will attempt to parse the target rule; if parsing fails, it will alternately try to skip the skip-rule and parse the target rule repeatedly until the target rule succeeds or both fail.
- `TryParseThenSkipGreedy`: Parser will attempt to parse the target rule; if parsing fails, it will greedily skip the skip-rule as many times as possible and then retry parsing the target rule.

## Deduplication

All rules and tokens are deduplicated when you call `builder.Build()`.  
Here you can see deduplication example:

```csharp
var builder = new ParserBuilder();

// Create rule that matches literal 'abc' token
builder.CreateRule("rule1")
    .Literal("abc");

// Create another rule with same token
builder.CreateRule("rule2")
    .Literal("abc");

// It's deduplicated here!
var parser = builder.Build();

// It's deduplicated!
Console.WriteLine(parser.GetRule("rule1").Id == parser.GetRule("rule2").Id); // true
```

Deduplication means you don't need to make that redundant links, like here:

```csharp
builder.CreateToken("LPAREN")   .Literal('(');
builder.CreateToken("RPAREN")   .Literal(')');
builder.CreateToken("COMMA")    .Literal(',');
builder.CreateToken("ID")       .Identifier();

builder.CreateRule("method")
    .Token("ID")
    .Token("LPAREN")
    .ZeroOrMoreSeparated(b => b.Token("ID"), s => s.Token("COMMA"))
    .Token("RPAREN")

builder.CreateRule("paren")
    .Token("LPAREN")
    .Token("ID")
    .Token("RPAREN");

var parser = builder.Build();

// Print count of tokens registered in parser: 
Console.WriteLine(parser.TokenPatterns.Length); // 4
```

You can just do this and make it more readable:

```csharp
builder.CreateRule("method")
    .Identifier()
    .Literal('(')
    .ZeroOrMoreSeparated(b => b.Identifier(), s => s.Literal(','))
    .Literal(')');

builder.CreateRule("paren")
    .Literal('(')
    .Identifier()
    .Literal(')');

var parser = builder.Build();
// It prints the same number:
Console.WriteLine(parser.TokenPatterns.Length); // 4
```

And you will get the **same** parser!

## Transformation and intermediate values

The true power of a parser lies in its ability to transform raw text into structured data. In `RCParsing`, this is achieved through a system of *intermediate values* and *transformation functions* attached to your rules.

### The `ParsedRuleResult` Object

When a complete rule is parsed, parser produces a `ParsedRuleResult` object that is a wrapper around intermadiate AST object. This object is a entirely lazy-evaluated, rich representation of the parse result, providing access to the captured text, the parsed value, child nodes, and other metadata. It is the primary context object passed to your transformation functions.

Key properties of `ParsedRuleResult`:

- `Text`: The substring of the original input that this rule captured;
- `Span`: The captured text as a `ReadOnlySpan<char>` for efficient processing;
- `Value`: The final transformed value of this rule, computed lazily by invoking the rule's value factory;
- `IntermediateValue`: The raw, un-transformed value produced during the initial parsing phase (e.g., a `Match` object for a regex token, or an escaped string for an `EscapedText` token);
- `Children`: An array of `ParsedRuleResult` objects representing the child nodes of this rule (e.g., the parts of a sequence). This array is also built lazily for performance;
- `IsToken`: Indicates if this result represents a token;
- `Rule`: The parser rule definition that produced this result.

### Value Propagation: Intermediate Values and `Pass`

During the parsing phase, some built-in token patterns generates an **intermediate value**:

- `Regex`: Returns the `Match` object;
- `EscapedText` (all variations): Returns the fully processed string with all escape sequences applied;
- `Literal`, `LiteralChar`, `LiteralChoice`: Return the exact literal string that was matched (useful for case-insensitive matching to get the original text);
- `Optional` and `Choice`: Return the intermediate value of their matched child.

For sequential tokens (tokens built by chaining patterns like `.Literal("\"").EscapedTextPrefix(...).Literal("\"")`), you use the `.Pass()` method to control which child's intermediate value is propagated upwards to become the token's own intermediate value. `.Pass(v => v[1])` tells the token to use the intermediate value from the second child (index 1) in the sequence.

### Transformation Functions

Transformation functions are defined using `.Transform()` on a rule. They take a `ParsedRuleResult` (`v`) and return the final value for that rule.

**Crucially, tokens themselves do not have transformation functions.** However, you can attach a default value factory to a token pattern using an overload like `.Regex(pattern, transformFunction)` or `.Regex(pattern).Transform(transformFunction)`. This factory is not used by the token itself but is inherited by any rule that uses that token and doesn't define its own transformation. This promotes reuse.

Inside a `.Transform()` function, you build your final value by inspecting the `ParsedRuleResult`:

- `v.Text`: Get the exact text the rule matched;
- `v.Children`: Access the results of child rules/tokens by index;
- `v.GetValue(int index)`: A shortcut to get the final `Value` of a specific child as `object`;
- `v.GetValue<T>(int index)`: A type-safe shortcut to get the final `Value` of specific type of a specific child;
- `v.IntermediateValue`: Access the raw intermediate value if the built-in pattern produced one;
- `v.GetIntermediateValue<T>()`: A shortcut to get the `IntermediateValue` of a specific child;
- `v.GetIntermediateValue<T>(int index)`: A type-safe shortcut to get the `IntermediateValue` of a specific child.

**Important!** Some rule types have predefined transormation functions:

- `Choice` and `Optional`: Passes the child's value through (if has, otherwise it will be `null`);
- `Repeat` and `RepeatSeparated` (all variants): Passes and array of children's `Value`s.

Here is detailed example of transformation functions:
```csharp
// Example: Transforming a simple arithmetic sequence
builder.CreateRule("expression")
    .Rule("value")
    .LiteralChoice("+", "-") // Child index 1
    .Rule("value")           // Child index 2
    .Transform(v => {
        // Get the final values of the child 'value' rules
        var leftOperand = v.GetValue<double>(0);
        var operatorSymbol = v.Children[1].Text; // Get the text of the literal
        var rightOperand = v.GetValue<double>(2);

        return operatorSymbol switch
        {
            "+" => leftOperand + rightOperand,
            "-" => leftOperand - rightOperand,
            _ => throw new InvalidOperationException($"Unexpected operator {operatorSymbol}")
        };
    });

// Example: A token with a default value factory. Rules using 'number' will get this value unless they override it.
builder.CreateToken("number")
    .Regex(@"-?\d+(?:\.\d+)?")
    .Transform(v => double.Parse(v.GetIntermediateValue<Match>().Value)); // v.Match is the intermediate value

// Example: Using .Pass() in a sequential token to propagate an intermediate value
builder.CreateToken("string")
    .Literal("\"")
    .EscapedTextPrefix(prefix: '\\', '\\', '\"') // Child 1 produces the final string as its intermediate value
    .Literal("\"")
    .Pass(v => v[1]) // Propagate the intermediate value from the EscapedTextPrefix child (index 1)
    .Transform(v => v.IntermediateValue); // Use the propagated intermediate value as the final value
```

This combination of intermediate values, `Pass`, and `Transform` provides a flexible and powerful mechanism to cleanly build your desired output model directly from the parse tree.

## Settings and their overide modes

Parser and every rule themselves can be configured to control some behaviors of parsing. Each rule's setting can be configured with a specific override mode that determines how it propagates through the rule hierarchy. Here is a list of settings that can be configured:

- **Skipping strategy**: Controls how the parser tries to skip the skip-rules, does not work if the skip-rule is not configured;
- **Skip-rule**: The target skip-rule that the parser will try to skip when parsing rules;
- **Error handling**: Defines how rules and tokens should act when encountering an error, record it to context, do not record it (ignore) or throw it (rarely usable). Also defines flags for error display behaviour in exceptions and using `ErrorFormatter` utility;
- **Caching (or memoization)**: Defines what kind of rules should interact with cache (none/default; token rules; other rules; all rules), very important to use it in complex grammars, but in simple grammars (like JSON) it can reduce speed up to 50% and eat your memory! But in complex grammars, using caching is crucial, because if you are not using it, you may get **millions** of recorded errors and wait **seconds** for parsing, especially when getting a syntax error. So, **test this thing in production scenarios**;
- **Max recursion depth**: The maximum recursion depth allowed when parsing, infinite by default.

Also, settings for each **rule** (not parser) can have *override modes*, which control inheritance behavior.  
Here is short example how you can confige the parser and each rule:

```csharp
// Configure the parser for skipping whitespaces
builder.Settings().Skip(r => r.Whitespaces());

builder.CreateRule("string")
    .Literal('"')
    .TextUntil('"')
    .Literal('"')
    // Call the Configure to apply settings for rule
	.Configure(c => c.NoSkipping()); // Remove skipping when parsing this rule
```

But if you do it like this, the parser won't skip any rules when parsing the `string` rule. Do this:

```csharp
builder.CreateRule("string")
    .Literal('"')
    .TextUntil('"')
    .Literal('"')
	.Configure(c => c.NoSkipping(), ParserSettingMode.LocalForChildrenOnly); // Apply the setting for the child rules, not the target rule (this rule is sequence currently)
```

Now parser will try to skip whitespaces before parsing the `string` rule, but it won't try to skip it when, for example, parsing the `TextUntil('"')` rule itself.

There is list of possible override/inheritance modes:

- **InheritForSelfAndChildren**, default: Applies the parent's setting for this element and all its children, ignoring any local or global *(parser)* settings.
- **LocalForSelfAndChildren**, default when configuration applied to rule: Applies the local setting for this element and all its children. This is the default when explicitly providing a local setting.
- **LocalForSelfOnly**: Applies the local setting for this element only, while propagating the parent's setting to all child elements.
- **LocalForChildrenOnly**: Applies the parent's setting for this element only, while propagating the local setting to all child elements.
- **GlobalForSelfAndChildren**: Applies the global setting for this element and all its children, ignoring any inheritance hierarchy.
- **GlobalForSelfOnly**: Applies the global setting for this element only, while propagating the parent's setting to all child elements.
- **GlobalForChildrenOnly**: Applies the parent's setting for this element only, while propagating the global setting to all child elements.

These modes provide fine-grained control over how settings propagate through your parser's rule hierarchy, allowing you to customize behavior at different levels of parsing.

Also, token patterns does not have their own configuration, but you can apply *default* configuration that will be applied to the rule that brings the token, just like `Transform` functions:

```csharp
// Configure the default configuration for token
builder.CreateToken("identifier")
    .Identifier()
    .Configure(c => c.IgnoreErrors());

// And it will be applied to the rule:
builder.CreateRule("id_rule")
    .Token("identifier");
```

## Complete JSON example

```csharp
var builder = new ParserBuilder();

// Configure whitespace and comment skip-rule
builder.Settings()
	.Skip(r => r.Rule("skip"), ParserSkippingStrategy.SkipBeforeParsingGreedy);

builder.CreateRule("skip")
	.Choice(
		b => b.Whitespaces(),
		b => b.Literal("//").TextUntil('\n', '\r'))
	.Configure(c => c.IgnoreErrors());

builder.CreateToken("string")
	.Literal("\"")
	.EscapedTextPrefix(prefix: '\\', '\\', '\"') // This sub-token automatically escapes the source string and puts it into intermediate value
	.Literal("\"")
	.Pass(v => v[1]) // Pass the EscapedTextPrefix's intermediate value up
	.Transform(v => v.IntermediateValue); // And use it as parsed value

builder.CreateToken("number")
	.Regex(@"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?", v => double.Parse(v.Text, CultureInfo.InvariantCulture));

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
	.Transform(v => v.GetValue(1));

builder.CreateRule("object")
	.Literal("{")
	.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","),
		allowTrailingSeparator: true, includeSeparatorsInResult: false,
		factory: v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary(k => k.Key, v => v.Value))
	.Literal("}")
	.Transform(v => v.GetValue(1));

builder.CreateRule("pair")
	.Token("string")
	.Literal(":")
	.Rule("value")
	.Transform(v => KeyValuePair.Create(v.GetValue<string>(0), v.GetValue(2)));

builder.CreateMainRule("content")
	.Rule("value")
	.EOF() // Sure that we captured all the input
	.Transform(v => v.GetValue(0));

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
```

# Comparison with other parsing libraries

This library is designed to be easy and flexible, not fast and memory-efficient, but it can compete with the other libraries by speed. It uses some layers of abstractions and may produce lots of intermediate errors and AST objects, and it impacts on memory usage.

Benchmark summary on JSON:
- `RCParsing`: Baseline, used for comparison;
- `Superpower`: ~1,64 times slower than `RCPasing`;
- `Pidgin`: ~2,98 times faster than `RCParsing`.

But `RCParsing` provides:
- An extremely readable Fluent API, just like BNF grammars, but in C#;
- Ability to automatically skip characters when parsing (useful for whitespace and comments), with up to 6 strategies (before/after, once/lazy/greedy);
- A huge set of very useful rules and tokens, like `Identifier` with custom predicates, `Regex`, `SeparatedRepeat` with option to allow trailing separator, `EscapedText` that allows ANY escaping strategy (double characters/prefix character/your own) with escaped result string building, `CustomToken` with own logic that may depend on parser parameter;
- Setting system with inheritance strategies, with this thing you can configure skip-rules for each rule, for example.
- Advanced error display: when encountering a syntax error, you may want to get lots of rule-specific errors with messages, or just expected tokens? You can choose!

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

More benchmarks will be later here...

# Projects using `RCParsing`

- [RCLargeLangugeModels](https://github.com/RomeCore/RCLargeLanguageModels): My project, used for `LLT`, the template Razor-like language with VERY *specific* syntax.

You can complete this list with your projects, just create a `Pull Request` on this `README.md`!

# Roadmap

This project will go further for making API more readable, compact and powerful!

There will be:
- **More optimizations**
- **More fluent API methods**
- **More useful tokens and rules**

But what is more important, there will be ***Blocking Tokens***! They will allow you to emit specific tokens on lexing stage, making parser **hybrid**. For example, we are parsing `Python` language, where indents is the main thing for describing where blocks are begins and ends. When using `RCParsing`, you can just do:

```csharp
// Skip whitespaces and single-line comments
builder.Settings().Skip(b = b.Choice(
    b => b.Whitespaces(),
    b => b.Literal('#').TextUntil('\n', '\r')
), ParserSkippingStrategy.SkipBeforeParsingGreedy);

// This thing will analyze text before parsing and emit 'INDENT' and 'DEDENT' blocking (virtual) tokens
builder.AddTokenizer(new IndentTokenizer("INDENT", "DEDENT"));

// Create a rule that uses these tokens
builder.CreateRule("block")
    .Token("INDENT")
    .OneOrMore(b => b.Rule("statement"))
    .Token("DEDENT");

// ...
// Other rules are hidden for demontrating purposes...
// ...

var parser = builder.Build();

string input =
"""
def test(n): # Indent
    return n
# Dedent
"""

// Will be parsed successfully
parser.ParseRule("function", input);

string invalidInput =
"""
def test(n): # Indent, Indent
        return n
# Dedent, Dedent
"""

// Will throw: Unexpected token 'INDENT', expected 'statement' rule
// Because first 'INDENT' was parsed, and second 'INDENT' is unexpected
parser.ParseRule("function", invalidInput);
```

Here is explanation:  
- Blocking tokens will block other tokens from parsing if they are not expected;
- We define *tokenizer* that emits `INDENT` and `DEDENT` blocking tokens into parsing context;
- If the blocking token is expected, parser will go further;
- If more than one blocking tokens contains in the same position, parser expects one of them.

# Contributing

### Contributions are welcome!

If you have an idea about this project, you can report it to `Issues`.  
For contributing code, please fork the repository and make your changes in a new branch. Once you're ready, create a pull request to merge your changes into the main branch. Pull requests should include a clear description of what was changed and why.