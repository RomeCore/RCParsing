using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Tests
{
	public class IndentedGrammarTests
	{
		[Fact]
		public void SimpleBlockParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings
				.Skip(b => b.Whitespaces().ConfigureForSkip(),
					ParserSkippingStrategy.TryParseThenSkip); // Allows to use whitespaces in the grammar but skips them in other places.
			
			builder.BarrierTokenizers // Adds indent and dedent tokenizer to the parser.
				.AddIndent(indentSize: 4, "INDENT", "DEDENT");

			builder.CreateRule("function")
				.Literal("def")
				.Whitespaces()
				.Identifier()
				.Literal('(')
				.Literal(')')
				.Literal(':')
				.Rule("block");

			builder.CreateRule("block")
				.Token("INDENT") // Always captures 'INDENT' token.
				.OneOrMore(b => b.Rule("statement"))
				.Token("DEDENT");

			builder.CreateRule("statement")
				.Identifier();

			builder.CreateMainRule("main")
				.ZeroOrMore(b => b.Rule("function"))
				.EOF(); // Sure that we capture all the input.

			var parser = builder.Build();

			string input =
			"""
			def foo():
				bar

				spam

			def baz():
				eggs
			""";

			parser.Parse(input);
		}

		[Fact]
		public void ComplexBlockParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings
				.Skip(b => b.Whitespaces().ConfigureForSkip());

			// Add the 'INDENT' and 'DEDENT' barrier tokenizer
			builder.BarrierTokenizers
				.AddIndent(indentSize: 4, "INDENT", "DEDENT");

			// Create the statement rule
			builder.CreateRule("statement")
				.Choice(
				b => b
					.Literal("def")
					.Identifier()
					.Literal("():")
					.Rule("block"),
				b => b
					.Literal("if")
					.Identifier()
					.Literal(":")
					.Rule("block"),
				b => b
					.Identifier()
					.Literal("=")
					.Identifier()
					.Literal(";"));

			// Create the 'block' rule that matches our 'INDENT' and 'DEDENT' barrier tokens
			builder.CreateRule("block")
				.Token("INDENT")
				.OneOrMore(b => b.Rule("statement"))
				.Token("DEDENT");

			builder.CreateMainRule("program")
				.ZeroOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			string inputStr =
			"""
			def a():
				b = c;
				c = a;
			a = p;
			if c:
				h = i;
				if b:
					a = aa;
			""";

			var ast = parser.Parse(inputStr);

			string invalidInputStr =
			"""
			def a():
			    b = c;
			    c = a;
			a = p;
			if c:
			    h = i;
			    if b:
			    a = aa;
			""";

			Assert.Throws<ParsingException>(() => parser.Parse(invalidInputStr));
			
			invalidInputStr =
			"""
			def a():
			    b = c;
			    c = a;
			a = p;
			if c:
			    h = i;
			    if b:
			            a = aa;
			""";

			Assert.Throws<ParsingException>(() => parser.Parse(invalidInputStr));
		}
	}
}