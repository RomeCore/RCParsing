# Table of contents

- [Main concepts](#main-concepts) - main concepts and principles of this framework
- [Rule and token building](#rule-and-token-building) - explains some things about how building tokens and rules working under the hood
- [Auto-skipping](#auto-skipping) - about configuring skip-rules and how to require whitespaces in rules when you are skipping them 
- [Deduplication](#deduplication) - why should not care about building your parser
- [Transformation and intermediate values](#transformation-and-intermediate-values) - how to build a usable value from AST
- [Settings and their overide modes](#settings-and-their-overide-modes) - about configuration of every rule with inheritance
- [Barrier tokens](#barrier-tokens) - how to parse Python-like indented syntax in ellegant manner
- [Initialization flags, debugging and performance](#initialization-flags-debugging-and-performance) - about static configuration and how it impacts on runtime and development speed

# Main concepts

`RCParsing` uses lexerless approach, but it has *tokens*, they are used as parser primitives, and they are not complex as *rules*. This library also supports *barrier tokens*.  

- **Token**: A token is a pattern that matches characters directly and it tries to match as much characters as possible, some token patterns may contain child tokens;
- **Rule**: A rule is a pattern that matches child rules and tokens based on behaviour.
- **Barrier token**: A token that has to be parsed and prevents other tokens from matching.

Parsers in this library is created via `ParserBuilder`s. When parser is being built, builder deduplicates rules and token patterns, and assigns IDs to them, then they are being compound into a `Parser`.

# Rule and token building

When you are building rules and tokens, there is some things that you may encounter.

When a second part of rule is added, it becomes a *sequence* rule:
```csharp
// Here is rule with single 'Identifier' token reference:
builder.CreateRule("rule1")
    .Identifier();

// Here is a sequence rule with two children rules, where first child have 'Identifier' token reference, and second - 'Whitespaces' token reference:
builder.CreateRule("rule2")
    .Identifier()
    .Whitespaces();
```

You cannot create empty rules and tokens, make references to unknown rules/tokens, or make direct cyclic references:
```csharp
var builder = new ParserBuilder();

// If you create an empty rule or token and don't fill it with anything, you will get an error
builder.CreateRule("rule");

// And this is prohibited too
builder.CreateRule("D")
    .Rule("F"); // F is not registered!

// This is also prohibited
builder.CreateRule("A")
    .Rule("B");
builder.CreateRule("B")
    .Rule("A");

// P.S. You will not get error for this, because reference is not direct, it's inside a sequence rule:
builder.CreateRule("C")
    .Rule("B") // If you remove this line - builder will throw an exception!
    .Rule("B");

// Here you will get a ParserBuildingException if you made one of these mistakes
builder.Build();
```

# Auto-skipping

Parser does not skip any characters by default. If you want to configure auto-skipping, you can do it by configuring the parser builder:

```csharp
builder.Settings
    .Skip(b => b.Whitespaces());
```

By default, if skip rule is set, parser will try to skip it before parsing every rule once. You can select the other skip strategy to skip it repeadetly:

```csharp
builder.Settings
    // Skip all whitespaces and C#-like comments
    .Skip(b => b.Choice(
        b => b.Whitespaces(), // Skip whitespaces
        b => b.Literal("//").TextUntil("\n", "\r"), // Skip single-line comments, newline characters will be skipped by upper choice (Whitespaces)
        b => b.Literal("/*").TextUntil("*/").Literal("*/")
    ).ConfigureForSkip(), // Prevents from error recording
    ParserSkippingStrategy.SkipBeforeParsingGreedy); // Tries to skip repeatedly until skip-rule fails
```

### How to allow whitespaces in rules when i skipping them?

This is a good question! Use this short example as my answer:

```csharp
builder.Settings
    .Skip(b => b.Whitespaces().ConfigureForSkip(),
        ParserSkippingStrategy.TryParseThenSkip);

builder.CreateRule("variable_declaration")
    .Literal("var")
    .Whitespaces() // <-- will be parsed!
    .Identifier()
    .Literal("=")
    .Rule("value");
```

When using `TryParseThenSkip` strategy, parser will try to parse first, then skip, then parse (`parse -> skip -> parse`, instead of `skip -> parse`). This allows to require rules that conflicts with skip-rules, but may be a bit slow and emit lots of unnecessary parsing errors, slightly impacting on allocation.

Here is detailed skip strategies description:

- `SkipBeforeParsing`: Parser will try to skip the skip-rule once before parsing the target rule (`skip -> parse`).
- `SkipBeforeParsingLazy`: Parser will repeatedly attempt to skip the skip-rule in an interleaved manner with the target rule, until the target rule succeeds or both skip and target rules fail. This allows incremental consumption of skip content (`skip -> parse -> skip -> parse -> ... -> parse`).
- `SkipBeforeParsingGreedy`: Parser will greedily skip the skip-rule as many times as possible before attempting to parse the target rule (`skip -> skip -> ... -> skip -> parse`).
- `TryParseThenSkip`: Parser will first attempt to parse the target rule; if parsing fails, it will skip the skip-rule once and then retry parsing the target rule (`parse -> skip -> parse`).
- `TryParseThenSkipLazy`: Parser will attempt to parse the target rule; if parsing fails, it will alternately try to skip the skip-rule and parse the target rule repeatedly until the target rule succeeds or both fail (`parse -> skip -> parse -> skip -> parse -> ... -> parse`).
- `TryParseThenSkipGreedy`: Parser will attempt to parse the target rule; if parsing fails, it will greedily skip the skip-rule as many times as possible and then retry parsing the target rule (`parse -> skip -> skip -> ... -> skip -> parse`).

# Deduplication

All rules and tokens are deduplicated when you call `builder.Build()`.  
Here you can see deduplication example:

```csharp
var builder = new ParserBuilder();

// Create rule that matches literal 'abc' token
builder.CreateRule("rule1")
    .Literal("abc");

// Create another rule with same token
builder.CreateRule("rule2")
    .Literal("abc");

// It's deduplicated here!
var parser = builder.Build();

// It's deduplicated!
Console.WriteLine(parser.GetRule("rule1").Id == parser.GetRule("rule2").Id); // True
```

Deduplication means you don't need to make that redundant links, like here:

```csharp
builder.CreateToken("LPAREN")   .Literal('(');
builder.CreateToken("RPAREN")   .Literal(')');
builder.CreateToken("COMMA")    .Literal(',');
builder.CreateToken("ID")       .Identifier();

builder.CreateRule("method")
    .Token("ID")
    .Token("LPAREN")
    .ZeroOrMoreSeparated(b => b.Token("ID"), s => s.Token("COMMA"))
    .Token("RPAREN")

builder.CreateRule("paren")
    .Token("LPAREN")
    .Token("ID")
    .Token("RPAREN");

var parser = builder.Build();

// Print count of tokens registered in parser: 
Console.WriteLine(parser.TokenPatterns.Length); // 4
```

You can just do this and make it more readable:

```csharp
builder.CreateRule("method")
    .Identifier()
    .Literal('(')
    .ZeroOrMoreSeparated(b => b.Identifier(), s => s.Literal(','))
    .Literal(')');

builder.CreateRule("paren")
    .Literal('(')
    .Identifier()
    .Literal(')');

var parser = builder.Build();
// It prints the same number:
Console.WriteLine(parser.TokenPatterns.Length); // 4
```

And you will get the **same** parser!

# Transformation and intermediate values

The true power of a parser lies in its ability to transform raw text into structured data. In `RCParsing`, this is achieved through a system of *intermediate values* and *transformation functions* attached to your rules.

### The `ParsedRuleResult` Object

When a complete rule is parsed, parser produces a `ParsedRuleResult` object that is a wrapper around intermadiate AST object. This object is a entirely lazy-evaluated, rich representation of the parse result, providing access to the captured text, the parsed value, child nodes, and other metadata. It is the primary context object passed to your transformation functions.

Key properties of `ParsedRuleResult`:

- `Text`: The substring of the original input that this rule captured;
- `Span`: The captured text as a `ReadOnlySpan<char>` for efficient processing;
- `Value`: The final transformed value of this rule, computed lazily by invoking the rule's value factory;
- `IntermediateValue`: The raw, un-transformed value produced during the initial parsing phase (e.g., a `Match` object for a regex token, or an escaped string for an `EscapedText` token);
- `Children`: An array of `ParsedRuleResult` objects representing the child nodes of this rule (e.g., the parts of a sequence). This array is also built lazily for performance;
- `IsToken`: Indicates if this result represents a token;
- `Rule`: The parser rule definition that produced this result.

### Value Propagation: Intermediate Values and `Pass`

During the parsing phase, some built-in token patterns generates an **intermediate value**:

- `Regex`: Returns the `Match` object;
- `EscapedText` (all variations): Returns the fully processed string with all escape sequences applied;
- `Literal`, `LiteralChar`, `LiteralChoice`: Return the exact literal string that was matched (useful for case-insensitive matching to get the original text);
- `Optional` and `Choice`: Return the intermediate value of their matched child.

For repeat or sequential tokens (tokens built by chaining patterns like `.Literal("\"").EscapedTextPrefix(...).Literal("\"")`), you use the `.Pass()` method to control which child's intermediate value is propagated upwards to become the token's own intermediate value. `.Pass(v => v[1])` tells the token to use the intermediate value from the second child (index 1) in the sequence.

### Transformation Functions

Transformation functions are defined using `.Transform()` on a rule. They take a `ParsedRuleResult` (`v`) and return the final value for that rule.

**Crucially, tokens themselves do not have transformation functions.** However, you can attach a default value factory to a token pattern using an overload like `.Regex(pattern, transformFunction)` or `.Regex(pattern).Transform(transformFunction)`. This factory is not used by the token itself but is inherited by any rule that uses that token and doesn't define its own transformation. This promotes reuse.

Inside a `.Transform()` function, you build your final value by inspecting the `ParsedRuleResult`:

- `v.Text`: Get the exact text the rule matched;
- `v.Children`: Access the results of child rules/tokens by index;
- `v.GetValue(int index)`: A shortcut to get the final `Value` of a specific child as `object`;
- `v.GetValue<T>(int index)`: A type-safe shortcut to get the final `Value` of specific type of a specific child;
- `v.IntermediateValue`: Access the raw intermediate value if the built-in pattern produced one;
- `v.GetIntermediateValue<T>()`: A shortcut to get the `IntermediateValue` of a specific child;
- `v.GetIntermediateValue<T>(int index)`: A type-safe shortcut to get the `IntermediateValue` of a specific child.

**Important!** Some rule types have predefined transormation functions:

- `Choice` and `Optional`: Passes the child's value through (if has, otherwise it will be `null`);
- `Repeat` and `RepeatSeparated` (all variants): Passes and array of children's `Value`s.
- `Token`: This rule with child token passes token's intermediate value.

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

# Settings and their overide modes

Parser and every rule themselves can be configured to control some behaviors of parsing. Each rule's setting can be configured with a specific override mode that determines how it propagates through the rule hierarchy. Here is a list of settings that can be configured:

- **Skipping strategy**: Controls how the parser tries to skip the skip-rules, does not work if the skip-rule is not configured.
- **Skip-rule**: The target skip-rule that the parser will try to skip when parsing rules.
- **Error handling**: Defines how rules and tokens should act when encountering an error, record it to context, do not record it (ignore) or throw it (rarely usable).
- **Barriers ignorance**: Whether ignore barriers or not.

Also, settings for each **rule** (not parser) can have *override modes*, which control inheritance behavior.  
Here is short example how you can confige the parser and each rule:

```csharp
// Configure the parser for skipping whitespaces
builder.Settings.Skip(r => r.Whitespaces());

builder.CreateRule("string")
    .Literal('"')
    .TextUntil('"')
    .Literal('"')
    // Call the Configure to apply settings for rule
	.Configure(c => c.NoSkipping()); // Remove skipping when parsing this rule
```

But if you do it like this, the parser won't skip any rules when parsing the `string` rule. Do this:

```csharp
builder.CreateRule("string")
    .Literal('"')
    .TextUntil('"')
    .Literal('"')
	.Configure(c => c.NoSkipping(), ParserSettingMode.LocalForChildrenOnly); // Apply the setting for the child rules, not the target rule (this rule is sequence currently)
```

Now parser will try to skip whitespaces before parsing the `string` rule, but it won't try to skip it when, for example, parsing the `Literal` or `TextUntil` tokens itself.

There is list of possible override/inheritance modes:

- **InheritForSelfAndChildren**, default: Applies the parent's setting for this element and all its children, ignoring any local or global *(parser)* settings.
- **LocalForSelfAndChildren**, default when configuration applied to rule: Applies the local setting for this element and all its children. This is the default when explicitly providing a local setting.
- **LocalForSelfOnly**: Applies the local setting for this element only, while propagating the parent's setting to all child elements.
- **LocalForChildrenOnly**: Applies the parent's setting for this element only, while propagating the local setting to all child elements.
- **GlobalForSelfAndChildren**: Applies the global setting for this element and all its children, ignoring any inheritance hierarchy.
- **GlobalForSelfOnly**: Applies the global setting for this element only, while propagating the parent's setting to all child elements.
- **GlobalForChildrenOnly**: Applies the parent's setting for this element only, while propagating the global setting to all child elements.

These modes provide fine-grained control over how settings propagate through your parser's rule hierarchy, allowing you to customize behavior at different levels of parsing.

Also, token patterns does not have their own configuration, but you can apply *default* configuration that will be applied to the rule that brings the token, just like `Transform` functions:

```csharp
// Configure the default configuration for token
builder.CreateToken("identifier")
    .Identifier()
    .Configure(c => c.IgnoreErrors());

// And it will be applied to the rule:
builder.CreateRule("id_rule")
    .Token("identifier");
```

# Barrier tokens

The barrier tokens is the clear way to handle complex tokens such as *indentation delta tokens* (in Python they known as `INDENT` and `DEDENT`).

Barrier tokens must be explicitly handled in the grammar rules. Unlike regular tokens that can be skipped or ignored, barrier tokens act as parsing gates - they block further token processing until they are properly matched by a grammar rule. If a barrier token appears in the input string but no grammar rule expects it at that position, parsing will fail with an error.

#### There is short example how to use them:

```csharp
var builder = new ParserBuilder();

builder.Settings
	.Skip(b => b.Whitespaces().ConfigureForSkip(),
		ParserSkippingStrategy.TryParseThenSkip); // Allows to use whitespaces in the grammar but skips them in other places.

builder.BarrierTokenizers // Adds indent and dedent tokenizer to the parser.
	.AddIndent(indentSize: 4, "INDENT", "DEDENT");

builder.CreateRule("function")
	.Literal("def")
	.Whitespaces()
	.Identifier()
	.Literal('(')
	.Literal(')')
	.Literal(':')
	.Rule("block");

builder.CreateRule("block")
	.Token("INDENT") // Always captures 'INDENT' token.
	.OneOrMore(b => b.Rule("statement"))
	.Token("DEDENT");

builder.CreateRule("statement")
	.Identifier()
	.Literal('(')
	.Literal(')');

builder.CreateMainRule("main")
	.ZeroOrMore(b => b.Rule("function"))
	.EOF(); // Sure that we capture all the input.

var parser = builder.Build();

string input =
"""
def foo():
	bar()

	spam()

def baz():
	eggs()
""";

parser.Parse(input);
```

#### Here is explanation of that example:

- First, parser will invoke all the barrier tokenizers (just `IndentTokenizer` in our case) and prepare barrier tokens for fast lookup in parsing process. Here is how barrier tokens are placed (they shown in `<>` chevrons):
    ```xml
    def foo():
        <INDENT>bar
        <-- Blank line is skipped
        spam

    <DEDENT>def baz():
        <INDENT>eggs
    <DEDENT> <-- This DEDENT is placed at the end of file (EOF)
    ```
- Parser successfully parses `def foo():` and begins to capture the `block` rule, with `INDENT` barrier passed (as it expected).
- When rule `block` is parsing, it tries to capture as more `statement`s as possible, but in front of second `def` it encounters `DEDENT` barrier token and fails, then `block` closes with `DEDENT` successfully parsed.
- Finally, parser will parse all the input and returns the AST.

# Initialization flags, debugging and performance

You can manually apply initialization flags on parser elements using `Setting` property on builder:
```csharp
builder.Settings.UseInitFlags(...);
```
Or use some of sugar versions:
```csharp
builder.Settings.UseInlining(); // Inlines some rules instead of calling the Parser
builder.Settings.UseFirstCharacterMatch(); // Will choose rules based on lookahead, not effective on simple grammars
builder.Settings.UseCaching(); // All elements will use memoization, impacts on performance but crucial for complex grammars

builder.Settings.WriteStackTrace(); // All elements will record stack trace for best debugging
builder.Settings.DetailedErrors(); // When parser throws errors, they will be display more information
builder.Settings.ErrorFormatting(
	ErrorFormattingFlags.DisplayRules |
	ErrorFormattingFlags.DisplayMessages |
	ErrorFormattingFlags.MoreGroups); // DetailedErrors is the sugar for this <--
builder.Settings.UseDebug(); // Uses both WriteStackTrace and DetailedErrors
```

Here is default error example on JSON grammar (ooops, i put extra comma in object!):

```
RCParsing.ParsingException : One or more errors occurred during parsing:

The line where the error occurred:
	"tags": ["tag1", "tag2", "tag3"],,
                   line 5, column 35 ^

',' is unexpected character, expected one of:
  'string'
  literal: '}'

...and more errors omitted.
```

And here is errors with `UseDebug()` mode:

```
RCParsing.ParsingException : One or more errors occurred during parsing:

Failed to parse token / Failed to parse sequence rule.

The line where the error occurred:
	"tags": ["tag1", "tag2", "tag3"],,
                   line 5, column 35 ^

',' is unexpected character, expected one of:
  'pair'
  'string'
  literal: '}'

['pair'] Stack trace (top call recently):
- SeparatedRepeat{0..} (allow trailing): 'pair' <--- here
  sep literal: ','
- Sequence 'object':
    literal: '{'
    SeparatedRepeat{0..} (allow trailing)... <-- here
    literal: '}'
- Choice 'value':
    'string'
    'number'
    'boolean'
    'null'
    'array'
    'object' <-- here
- Sequence 'content':
    'value' <-- here
    [EOF]

['object'] Stack trace (top call recently):
- Choice 'value':
    'string'
    'number'
    'boolean'
    'null'
    'array'
    'object' <-- here
- Sequence 'content':
    'value' <-- here
    [EOF]

===== NEXT ERROR =====

Failed to parse token

The line where the error occurred:
	"tags": ["tag1", "tag2", "tag3"],,
                 line 5, column 33 ^

']' is unexpected character, expected literal: ','

===== NEXT ERROR =====

...
```

There is JSON benchmark demonstrating how different settings impacts on performance:

| Method                  | Mean      | Error     | StdDev   | Ratio | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|------------------------ |----------:|----------:|---------:|------:|---------:|---------:|---------:|-----------:|------------:|
| JsonBigInlinedNoValue   | 179.97 us | 11.005 us | 0.603 us |  0.56 |  16.1133 |   5.3711 |        - |  263.34 KB |        0.52 |
| JsonBigInlined          | 253.61 us | 30.213 us | 1.656 us |  0.79 |  31.2500 |  24.4141 |        - |  510.75 KB |        1.00 |
| JsonBig                 | 320.36 us | 13.648 us | 0.748 us |  1.00 |  31.2500 |  24.4141 |        - |  510.75 KB |        1.00 |
| JsonBigDebug            | 324.26 us | 29.745 us | 1.630 us |  1.01 |  32.2266 |  19.0430 |        - |  527.34 KB |        1.03 |
| JsonBigDebugMemoized    | 675.42 us | 32.093 us | 1.759 us |  2.11 | 117.1875 | 117.1875 | 117.1875 | 1037.89 KB |        2.03 |
|                         |           |           |          |       |          |          |          |            |             |
| JsonShortInlinedNoValue |  10.13 us |  0.287 us | 0.016 us |  0.60 |   0.8698 |   0.0153 |        - |   14.45 KB |        0.52 |
| JsonShortInlined        |  13.82 us |  1.976 us | 0.108 us |  0.82 |   1.7090 |   0.0763 |        - |   28.02 KB |        1.00 |
| JsonShort               |  16.90 us |  0.939 us | 0.051 us |  1.00 |   1.7090 |   0.0610 |        - |   28.02 KB |        1.00 |
| JsonShortDebug          |  17.35 us |  2.230 us | 0.122 us |  1.03 |   1.7700 |   0.0610 |        - |   28.93 KB |        1.03 |
| JsonShortDebugMemoized  |  27.34 us |  1.097 us | 0.060 us |  1.62 |   3.2043 |   0.3357 |        - |   52.48 KB |        1.87 |

Note: `*NoValue` methods doesn't calculate value from AST, just parsing text and returns AST.