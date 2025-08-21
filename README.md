# RCParsing

A very flexible and readable .NET parser building library.

It provides an easy way to fully construct your own parser from grammar to completed object creation. Since parser built via this library is lexerless, you don't need to prioritize your tokens and you can mix code with text containing the keywords using this library! 

# Installation

You can install the package via NuGet Package Manager:

```
dotnet add package RCParsing
```

Or via package manager console:

```
Install-Package RCParsing
```

# Basic example

```csharp
using RCParsing;

// First, you need to create a builder
var builder = new ParserBuilder();

// Configure the auto-skip (you can replace it with any parser rule)
builder.Settings()
    .Skip(b => b.Whitespaces());

// Create the tokens
builder.CreateToken("identifier")
    .UnicodeIdentifier();
builder.CreateToken("number")
    .Regex(@"-?\d(?:\.\d+)?");

// Create the choice value rule
builder.CreateRule("value")
    .Choice(
        b => b.Token("identifier"),
        b => b.Token("number"));

// Create a sequential expression rule
builder.CreateRule("expression")
    .Rule("value")
    .LiteralChoice("+", "-")
    .Rule("value");

// Build the parser
var parser = builder.Build();

// Parse a string using 'expression rule' and get the raw AST
var parsedRule = parser.ParseRule("expression", "abc + 123");

Console.WriteLine(parsedRule.Children[0].Text); // abc
Console.WriteLine(parsedRule.Children[1].Text); // +
Console.WriteLine(parsedRule.Children[2].Text); // 123
```