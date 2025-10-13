# LiteralChar

The `LiteralChar` token in `RCParsing` matches a specific single character. It’s a specialized version of the `Literal` token for single-character matches.

## Overview

The `LiteralChar` token matches a single character exactly, optionally with case-insensitive comparison. It produces the original literal character (not the matched character) as its intermediate value.

## Example

Here’s an example of matching a single character:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("equals")
    .Literal('=');

var parser = builder.Build();
var result = parser.MatchToken("equals", "=");
var value = result.GetIntermediateValue<char>(); // '='
```

With case-insensitive matching:

```csharp
builder.CreateToken("letter_a")
    .Literal('a', StringComparison.OrdinalIgnoreCase);

builder.CreateToken("letter_b")
    .LiteralIgnoreCase('b');

var parser = builder.Build();

var result1 = parser.MatchToken("letter_a", "A");
var result2 = parser.MatchToken("letter_b", "B");

var value1 = result1.GetIntermediateValue<char>(); // 'a'
var value2 = result2.GetIntermediateValue<char>(); // 'b'
```

## Use Cases

- **Single Symbols**: Match operators or punctuation like `=` or `;`.
- **Case-Insensitive Characters**: Parse letters regardless of case.

## Intermediate Values

The `LiteralChar` token produces the original character (that was specified in construction) as its intermediate value:

```csharp
builder.CreateToken("comma")
    .Literal(',');

var parser = builder.Build();
var result = parser.MatchToken("comma", ",");
var value = result.GetIntermediateValue<char>(); // ','
```

## Notes

- **Single Character**: Only matches one character.
- **Case Sensitivity**: Use `StringComparison` for case-insensitive matching.

## Related Tutorials

- [Literal Token](literal)
- [LiteralChoice Token](literal-choice)
- [Character Token](character)