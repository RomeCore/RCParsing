---
title: Barrier tokens
icon: indent
---

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