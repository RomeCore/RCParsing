# Character

The `Character` token in `RCParsing` matches a single character based on a predicate. It’s a flexible way to match specific characters without hardcoding them.

## Overview

The `Character` token uses a predicate function to determine if a single character matches. It does not produce an intermediate value by default, but you can use combinators like `CaptureText` to capture the matched character.

## Example

Here’s an example of matching a lowercase letter:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("lowercase")
    .Char(c => c >= 'a' && c <= 'z');

var parser = builder.Build();
var result = parser.MatchToken("lowercase", "x");
```

With captured text:

```csharp
builder.CreateToken("lowercase")
    .CaptureText(b => b.Char(c => c >= 'a' && c <= 'z'));

var parser = builder.Build();
var result = parser.MatchToken("lowercase", "x");
var value = result.GetIntermediateValue<string>(); // "x"
```

## Use Cases

- **Specific Characters**: Match letters, digits, or custom character sets.
- **Flexible Matching**: Use predicates for dynamic character selection.
- **Simple Tokens**: Define single-character tokens with custom rules.

## Intermediate Values

The `Character` token does not produce an intermediate value unless wrapped with a combinator:

```csharp
builder.CreateToken("digit")
    .CaptureText(b => b.Char(c => char.IsDigit(c)));

var parser = builder.Build();
var result = parser.ParseRule("digit", "5");
var value = result.GetIntermediateValue<string>(); // "5"
```

## Notes

- **Single Character**: Matches exactly one character.
- **Predicate-Based**: Use any boolean function to define valid characters.

## Related Tutorials

- [RepeatCharacters Token](combinators/repeat-characters)
- [Identifier Token](identifier)
- [LiteralChar Token](literal-char)