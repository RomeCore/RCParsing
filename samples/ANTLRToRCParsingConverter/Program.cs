using ANTLRToRCParsingConverter;

var antlr =
"""
// The example grammar for an arithmetic expression.
expr : INT | expr ('+' | '-') expr ;
""";

Console.WriteLine(ANTLRParser.Parse(antlr));