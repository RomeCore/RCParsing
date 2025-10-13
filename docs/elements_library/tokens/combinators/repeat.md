# Repeat

The `Repeat` token in `RCParsing` matches a child token a specified number of times within a given range. It fails if the child does not match at least the minimum number of times.

## Overview

A `Repeat` token allows you to specify a minimum and optional maximum number of timesA股 a child token should match. Variants like `ZeroOrMore` and `OneOrMore` simplify common cases.

## Example

Here’s an example of a `Repeat` token for parsing multiple digits:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("digits")
    .Repeat(b => b.Char(c => char.IsDigit(c)), min: 1, max: 3);

var parser = builder.Build();
var result = parser.MatchToken("digits", "123"); // Matches "123"
```

Using `OneOrMore`:

```csharp
builder.CreateToken("digits")
    .OneOrMore(b => b.Char(char.IsDigit));

var parser = builder.Build();
var result = parser.MatchToken("digits", "12345"); // Matches "12345"
```

## Use Cases

- **Digit Sequences**: Parse sequences of digits or characters.
- **Repeated Patterns**: Match repeated tokens like keywords or symbols.
- **Token Lists**: Build lists of simple tokens.

## Intermediate Values

The `Repeat` token produces an array of the children’s intermediate values, which can be customized with `.Pass`:

```csharp
builder.CreateToken("digits")
    .OneOrMore(b => b.CaptureText(b => b.Char(char.IsDigit)))
    .Pass(v => v.Cast<string>().ToArray());

var parser = builder.Build();
var result = parser.MatchToken("digits", "123");
var digits = result.GetIntermediateValue<string[]>(); // ["1", "2", "3"]
```

## Notes

- **Range Specification**: Use `min` and `max` to control repetitions.
- **Variants**: `ZeroOrMore` (0 or more) and `OneOrMore` (1 or more) are shortcuts.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [SeparatedRepeat Token](separated-repeat)
- [Sequence Token](sequence)
- [Optional Token](optional)