# Lookahead

The `Lookahead` rule in `RCParsing` ensures that a specified child rule or token matches or does not match at the current position without consuming any input. It’s useful for enforcing context-sensitive constraints in your grammar.

## Overview

A `NegativeLookahead` rule checks if the input at the current position does not match the specified child rule or token. If the child would match, the rule fails; if it would not match, the rule succeeds without consuming input. This is a zero-width assertion, meaning it does not advance the parser’s position.
A `PositiveLookahead` rule checks if the input at the current position matches the specified child rule or token. If the child matches, the rule succeeds without consuming input; if it does not match, the rule fails.

## Example

Here’s an example of using `Lookahead` to ensure a number has '1' as first character and ensure an identifier is not a reserved keyword:

```csharp
var builder = new ParserBuilder();

builder.CreateRule("number_with_1")
    .PositiveLookahead(b => b.Literal("1"))
    .Number<int>();

builder.CreateRule("non_reserved_identifier")
    .NegativeLookahead(b => b.KeywordChoice("if", "else", "while"))
    .Identifier();

var parser = builder.Build();

var result1 = parser.ParseRule("number_with_1", "123"); // Succeeds
var result2 = parser.ParseRule("number_with_1", "456"); // Fails

var result3 = parser.ParseRule("non_reserved_identifier", "variable"); // Succeeds
var result4 = parser.ParseRule("non_reserved_identifier", "if"); // Fails
```

## Use Cases

- **Context Checking**: Ensure specific patterns follow without consuming them.
- **Reserved Keywords**: Prevent identifiers from matching reserved words.
- **Syntax Validation**: Verify that a construct is followed or not followed by expected tokens.

## Notes

- **Zero-Width**: Does not consume input, so the parser position remains unchanged.
- **Performance**: Use sparingly in performance-critical grammars.

## Related Tutorials

- [Sequence Rule](sequence)
- [Number Token](../tokens/number)