# Table of contents

- [Main concepts](#main-concepts) - core concepts and principles of this framework
- [Rule and token building](#rule-and-token-building) - explains some things about how building tokens and rules working under the hood
- [Auto-skipping](#auto-skipping) - about configuring skip-rules and how to require whitespaces in rules when you are skipping them 
- [Deduplication](#deduplication) - why you should not worry about references when building your parser
- [Transformation and intermediate values](#transformation-and-intermediate-values) - how to build a usable value from AST
- [Settings and their override modes](#settings-and-their-override-modes) - about configuration of every rule with inheritance
- [Barrier tokens](#barrier-tokens) - how to parse Python-like indented syntax in elegant manner
- [Initialization flags, debugging and performance](#initialization-flags-debugging-and-performance) - about static configuration and how it impacts on runtime and development speed
- [Rule types](#rule-types)
    - [Sequence](#sequence)
    - [Choice](#choice)
    - [Optional](#optional)
    - [Repeat](#repeat)
    - [SeparatedRepeat](#separatedrepeat)
- [Token pattern types](#token-pattern-types)
    - [Combination primitives](#combination-primitives)
        - [Sequence](#sequence-1)
        - [Choice](#choice-1)
        - [Optional](#optional-1)
        - [Repeat](#repeat-1)
    - [Matching primitives](#matching-primitives)
        - [EOF](#eof)
        - [Literal, LiteralChar and LiteralChoice](#literal-literalchar-and-literalchoice)
        - [Number](#number)
        - [Regex](#regex)
        - [EscapedText](#escapedtext)
        - [Predicate-based (Character, RepeatCharacters, Identifier, Whitespaces)](#predicate-based-character-repeatcharacters-identifier-whitespaces)
        - [Custom tokens](#custom-tokens)

# Main concepts

`RCParsing` uses lexerless approach, but it has *tokens*, they are used as parser primitives, and they are not complex as *rules*. This library also supports *barrier tokens*.  

- **Token**: A token is a pattern that matches characters directly and it tries to match as much characters as possible, some token patterns may contain child tokens;
- **Rule**: A rule is a more complex pattern that matches child rules or tokens, based on behaviour, but not the characters directly.
- **Barrier token**: A token that **must** be parsed and prevents other tokens from matching.

Parsers in this library is created via `ParserBuilder`s. When parser is being built, builder deduplicates rules and token patterns, and assigns IDs to them, then they are being compound into a `Parser`.

# Rule and token building

First, you need to create a `ParserBuilder` to let all the IDs assignments, deduplication and other hard work to it:
```csharp
var builder = new ParserBuilder();
```

And you need to create rules to begin parsing anything:
```csharp
builder.CreateRule("my_rule")
    .LiteralChoice("foo", "bar"); // Adds the token reference to rule
```

Then, build the parser and parse the string:
```csharp
var parser = builder.Build();

// Parse the "foo" string into AST (Abstract Syntax Tree) using the "my_rule"!
var ast = parser.ParseRule("my_rule", "foo");
```

You want to avoid specifying the `my_rule` manually every time when using the `Parser`? Here you go!  
Just create the main rule and parser calling become more compact:

```csharp
// Create the Main rule
builder.CreateMainRule("my_rule")
    .LiteralChoice("foo", "bar");

var parser = builder.Build();

// It automatically uses rule that we choose as main, the "my_rule"
var ast = parser.Parse("foo");
```

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

**Important!**  
You cannot create empty rules and tokens, make references to unknown rules/tokens, or make direct cyclic references:
```csharp
var builder = new ParserBuilder();

// If you create an empty rule or token and don't fill it with anything, you will get an error
builder.CreateRule("rule");

// And this is prohibited too (reference to unknown rule)
builder.CreateRule("A")
    .Rule("B"); // B is not registered!

// This is also prohibited (cyclic references)
builder.CreateRule("C")
    .Rule("D");
builder.CreateRule("D")
    .Rule("C");

// P.S. You will not get error for this, because reference is not direct, it's inside a sequence rule (left-recursion is currently not detectable yet):
builder.CreateRule("C")
    .Rule("D") // If you remove this line - builder will throw an exception!
    .Rule("D");

// Here you will get a ParserBuildingException if you made one of these mistakes
builder.Build();
```

Also note that you cannot attach rules to tokens, like this:
```csharp
builder.CreateToken("token")
    .Rule("some_rule"); // Ehehe, this method does not exist!
```

# Auto-skipping

Parser does not skip any characters by default. If you want to configure auto-skipping, you can do it by configuring the parser builder:

```csharp
builder.Settings
    .Skip(b => b.Whitespaces());
```

You also may want to prevent this rule from recording errors, then do this:

```csharp
builder.Settings
    .Skip(b => b.Whitespaces().ConfigureForSkip());
// Or use the shortcut:
builder.Settings.SkipWhitespaces();
```

By default, if skip rule is set, parser will try to skip it before parsing every rule once (`skip -> parse`). You can select the other skip strategy to skip it repeatedly:

```csharp
builder.Settings
    // Skip all whitespaces and C#-like comments
    .Skip(b => b.Choice(
        b => b.Whitespaces(), // Skip whitespaces
        b => b.Literal("//").TextUntil("\n", "\r"), // Skip single-line comments, newline characters will be skipped by upper choice (Whitespaces)
        b => b.Literal("/*").TextUntil("*/").Literal("*/") // Multi-line comments
    ).ConfigureForSkip(), // Prevents from error recording
    ParserSkippingStrategy.SkipBeforeParsingGreedy); // Tries to skip repeatedly until skip-rule fails
```

Then parser will skip like this: `skip -> skip -> ... -> skip -> parse`.

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

When using `TryParseThenSkip` strategy, parser will try to parse first, then skip, then parse (`parse -> skip -> parse`, instead of `skip -> parse`). This allows to require rules that conflicts with skip-rules, but may be a bit slow and emit some unnecessary parsing errors into context, slightly impacting on allocation and performance.

### Supported skip strategies

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

// It's deduplicated here:
var parser = builder.Build();

// They have same IDs!
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

When a complete rule is parsed, parser produces a `ParsedRuleResult` object that is a wrapper around intermediate AST object. This object is **entirely lazy-evaluated**, rich representation of the parse result, providing access to the captured text, the parsed value, child nodes, and other metadata. It is the primary context object passed to your transformation functions.

Key properties of `ParsedRuleResult`:

- `Text`: The substring of the original input that this rule captured.
- `Span`: The captured text as a `ReadOnlySpan<char>` for efficient processing.
- `Value`: The final transformed value of this rule, computed lazily by invoking the rule's value factory.
- `IntermediateValue`: The raw, un-transformed value produced during the main parsing phase (e.g., a `Match` object for a regex token, or an escaped string for an `EscapedText` token).
- `Children`: An array of `ParsedRuleResult` objects representing the child nodes of this rule (e.g., the parts of a sequence). This array is also built lazily for performance.
- `IsToken`: Indicates if this result represents a token.
- `Rule`: The parser rule definition that produced this result.

### Value Propagation: Intermediate Values and `Pass`

During the parsing phase, some built-in token patterns generates an **intermediate value**:

- `Regex`: Returns the `Match` object.
- `EscapedText` (all variations): Returns the fully processed string with all escape sequences applied.
- `Number`: Returns the `double`, `float`, `int` or any other supported C# numeric type, based on `NumberType` flag or generic argument when you created it.
- `Literal`, `LiteralChar`, `LiteralChoice`: Return the exact literal string that was matched (useful for case-insensitive matching to get the original text).
- `Optional` and `Choice`: Return the intermediate value of their matched child.

For repeat or sequential tokens (tokens built by chaining patterns like `.Literal("\"").EscapedTextPrefix(...).Literal("\"")`), you use the `.Pass()` method to control which child's intermediate value is propagated upwards to become the token's own intermediate value. `.Pass(v => v[1])` tells the token to use the intermediate value from the second child (index 1) in the sequence.

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

Also, **intermediate values** have own propagation system. Since token's AST nodes is leaf and cannot have their own children, they have intermediate values system instead. Tokens that can have child tokens propogates intermediate values based on their type:

- `Choice` and `Optional`: Passes child's intermediate value up if has.
- `Repeat` and `Sequence`: Passes children's value using the passage function, that you can define via `Pass` using function or index to pass.
- `TokenRule` passes child token's intermediate value.

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

# Settings and their override modes

Parser and every rule themselves can be configured to control some behaviors of parsing. Each rule's setting can be configured with a specific override mode that determines how it propagates through the rule hierarchy. Here is a list of settings that can be configured:

- **Skipping strategy**: Controls how the parser tries to skip the skip-rules, does not work if the skip-rule is not configured.
- **Skip-rule**: The target skip-rule that the parser will try to skip when parsing rules.
- **Error handling**: Defines how rules and tokens should act when encountering an error, record it to context, do not record it (ignore) or throw it (rarely usable).
- **Barriers ignorance**: Whether ignore barrier tokens or not.

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

- **InheritForSelfAndChildren**, default: Applies the parent's setting for this element and all its children, ignoring any local or global (parser) settings.
- **LocalForSelfAndChildren**, default when configuration applied to rule: Applies the local setting for this element and all its children. This is the default when explicitly providing a local setting.
- **LocalForSelfOnly**: Applies the local setting for this element only, while propagating the parent's setting to all child elements.
- **LocalForChildrenOnly**: Applies the parent's setting for this element only, while propagating the local setting to all child elements.
- **GlobalForSelfAndChildren**: Applies the global setting for this element and all its children, ignoring any inheritance hierarchy.
- **GlobalForSelfOnly**: Applies the global setting for this element only, while propagating the parent's setting to all child elements.
- **GlobalForChildrenOnly**: Applies the parent's setting for this element only, while propagating the global setting to all child elements.

These modes provide fine-grained control over how settings propagate through your parser's rule hierarchy, allowing you to customize behavior at different levels of parsing.

Also, token patterns does not have their own configuration, but you can apply *default* configuration that will be applied to the rule that brings the token, just like `Transform` functions:

```csharp
// Attach the default configuration for token
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

Some settings in the parser elements (tokens and rules) can be *static* (unlike *runtime* settings that support inheritance, e.g. a skip-rule, skip strategy and error handling) and may be applied to specific elements at the **compiling** stage of building (the `builder.Build()` call).

You can apply initialization flags on parser elements using `Setting` property on builder:
```csharp
builder.Settings.UseInitFlags(...);
```

There is example of applying `EnableMemoization` initialization flag:
```csharp
builder.Settings.UseInitFlags(ParserInitFlags.EnableMemoization);
```

And there is some of shortcut/sugar versions:
```csharp
builder.Settings.UseInlining(); // Inlines some rules instead of calling the Parser
builder.Settings.UseFirstCharacterMatch(); // Will choose rules based on lookahead, not effective on simple grammars
builder.Settings.UseCaching(); // All elements will use memoization, impacts on performance but crucial for complex grammars

builder.Settings.WriteStackTrace(); // All elements will record stack trace for best debugging
builder.Settings.DetailedErrors(); // When parser throws errors, they will display more information
builder.Settings.ErrorFormatting(
	ErrorFormattingFlags.DisplayRules |
	ErrorFormattingFlags.DisplayMessages |
	ErrorFormattingFlags.MoreGroups); // DetailedErrors is the sugar for this <--
builder.Settings.UseDebug(); // Uses both WriteStackTrace and DetailedErrors
```

If you use one previous methods, initialization flags will be applied to *ALL* parser elements.

*Want to apply these flags more precisely?*  
There is example how to apply `StackTraceWriting` initialization flag on the `TokenParserRule` that holds `WhitespacesTokenPattern`:

```csharp
builder.Settings.UseInitFlagsOn(ParserInitFlags.StackTraceWriting,
	elem => elem is TokenParserRule tokenRule &&
	tokenRule.TokenPattern is WhitespacesTokenPattern);
```

Here is default error example on JSON grammar (ooops, i put extra comma in object definition):

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

| Method                   | Mean       | Error      | StdDev    | Ratio | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|------------------------- |-----------:|-----------:|----------:|------:|---------:|---------:|---------:|-----------:|------------:|
| JsonBig_InlinedNoValue   | 168.238 us |  6.6452 us | 0.3642 us |  0.59 |  12.2070 |   2.9297 |        - |  200.61 KB |        0.42 |
| JsonBig_Inlined          | 229.147 us | 16.2147 us | 0.8888 us |  0.80 |  26.8555 |  13.4277 |        - |  442.37 KB |        0.93 |
| JsonBig_Default          | 287.437 us | 20.5160 us | 1.1245 us |  1.00 |  28.8086 |  15.6250 |        - |  474.43 KB |        1.00 |
| JsonBig_Debug            | 307.931 us | 34.3045 us | 1.8803 us |  1.07 |  29.7852 |  15.6250 |        - |  491.02 KB |        1.03 |
| JsonBig_DebugMemoized    | 601.280 us | 41.6892 us | 2.2851 us |  2.09 | 117.1875 | 117.1875 | 117.1875 | 1001.57 KB |        2.11 |
|                          |            |            |           |       |          |          |          |            |             |
| JsonShort_InlinedNoValue |   9.084 us |  0.3575 us | 0.0196 us |  0.57 |   0.7629 |   0.0153 |        - |   12.52 KB |        0.45 |
| JsonShort_Inlined        |  12.661 us |  1.7144 us | 0.0940 us |  0.79 |   1.5717 |   0.0763 |        - |   25.86 KB |        0.93 |
| JsonShort_Default        |  15.931 us |  1.3110 us | 0.0719 us |  1.00 |   1.6785 |   0.1221 |        - |   27.83 KB |        1.00 |
| JsonShort_Debug          |  16.499 us |  1.4400 us | 0.0789 us |  1.04 |   1.7395 |   0.0916 |        - |   28.73 KB |        1.03 |
| JsonShort_DebugMemoized  |  26.105 us |  0.4959 us | 0.0272 us |  1.64 |   3.2043 |   0.3052 |        - |   52.28 KB |        1.88 |

Note: `*NoValue` methods doesn't calculate value from AST, just parsing text and returns AST.

# Rule types

### Sequence

Matches child rules in a sequential manner, fails when any rule in the sequence fails.  
This type of rule is created automatically when you add a second rule or call the `ToSequence()` method.
```csharp
builder.CreateRule("sequence")
    .Rule("A")
    .Literal("=")
    .Token("B");

builder.CreateRule("another_sequence")
    .Token("A")
    .ToSequence(); // Sequence that contains one element!
```

### Choice

Matches any child rule, trying them one-by-one until the last one fails.  
**Order of choices matters!**
```csharp
builder.CreateRule("choice")
    .Choice(
        b => b.Rule("A"),
        b => b.Token("B"),
        b => b.Regex("(C|D)")
    );
```

### Optional
The rule that cannot fail, and if child rule fails, it just not captures any text and marks children array as empty.
```csharp
builder.CreateRule("optional")
    .Optional(
        b => b.Literal("I").Literal("AM").Literal("OPTIONAL!")
    );
```

### Repeat
Matches a specific range of repeated child rule. Fails if child rule does not repeat a minimum amount of times. Simple.
```csharp
builder.CreateRule("repeat_1_to_3")
    .Repeat(b => b.Rule("rule_to_repeat"), min: 1, max: 3); // [1..3]
    
builder.CreateRule("repeat_2_to_inf")
    .Repeat(b => b.Rule("rule_to_repeat"), min: 2); // [2..]
    
builder.CreateRule("repeat_0_to_inf")
    .ZeroOrMore(b => b.Rule("rule_to_repeat")); // [0..]
    
builder.CreateRule("repeat_1_to_inf")
    .OneOrMore(b => b.Rule("rule_to_repeat")); // [1..]
```

### SeparatedRepeat
This is very useful pattern for lists and operators. Can allow to parse trailing separators (the `allowTrailingSeparator` parameter) and has option to include separators in AST (the `includeSeparatorsInResult` parameter).
```csharp
builder.CreateRule("separated_repeat_1_to_3")
    .RepeatSeparated(b => b.Rule("rule_to_repeat"), s => s.Rule("separator"), min: 1, max: 3, allowTrailingSeparator: true); // [1..3]
    
builder.CreateRule("separated_repeat_2_to_inf")
    .RepeatSeparated(b => b.Rule("rule_to_repeat"), s => s.Rule("separator"), min: 2, includeSeparatorsInResult: true); // [2..]
    
builder.CreateRule("separated_repeat_0_to_inf")
    .ZeroOrMoreSeparated(b => b.Rule("rule_to_repeat"), s => s.Rule("separator")); // [0..]
    
builder.CreateRule("separated_repeat_1_to_inf")
    .OneOrMoreSeparated(b => b.Rule("rule_to_repeat"), s => s.Rule("separator")); // [1..]
```

# Token pattern types

## Combination primitives
Here is the same combination primitives as rules, except for `SeparatedRepeat`. They works the same:

### Sequence
```csharp
builder.CreateToken("sequence")
    .Token("A")
    .Literal("=")
    .Token("B");

builder.CreateToken("sequence")
    .Token("seq")
    .ToSequence();
```
You can apply passage function to sequence token to pass intermediate value up:
```csharp
builder.CreateToken("string")
    .Literal('"')
    .TextUntil('"') // Puts the captured string into intermediate value
    .Literal('"')
    .Pass(v => v[1]);

    // Or use the shorcut:
    .Pass(1);
```

### Choice
```csharp
builder.CreateToken("choice")
    .Choice(
        b => b.Token("A"),
        b => b.Regex("(B|C)+"),
        b => b.Identifier()
    );
```

### Optional
```csharp
builder.CreateToken("optional")
    .Optional(
        b => b.LiteralChoice("A", "B", "C")
    );
```

### Repeat
```csharp
builder.CreateToken("repeat_1_to_3")
    .Repeat(b => b.Token("token_to_repeat"), min: 1, max: 3); // [1..3]
    
builder.CreateToken("repeat_2_to_inf")
    .Repeat(b => b.Token("token_to_repeat"), min: 2); // [2..]
    
builder.CreateToken("repeat_0_to_inf")
    .ZeroOrMore(b => b.Token("token_to_repeat")); // [0..]
    
builder.CreateToken("repeat_1_to_inf")
    .OneOrMore(b => b.Token("token_to_repeat")); // [1..]
```

## Matching primitives

### EOF
Matches the end of input (when there is no characters to match)
```csharp
builder.CreateToken("eof").EOF();
```
Useful when you are want to sure that parser captured all the input:
```csharp
builder.CreateMainRule("main")
    .ZeroOrMore(b => b.Rule("statement"))
    .EOF();
```

### Literal, LiteralChar and LiteralChoice
Matches a literal string or character or choice of them using Trie (matches longest possible choice).  
```csharp
builder.CreateToken("literal")
    .Literal('a')
    .Literal("str")
    .LiteralChoice("abc", "def"); // Uses trie for efficient match
```
Also, the `StringComparison` and `StringComparer` can be applied to them:
```csharp
builder.CreateToken("literal")
    .Literal('a', StringComparison.OrdinalIgnoreCase)
    .Literal("another str", StringComparison.InvariantCulture)
    .LiteralChoice(["abc", "def"], StringComparer.OrdinalIgnoreCase);
```

### Number
Matches a number based on `NumberFlags` and converts it into intermediate value based on `NumberType`.  
Tries to match the most possible characters (e.g. if you disallow implicit fractional part, when it parses "5.", it matches only the "5").
```csharp
// You can create a token that matches numbers using various ways:
builder.CreateToken("int")
    .Number<int>(); // Retrieves sign from generic argument
builder.CreateToken("uint")
    .Number<int>(signed: false); // Forsely removes the sign
builder.CreateToken("int")
    .Number<uint>(signed: true); // Or adds

// And using manual flags:
builder.CreateToken("int")
    .Number<int>(NumberFlags.Integer);

// It will convert to specified generic type:
builder.CreateToken("int")
    .Number<double>(NumberFlags.Integer); // Matches only signed integers but converts it into double.

// Or to the specified NumberType:
builder.CreateToken("int")
    .Number(NumberType.Double, NumberFlags.Integer); // Works the same

builder.CreateToken("float_or_int")
    .Number(NumberFlags.Float); // Converts the number to int if it does not matches a fractional dot

builder.CreateRule("float_or_int_rule").Token("float_or_int");
var parser = builder.Build();

var intType = parser.ParseRule("float_or_int_rule", "10").IntermediateValue.GetType(); // System.Int32
var floatType = parser.ParseRule("float_or_int_rule", "10.01").IntermediateValue.GetType(); // System.Float
```
There is available flags:
| Flag | Description | Example |
|------|-------------|---------|
| `Signed` | Allows leading '+' or '-' sign | `-123`, `+45` |
| `DecimalPoint` | Allows decimal point in the number | `3.14`, `2.` |
| `Exponent` | Allows exponent part for scientific notation | `1.5e10`, `2E-5` |
| `ImplicitIntegerPart` | Allows implicit integer part before decimal point | `.5` (parsed as `0.5`) |
| `ImplicitFractionalPart` | Allows implicit fractional part after decimal point | `5.` (parsed as `5.0`) |

| **Common Combinations** | Flags Included | Description |
|-------------------------|----------------|-------------|
| `Integer` | `Signed` | Standard signed integers |
| `UnsignedInteger` | `None` | Unsigned integers only |
| `Float` | `Signed \| DecimalPoint \| ImplicitIntegerPart \| ImplicitFractionalPart` | Standard floating-point numbers |
| `UnsignedFloat` | `DecimalPoint \| ImplicitIntegerPart \| ImplicitFractionalPart` | Unsigned floating-point numbers |
| `StrictFloat` | `Signed \| DecimalPoint` | Strict floating-point (no implicit parts) |
| `StrictUnsignedFloat` | `DecimalPoint` | Strict unsigned floating-point |
| `Scientific` | `Float \| Exponent` | Standard scientific notation |
| `UnsignedScientific` | `UnsignedFloat \| Exponent` | Unsigned scientific notation |
| `StrictScientific` | `StrictFloat \| Exponent` | Strict scientific notation |
| `StrictUnsignedScientific` | `StrictUnsignedFloat \| Exponent` | Strict unsigned scientific notation |

### Regex
Matches a regular expression. Wraps pattern into "\G{Pattern}" to avoid `SubString` allocations.
```csharp
builder.CreateToken("regex")
    .Regex(@"(abc|def)"); // Compiled is used by default
// You can also define your own options:
builder.CreateToken("regex")
    .Regex(@"(abc|def|\d+)", RegexOptions.NonBacktracking | RegexOptions.Compiled);
```
It puts the `Match` object as intermediate value:
```csharp
var match = parser.ParseRule("regex_rule", "def").GetIntermediateValue<Match>();
```

### EscapedText
The most powerful thing for making text escape sequences! Or just for matching characters until one of specified sequences.  
It uses Trie for fast lookup.  
You can use predefined strategies, such as character prefix and double characters:
```csharp
builder.CreateToken("json-like_text")
    .EscapedTextPrefix(prefix: '\\', '\\', '\"');
builder.CreateToken("sql-like_text")
    .EscapedTextDoubleChars('\\', '\"');

// Or use the strings instead of characters:
builder.CreateToken("json-like_text")
    .EscapedTextPrefix(prefix: "\\", "ab", "bc");
builder.CreateToken("sql-like_text")
    .EscapedTextDoubleSequences("ab", "bc");
```
And it puts escaped text into intermediate value:
```csharp
string input =
"""
\"abc\\def
""";

var escaped = parser.ParseRule("json-like_text_rule", input)
    .GetIntermediateValue<string>(); // "abc\def
```

And you can capture the text until one of the sequences (it not captures these specified sequences):
```csharp
builder.CreateToken("text_until_new_line")
    .TextUntil('\n', '\r');
```

Also manual definition can be used:
```csharp
var escapeMappings = new Dictionary<string, string>
{
    ["ab"] = "bc",
    ["def"] = "123",
    ["\\\""] = "\""
};
var forbiddenSequences = ["@"];

builder.CreateToken("manual_text")
    .EscapedText(escapeMappings, forbiddenSequences);

var escaped = parser.ParseRule("manual_text_rule", "abcdef123\\\"456@")
    .GetIntermediateValue<string>(); // "bcc123123"456
```

### Predicate-based (Character, RepeatCharacters, Identifier, Whitespaces)

There is some convenient, predicate based token patterns with predefined predicates. They do not produce intermediate values.

```csharp
// Character
builder.CreateToken("char")
    .Char(c => c >= 'a' && c <= 'z');

// Repeat characters
builder.CreateToken("chars_3_to_inf")
    .Chars(c => c >= 'a' && c <= 'z', min: 3); // [3..]
builder.CreateToken("chars_3_to_10")
    .Chars(c => c >= 'A' && c <= 'Z', min: 3, max: 10); // [3..10]
builder.CreateToken("chars_0_to_inf")
    .ZeroOrMoreChars(c => c >= 'B' && c <= 'Q'); // [0..]
builder.CreateToken("chars_1_to_inf")
    .OneOrMoreChars(c => c >= '0' && c <= '9'); // [1..]

// The whitespaces (wrapper for RepeatCharacters)
builder.CreateToken("ws")
    .Whitespaces(); // equivalent to .OneOrMoreChars(char.IsWhiteSpace), but holds other type

// Identifier (minimum length must be greater than zero!)
builder.CreateToken("custom_identifier_1_to_inf")
    .Identifier(start => start >= 'a' && start <= 'z',
        cont => cont >= '0' && cont <= '9'); // Matches a 'd123' sequence in range [1..]
    
builder.CreateToken("custom_identifier_3_to_inf")
    .Identifier(start => start >= 'a' && start <= 'z',
        cont => cont >= '0' && cont <= '9',
        minLength: 3); // [3..]

builder.CreateToken("custom_identifier_3_to_5")
    .Identifier(start => start >= 'a' && start <= 'z',
        cont => cont >= '0' && cont <= '9',
        minLength: 3, maxLength: 5); // [3..5]

builder.CreateToken("ascii_identifier")
    .Identifier(); // Default implementation, [1..]

builder.CreateToken("unicode_identifier")
    .UnicodeIdentifier(); // [1..]
```

### Custom tokens

You can create custom tokens from a `TokenPattern` implementation:

```csharp
var myToken = new MyTokenPattern();

builder.CreateToken("my_own_token")
    .AddToken(myToken);

builder.CreateRule("my_token_in_rule")
    .AddToken(myToken);
```

Or using a `CustomTokenPattern` with function:

```csharp
builder.CreateToken("custom")
	.Custom((self, input, start, end, parameter) =>
	{
        var context = (char)parameter;

        // Fail if input at current position is not equal to context character
		if (start >= end || input[start] != context)
			return ParsedElement.Fail;

        // Capture the character
		return new ParsedElement(elementId: self.Id,
			startIndex: start,
			length: 1,
			intermediateValue: "my intermediate value!");
	});

builder.CreateRule("custom_rule").Token("custom");
var parser = builder.Build();

var result = parser.ParseRule("custom_rule", "x", parameter: 'x').GetIntermediateValue<string>(); // "my intermediate value!"
```