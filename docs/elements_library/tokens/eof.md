# EOF

The `EOF` token in `RCParsing` matches the end of the input or the current barrier position. It’s used to ensure that the parser has consumed all input.

## Overview

The `EOF` token succeeds only when there are no more characters to parse. It’s essential for validating complete input parsing.

## Example

Here’s an example of ensuring all input is parsed:

```csharp
var builder = new ParserBuilder();

builder.Settings.SkipWhitespaces();

builder.CreateRule("main")
    .ZeroOrMore(b => b.Identifier())
    .EOF();

var parser = builder.Build();
var result = parser.ParseRule("main", "a b c"); // Succeeds
var result2 = parser.ParseRule("main", "a b c d"); // Succeeds
```

## Use Cases

- **Complete Parsing**: Ensure no trailing characters remain.
- **Grammar Termination**: Mark the end of a grammar rule.
- **Barrier Handling**: Match the end of a barrier-delimited section.

## Intermediate Values

The `EOF` token does not produce an intermediate value:

```csharp
builder.CreateToken("eof")
    .EOF();

var parser = builder.Build();
var result = parser.MatchToken("eof", "");
var value = result.TryGetIntermediateValue<object>(); // null
```

## Notes

- **Zero-Width**: Does not consume input unless at the end.

## Related Tutorials

- [Sequence Token](combinators/sequence)
- [Choice Token](combinators/choice)