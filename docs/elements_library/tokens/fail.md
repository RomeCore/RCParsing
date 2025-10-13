# Fail

The `Fail` token in `RCParsing` always fails without consuming input. It’s used to explicitly block a parsing path in a grammar.

## Overview

The `Fail` token is a zero-width token that immediately fails, producing no intermediate value. It’s useful for enforcing constraints or preventing certain matches in `Choice` or other combinators.

## Example

```csharp
var builder = new ParserBuilder();

builder.CreateToken("no_match")
    .Fail();

var parser = builder.Build();
var result = parser.MatchToken("no_match", "anything"); // Fails
```

## Use Cases

- **Block Paths**: Prevent specific branches in a `Choice` token.
- **Constraints**: Enforce parsing rules by failing invalid paths.
- **Debugging**: Test parser behavior by forcing failure.

## Intermediate Values

The `Fail` token never succeeds, so it produces no intermediate value.

## Notes

- **Zero-Width**: Does not consume any input.
- **Always Fails**: Useful for explicitly invalid patterns.

## Related Tutorials

- [Empty Token](empty)
- [Choice Token](combinators/choice)
- [Lookahead Token](combinators/lookahead)