# TextUntil

The `TextUntil` token in `RCParsing` matches any text until one of the specified sequences is encountered, without including those sequences in the match. It’s useful for capturing raw text up to a delimiter.

## Overview

The `TextUntil` token captures all characters until it encounters one of the specified terminating sequences (e.g., newlines or specific strings). It does not include the terminating sequence in the match and produces the captured text as its intermediate value.

## Example

Here’s an example of capturing text until a newline:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("text_until_newline")
    .TextUntil('\n', '\r');

var parser = builder.Build();
var result = parser.MatchToken("text_until_newline", "hello world\n");
var value = result.GetIntermediateValue<string>(); // "hello world"
```

With multiple terminators:

```csharp
builder.CreateToken("text_until_delimiter")
    .TextUntil(";", ",");

var parser = builder.Build();
var result = parser.MatchToken("text_until_delimiter", "data;");
var value = result.GetIntermediateValue<string>(); // "data"
```

## Use Cases

- **Comment Parsing**: Capture text until a newline or other delimiter.
- **String Content**: Extract content before a closing quote or marker.
- **Custom Text**: Parse text until specific tokens or sequences.

## Intermediate Values

The `TextUntil` token produces the captured text as its intermediate value:

```csharp
builder.CreateToken("text_until_comma")
    .TextUntil(',');

var parser = builder.Build();
var result = parser.MatchToken("text_until_comma", "abc,def");
var value = result.GetIntermediateValue<string>(); // "abc"
```

## Notes

- **Terminators Excluded**: The matched text does not include the terminating sequence.
- **Trie-Based**: Uses a trie for efficient matching of terminators.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [EscapedText Token](escaped-text)
- [CaptureText Token](combinators/capture-text)
- [Sequence Token](combinators/sequence)