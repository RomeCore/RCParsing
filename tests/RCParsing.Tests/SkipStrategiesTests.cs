using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Tests
{
	/// <summary>
	/// Tests for skip strategies in the parser builder.
	/// </summary>
	public class SkipStrategiesTests
	{
		[Fact]
		public void DoNotSkipByDefault()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("A").Literal("A");
			builder.CreateRule("B").Literal("B");
			builder.CreateMainRule().OneOrMore(b => b.Choice(b => b.Rule("A"), b => b.Rule("B")));

			var parser = builder.Build();

			var ast1 = parser.Parse("AB");
			Assert.True(ast1.Success);
			Assert.Equal("AB", ast1.Text);

			var ast2 = parser.Parse("ABA AB");
			Assert.True(ast2.Success);
			Assert.Equal("ABA", ast2.Text);
		}

		[Fact]
		public void SkipBeforeParsing_Simple()
		{
			var builder = new ParserBuilder();

			// Configure skipping: skip spaces once before parsing
			builder.Settings
				.Skip(r => r.Literal(" "), ParserSkippingStrategy.SkipBeforeParsing);

			builder.CreateRule("word").Regex(@"\w+");
			builder.CreateMainRule().OneOrMore(b => b.Rule("word"));

			var parser = builder.Build();

			var result = parser.Parse("hello world  hey"); // Should skip one space before parsing "world" but not the second one
			Assert.True(result.Success);

			var capturedText = string.Join("", result.GetJoinedChildren().Select(c => c.Text));
			Assert.Equal("helloworld", capturedText); // Space was skipped once between words
		}
	}
}