# Literal

The `Literal` token in `RCParsing` matches a specific string exactly. It’s used for parsing fixed strings, such as keywords or operators.

## Overview

The `Literal` token matches a predefined string, optionally with case-insensitive comparison. It produces the original construction-specified string as its intermediate value.

## Example

Here’s an example of matching a keyword:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("if")
    .Literal("if");

var parser = builder.Build();
var result = parser.MatchToken("if", "if");
var value = result.GetIntermediateValue<string>(); // "if"
```

With case-insensitive matching:

```csharp
builder.CreateToken("if")
    .Literal("if", StringComparison.OrdinalIgnoreCase);

builder.CreateToken("else")
    .LiteralIgnoreCase("else");

var parser = builder.Build();

var result1 = parser.MatchToken("if", "If");
var result2 = parser.MatchToken("else", "ElSe");

var value1 = result1.GetIntermediateValue<string>(); // "if"
var value2 = result2.GetIntermediateValue<string>(); // "else"
```

## Use Cases

- **Operators**: Parse symbols like `+` or `=`.
- **Fixed Strings**: Match specific string literals in the grammar.

## Intermediate Values

The `Literal` token produces the original string that was specified in token construction as its intermediate value:

```csharp
builder.CreateToken("operator")
    .Literal("+");

var parser = builder.Build();
var result = parser.MatchToken("operator", "+");
var value = result.GetIntermediateValue<string>(); // "+"
```

## Notes

- **Case Sensitivity**: Use `StringComparison` for case-insensitive matching.
- **Efficient Matching**: Uses a trie for fast lookup.
- **Simple Match**: Matches exactly the specified string without additional checks, for keywords use `Keyword` token [instead](keyword).

## Related Tutorials

- [LiteralChar Token](literal-char)
- [LiteralChoice Token](literal-choice)
- [Keyword Token](keyword)