# Choice

The `Choice` token in `RCParsing` attempts to match one of several child tokens, trying them in order until one succeeds or all fail. It’s useful for defining alternative token patterns, such as different literals or patterns.

## Overview

A `Choice` token tries each child token in the specified order, succeeding with the first match and propagating its intermediate value. If all children fail, the token fails.

## Example

Here’s an example of a `Choice` token for matching different keywords:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("keyword")
    .Choice(
        b => b.Literal("if"),
        b => b.Literal("else"),
        b => b.Literal("while")
    );

var parser = builder.Build();
var result = parser.MatchToken("keyword", "if");
var value = result.GetIntermediateValue<string>(); // "if"
```

## LongestChoice and ShortestChoice

The `LongestChoice` and `ShortestChoice` variants control which match is selected when multiple children could match:

- **LongestChoice**: Selects the first child that consumes the most input.
- **ShortestChoice**: Selects the first child that consumes the least input.

## Example

```csharp
var builder = new ParserBuilder();

builder.CreateToken("longest_keyword")
    .LongestChoice(
        b => b.Literal("pre"),
        b => b.Literal("prefix")
    );

builder.CreateToken("shortest_keyword")
    .ShortestChoice(
        b => b.Literal("prefix"),
        b => b.Literal("pre")
    );

var parser = builder.Build();
var result1 = parser.MatchToken("longest_keyword", "prefixx"); // Matches "prefix"
var result2 = parser.MatchToken("shortest_keyword", "prefixx"); // Matches "pre"
```

## Use Cases

- **Keyword Matching**: Match one of several keywords.
- **Pattern Alternatives**: Parse different numeric formats or literals.
- **Ambiguity Resolution**: Use `LongestChoice` or `ShortestChoice` for specific match behavior.

## Intermediate Values

The `Choice` token propagates the matched child’s intermediate value:

```csharp
builder.CreateToken("value")
    .Choice(
        b => b.Number<int>(),
        b => b.Literal("true")
    );

var parser = builder.Build();
var result = parser.MatchToken("value", "123");
var value = result.GetIntermediateValue<int>(); // 123
```

## Notes

- **Order Matters**: Place specific patterns first to avoid premature matches.
- **Longest/Shortest**: Use for disambiguating overlapping patterns.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [First Token](first)
- [Second Token](second)
- [Sequence Token](sequence)