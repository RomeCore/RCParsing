# EscapedText

The `EscapedText` token in `RCParsing` matches text with escape sequences, such as those found in quoted strings. It’s optimized for parsing text with predefined or custom escape mappings.

## Overview

The `EscapedText` token matches text until it encounters specified sequences, applying escape mappings to produce a processed string. It supports predefined strategies like character prefixes or double characters and allows custom escape mappings. The token produces the unescaped text as its intermediate value.

## Example

Here’s an example of parsing a JSON-like string:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("json_string")
    .EscapedTextPrefix(prefix: '\\', '\\', '\"');

var parser = builder.Build();
var result = parser.MathToken("json_string", "hello\\\"world");
var value = result.GetIntermediateValue<string>(); // "hello"world"
```

With custom escape mappings:

```csharp
var escapeMappings = new Dictionary<string, string>
{
    { "\\q", "\"" },
    { "\\n", "\n" }
};
var forbiddenSequences = new[] { "@" };

builder.CreateToken("custom_text")
    .EscapedText(escapeMappings, forbiddenSequences);

var parser = builder.Build();
var result = parser.MatchToken("custom_text", "hello\\qworld\\n");
var value = result.GetIntermediateValue<string>(); // "hello"world\n"
```

## Use Cases

- **Quoted Strings**: Parse strings with escape sequences, like JSON or C# strings.
- **Custom Escapes**: Handle domain-specific escape sequences.
- **Text Processing**: Extract and process text with defined transformations.

## Intermediate Values

The `EscapedText` token produces the processed (unescaped) string as its intermediate value:

```csharp
builder.CreateToken("sql_string")
    .EscapedTextDoubleChars('\\', '\"');

var parser = builder.Build();
var result = parser.MatchToken("sql_string", "hello''world");
var value = result.GetIntermediateValue<string>(); // "hello'world"
```

## Notes

- **Trie-Based**: Uses a trie for efficient matching of escape sequences.
- **Predefined Strategies**: Supports `EscapedTextPrefix` and `EscapedTextDoubleChars`.
- **Custom Mappings**: Allows flexible escape sequence definitions.

## Related Tutorials

- [TextUntil Token](text-until)
- [CaptureText Token](combinators/capture-text)
- [Between Token](combinators/between)