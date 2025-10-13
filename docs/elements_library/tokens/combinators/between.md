# Between

The `Between` token in `RCParsing` matches a sequence of three child tokens and propagates the intermediate value of the middle token. It’s optimized for patterns like quoted strings or bracketed expressions.

## Overview

The `Between` token is a specialized sequence that matches a prefix, a main token, and a suffix, but only calculates propagates the main token’s value. This reduces boilerplate compared to a standard `Sequence` with `.Pass(1)` or `.Pass(v => v[1])`.

## Example

Here’s an example of parsing a quoted string:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("string")
    .Between(
        b => b.Literal("\""),
        b => b.EscapedTextPrefix(prefix: '\\', '\\', '\"'),
        b => b.Literal("\"")
    );

var parser = builder.Build();
var result = parser.MatchToken("string", "\"hello\\\"world\"");
var value = result.GetIntermediateValue<string>(); // "hello"world"
```

## Use Cases

- **Quoted Strings**: Parse strings with delimiters, e.g., `"hello"`.
- **Bracketed Expressions**: Match content between parentheses or braces.
- **Delimited Patterns**: Extract content between specific tokens.

## Intermediate Values

The `Between` token automatically propagates the middle child’s intermediate value, while not calculating other children's values:

```csharp
builder.CreateToken("number")
    .Between(
        b => b.CaptureText(b => b.Literal("(")), // CaptureText will just match child without doing allocations via making substring
        b => b.Number<int>(),                    // Value of this token value will be calculated and propagated
        b => b.CaptureText(b => b.Literal(")"))  // But this will ignored
    );

var parser = builder.Build();
var result = parser.MatchToken("number", "(123)");
var value = result.GetIntermediateValue<int>(); // 123
```

## Notes

- **Optimized**: More efficient than a `Sequence` with `.Pass(1)`.
- **Three Children**: Requires exactly three child tokens.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [First Token](first)
- [Second Token](second)
- [Sequence Token](sequence)