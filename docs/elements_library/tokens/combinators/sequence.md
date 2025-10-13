# Sequence

The `Sequence` token in `RCParsing` matches a series of child tokens in a specific order, failing if any child fails. It’s used to define composite token patterns, such as quoted strings or complex literals.

## Overview

A `Sequence` token is created when multiple child tokens are added to a token definition. It ensures that the input matches each child token in order, producing an intermediate value based on the `Pass` combinator or child values.

## Example

Here’s an example of a `Sequence` token for parsing a quoted string:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("string")
    .Literal("\"")
    .EscapedTextPrefix(prefix: '\\', '\\', '\"')
    .Literal("\"")
    .Pass(1); // Propagate the EscapedTextPrefix value

var parser = builder.Build();
var result = parser.MatchToken("string", "\"hello\\\"world\"");
var value = result.GetIntermediateValue<string>(); // "hello"world"
```

## Use Cases

- **Quoted Strings**: Parse strings with delimiters, e.g., `"hello"`.
- **Structured Tokens**: Combine multiple tokens into a single unit.

## Intermediate Values

The `Sequence` token propagates the intermediate value of one of its children using `.Pass(index)` or a custom passage function:

```csharp
builder.CreateToken("version")
    .Literal("v")
    .Number<int>()
    .Pass(v => v[1]); // Propagate the number's value

var parser = builder.Build();
var result = parser.MatchToken("version", "v123");
var value = result.GetIntermediateValue<int>(); // 123
```

## Notes

- **Automatic Creation**: Adding multiple tokens implicitly creates a `Sequence`.
- **Pass Combinator**: Use `.Pass()` to control which child’s value is propagated.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [Choice Token](choice)
- [Optional Token](optional)
- [Between Token](between)