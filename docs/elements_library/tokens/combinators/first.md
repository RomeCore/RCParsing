# First

The `First` token in `RCParsing` matches a sequence of two child tokens and propagates the intermediate value of the first token. It’s optimized for patterns where only the first part of a sequence is needed.

## Overview

The `First` token is a specialized sequence that matches two tokens but only calculates and propagates the first token’s value. This is useful for patterns where a prefix is the primary focus.

## Example

Here’s an example of parsing a prefixed identifier:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("prefixed_identifier")
    .First(
        b => b.Literal("id_"),
        b => b.Identifier()
    );

var parser = builder.Build();
var result = parser.MatchToken("prefixed_identifier", "id_variable");
var value = result.GetIntermediateValue<string>(); // "id_"
```

## Use Cases

- **Prefixed Tokens**: Match prefixes like `id_` or `const_`.
- **Structured Patterns**: Extract the first part of a two-token sequence.
- **Simplified Parsing**: Avoid manual value propagation.

## Intermediate Values

The `First` token propagates the first child’s intermediate value, while the second token’s value is ignored and not calculated:

```csharp
builder.CreateToken("prefixed_number")
    .First(
        b => b.Literal("+"),
        b => b.Number<int>() // 'First' token says the number token that it should not calculate intermediate value
    );

var parser = builder.Build();
var result = parser.MatchToken("prefixed_number", "+123");
var value = result.GetIntermediateValue<string>(); // "+"
```

## Notes

- **Optimized**: More efficient than a `Sequence` with `.Pass(0)`.
- **Two Children**: Requires exactly two child tokens.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [Between Token](between)
- [Second Token](second)
- [Sequence Token](sequence)