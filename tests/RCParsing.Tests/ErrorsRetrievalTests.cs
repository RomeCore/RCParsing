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
	}
}