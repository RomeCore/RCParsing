---
title: Rule and token building
icon: puzzle-piece
---

Parsers in this library is created via `ParserBuilder`s. When parser is being built, builder deduplicates rules and token patterns, and assigns IDs to them, then they are being compound into a `Parser`.  
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

### Important!

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