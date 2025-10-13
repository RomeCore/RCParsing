# Keyword

The `Keyword` token in `RCParsing` matches a specific string but fails if it’s followed by certain characters, such as letters or digits. It’s used for parsing keywords that must not be part of a larger word.

## Overview

The `Keyword` token ensures that the matched string is not followed by characters that would make it part of a larger identifier. It uses predicates to define forbidden trailing characters and produces the construction-specified string as its intermediate value.

## Example

Here’s an example of parsing a keyword:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("var")
    .Keyword("var");

var parser = builder.Build();
var result1 = parser.MatchToken("var", "var"); // Succeeds
var result2 = parser.MatchToken("var", "var "); // Succeeds
var result3 = parser.MatchToken("var", "var;"); // Succeeds
var result4 = parser.MatchToken("var", "variable"); // Fails
```

With a custom predicate:

```csharp
builder.CreateToken("var")
    .Keyword("var", c => char.IsDigit(c)); // Fails if followed by a digit

var parser = builder.Build();
var result = parser.MatchToken("var", "var1"); // Fails
```

## Use Cases

- **Language Keywords**: Match `var`, `if`, or `while` as distinct tokens.
- **Boundary Enforcement**: Ensure keywords are not part of larger words.
- **Custom Constraints**: Use predicates to define specific trailing character rules.

## Intermediate Values

The `Keyword` token produces the original string that was specified in construction as its intermediate value:

```csharp
builder.CreateToken("if")
    .KeywordIgnoreCase("iF");

var parser = builder.Build();
var result = parser.MatchToken("if", "If");
var value = result.GetIntermediateValue<string>(); // "iF"
```

## Notes

- **Default Predicates**: Uses ASCII or Unicode identifier predicates by default.
- **Case Sensitivity**: Supports `StringComparison` for case-insensitive matching.

## Related Tutorials

- [KeywordChoice Token](keyword-choice)
- [Literal Token](literal)
- [Identifier Token](identifier)