# Regex

The `Regex` token in `RCParsing` matches a regular expression pattern in the input. It’s a powerful tool for parsing complex patterns that cannot be easily defined with other token types.

## Overview

The `Regex` token uses .NET regular expressions to match patterns in the input. It automatically wraps the pattern in `\G` to ensure matching starts at the current position, avoiding unnecessary allocations. The token produces a `Match` object as its intermediate value, which can be transformed into a specific value.

## Example

Here’s an example of matching a simple pattern:

```csharp
var builder = new ParserBuilder();

builder.CreateRule("regex")
    .Regex(@"(abc|def)");

var parser = builder.Build();
var result = parser.ParseRule("regex", "def");
var value = result.GetIntermediateValue<Match>().Value; // "def"
```

With transformation:

```csharp
builder.CreateRule("number")
    .Regex(@"-?\d+(?:\.\d+)?")
    .Transform(v => double.Parse(v.GetIntermediateValue<Match>().Value));

var parser = builder.Build();
var result = parser.ParseRule("number", "123.45");
var value = result.GetIntermediateValue<double>(); // 123.45
```

## Use Cases

- **Complex Patterns**: Match patterns like email addresses, URLs, or custom formats.
- **Numeric Parsing**: Parse numbers with specific formats (e.g., decimals or scientific notation).
- **Custom Tokens**: Define tokens that don’t fit standard primitives.

## Intermediate Values

The `Regex` token produces a `Match` object as its intermediate value:

```csharp
builder.CreateToken("identifier")
    .Regex(@"[a-zA-Z_][a-zA-Z0-9_]*");

var regex = new Regex(@"\G(?:.)+?"""); // It's recommended to prepend '\G' to the pattern
builder.CreateToken("my_regex")
    .Regex(regex);

var unlim_regex = new Regex(@"\d+");   // But you can remove it to disable limits from regex matching
builder.CreateToken("unlim_regex")
    .Regex(unlim_regex);

var parser = builder.Build();

var result1 = parser.MatchToken("identifier", "variable");
var match1 = result1.GetIntermediateValue<Match>(); // Match object for "variable"

var result2 = parser.MatchToken("unlim_regex", "aa123bb");
var match2 = result2.GetIntermediateValue<Match>(); // Match object for "123"
```

## Notes

- **\G Anchor**: Automatically added to ensure matching starts at the current position.
- **Options**: Supports `RegexOptions` like `Compiled` or `NonBacktracking` for performance, uses `Compiled` by default.
- **Performance**: Regular expressions can be slower than other primitives; use sparingly for critical paths.

## Related Tutorials

- [Number Token](number)
- [Identifier Token](identifier)
- [Map Token](combinators/map)