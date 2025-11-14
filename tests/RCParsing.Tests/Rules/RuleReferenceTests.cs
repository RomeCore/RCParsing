using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Tests.Rules
{
	public class RuleReferenceTests
	{
		[Fact]
		public void SelfReferenceRule()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("Loop").Rule("Loop");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void RuleCircularReference()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("A")
				.Rule("B");

			builder.CreateRule("B")
				.Rule("A");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void TokenCircularReference()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Token("integer");

			builder.CreateToken("integer")
				.Token("number");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void IndirectCircularReferenceDeep()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("A").Rule("B");
			builder.CreateRule("B").Rule("C");
			builder.CreateRule("C").Rule("A");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void SameReferenceRule()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Regex(@"\d+");

			builder.CreateToken("int").Token("number");
			builder.CreateToken("integer").Token("int");
			builder.CreateToken("double").Token("number");

			var parser = builder.Build();

			Assert.Single(parser.TokenPatterns);
			Assert.True(parser.GetTokenPattern("number").Id == parser.GetTokenPattern("integer").Id);
			Assert.True(parser.GetTokenPattern("number").Id == parser.GetTokenPattern("int").Id);
			Assert.True(parser.GetTokenPattern("double").Id == parser.GetTokenPattern("int").Id);
		}

		[Fact]
		public void AliasesOrderRule()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Regex(@"\d+");

			builder.CreateToken("int").Token("number");
			builder.CreateToken("integer").Token("int");
			builder.CreateToken("double").Token("number");

			var parser = builder.Build();

			Assert.Equal(["number", "int", "integer", "double"], parser.GetTokenPattern("number").Aliases);
		}

		[Fact]
		public void RuleDeduplication()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("rule1")
				.Literal("abc");

			builder.CreateRule("rule2")
				.Literal("abc");

			var parser = builder.Build();

			Assert.True(parser.GetRule("rule1").Id == parser.GetRule("rule2").Id);
		}
		
		[Fact]
		public void RuleDeduplication_Advanced()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("rule1")
				.LongestChoice(
					b => b.OneOrMore(b => b.Literal("abc")),
					b => b.Optional(b => b.Repeat(b => b.Number<int>(), 10, 15))
				);

			builder.CreateRule("rule2")
				.LongestChoice(
					b => b.OneOrMore(b => b.Literal("abc")),
					b => b.Optional(b => b.Repeat(b => b.Number<int>(), 10, 15))
				);

			var parser = builder.Build();

			Assert.True(parser.GetRule("rule1").Id == parser.GetRule("rule2").Id);

			// Literal + Number
			Assert.Equal(2, parser.TokenPatterns.Count);

			// LongestChoice, OneOrMore, Optional, Repeat + 2 tokens
			Assert.Equal(6, parser.Rules.Count);
		}

		[Fact]
		public void AutoTokenRuleCreation()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("token")
				.Literal("something");

			var parser = builder.Build();

			Assert.Single(parser.TokenPatterns);
			Assert.Single(parser.Rules);

			var result = parser.TryParseRule("token", "something");
			Assert.True(result.Success);
			Assert.Equal(9, result.Length);
		}
	}
}