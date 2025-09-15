using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Tests
{
	public class ErrorsRetrievalTests
	{
		[Fact]
		public void SimpleArglistError()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateMainRule()
				.Identifier()
				.Literal("(")
				.List(b => b.Identifier())
				.Literal(")");

			var parser = builder.Build();

			// Should fail after trailing comma, on ')'
			var exception = Assert.Throws<ParsingException>(() => parser.Parse("id1 (id2, )"));
			var lastGroup = exception.Groups.Last!;
			Assert.Equal(11, lastGroup.Column);
			Assert.Contains("identifier", lastGroup.Expected.Select(e => e.ToString()));

			// Should fail on first character
			exception = Assert.Throws<ParsingException>(() => parser.Parse("1id (id2)"));
			lastGroup = exception.Groups.Last!;
			Assert.Equal(1, lastGroup.Column);
			Assert.Contains("identifier", lastGroup.Expected.Select(e => e.ToString()));

			// Should fail on '('
			exception = Assert.Throws<ParsingException>(() => parser.Parse("id {id2}"));
			lastGroup = exception.Groups.Last!;
			Assert.Equal(4, lastGroup.Column);
			Assert.Contains("literal '('", lastGroup.Expected.Select(e => e.ToString()));
		}

		[Fact]
		public void NestedErrorInComplexStructure()
		{
			// We use simplified JSON here
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","))
				.Literal("}");

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value");

			builder.CreateRule("value")
				.Choice(
					c => c.Token("number"),
					c => c.Token("string"),
					c => c.Rule("array"),
					c => c.Rule("object")
				);

			builder.CreateRule("array")
				.Literal("[")
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","))
				.Literal("]");

			builder.CreateToken("string").Literal("\"").EscapedTextPrefix('\\', '\"').Literal("\"").Pass(1);
			builder.CreateToken("number").Number<double>();

			builder.CreateMainRule().Rule("object").EOF();

			var parser = builder.Build();

			var invalidInput = """
			{
				"key1": [1 2],
				"key2": "value"
			}
			""";

			// Should fail in the array, after
			var exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));
			Assert.Contains("literal ','", exception.Groups.Last!.Expected.ToString());
			Assert.Contains("literal ']'", exception.Groups.Last!.Expected.ToString());
			Assert.Equal(2, exception.Groups.Last!.Line);
			Assert.Equal(13, exception.Groups.Last!.Column);
		}
	}
}