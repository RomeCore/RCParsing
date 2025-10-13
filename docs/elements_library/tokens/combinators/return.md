# Return

The `Return` token in `RCParsing` matches a child token and returns a fixed value, ignoring the child’s intermediate value. It’s useful for assigning constant values to specific matches.

## Overview

The `Return` token wraps a single child token and returns a predefined value when the child matches, while not calculating the child's value. This is ideal for mapping specific tokens to fixed values, such as booleans or enums.

## Example

Here’s an example of returning a boolean for a literal:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("true")
    .Return(b => b.Literal("true"), true);

var parser = builder.Build();
var result = parser.MatchToken("true", "true");
var value = result.GetIntermediateValue<bool>(); // true
```

## Use Cases

- **Constant Values**: Map keywords to booleans or enums.
- **Simplified Parsing**: Assign fixed values to specific tokens.
- **Semantic Tokens**: Represent specific tokens with constant meanings.

## Intermediate Values

The `Return` token produces the fixed value specified, while instructs the child to not calculate intermediate value:

```csharp
builder.CreateToken("number")
    .Return(b => b.Number<int>(), "yes, this is number");

var parser = builder.Build();
var result = parser.MatchToken("number", "123");
var value = result.GetIntermediateValue<string>(); // "yes, this is number"
```

## Notes

- **Fixed Value**: The returned value is constant and ignores the child’s value.
- **Single Child**: Only one child token can be specified.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [Map Token](map)
- [CaptureText Token](capture-text)
- [Sequence Token](sequence)