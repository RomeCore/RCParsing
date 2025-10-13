# Spaces

The `Spaces` token in `RCParsing` matches one or more space or tab characters, excluding newlines. It’s useful when you need to match horizontal whitespace without capturing newlines.

## Overview

The `Spaces` token matches sequences of spaces (` `) and tabs (`\t`). It’s a more specific version of `Whitespaces`, excluding newline characters (`\r`, `\n`).

## Example

Here’s an example of matching spaces and tabs:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("spaces")
    .Spaces();

var parser = builder.Build();
var result = parser.MatchToken("spaces", " \t ");
```

## Use Cases

- **Horizontal Spacing**: Match spaces or tabs between tokens.
- **Formatting Rules**: Parse indentation or spacing in structured text.
- **Whitespace Control**: Use when newlines need separate handling.

## Intermediate Values

The `Spaces` token does not produce an intermediate value unless wrapped with a combinator:

```csharp
builder.CreateToken("spaces")
    .CaptureText(b => b.Spaces());

var parser = builder.Build();
var result = parser.MatchToken("spaces", " \t");
var value = result.GetIntermediateValue<string>(); // " \t"
```

## Notes

- **Excludes Newlines**: Does not match `\r` or `\n`.
- **Multiple Characters**: Matches one or more space/tab characters.

## Related Tutorials

- [Whitespaces Token](whitespaces)
- [Newline Token](newline)
- [SkipWhitespaces Token](combinators/skip-whitespaces)