# Optional

The `Optional` rule in `RCParsing` attempts to match a child rule or token but never fails, even if the child does not match. It’s useful for parsing optional components in a grammar, such as optional semicolons or parameters.

## Overview

An `Optional` rule wraps a single child rule or token. If the child matches, its value and text are captured; if it fails, the rule succeeds with an empty children array and no consumed text. This makes it ideal for parts of the grammar that may or may not appear.

## Example

Here’s an example of an `Optional` rule for parsing an optional semicolon at the end of a statement:

```csharp
var builder = new ParserBuilder();

builder.CreateRule("statement")
    .Identifier()
    .Optional(b => b.Literal(";"));

var parser = builder.Build();
var result1 = parser.ParseRule("statement", "x;"); // Matches with semicolon
var result2 = parser.ParseRule("statement", "x");  // Matches without semicolon
```

## Use Cases

- **Optional Terminators**: Parse optional semicolons or commas in languages like JavaScript.
- **Optional Parameters**: Handle optional function parameters or modifiers.
- **Flexible Syntax**: Allow optional keywords or tokens in a grammar.

## Transformation

The `Optional` rule propagates the child’s value if it matches; otherwise, the value is `null`. You can use a transformation to handle this:

```csharp
builder.CreateRule("statement")
    .Identifier()
    .Optional(b => b.Literal(";"))
    .Transform(v => new Statement
    {
        Name = v.Children[0].Text,
        HasSemicolon = v.Children[1].Children.Count == 1
    });

var parser = builder.Build();
var result = parser.ParseRule("statement", "x;");
var statement = result.Value as Statement; // Name: "x", HasSemicolon: true
```

## Notes

- **Never Fails**: The `Optional` rule always succeeds, making it safe for optional grammar components.
- **Empty Children**: If the child does not match, the `Children` array is empty, and the `Value` is `null` unless transformed.
- **Single Child**: Only one child rule or token can be specified.

## Related Tutorials

- [Sequence Rule](sequence)
- [Choice Rule](choice)
- [Repeat Rule](repeat)