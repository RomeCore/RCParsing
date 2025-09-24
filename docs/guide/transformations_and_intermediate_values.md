---
title: Transformation and intermediate values
icon: diagram-project
---

The true power of a parser lies in its ability to transform raw text into structured data. In `RCParsing`, this is achieved through a system of *intermediate values* and *transformation functions* attached to your rules.

### Value Propagation: Intermediate Values, Combinators and `Pass`

During the parsing phase, some built-in token patterns generates an **intermediate value**:

- `Regex`: Returns the `Match` object.
- `EscapedText` (all variations): Returns the fully processed string with all escape sequences applied.
- `Number`: Returns the `double`, `float`, `int` or any other supported C# numeric type, based on `NumberType` flag or generic argument when you created it.
- `Literal`, `LiteralChar`, `LiteralChoice`: Return the exact literal string that was matched (useful for case-insensitive matching to get the original text).
- `Optional` and `Choice`: Return the intermediate value of their matched child.
- **For others:** You can look down to the token patterns library and see what they returns.

For repeat or sequential tokens (tokens built by chaining patterns like `.Literal("\"").EscapedTextPrefix(...).Literal("\"")`), you use the `.Pass()` method to control which child's intermediate value is propagated upwards to become the token's own intermediate value. `.Pass(v => v[1])` tells the token to use the intermediate value from the second child (index 1) in the sequence:

```csharp
builder.CreateToken("string")
	.Literal('"')
	.EscapedTextPrefix(prefix: '\\', '\\', '\"') // Value of this element will be passed up
	.Literal('"')
	.Pass(index: 1);
```

Or use `Between` combinator:

```csharp
builder.CreateToken("string")
	.Between(
		b => b.Literal('"'),
		b => b.EscapedTextPrefix(prefix: '\\', '\\', '\"'), // Value of this element (middle) will be passed up
		b => b.Literal('"')
	);
```

There is the useful combinators:

- `Between`: Matches a sequence of three elements and passes up the middle element.
- `First`: Matches a sequence of two elements and passes up the first element.
- `Second`: Matches a sequence of two elements and passes up the second element.
- `Map`: Matches a single element and applies the function to transform child's value.
- `Return`: Matches a single element and returns the specified fixed value.
- `CaptureText`: Matches a single element and captures the text of the child match.
- **For more combinators you can look down in the library**.

Since token's AST nodes is leaf and cannot have their own children, they have intermediate values system instead. Tokens that can have child tokens propogates intermediate values based on their type:

- `Choice` and `Optional`: Passes child's intermediate value up if has.
- `Repeat` and `Sequence`: Passes children's value using the passage function, that you can define via `Pass` using function or index to pass.

### The `ParsedRuleResult` Object

When a complete rule is parsed, parser produces a `ParsedRuleResult` object that is a wrapper around intermediate AST object. This object is **entirely lazy-evaluated**, rich representation of the parse result, providing access to the captured text, the parsed value, child nodes, and other metadata. It is the primary context object passed to your transformation functions.

Key properties of `ParsedRuleResult`:

- `Text`: The substring of the original input that this rule captured.
- `Span`: The captured text as a `ReadOnlySpan<char>` for efficient processing.
- `Value`: The final transformed value of this rule, computed lazily by invoking the rule's value factory.
- `IntermediateValue`: The raw, un-transformed value produced during the main parsing phase (e.g., a `Match` object for a regex token, or an escaped string for an `EscapedText` token).
- `Children`: An array of `ParsedRuleResult` objects representing the child nodes of this rule (e.g., the parts of a sequence). This array is also built lazily for performance.
- `IsToken`: Indicates if this result represents a token.
- `Rule`: The parser rule definition that produced this result.

### Transformation Functions

Transformation functions are defined using `.Transform()` on a rule. They take a `ParsedRuleResult` (`v`) and return the final value for that rule.

**Crucially, tokens themselves do not have transformation functions.** However, you can attach a default value factory to a token pattern using an overload like `.Regex(pattern, factory: transformFunction)` or `.Regex(pattern).Transform(transformFunction)`. This factory is not used by the token itself but is inherited by any rule that uses that token and doesn't define its own transformation. This promotes reuse.

Inside a `.Transform()` function, you build your final value by inspecting the `ParsedRuleResult`:

- `v.Text`: Get the exact text the rule matched.
- `v.Children`: Access the results of child rules/tokens by index.
- `v.GetValue(int index)`: A shortcut to get the final `Value` of a specific child as `object`.
- `v.GetValue<T>(int index)`: A type-safe shortcut to get the final `Value` of specific type of a specific child.
- `v.IntermediateValue`: Access the raw intermediate value if the built-in pattern produced one.
- `v.GetIntermediateValue<T>()`: A shortcut to get the `IntermediateValue` of a specific child.
- `v.GetIntermediateValue<T>(int index)`: A type-safe shortcut to get the `IntermediateValue` of a specific child.

**Important!** Some rule types have predefined transormation functions:

- `Choice` and `Optional`: Passes the child's value through (if has, otherwise it will be `null`).
- `Repeat` and `RepeatSeparated` (all variants): Passes and array of children's `Value`s.
- `Token`: Rule with child token passes token's intermediate value.

Here is detailed example of transformation functions:
```csharp
// Example: Transforming a simple arithmetic sequence
builder.CreateRule("expression")
    .Rule("value")
    .LiteralChoice("+", "-") // Child index 1
    .Rule("value")           // Child index 2
    .Transform(v => {
        // Get the final values of the child 'value' rules
        var leftOperand = v.GetValue<double>(0);
        var operatorSymbol = v.Children[1].Text; // Get the text of the literal
        var rightOperand = v.GetValue<double>(2);

        return operatorSymbol switch
        {
            "+" => leftOperand + rightOperand,
            "-" => leftOperand - rightOperand,
            _ => throw new InvalidOperationException($"Unexpected operator {operatorSymbol}")
        };
    });

// Example: A token with a default value factory. Rules using 'number' will get this value unless they override it.
builder.CreateToken("number")
    .Regex(@"-?\d+(?:\.\d+)?")
    .Transform(v => double.Parse(v.GetIntermediateValue<Match>().Value)); // v.Match is the intermediate value for regular expressions

// Example: Using .Pass() in a sequential token to propagate an intermediate value
builder.CreateToken("string")
    .Literal("\"")
    .EscapedTextPrefix(prefix: '\\', '\\', '\"') // Child 1 produces the final string as its intermediate value
    .Literal("\"")
    .Pass(1); // Propagate the intermediate value from the EscapedTextPrefix child (index 1) and it automatically transforms into a Value
```

This combination of intermediate values, `Pass`, and `Transform` provides a flexible and powerful mechanism to cleanly build your desired output model directly from the parse tree.