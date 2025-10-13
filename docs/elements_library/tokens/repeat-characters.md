# RepeatCharacters

The `RepeatCharacters` token in `RCParsing` matches a sequence of characters based on a predicate, within a specified range of repetitions. It’s useful for parsing sequences like digits or letters.

## Overview

The `RepeatCharacters` token matches one or more characters that satisfy a predicate, with optional minimum and maximum repetition counts. Variants like `ZeroOrMoreChars` and `OneOrMoreChars` simplify common cases. It does not produce an intermediate value by default.

## Example

Here’s an example of matching multiple digits:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("digits")
    .OneOrMoreChars(c => char.IsDigit(c));

var parser = builder.Build();
var result = parser.MatchToken("digits", "12345");
```

With captured text:

```csharp
builder.CreateToken("digits")
    .CaptureText(b => b.OneOrMoreChars(c => char.IsDigit(c)));

var parser = builder.Build();
var result = parser.MatchToken("digits", "12345");
var value = result.GetIntermediateValue<string>(); // "12345"
```

## Use Cases

- **Digit Sequences**: Parse sequences like phone numbers or IDs.
- **Letter Sequences**: Match words or identifiers with specific character sets.
- **Custom Patterns**: Define repeated character patterns with predicates.

## Intermediate Values

The `RepeatCharacters` token does not produce an intermediate value unless wrapped with a combinator:

```csharp
builder.CreateToken("letters")
    .CaptureText(b => b.Chars(c => char.IsLetter(c), min: 2, max: 5));

var parser = builder.Build();
var result = parser.MatchToken("letters", "helloo");
var value = result.GetIntermediateValue<string>(); // "hello"
```

## Notes

- **Range Specification**: Use `min` and `max` to control repetitions.
- **Variants**: `ZeroOrMoreChars` and `OneOrMoreChars` are shortcuts.

## Related Tutorials

- [Character Token](character)
- [Identifier Token](identifier)
- [Repeat Token](combinators/repeat)