---
title: Deduplication
icon: shuffle
---

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