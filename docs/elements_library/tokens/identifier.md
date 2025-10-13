# Identifier

The `Identifier` token in `RCParsing` matches sequences of characters that form identifiers, such as variable names or function names, with customizable start and continue predicates.

## Overview

The `Identifier` token matches a sequence starting with a character that satisfies a start predicate, followed by zero or more characters that satisfy a continue predicate. It supports ASCII and Unicode identifiers by default and allows custom predicates for flexibility.

## Example

Here’s an example of matching a standard ASCII identifier:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("identifier")
    .Identifier();

var parser = builder.Build();
var result = parser.MatchToken("identifier", "variable_123");
```

With custom predicates:

```csharp
builder.CreateToken("custom_identifier")
    .Identifier(
        start => char.IsLetter(start),
        cont => char.IsLetterOrDigit(cont) || cont == '-',
        minLength: 2
    );

var parser = builder.Build();
var result = parser.MatchToken("custom_identifier", "ab-12");
```

## Use Cases

- **Variable Names**: Parse identifiers in programming languages.
- **Custom Identifiers**: Match domain-specific identifiers with custom rules.
- **Standard Identifiers**: Use default ASCII or Unicode identifier rules.

## Intermediate Values

The `Identifier` token does not produce an intermediate value unless wrapped with a combinator:

```csharp
builder.CreateToken("identifier")
    .CaptureText(b => b.Identifier());

var parser = builder.Build();
var result = parser.MatchToken("identifier", "variable");
var value = result.GetIntermediateValue<string>(); // "variable"
```

## Notes

- **Default Predicates**: ASCII (`[a-zA-Z_][a-zA-Z0-9_]*`) or Unicode (`char.IsLetter` for start, `char.IsLetterOrDigit` or `_` for continue).
- **Minimum Length**: Must be at least 1; use `minLength` for custom constraints.

## Related Tutorials

- [Character Token](character)
- [RepeatCharacters Token](combinators/repeat-characters)
- [Keyword Token](keyword)