# Number

The `Number` token in `RCParsing` matches numeric values based on specified `NumberFlags` and converts them into an intermediate value based on the `NumberType` or generic type provided. It’s designed to parse integers, floating-point numbers, and scientific notation with flexible configuration.

## Overview

The `Number` token matches numeric patterns, such as integers (`123`), floating-point numbers (`3.14`), or scientific notation (`1.5e10`). It uses `NumberFlags` to control the allowed format (e.g., signs, decimal points, exponents) and converts the matched text to a specified type (e.g., `int`, `double`, `float`). The parser attempts to match the maximum number of valid characters, ensuring precise parsing (e.g., for input `5.`, it matches only `5` if `ImplicitFractionalPart` is not allowed).

## Example

Here’s an example of parsing different numeric formats:

```csharp
var builder = new ParserBuilder();

// Signed integer
builder.CreateToken("int")
    .Number<int>(); // Automatically includes Signed flag

// Unsigned integer
builder.CreateToken("uint")
    .Number<int>(signed: false);

// Signed integer converted to double
builder.CreateToken("int_to_double")
    .Number<double>(NumberFlags.Integer);

// Floating-point with automatic type selection
builder.CreateToken("float_or_int")
    .Number(NumberFlags.Float); // Converts to int if no decimal point

var parser = builder.Build();
var intResult = parser.MatchToken("int", "-123").GetIntermediateValue<int>(); // -123
var uintResult = parser.MatchToken("uint", "123").GetIntermediateValue<int>(); // 123
var doubleResult = parser.MatchToken("int_to_double", "-123").GetIntermediateValue<double>(); // -123.0
var floatOrIntResult1 = parser.MatchToken("float_or_int", "10").GetIntermediateValue().GetType(); // System.Int32
var floatOrIntResult2 = parser.MatchToken("float_or_int", "10.01").GetIntermediateValue().GetType(); // System.Single
```

## NumberFlags

The `Number` token supports the following flags to customize parsing behavior:

| Flag | Description | Example |
|------|-------------|---------|
| `Signed` | Allows leading `+` or `-` sign | `-123`, `+45` |
| `DecimalPoint` | Allows decimal point in the number | `3.14`, `2.` |
| `Exponent` | Allows exponent part for scientific notation | `1.5e10`, `2E-5` |
| `ImplicitIntegerPart` | Allows implicit integer part before decimal point | `.5` (parsed as `0.5`) |
| `ImplicitFractionalPart` | Allows implicit fractional part after decimal point | `5.` (parsed as `5.0`) |

### Common Flag Combinations

The following predefined combinations simplify common numeric formats:

| Combination | Flags Included | Description |
|-------------|----------------|-------------|
| `Integer` | `Signed` | Standard signed integers |
| `UnsignedInteger` | `None` | Unsigned integers only |
| `Float` | `Signed \| DecimalPoint \| ImplicitIntegerPart \| ImplicitFractionalPart` | Standard floating-point numbers |
| `UnsignedFloat` | `DecimalPoint \| ImplicitIntegerPart \| ImplicitFractionalPart` | Unsigned floating-point numbers |
| `StrictFloat` | `Signed \| DecimalPoint` | Strict floating-point (no implicit parts) |
| `StrictUnsignedFloat` | `DecimalPoint` | Strict unsigned floating-point |
| `Scientific` | `Float \| Exponent` | Standard scientific notation |
| `UnsignedScientific` | `UnsignedFloat \| Exponent` | Unsigned scientific notation |
| `StrictScientific` | `StrictFloat \| Exponent` | Strict scientific notation |
| `StrictUnsignedScientific` | `StrictUnsignedFloat \| Exponent` | Strict unsigned scientific notation |

## Use Cases

- **Numeric Parsing**: Parse integers, floating-point numbers, or scientific notation in expressions.
- **Type Conversion**: Convert matched text to specific numeric types (`int`, `double`, `float`).
- **Custom Formats**: Define strict or flexible numeric formats for domain-specific parsing.
- **Dynamic Type Selection**: Use `NumberFlags.Float` to automatically choose `int` or `float` based on the presence of a decimal point.

## Intermediate Values

The `Number` token produces an intermediate value of the specified type (`int`, `uint`, `double`, `float`, etc.) or dynamically selects the type based on the input and flags:

```csharp
builder.CreateToken("scientific")
    .Number<double>(NumberFlags.Scientific);

var parser = builder.Build();
var result = parser.ParseRule("scientific", "1.5e10");
var value = result.GetIntermediateValue<double>(); // 15000000000.0
```

For `NumberFlags.Float`, the token produces `int` for integers and `float` for floating-point numbers:

```csharp
builder.CreateToken("float_or_int")
    .Number(NumberFlags.Float);

var parser = builder.Build();
var intResult = parser.MatchToken("float_or_int", "10").GetIntermediateValue().GetType(); // System.Int32
var floatResult = parser.MatchToken("float_or_int", "10.01").GetIntermediateValue().GetType(); // System.Single
```

## Notes

- **Maximal Matching**: Matches the maximum valid number of characters (e.g., `5.` matches only `5` if `ImplicitFractionalPart` is not allowed).
- **Type Safety**: Ensures the matched number can be converted to the specified type, failing if the conversion is invalid.
- **Signed Parameter**: Overrides the default sign behavior when using generic types (e.g., `Number<int>(signed: true)`).

## Related Tutorials

- [Regex Token](regex)
- [Map Token](combinators/map)
- [Sequence Token](combinators/sequence)