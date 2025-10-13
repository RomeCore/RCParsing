# Map Token

The `Map` token in `RCParsing` matches a single child token and applies a transformation function to its intermediate value. It’s used to convert the matched value into a different type or format, making it a powerful tool for shaping the output of token matches.

## Overview

The `Map` token wraps a single child token and uses a provided transformation function to process its intermediate value. This allows you to convert raw matched data (e.g., a string or character) into a more meaningful type, such as an integer, boolean, or custom object. It’s particularly useful when you need to perform type conversions or custom logic on a token’s output without relying on rule-level transformations.

## Example

Here’s an example of using a `Map` token to convert a literal string into an integer:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("number")
    .Map(b => b.LiteralChoice("1", "2", "3"), val => int.Parse((string)val));

var parser = builder.Build();
var result = parser.ParseRule("number", "2");
var value = result.GetIntermediateValue<int>(); // 2
```

In this example:
- The `LiteralChoice` token matches one of `"1"`, `"2"`, or `"3"`, producing a string as its intermediate value.
- The `Map` token applies `int.Parse` to convert the matched string into an integer.

You can also use a generic version for better look:

```csharp
builder.CreateToken("number")
    .Map<string>(b => b.LiteralChoice("1", "2", "3"), str => int.Parse(str));

var parser = builder.Build();
var result = parser.ParseRule("number", "3");
var value = result.GetIntermediateValue<int>(); // 3
```

## Use Cases

- **Type Conversion**: Convert matched strings to numbers, booleans, or other types (e.g., parsing `"true"` to `true`).
- **Data Normalization**: Transform raw input into a standardized format, such as converting case or trimming.
- **Simplifying Rules**: Perform transformations at the token level to reduce complexity in rule transformations.

## Intermediate Values

The `Map` token produces the result of the transformation function applied to the child token’s intermediate value. This allows precise control over the output type:

```csharp
builder.CreateToken("boolean")
    .Map<string>(b => b.LiteralChoice("true", "false"), s => s == "true");

var parser = builder.Build();
var result = parser.ParseRule("boolean", "true");
var value = result.GetIntermediateValue<bool>(); // true
```

Here, the `LiteralChoice` produces a string (`"true"` or `"false"`), and the `Map` token transforms it into a boolean.

## Notes

- **Single Child**: The `Map` token can only wrap one child token.
- **Transformation Function**: The function must accept the child’s intermediate value type and return the desired type.
- **No Rules**: Tokens cannot reference rules, only other tokens, so the child must be a token pattern.

## Related Tutorials

- [Return Token](return)
- [CaptureText Token](capture-text)
- [Sequence Token](sequence)