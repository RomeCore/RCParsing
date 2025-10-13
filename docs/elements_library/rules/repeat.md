# Repeat

The `Repeat` rule in `RCParsing` matches a child rule or token a specified number of times within a given range. It fails if the child does not match at least the minimum number of times. This rule is useful for parsing lists or repeated patterns.

## Overview

A `Repeat` rule allows you to specify a minimum and optional maximum number of times a child rule or token should match. Variants like `ZeroOrMore` and `OneOrMore` simplify common cases.

## Example

Here’s an example of a `Repeat` rule for parsing a list of identifiers:

```csharp
var builder = new ParserBuilder();

builder.Settings.SkipWhitespaces();

builder.CreateRule("identifiers")
    .Repeat(b => b.Identifier(), min: 1, max: 3); // Matches 1 to 3 identifiers

var parser = builder.Build();
var result1 = parser.ParseRule("identifiers", "a b c"); // Matches "a", "b", "c"
var result2 = parser.ParseRule("identifiers", "d e f g"); // Matches "d", "e", "f"
var result3 = parser.ParseRule("identifiers", "d e 0 g"); // Matches "d", "e"
```

Using `OneOrMore` for a similar effect:

```csharp
builder.CreateRule("identifiers")
    .OneOrMore(b => b.Identifier()); // Matches 1 or more identifiers

var parser = builder.Build();
var result = parser.ParseRule("identifiers", "a b c d"); // Matches all identifiers
```

## Use Cases

- **Lists**: Parse lists of items, such as variables or arguments.
- **Repeated Patterns**: Match repeated keywords or tokens, e.g., multiple `else` clauses.
- **Flexible Counts**: Allow a variable number of matches within a range.

## Transformation

The `Repeat` rule produces an array of the children’s values:

```csharp
builder.CreateRule("identifiers")
    .OneOrMore(b => b.Identifier())
    .Transform(v => v.Children.Select(c => c.Text).ToArray());

var parser = builder.Build();
var result = parser.ParseRule("identifiers", "a b c");
var identifiers = result.Value as string[]; // ["a", "b", "c"]
```

## Notes

- **Range Specification**: Use `min` and `max` to control the number of matches. Omit `max` for unbounded repeats.
- **Variants**: `ZeroOrMore` (0 or more) and `OneOrMore` (1 or more) are shortcuts for common cases.
- **Failure**: Fails if fewer than `min` matches are found.

## Related Tutorials

- [SeparatedRepeat Rule](separated-repeat)
- [Optional Rule](optional)
- [Sequence Rule](sequence)