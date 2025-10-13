# Lookahead

The `Lookahead` token in `RCParsing` ensures that a specified child token does not match at the current position without consuming input. It’s a zero-width assertion for enforcing constraints.

## Overview

A `NegativeLookahead` token checks if the input does not match the specified child token. If the child would match, the token fails; if it would not match, the token succeeds without consuming input.
A `PositiveLookahead` token checks if the input matches the specified child token. If the child matches, the token succeeds without consuming input; if it does not match, the token fails.

## Example

Here’s an example of ensuring an identifier is not a keyword:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("non_keyword_identifier")
    .NegativeLookahead(b => b.KeywordChoice("if", "else"))
    .Identifier();

var parser = builder.Build();
var result1 = parser.MatchToken("non_keyword_identifier", "variable"); // Succeeds
var result2 = parser.MatchToken("non_keyword_identifier", "if"); // Fails
```

## Use Cases

- **Keyword Avoidance**: Prevent matching reserved keywords.
- **Pattern Constraints**: Ensure specific tokens are not present.
- **Context-Sensitive Tokens**: Enforce rules based on what follows.

## Intermediate Values

The `NegativeLookahead` token does not produce an intermediate value since its child needs to be failed, but `PositiveLookahead` propagates intermediate value from its child:

```csharp
builder.CreateToken("non_keyword_identifier")
    .NegativeLookahead(b => b.KeywordChoice("if", "else"))
    .CaptureText(b => b.Identifier())
    .Pass(v => v[1]);

builder.CreateToken("number_with_leading_one")
    .PositiveLookahead(b => b.Literal("1"))
    .Number<int>()
    .Pass(v => (string)v[0] + ((int)v[1]).ToString());

var parser = builder.Build();

var result1 = parser.MatchToken("non_keyword_identifier", "variable");
var value1 = result1.GetIntermediateValue<string>(); // "variable"

var result2 = parser.MatchToken("number_with_leading_one", "123");
var value2 = result2.GetIntermediateValue<string>(); // "1123" string
```

## Notes

- **Zero-Width**: Does not consume input.
- **Failure on Match**: Fails if the child token would match.
- **Parser Guidance**: Avoid incorrect parsing paths.
- **Context Checking**: Ensure specific tokens follow without consuming them.
- **No Rules**: Tokens cannot reference rules, only other tokens.

## Related Tutorials

- [Identifier Token](../identifier)
- [Choice Token](choice)