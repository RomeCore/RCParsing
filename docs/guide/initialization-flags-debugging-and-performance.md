---
title: Initialization flags, debugging and performance
icon: chart-line
---

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

builder.Settings.WriteStackTrace(); // All elements will record stack trace, providing a brief look at the structure of the current rule context
builder.Settings.RecordWalkTrace(); // All elements will record walk trace, the list of logs displaying what rules was trying to parse with useful information
builder.Settings.DetailedErrors(); // When parser throws errors, they will display more information
builder.Settings.ErrorFormatting(
	ErrorFormattingFlags.DisplayRules |
	ErrorFormattingFlags.DisplayMessages |
	ErrorFormattingFlags.MoreGroups); // DetailedErrors is the sugar for this
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
RCParsing.ParsingException : An error occurred during parsing:

The line where the error occurred:
	"tags": ["tag1", "tag2", "tag3"],,
                   line 5, column 35 ^

',' is unexpected character, expected one of:
  'string'
  literal: '}'

...and more errors omitted.
```

And here is errors with `UseDebug()` setting applied:

```
RCParsing.ParsingException : An error occurred during parsing:

'string': Failed to parse token.
'pair': Failed to parse sequence rule.
(SeparatedRepeat[0..]...): Expected element after separator, but found none.

The line where the error occurred (position 109):
	"tags": ["tag1", "tag2", "tag3"],,
                   line 5, column 35 ^

',' is unexpected character, expected one of:
  'string'
  'pair'
  SeparatedRepeat[0..]: 'pair' sep literal ','

['string'] Stack trace (top call recently):
- Sequence 'pair':
    'string' <-- here
    literal ':'
    'value'
- SeparatedRepeat[0..]: 'pair' <-- here
  sep literal ','
- Sequence 'object':
    literal '{'
    SeparatedRepeat[0..]... <-- here
    literal '}'
- Choice 'value':
    'string'
    'number'
    'boolean'
    'null'
    'array'
    'object' <-- here
- Sequence:
    'value' <-- here
    end of file

... and more errors omitted

Walk Trace:

... 310 hidden parsing steps. Total: 340 ...
[SUCCESS] pos:107   literal ']' matched: ']' [1 chars]
[SUCCESS] pos:84    'array' matched: '["tag1", "tag2", "tag3"]' [24 chars]
[SUCCESS] pos:84    'value' matched: '["tag1", "tag2", "tag3"]' [24 chars]
[SUCCESS] pos:76    'pair' matched: '"tags": ["tag1" ..... ", "tag3"]' [32 chars]
[ENTER]   pos:108   'skip'
[ENTER]   pos:108   whitespaces
[FAIL]    pos:108   whitespaces failed to match: ',,\r\n\t"isActive"...'
[ENTER]   pos:108   Sequence...
[ENTER]   pos:108   literal '//'
[FAIL]    pos:108   literal '//' failed to match: ',,\r\n\t"isActive"...'
[FAIL]    pos:108   Sequence... failed to match: ',,\r\n\t"isActive"...'
[FAIL]    pos:108   'skip' failed to match: ',,\r\n\t"isActive"...'
[ENTER]   pos:108   literal ','
[SUCCESS] pos:108   literal ',' matched: ',' [1 chars]
[ENTER]   pos:109   'skip'
[ENTER]   pos:109   whitespaces
[FAIL]    pos:109   whitespaces failed to match: ',\r\n\t"isActive":...'
[ENTER]   pos:109   Sequence...
[ENTER]   pos:109   literal '//'
[FAIL]    pos:109   literal '//' failed to match: ',\r\n\t"isActive":...'
[FAIL]    pos:109   Sequence... failed to match: ',\r\n\t"isActive":...'
[FAIL]    pos:109   'skip' failed to match: ',\r\n\t"isActive":...'
[ENTER]   pos:109   'pair'
[ENTER]   pos:109   'string'
[FAIL]    pos:109   'string' failed to match: ',\r\n\t"isActive":...'
[FAIL]    pos:109   'pair' failed to match: ',\r\n\t"isActive":...'
[FAIL]    pos:4     SeparatedRepeat[0..]... failed to match: '"id": 1,\r\n\t"nam...'
[FAIL]    pos:0     'object' failed to match: '{\r\n\t"id": 1,\r\n\t...'
[FAIL]    pos:0     'value' failed to match: '{\r\n\t"id": 1,\r\n\t...'
[FAIL]    pos:0     Sequence... failed to match: '{\r\n\t"id": 1,\r\n\t...'

... End of walk trace ...
```

There is JSON benchmark demonstrating how different settings impacts on performance:

| Method               | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|------:|--------:|---------:|---------:|---------:|----------:|------------:|
| Default              | 193.36 us |  1.551 us |  0.689 us |  1.00 |    0.00 |  13.1836 |   3.9063 |        - | 216.68 KB |        1.00 |
| NoValue              | 160.82 us |  6.315 us |  2.252 us |  0.83 |    0.01 |   8.0566 |   2.4414 |        - | 134.78 KB |        0.62 |
| OptimizedWhitespaces | 126.71 us |  0.771 us |  0.342 us |  0.66 |    0.00 |  13.1836 |   3.9063 |        - | 216.68 KB |        1.00 |
| Inlined              | 143.75 us |  0.779 us |  0.278 us |  0.74 |    0.00 |  13.1836 |   3.9063 |        - | 216.68 KB |        1.00 |
| FirstCharacterMatch  | 150.79 us |  1.567 us |  0.559 us |  0.78 |    0.00 |   9.7656 |   2.1973 |        - | 160.61 KB |        0.74 |
| IgnoreErrors         | 185.20 us |  1.546 us |  0.687 us |  0.96 |    0.00 |   9.2773 |   1.9531 |        - | 152.59 KB |        0.70 |
| StackTrace           | 218.43 us |  1.977 us |  0.705 us |  1.13 |    0.01 |  21.4844 |   8.3008 |        - | 353.79 KB |        1.63 |
| WalkTrace            | 314.81 us |  2.223 us |  0.987 us |  1.63 |    0.01 |  94.2383 |  85.4492 |  75.1953 |  601.1 KB |        2.77 |
| LazyAST              | 221.00 us |  3.437 us |  1.526 us |  1.14 |    0.01 |  20.7520 |   9.2773 |        - |  341.2 KB |        1.57 |
| RecordSkipped        | 199.96 us |  1.016 us |  0.451 us |  1.03 |    0.00 |  15.6250 |   5.1270 |        - | 256.71 KB |        1.18 |
| Memoized             | 489.61 us |  4.543 us |  2.017 us |  2.53 |    0.01 | 107.4219 | 106.4453 |  86.9141 | 667.39 KB |        3.08 |
| FastestNoValue       |  62.98 us |  0.373 us |  0.166 us |  0.33 |    0.00 |   4.2725 |   0.8545 |        - |   70.7 KB |        0.33 |
| Fastest              |  93.34 us |  0.381 us |  0.169 us |  0.48 |    0.00 |   9.2773 |   2.0752 |        - | 152.59 KB |        0.70 |
| Slowest              | 830.04 us | 57.607 us | 25.578 us |  4.29 |    0.12 | 197.2656 | 196.2891 | 152.3438 | 1353.4 KB |        6.25 |

Note: `*NoValue` method doesn't calculate value via transformation functions, just parsing text and returns AST.