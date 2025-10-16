---
title: Overview
---

The `RCParsing` library provides a rich set of rule types and token patterns to construct your grammar. Rules define the hierarchical structure of your parser, while tokens serve as the fundamental units for matching input and producing intermediate values. Below, we introduce the available rule types and token patterns to help you build powerful parsers.

## Rule Types
Rules define the structure of your grammar and produce AST nodes. Learn about the different types of rules and how to use them:

- [Sequence](./rules/sequence): Match child rules in order.
- [Choice](./rules/choice): Match one of several possible rules.
- [Optional](./rules/optional): Match a rule optionally without failing.
- [Repeat](./rules/repeat): Match a rule multiple times within a range.
- [SeparatedRepeat](./rules/separated-repeat): Match a rule multiple times with separators.
- [Lookahead](./rules/lookahead): Ensure a rule does match/does not match ahead in the input.

## Token Pattern Types
Tokens are the building blocks of parsing, consuming input and producing intermediate values. Explore the various token patterns:

### Combination Primitives
- [Sequence](./tokens/combinators/sequence): Match child tokens in order.
- [Choice](./tokens/combinators/choice): Match one of several possible tokens.
- [Optional](./tokens/combinators/optional): Match a token optionally.
- [Repeat](./tokens/combinators/repeat): Match a token multiple times.
- [SeparatedRepeat](./tokens/combinators/separated-repeat): Match a token multiple times with separators.
- [Between](./tokens/combinators/between): Match a sequence and propagate the middle token's value.
- [First](./tokens/combinators/first): Match a sequence and propagate the first token's value.
- [Second](./tokens/combinators/second): Match a sequence and propagate the second token's value.
- [Map](./tokens/combinators/map): Transform a token's intermediate value.
- [Return](./tokens/combinators/return): Return a fixed value for a token.
- [CaptureText](./tokens/combinators/capture-text): Capture the matched text as the intermediate value.
- [SkipWhitespaces](./tokens/combinators/skip-whitespaces): Skip whitespace before matching a token.
- [Lookahead](./tokens/combinators/lookahead): Ensure a token does match/does not match ahead.

### Matching Primitives
- [EOF](./tokens/eof): Match the end of input.
- [Empty](./tokens/empty): Always match nothing.
- [Fail](./tokens/fail): Always fails.
- [Literal](./tokens/literal): Match a specific string.
- [LiteralChar](./tokens/literal-char): Match a specific character.
- [LiteralChoice](./tokens/literal-choice): Match one of several strings.
- [Keyword](./tokens/keyword): Match a string not followed by specific characters.
- [KeywordChoice](./tokens/keyword-choice): Match one of several keywords.
- [Number](./tokens/number): Match and parse numeric values.
- [Regex](./tokens/regex): Match a regular expression.
- [EscapedText](./tokens/escaped-text): Match text with escape sequences.
- [TextUntil](./tokens/text-until): Match text until specific sequences.
- [Character](./tokens/character): Match a single character based on a predicate.
- [RepeatCharacters](./tokens/repeat-characters): Match multiple characters based on a predicate.
- [Identifier](./tokens/identifier): Match identifiers like variable names.
- [Whitespaces](./tokens/whitespaces): Match whitespace characters.
- [Spaces](./tokens/spaces): Match spaces and tabs.
- [Newline](./tokens/newline): Match newline sequences.
- [Custom Tokens](./tokens/custom): Create custom token patterns.