# Choice

The `Choice` rule in `RCParsing` attempts to match one of several child rules or tokens, trying them in the order specified until one succeeds or all fail. It’s ideal for defining alternatives in your grammar, such as different types of expressions or statements.

## Overview

A `Choice` rule is used when the input could match one of multiple possible patterns. The order of choices matters, as the parser tries each child in sequence. If a child matches, the rule succeeds and propagates the matched child’s value. If all children fail, the rule fails.

## Example

Here’s an example of a `Choice` rule for parsing different types of values (e.g., number, string, or identifier):

```csharp
var builder = new ParserBuilder();

builder.CreateRule("value")
    .Choice(
        b => b.Number<double>(),    // Try number first
        b => b.Token("string"),     // Then string
        b => b.Identifier()         // Finally identifier
    );

builder.CreateToken("string")
    .Between(
        b => b.Literal("\""),
        b => b.EscapedTextPrefix(prefix: '\\', '\\', '\"'),
        b => b.Literal("\"")
    );

var parser = builder.Build();
var result = parser.ParseRule("value", "123.45"); // Matches number
var result2 = parser.ParseRule("value", "\"hello\""); // Matches string
var result3 = parser.ParseRule("value", "variable"); // Matches identifier
```

## LongestChoice and ShortestChoice

The `RCParsing` library also supports `LongestChoice` and `ShortestChoice` variants, which modify how choices are selected:

- **LongestChoice**: Selects the first child that consumes the most input. Useful for resolving ambiguities where multiple choices could match.
- **ShortestChoice**: Selects the first child that consumes the least input. Useful for prioritizing shorter matches in specific contexts.

### Example of LongestChoice and ShortestChoice

```csharp
var builder = new ParserBuilder();

builder.CreateRule("longest_value")
    .LongestChoice(
        b => b.Literal("prefix"),       // Matches "prefix"
        b => b.Literal("prefix_longer") // Matches "prefix_longer"
    );

builder.CreateRule("shortest_value")
    .ShortestChoice(
        b => b.Literal("prefix"),        // Matches "prefix"
        b => b.Literal("prefix_longer"), // Matches "prefix_longer"
        b => b.Literal("pre")            // Matches "pre"
    );

var parser = builder.Build();
var result1 = parser.ParseRule("longest_value", "prefix_longer"); // Matches "prefix_longer"
var result2 = parser.ParseRule("shortest_value", "prefix_longer"); // Matches "pre"
```

In this example:
- `LongestChoice` prefers `prefix_longer` because it consumes more input.
- `ShortestChoice` prefers `pre` because it consumes less input.

## Use Cases

- **Expression Parsing**: Match different expression types (e.g., number, string, identifier).
- **Statement Types**: Parse different statement kinds (e.g., `if`, `while`, `for`).
- **Ambiguity Resolution**: Use `LongestChoice` or `ShortestChoice` to control which match is selected when multiple options are valid.

## Transformation

The `Choice` rule propagates the matched child’s value by default, there is also a `Transform` method that allows you to modify the:

```csharp
builder.CreateRule("value")
    .Choice(
        b => b.Number<double>(),
        b => b.Token("string"),
        b => b.Identifier()
    )
    .Transform(v => v.Children[0].Value); // Pass through the matched child's value

var parser = builder.Build();
var result = parser.ParseRule("value", "123.45");
var value = result.Value; // double: 123.45
```

## Notes

- **Order Matters**: The parser tries choices in the order defined. Place more specific patterns first to avoid premature matches.
- **Longest/Shortest Choice**: Use these variants when you need explicit control over ambiguous matches.
- **Intermediate Values**: The matched child’s intermediate value is propagated unless overridden by a transformation.

## Related Tutorials

- [Sequence Rule](sequence)
- [Optional Rule](optional)
- [Lookahead Rule](lookahead)