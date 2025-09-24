---
title: Auto-skipping
icon: material-symbols:skip-next-rounded
---

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

P.S.: Use the `Keyword` instead of `Literal` + `Whitespaces` in real parsers.

When using `TryParseThenSkip` strategy, parser will try to parse first, then skip, then parse (`parse -> skip -> parse`, instead of `skip -> parse`). This allows to require rules that conflicts with skip-rules, but may be a bit slow and emit some unnecessary parsing errors into context, slightly impacting on allocation and performance.

### Supported skip strategies

- `SkipBeforeParsing`: Parser will try to skip the skip-rule once before parsing the target rule (`skip -> parse`).
- `SkipBeforeParsingLazy`: Parser will repeatedly attempt to skip the skip-rule in an interleaved manner with the target rule, until the target rule succeeds or both skip and target rules fail. This allows incremental consumption of skip content (`skip -> parse -> skip -> parse -> ... -> parse`).
- `SkipBeforeParsingGreedy`: Parser will greedily skip the skip-rule as many times as possible before attempting to parse the target rule (`skip -> skip -> ... -> skip -> parse`).
- `TryParseThenSkip`: Parser will first attempt to parse the target rule; if parsing fails, it will skip the skip-rule once and then retry parsing the target rule (`parse -> skip -> parse`).
- `TryParseThenSkipLazy`: Parser will attempt to parse the target rule; if parsing fails, it will alternately try to skip the skip-rule and parse the target rule repeatedly until the target rule succeeds or both fail (`parse -> skip -> parse -> skip -> parse -> ... -> parse`).
- `TryParseThenSkipGreedy`: Parser will attempt to parse the target rule; if parsing fails, it will greedily skip the skip-rule as many times as possible and then retry parsing the target rule (`parse -> skip -> skip -> ... -> skip -> parse`).

### Optimized whitespace skipping

You can use `SkipWhitespacesOptimized()` flag on parser settings to enable the direct skipping strategy in the parser, disabling any other skip rules and strategies, granting around ~10% speed boost:

```csharp
builder.Settings
	.SkipWhitespacesOptimized();

// Important: the next setting will not work!
builder.Settings
	.Skip(b => b.Rule("skip"));
```