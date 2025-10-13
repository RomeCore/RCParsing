# Empty

The `Empty` token in `RCParsing` matches zero characters at the current position and always succeeds. It’s a zero-width token used for placeholder or anchoring purposes in parsing.

## Overview

The `Empty` token does not consume any input and succeeds immediately, producing no intermediate value by default. It’s useful for marking positions or enabling optional parsing without affecting the input.

## Example

Here’s an example of using the `Empty` token as a placeholder:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("empty")
    .Empty();

var parser = builder.Build();
var result = parser.MatchToken("empty", "text"); // Succeeds at position 0
```

## Use Cases

- **Placeholder**: Use in sequences where a token is optional but a position must be marked.
- **Anchoring**: Anchor rules or tokens without consuming input.
- **Default Cases**: Provide a no-op token in choice constructs.

## Intermediate Values

The `Empty` token produces no intermediate value unless wrapped with a combinator:

```csharp
builder.CreateToken("empty")
    .Return(b => b.Empty(), true);

var parser = builder.Build();
var result = parser.MatchToken("empty", "text");
var value = result.GetIntermediateValue<bool>(); // true
```

## Notes

- **Zero-Width**: Does not consume any input.
- **Always Succeeds**: Never fails, making it safe for optional positions.

## Related Tutorials

- [Sequence Token](combinators/sequence)
- [Optional Token](combinators/optional)
- [Return Token](combinators/return)