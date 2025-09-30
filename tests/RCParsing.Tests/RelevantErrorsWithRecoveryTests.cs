using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests
{
	public class RelevantErrorsWithRecoveryTests
	{
		[Fact]
		public void RelevantGroups_SimpleRecovery_PicksFurthestError()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Literal("var")
				.RequiredWhitespaces()
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';')));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			string input = "var a = b garbage garbage ; var c = d";

			var ex = Assert.Throws<ParsingException>(() => parser.Parse(input));
			var groups = ex.Groups;

			Assert.NotEmpty(groups);
			Assert.NotEmpty(groups.RelevantGroups);

			var lastRelevant = groups.RelevantGroups[^1];
			Assert.Contains("literal ';'", lastRelevant.Expected.ToString());
		}

		[Fact]
		public void RelevantGroups_AfterMultipleRecoveries_PicksMaximumInEachSegment()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';'), repeat: true));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			string input = "a = b; c = d garbage ; e = f nonsense ; g = h";

			var ex = Assert.Throws<ParsingException>(() => parser.Parse(input));
			var groups = ex.Groups;

			Assert.NotEmpty(groups.RelevantGroups);

			Assert.True(groups.RelevantGroups.Count >= 1);

			Assert.Equal(groups.RelevantGroups.Last(), groups.ReversedRelevant.First());
		}

		[Fact]
		public void RelevantGroups_StopRulePreventsFurtherRecovery()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';'),
					stop => stop.Literal("end"), repeat: true));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			string input = "a = b; c = d error here end e = f;";

			var ex = Assert.Throws<ParsingException>(() => parser.Parse(input));
			var groups = ex.Groups;

			Assert.Single(groups.RelevantGroups);
			var rel = groups.RelevantGroups.First();
			Assert.Contains("literal ';'", rel.Expected.ToString());
		}

		[Fact]
		public void RelevantGroups_NestedStructureRecovery()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			// statement recovery
			builder.CreateRule("statement")
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';'), s => s.Literal('}')));

			// block recovery
			builder.CreateRule("block")
				.Literal('{')
				.OneOrMore(b => b.Rule("statement"))
				.Literal('}')
				.RecoveryLast(r => r.SkipUntil(a => a.Literal('}')));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("block"))
				.EOF();

			var parser = builder.Build();

			string input = @"
			{
				x = y;
				garbage inside statement
				z = w;
			}
			{
				corrupted block
			}";

			var ex = Assert.Throws<ParsingException>(() => parser.Parse(input));
			var groups = ex.Groups;

			Assert.True(groups.RelevantGroups.Count >= 2);

			var reversed = groups.ReversedRelevant;
			Assert.Contains("literal '='", reversed.First().Expected.ToString());
		}
	}

}