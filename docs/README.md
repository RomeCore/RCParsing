---
home: true
icon: house
title: Project home
bgImageStyle:
  background-attachment: fixed

heroText: RCParsing
tagline: The ultimate .NET parsing framework for language development and data scraping
actions:
  - text: How to Use
    icon: lightbulb
    link: ./demo/
    type: primary

  - text: Docs
    link: ./guide/

highlights:
  - header: Fluent BNF-like API
    description: Define complex grammars with elegant C# syntax that reads like clean BNF notation.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/2-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/2-dark.svg
    features:
      - title: Declarative Grammar
        icon: material-symbols:format-paint
        details: Write parsers that look like formal grammar specifications

      - title: Rich Built-in Toolset
        icon: puzzle-piece
        details: 30+ of parser element types, including regex, identifiers, keywords, numbers, escaped strings, separated lists and more

      - title: Hybrid Transformation
        icon: bolt
        details: Combine lazy AST and immediate evaluations within a single parser

  - header: Barrier Tokens for Indentation
    description: Parse Python, YAML, and other indent-sensitive languages with built-in INDENT/DEDENT support.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/4-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/4-dark.svg
    highlights:
      - title: Automatic Indent Detection
        icon: indent
        details: Built-in tokenizers handle indentation changes automatically
      
      - title: Python-like Languages
        icon: mdi:console
        details: Perfect for languages with significant whitespace
        
      - title: No Manual Tracking
        icon: hand
        details: No need to manually track indentation levels

  - header: Incremental Parsing
    description: Edit large documents with instant feedback using persistent AST and efficient re-parsing.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/3-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/3-dark.svg
    features:
      - title: Persistent AST
        icon: diagram-project
        details: AST maintains structure between edits for optimal performance

      - title: LSP Ready
        icon: rocket
        details: Perfect for Language Server Protocol implementations

      - title: Real-time Editing
        icon: clock
        details: Instant feedback for editors and IDEs

  - header: Superior Debugging Experience
    description: Get detailed error messages with stack traces, walk traces, and precise source locations.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/6-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/6-dark.svg
    highlights:
      - title: Rule Stack Traces
        icon: layer-group
        details: See exactly which rules failed during parsing
        
      - title: Walk Traces
        icon: shoe-prints
        details: Detailed step-by-step parsing execution logs
        
      - title: Precise Error Locations
        icon: map-pin
        details: Line and column information for quick debugging

  - header: Blazing Fast Performance
    description: Outperforms popular .NET parsing libraries with optimized modes and efficient memory usage.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/8-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/8-dark.svg
    features:
      - title: Benchmark Proven
        icon: chart-line
        details: Faster than Parlot, Pidgin, ANTLR, and Superpower

      - title: Multiple Optimization Modes
        icon: gears
        details: Choose between default, optimized, and token-combination modes
        
      - title: Memory Efficient
        icon: memory
        details: Excellent allocation patterns and lazy evaluation

  - header: Lexerless Architecture
    description: Parse directly from raw text without token priority headaches or separate lexer phase.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/5-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/5-dark.svg
    highlights:
      - title: No Token Conflicts
        icon: peace
        details: Avoid lexer token priority issues common in traditional parsers
        
      - title: Embedded Keywords
        icon: key
        details: Handle keywords inside strings and complex contexts seamlessly
        
      - title: Dynamic Token Matching
        icon: shuffle
        details: Tokens are lightweight matching primitives, not rigid phases

  - header: Advanced Pattern Matching
    description: Find all occurrences of complex patterns in text with detailed AST information.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/7-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/7-dark.svg
    features:
      - title: FindAllMatches Method
        icon: material-symbols:search
        details: Extract all pattern occurrences from any text

      - title: Complex Pattern Support
        icon: network-wired
        details: Match sophisticated structures beyond regex capabilities
        
      - title: Transformation Integration
        icon: mdi:magic
        details: Apply transformations to matched patterns automatically

  - header: Error Recovery Strategies
    description: Define custom recovery strategies per rule to handle syntax errors gracefully.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/9-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/9-dark.svg
    highlights:
      - title: Custom Recovery
        icon: shield
        details: Define how each rule should recover from errors
        
      - title: Multiple Error Handling
        icon: material-symbols:warning-rounded
        details: Continue parsing after encountering errors
        
      - title: Production Ready
        icon: material-symbols:check-circle
        details: Robust error handling for real-world usage

  - header: Batteries Included
    description: Comprehensive built-in tokens and rules for common parsing scenarios.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/10-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/10-dark.svg
    features:
      - title: Common Tokens
        icon: toolbox
        details: Numbers, keywords, identifiers, escaped strings and custom tokens
        
      - title: Collection Helpers
        icon: list
        details: Separated lists, zero-or-more, one-or-more patterns
        
      - title: Text Processing
        icon: text-height
        details: Escaped text, quoted strings, custom delimiters

  - header: Broad Compatibility
    description: Runs on .NET Standard 2.0+, .NET Framework 4.6.1+, .NET 6.0, and .NET 8.0.
    color: White
    bgImage: https://theme-hope-assets.vuejs.press/bg/1-light.svg
    bgImageDark: https://theme-hope-assets.vuejs.press/bg/1-dark.svg
    highlights:
      - title: .NET Standard 2.0
        icon: material-symbols:desktop-windows-outline
        details: Maximum compatibility across .NET ecosystems
        
      - title: Legacy Support
        icon: material-symbols:history
        details: Works with older .NET Framework versions
        
      - title: Modern Runtimes
        icon: star
        details: Optimized for .NET 6+ and future versions

copyright: false
footer: MIT Licensed | Copyright © 2025 RomeCore
---
