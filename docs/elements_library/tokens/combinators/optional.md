# Optional

The `Optional` token in `RCParsing` attempts to match a child token but never fails, even if the child does not match. It’s useful for parsing optional token patterns, such as optional prefixes or suffixes.

## Overview

An `Optional` token wraps a single child token. If the child matches, its intermediate value is propagated; if it fails, the token succeeds with a `null` intermediate value and no consumed text.

## Example

Here’s an example of an `Optional` token for parsing an optional sign before a number:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("signed_number")
    .Optional(b => b.LiteralChoice("+", "-"))
    .Number<int>(signed: false); 

var parser = builder.Build();
var result1 = parser.MatchToken("signed_number", "+123"); // Matches "+123"
var result2 = parser.MatchToken("signed_number", "123");  // Matches "123"
```

## Use Cases

- **Optional Prefixes/Suffixes**: Parse optional signs or modifiers.
- **Flexible Tokens**: Allow optional delimiters or keywords.
- **Simplified Parsing**: Handle optional components without separate rules.

## Intermediate Values

The `Optional` token propagates the child’s intermediate value if it matches, or `null` if it does not:

```csharp
builder.CreateToken("signed_number")
    .Optional(b => b.LiteralChoice("+", "-"))
    .Number<int>(signed: false)
    .Pass(v => new { Sign = v[0] as string ?? "", Value = (int)v[1] });

var parser = builder.Build();
var result = parser.MatchToken("signed_number", "+123");
var value = result.GetIntermediateValue(); // { Sign = "+", Value = 123 }
```

## Notes

- **Never Fails**: Always succeeds, making it safe for optional patterns.
- **Single Child**: Only one child token can be specified.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [Sequence Token](sequence)
- [Choice Token](choice)
- [Repeat Token](repeat)