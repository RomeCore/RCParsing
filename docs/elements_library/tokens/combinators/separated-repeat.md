# SeparatedRepeat

The `SeparatedRepeat` token in `RCParsing` matches a child token multiple times, interleaved with a separator token. It’s ideal for parsing delimited lists within a single token.

## Overview

A `SeparatedRepeat` token specifies a child token to repeat, a separator token, and options for minimum/maximum repetitions, trailing separators, and whether to include separators in the result.

## Example

Here’s an example of parsing a comma-separated list of digits:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("digit_list")
    .OneOrMoreSeparated(
        b => b.Char(c => char.IsDigit(c)),
        s => s.Literal(",")
    );

var parser = builder.Build();
var result = parser.MatchToken("digit_list", "1,2,3"); // Matches "1,2,3"
```

With trailing separator:

```csharp
builder.CreateToken("digit_list")
 .OneOrMoreSeparated(
        b => b.Char(c => char.IsDigit(c)),
        s => s.Literal(","),
        allowTrailingSeparator: true
    );

var parser = builder.Build();
var result = parser.MatchToken("digit_list", "1,2,3,"); // Matches with trailing comma
```

## Use Cases

- **Delimited Lists**: Parse comma-separated digits or identifiers.
- **Operator Sequences**: Match sequences like `a + b + c`.
- **Structured Tokens**: Build complex tokens with separators.

## Intermediate Values

The `SeparatedRepeat` token produces an array of the repeated children’s intermediate values, optionally including separators:

```csharp
builder.CreateToken("number_list")
    .OneOrMoreSeparated(
        b => b.Number<int>(),
        s => s.Literal(",")
    )
    .Pass(v => v.Cast<int>().ToArray());

builder.CreateToken("plus_expression")
    .OneOrMoreSeparated(
        b => b.Number<int>(),
        s => s.LiteralChoice("+", "-"),
        includeSeparatorsInResult: true
    )
    .Pass(v => {
        int value = (int)v[0];
        for (int i = 1; i < v.Count; i += 2)
        {
            var op = (string)v[i];
            var nextValue = (int)v[i + 1];
            value = op == "+" ? value + nextValue : value - nextValue;
        }
        return value;
    });

var parser = builder.Build();

var result1 = parser.MatchToken("number_list", "12,34,56");
var numbers = result1.GetIntermediateValue<int[]>(); // [12, 34, 56]

var result2 = parser.MatchToken("plus_expression", "12+34-56+78-12");
var value = result2.GetIntermediateValue<int>(); // 66
```

## Notes

- **Trailing Separators**: Enable with `allowTrailingSeparator: true`.
- **Separator Inclusion**: Use `includeSeparatorsInResult: true` to include separators.
- **Variants**: `ZeroOrMoresSeparated` and `OneOrMoreSeparated` simplify common cases.

## Related Tutorials

- [Repeat Token](repeat)
- [Sequence Token](sequence)
- [Choice Token](choice)