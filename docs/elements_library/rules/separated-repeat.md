# SeparatedRepeat

The `SeparatedRepeat` rule in `RCParsing` matches a child rule or token multiple times, interleaved with a separator rule or token. It’s ideal for parsing lists with delimiters, such as comma-separated values.

## Overview

A `SeparatedRepeat` rule allows you to specify a child rule to repeat, a separator rule, and options for minimum/maximum repetitions, trailing separators, and whether to include separators in the AST.

## Example

Here’s an example of parsing a comma-separated list of identifiers:

```csharp
var builder = new ParserBuilder();

builder.CreateRule("identifier_list")
    .OneOrMoreSeparated(
        b => b.Identifier(),
        s => s.Literal(",")
    );

var parser = builder.Build();
var result = parser.ParseRule("identifier_list", "a,b,c"); // Matches "a", "b", "c"
```

With trailing separator allowed:

```csharp
builder.CreateRule("identifier_list")
    .OneOrMoreSeparated(
        b => b.Identifier(),
        s => s.Literal(","),
        allowTrailingSeparator: true
    );

var parser = builder.Build();
var result = parser.ParseRule("identifier_list", "a,b,c,"); // Matches with trailing comma
```

## Use Cases

- **Lists**: Parse comma-separated arguments or array elements.
- **Expressions**: Handle operators in expressions, e.g., `a + b + c`.
- **Configuration Files**: Parse key-value pairs separated by delimiters.

## Transformation

The `SeparatedRepeat` rule produces an array of the repeated children’s values, optionally including separators if `includeSeparatorsInResult` is `true`:

```csharp
builder.CreateRule("identifier_list")
    .OneOrMoreSeparated(
        b => b.Identifier(),
        s => s.Literal(",")
    )
    .Transform(v => v.Children.Select(c => c.Text).ToArray());

var parser = builder.Build();
var result = parser.ParseRule("identifier_list", "a,b,c");
var identifiers = result.Value as string[]; // ["a", "b", "c"]
```

```csharp
builder.CreateRule("plus_expression")
    .OneOrMoreSeparated(
        b => b.Number<int>(),
        s => s.LiteralChoice("+"),
        includeSeparatorsInResult: true
    )
    .TransformFoldLeft<int, string, int>((l, o, r) => o == "+" ? l + r : l - r);

var parser = builder.Build();
var result = parser.ParseRule("plus_expression", "1+2+3");
var value = result.Value as int; // 6
```

## Notes

- **Trailing Separators**: Set `allowTrailingSeparator: true` to allow lists like `a,b,c,`.
- **Separator Inclusion**: Use `includeSeparatorsInResult: true` to include separators in the `Children` array.
- **Variants**: `ZeroOrMoreSeparated` and `OneOrMoreSeparated` simplify common cases.

## Related Tutorials

- [Repeat Rule](repeat)
- [Sequence Rule](sequence)
- [Choice Rule](choice)