# LiteralChoice

The `LiteralChoice` token in `RCParsing` matches one of several specified strings, using a trie for efficient matching. It’s ideal for parsing multiple possible literals, such as keywords.

## Overview

The `LiteralChoice` token tries to match one of a set of strings, selecting the longest match by default. It produces the matched construction-specified string as its intermediate value.

## Example

Here’s an example of matching multiple keywords:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("keyword")
    .LiteralChoice("if", "e", "else", "while");

var parser = builder.Build();
var result = parser.MatchToken("keyword", "else");
var value = result.GetIntermediateValue<string>(); // "else"
```

With case-insensitive matching:

```csharp
builder.CreateToken("keyword")
    .LiteralChoice(new[] { "if", "else", "while" }, StringComparer.OrdinalIgnoreCase);

builder.CreateToken("another_keyword")
    .LiteralChoiceIgnoreCase("if", "else", "while");

var parser = builder.Build();
var result1 = parser.MatchToken("keyword", "ELSE");
var result2 = parser.MatchToken("another_keyword", "WHile");
var value1 = result.GetIntermediateValue<string>(); // "else"
var value2 = result.GetIntermediateValue<string>(); // "while"
```

## Use Cases

- **Multiple Keywords**: Match a set of keywords efficiently.
- **Literal Sets**: Parse one of several fixed strings.
- **Efficient Matching**: Use a trie for fast lookup of multiple literals.

## Intermediate Values

The `LiteralChoice` token produces the matched construction-specified string as its intermediate value:

```csharp
builder.CreateToken("operator")
    .LiteralChoice("+", "-", "*");

var parser = builder.Build();
var result = parser.ParseRule("operator", "*");
var value = result.GetIntermediateValue<string>(); // "*"
```

## Notes

- **Trie-Based**: Uses a trie for efficient matching of multiple strings.
- **Case Sensitivity**: Use `StringComparer` for case-insensitive matching.

## Related Tutorials

- [Literal Token](literal)
- [LiteralChar Token](literal-char)
- [KeywordChoice Token](keyword-choice)