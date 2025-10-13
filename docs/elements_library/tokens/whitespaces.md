# Whitespaces

The `Whitespaces` token in `RCParsing` matches one or more whitespace characters, including spaces, tabs, carriage returns, and newlines. It’s useful for parsing insignificant whitespace.

## Overview

The `Whitespaces` token matches sequences of whitespace characters (` `, `\t`, `\r`, `\n`). It’s commonly used in rules or as a skip rule to ignore whitespace between tokens.

## Example

Here’s an example of matching whitespace:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("ws")
    .Whitespaces();

var parser = builder.Build();
var result = parser.MatchToken("ws", " \t\n");
```

## Use Cases

- **Whitespace Skipping**: Use as a skip rule to ignore whitespace.
- **Grammar Rules**: Explicitly match whitespace in specific contexts.
- **Formatting**: Parse whitespace in structured formats like JSON.

## Intermediate Values

The `Whitespaces` token does not produce an intermediate value unless wrapped with a combinator:

```csharp
builder.CreateToken("ws")
    .CaptureText(b => b.Whitespaces());

var parser = builder.Build();
var result = parser.MatchToken("ws", " \t");
var value = result.GetIntermediateValue<string>(); // " \t"
```

## Notes

- **Multiple Characters**: Matches one or more whitespace characters.
- **Common Use**: Often used with `SkipWhitespaces` or as a skip rule.

## Related Tutorials

- [Spaces Token](spaces)
- [Newline Token](newline)
- [SkipWhitespaces Token](combinators/skip-whitespaces)