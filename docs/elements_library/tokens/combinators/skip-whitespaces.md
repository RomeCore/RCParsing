# SkipWhitespaces

The `SkipWhitespaces` token in `RCParsing` skips whitespace characters before matching a child token. It’s useful for ignoring insignificant whitespace in token patterns.

## Overview

The `SkipWhitespaces` token wraps a single child token and skips any whitespace characters (spaces, tabs, newlines) before attempting to match the child. This simplifies token definitions where whitespace is irrelevant.

## Example

Here’s an example of skipping whitespace before a number:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("number")
    .SkipWhitespaces(b => b.Number<int>());

var parser = builder.Build();
var result = parser.MatchToken("number", "   123");
var value = result.GetIntermediateValue<int>(); // 123
```

## Use Cases

- **Whitespace Ignorance**: Parse tokens regardless of preceding whitespace.
- **Simplified Tokens**: Avoid explicit whitespace rules in token definitions.
- **Clean Parsing**: Handle input with variable whitespace.

## Intermediate Values

The `SkipWhitespaces` token propagates the child’s intermediate value:

```csharp
builder.CreateToken("keyword")
    .SkipWhitespaces(b => b.KeywordIgnoreCase("if"));

var parser = builder.Build();
var result = parser.MatchToken("keyword", "  IF");
var value = result.GetIntermediateValue<string>(); // "if"
```

## Notes

- **Whitespace Definition**: Skips spaces, tabs, newlines, and carriage returns.
- **Single Child**: Only one child token can be specified.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [Whitespaces Token](../whitespaces)
- [Sequence Token](sequence)
- [Number Token](../number)