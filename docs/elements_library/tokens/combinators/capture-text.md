# CaptureText

The `CaptureText` token in `RCParsing` matches a child token and captures its matched text as the intermediate value. It’s useful for extracting the raw text of a match without further processing.

## Overview

The `CaptureText` token wraps a single child token and sets its matched text as the intermediate value with optional trimming settings, ignoring the child’s own intermediate value. This is ideal for cases where you need the exact text matched by a token.

## Example

Here’s an example of capturing the text of an escaped, raw string:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("escaped_string")
    .CaptureText(b => b.EscapedTextPrefix(prefix: '\\', '\\', '\"')); // Important note: child (EscapedTextPrefix) won't calculate intermediate value

builder.CreateToken("trimmed_number_string")
    .CaptureText(b => b.Number<int>(), trimStart: 1, trimEnd: 1);

var parser = builder.Build();

var result1 = parser.MatchToken("escaped_string", "hello\\\"world");
var value1 = result1.GetIntermediateValue<string>(); // "hello\"world"

var result2 = parser.MatchToken("trimmed_number_string", "1234");
var value2 = result2.GetIntermediateValue<string>(); // "23"
```

## Use Cases

- **Raw Text Extraction**: Capture the exact text matched by a token.
- **Simple Parsing**: Use the matched text directly without transformation.

## Intermediate Values

The `CaptureText` token produces the matched text as a string, while ignoring and not calculating value of child token:

```csharp
builder.CreateToken("identifier")
    .CaptureText(b => b.Identifier());

var parser = builder.Build();
var result = parser.MatchToken("identifier", "variable");
var value = result.GetIntermediateValue<string>(); // "variable"
```

## Notes

- **Child Value Is Not Calculated**: Says that the child intermediate value should not be calculated and gives only the matched text as its intermediate value.
- **Single Child**: Only one child token can be specified.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [Map Token](map)
- [Return Token](return)
- [EscapedText Token](../escaped-text)