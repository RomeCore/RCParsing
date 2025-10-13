---
title: Main concepts
icon: material-symbols:question-mark
---

`RCParsing` uses lexerless approach, but it has *tokens*, they are used as parser primitives, and they are not complex as *rules*, having a limited error handling and configuration. This library also supports *barrier tokens*, unlocking hybrid mode.  

### Token
A **Token** is the smallest matching unit, a primitive that consumes input and produces an intermediate value. Tokens are ideal for:
* Terminal symbols (keywords, operators: `"if"`, `"+"`) and atomic values (numbers, strings, identifiers).
* Complex patterns with immediate value transformation and zero-allocation (combinator-style).

**Key trait:** They cannot reference Rules, making them the foundation for all parsing. And they **cannot** contain inner barrier tokens.

### Rule
A **Rule** is a composite structure built from Tokens and other Rules. It defines the grammar's structure and produces an Abstract Syntax Tree (AST) node. Rules are used for:
* Non-terminal symbols (expressions, statements, blocks).
* Organizing the grammar hierarchy (`expression` -> `term` -> `factor`).
* Constructing the AST with `Transform` functions for semantic analysis.

**Key trait:** They form the recursive structure of your grammar and build the parse tree.

### Barrier Token
A **Barrier Token** is a special pseudo-token injected by a `BarrierTokenizer` (like the indent/dedent tokenizer). They are not defined in the grammar but are crucial for parsing context-sensitive syntax.
* **Purpose:** To handle structures where traditional tokenization is insufficient.
* **Primary Example:** Parsing indentation-based languages (Python, YAML). The tokenizer analyzes whitespace to emit `INDENT` `DEDENT` and optinal `NEWLINE` tokens, which act as explicit markers for block boundaries, mimicking the role of `{` and `}` in other languages.

**Key trait:** They are generated and baked entirely before parsing, enabling recognition of non-regular structures.