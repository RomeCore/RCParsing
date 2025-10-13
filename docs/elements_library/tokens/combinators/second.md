# Second

The `Second` token in `RCParsing` matches a sequence of two child tokens and propagates the intermediate value of the second token. It’s optimized for patterns where the second part of a sequence is needed.

## Overview

The `Second` token is a specialized sequence that matches two tokens but only propagates the second token’s value. This is useful for patterns where a suffix or main content follows a prefix.

## Example

Here’s an example of parsing an identifier with a suffix:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("suffixed_identifier")
    .Second(
        b => b.Literal("_"),
        b => b.CaptureText(b => b.Identifier())
    );

var parser = builder.Build();
var result = parser.MatchToken("suffixed_identifier", "_variable");
var value = result.GetIntermediateValue<string>(); // "variable"
```

## Use Cases

- **Suffixed Tokens**: Match identifiers with suffixes like `_id`.
- **Structured Patterns**: Extract the second part of a two-token sequence.
- **Simplified Parsing**: Avoid manual value propagation.

## Intermediate Values

The `Second` token propagates the second child’s intermediate value:

```csharp
builder.CreateToken("suffixed_number")
    .Second(
        b => b.Literal("#"),
        b => b.Number<int>()
    );

var parser = builder.Build();
var result = parser.MatchToken("suffixed_number", "#123");
var value = result.GetIntermediateValue<int>(); // 123
```

## Notes

- **Optimized**: More efficient than a `Sequence` with `.Pass(1)`.
- **Two Children**: Requires exactly two child tokens.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [Between Token](between)
- [First Token](first)
- [Sequence Token](sequence)