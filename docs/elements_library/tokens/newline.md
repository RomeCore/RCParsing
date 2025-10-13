# Newline

The `Newline` token in `RCParsing` matches a single newline sequence, such as `\n`, `\r`, or `\r\n`. It’s useful for parsing line breaks in structured text.

## Overview

The `Newline` token matches any single newline sequence, ensuring consistent handling of line endings across platforms. It does not match multiple newlines unless wrapped in a `Repeat` token.

## Example

Here’s an example of matching a newline:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("nl")
    .Newline();

var parser = builder.Build();
var result = parser.MatchToken("nl", "\n");
```

## Use Cases

- **Line Breaks**: Parse line endings in text files or scripts.
- **Structured Formats**: Handle newlines in formats like CSV or YAML.
- **Grammar Rules**: Explicitly match newlines in specific contexts.

## Intermediate Values

The `Newline` token does not produce an intermediate value unless wrapped with a combinator:

```csharp
builder.CreateToken("nl")
    .CaptureText(b => b.Newline());

var parser = builder.Build();
var result = parser.MatchToken("nl", "\r\n");
var value = result.GetIntermediateValue<string>(); // "\r\n"
```

## Notes

- **Single Newline**: Matches exactly one newline sequence.
- **Cross-Platform**: Handles `\n`, `\r`, and `\r\n`.

## Related Tutorials

- [Whitespaces Token](whitespaces)
- [Spaces Token](spaces)
- [Repeat Token](combinators/repeat)