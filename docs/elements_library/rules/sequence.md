# Sequence

The `Sequence` rule in `RCParsing` matches a series of child rules or tokens in a specific order. It fails if any child in the sequence fails to match. This rule is fundamental for defining structured patterns, such as variable declarations or function calls.

## Overview

A `Sequence` rule is created automatically when you add multiple child rules or tokens to a rule definition. It ensures that the input matches each child in the exact order specified. This is useful for parsing constructs that require a strict sequence, such as `var x = 5;` in a programming language.

## Example

Here's an example of a `Sequence` rule for parsing a simple variable declaration:

```csharp
var builder = new ParserBuilder();

builder.CreateRule("variable_declaration")
    .Keyword("var")        // First child: keyword "var"
    .Identifier()          // Second child: variable name
    .Literal("=")          // Third child: equals sign
    .Number<int>();        // Fourth child: integer value

var parser = builder.Build();
var result = parser.ParseRule("variable_declaration", "var x = 123");
```

In this example:
- The rule matches keyword `var`, an identifier (e.g., `x`), an equals sign (`=`), and an integer (e.g., `123`).
- If any part fails (e.g., missing `=`), the entire sequence fails.

## Use Cases

- **Variable Declarations**: Match patterns like `var name = value`.
- **Function Calls**: Parse `func(arg1, arg2)`.
- **Other complex structures**.

## Transformation

You can attach a transformation function to a `Sequence` rule to build a structured output from the parsed children:

```csharp
builder.CreateRule("variable_declaration")
    .Keyword("var")
    .Identifier()
    .Literal("=")
    .Number<int>()
    .Transform(v => new VariableDeclaration
    {
        Name = v.Children[1].Text, // Identifier text
        Value = v.GetValue<int>(3) // Number value
    });

var parser = builder.Build();
var result = parser.ParseRule("variable_declaration", "var x = 123");
var declaration = result.Value as VariableDeclaration;
```

## Notes

- **Automatic Creation**: Calling multiple methods like `.Literal()` or `.Rule()` in a rule definition implicitly creates a `Sequence`.
- **Explicit Sequence**: Use `.ToSequence()` for a single child to explicitly mark it as a sequence (rarely needed).
- **Failure Behavior**: If any child fails, the entire sequence fails, and an error is recorded unless configured otherwise.

## Related Tutorials

- [Choice Rule](choice)
- [Optional Rule](optional)