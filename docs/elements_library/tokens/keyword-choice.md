# KeywordChoice

The `KeywordChoice` token in `RCParsing` matches one of several specified strings, ensuring none are followed by certain characters, such as letters or digits. It’s ideal for parsing multiple keywords efficiently while enforcing word boundaries.

## Overview

The `KeywordChoice` token is similar to `LiteralChoice` but adds a boundary check to ensure the matched string is not followed by characters that would make it part of a larger word (e.g., letters or digits). It uses predicates to define forbidden trailing characters and produces the matched construction-specified string as its intermediate value. This token is optimized with a trie for efficient matching.

## Example

Here’s an example of parsing multiple keywords:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("keyword")
    .KeywordChoice("var", "int", "double");

var parser = builder.Build();
var result1 = parser.MatchToken("keyword", "int"); // Succeeds
var result2 = parser.MatchToken("keyword", "integer"); // Fails
```

With a custom predicate and case-insensitive matching:

```csharp
builder.CreateToken("keyword")
    .KeywordChoice(
        new[] { "var", "int", "double" },
        predicate: c => char.IsDigit(c),
        comparer: StringComparer.OrdinalIgnoreCase
    );

builder.CreateToken("another_keyword")
    .KeywordChoiceIgnoreCase("while", "for");

var parser = builder.Build();
var result1 = parser.MatchToken("keyword", "INT"); // Succeeds, produces "int" as intermediate value
var result2 = parser.MatchToken("keyword", "int123"); // Fails
var result3 = parser.MatchToken("another_keyword", "FOr"); // Succeeds, produces "for" as intermediate value
```

## Use Cases

- **Multiple Keywords**: Match a set of language keywords like `var`, `int`, or `double`.
- **Boundary Enforcement**: Ensure keywords are not part of larger identifiers.
- **Case-Insensitive Parsing**: Handle keywords regardless of case with `StringComparer`.

## Intermediate Values

The `KeywordChoice` token produces the matched construction-specified string as its intermediate value:

```csharp
builder.CreateToken("keyword")
    .KeywordChoiceIgnoreCase("if", "else", "while");

var parser = builder.Build();
var result = parser.MatchToken("keyword", "elSE!");
var value = result.GetIntermediateValue<string>(); // "else"
```

## Notes

- **Default Predicates**: Uses ASCII or Unicode identifier predicates by default (e.g., letters, digits, or underscore).
- **Trie-Based**: Uses a trie for efficient matching of multiple strings.
- **Custom Predicates**: Define custom trailing character checks with a predicate function.

## Related Tutorials

- [Keyword Token](keyword)
- [LiteralChoice Token](literal-choice)
- [Identifier Token](identifier)