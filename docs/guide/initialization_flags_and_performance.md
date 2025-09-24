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

| Method               | Mean      | Error     | StdDev   | Ratio | Gen0     | Gen1     | Gen2     | Allocated  | Alloc Ratio |
|--------------------- |----------:|----------:|---------:|------:|---------:|---------:|---------:|-----------:|------------:|
| Default              | 163.94 us |  5.396 us | 0.296 us |  1.00 |  13.1836 |   3.6621 |        - |  217.13 KB |        1.00 |
| NoValue              | 131.06 us | 10.305 us | 0.565 us |  0.80 |   8.0566 |   2.4414 |        - |  135.23 KB |        0.62 |
| OptimizedWhitespaces | 128.56 us | 14.028 us | 0.769 us |  0.78 |  13.1836 |   3.6621 |        - |  217.13 KB |        1.00 |
| Inlined              | 147.78 us |  6.512 us | 0.357 us |  0.90 |  13.1836 |   3.6621 |        - |  217.13 KB |        1.00 |
| FirstCharacterMatch  | 136.95 us |  8.838 us | 0.484 us |  0.84 |   9.7656 |   2.1973 |        - |  161.05 KB |        0.74 |
| IgnoreErrors         | 150.78 us |  4.201 us | 0.230 us |  0.92 |   9.2773 |   2.1973 |        - |  153.04 KB |        0.70 |
| StackTrace           | 177.62 us | 12.680 us | 0.695 us |  1.08 |  17.8223 |   6.5918 |        - |  295.13 KB |        1.36 |
| WalkTrace            | 335.25 us | 30.500 us | 1.672 us |  2.04 |  90.8203 |  90.8203 |  90.8203 |  601.34 KB |        2.77 |
| LazyAST              | 188.04 us | 12.742 us | 0.698 us |  1.15 |  20.7520 |   7.0801 |        - |  341.65 KB |        1.57 |
| RecordSkipped        | 165.97 us |  3.026 us | 0.166 us |  1.01 |  15.6250 |   6.5918 |        - |  257.16 KB |        1.18 |
| Memoized             | 485.41 us |  4.865 us | 0.267 us |  2.96 |  99.6094 |  99.6094 |  99.6094 |  667.67 KB |        3.08 |
| FastestNoValue       |  65.22 us |  2.583 us | 0.142 us |  0.40 |   4.2725 |   0.8545 |        - |   71.14 KB |        0.33 |
| Fastest              |  95.69 us |  7.965 us | 0.437 us |  0.58 |   9.2773 |   2.1973 |        - |  153.04 KB |        0.70 |
| Slowest              | 696.84 us | 31.728 us | 1.739 us |  4.25 | 199.2188 | 199.2188 | 199.2188 | 1294.44 KB |        5.96 |

Note: `*NoValue` method doesn't calculate value via transformation functions, just parsing text and returns AST.