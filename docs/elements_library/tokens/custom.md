# Custom

The `Custom` token in `RCParsing` allows you to define a token with custom matching logic using a `TokenPattern` implementation or a function. It’s ideal for specialized parsing needs that standard tokens cannot handle.

## Overview

The `Custom` token lets you implement a custom `TokenPattern` or provide a function to define how the token matches input and produces an intermediate value. This provides maximum flexibility for domain-specific parsing.

## Example

Here’s an example of a custom token that matches a specific character:

```csharp
var builder = new ParserBuilder();

builder.CreateToken("custom")
    .Custom((self, input, start, end, parameter, calculateIntermediateValue) =>
    {
        var contextChar = (char)parameter;

        if (start >= end || input[start] != contextChar)
            return ParsedElement.Fail;
        
        return new ParsedElement(
            startIndex: start,
            length: 1,
            intermediateValue: "matched " + contextChar
        );
    });

var parser = builder.Build();
var result = parser.MatchToken("custom", "x", parameter: 'x');
var value = result.GetIntermediateValue<string>(); // "matched x"
```

Using a custom implementation of `TokenPattern`:

```csharp
public class MyTokenPattern : TokenPattern
{
    public override ParsedElement Match(
        string input,
        int start,
        int end,
        object parameter,
        bool calculateIntermediateValue,
        ref ParsingError furthestError)
    {
        if (start >= end || input[start] != 'z')
            return ParsedElement.Fail;
        
        return new ParsedElement(
            startIndex: start,
            length: 1,
            intermediateValue: "z"
        );
    }

    // It's recommended to implement these methods too
    public override int GetHashCode() { ... }
    public override bool Equals(object? obj) { ... }
}

var builder = new ParserBuilder();
builder.CreateToken("my_token")
    .Token(new MyTokenPattern());

var parser = builder.Build();
var result = parser.ParseRule("my_token", "z");
var value = result.GetIntermediateValue<string>(); // "z"
```

## Use Cases

- **Domain-Specific Tokens**: Match custom patterns not covered by standard tokens.
- **Complex Logic**: Implement specialized matching rules.

## Intermediate Values

The `Custom` token produces the intermediate value specified in the `ParsedElement`. Also, calculation can be prevented when not needed (e.g. in left or right child within `Between` token).

```csharp
builder.CreateToken("custom")
    .Custom((self, input, start, end, parameter, calculateIntermediateValue) =>
    {
        if (start >= end || input[start] != 'a')
            return ParsedElement.Fail;
        
        return new ParsedElement(start, 1, 42);
    });

var parser = builder.Build();
var result = parser.ParseRule("custom", "a");
var value = result.GetIntermediateValue<int>(); // 42
```

## Notes

- **Flexibility**: Allows complete control over matching and value production.

## Related Tutorials

- [Regex Token](regex)
- [Character Token](character)
- [Map Token](map)