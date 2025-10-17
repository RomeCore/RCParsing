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

		[Fact]
		public void RelevantGroups_SimpleRecovery_WhenSuccessParsing()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Keyword("var")
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';'), repeat: true));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF()
					.RecoveryLast(r => r.FindNext());

			var parser = builder.Build();

			string input1 = "var a = b; var c = d garbage; var g = j;";
			string input2 = "var a = b; var c = d garbage; var b = c; var g = j garbage;";

			var result1 = parser.Parse(input1);
			var groups1 = result1.CreateErrorGroups();

			Assert.NotEmpty(groups1);
			Assert.NotEmpty(groups1.RelevantGroups);

			var lastRelevant1 = groups1.ReversedRelevant[0];

			Assert.Equal(1, lastRelevant1.Line);
			Assert.Equal(22, lastRelevant1.Column);
			Assert.Contains("literal ';'", lastRelevant1.Expected.ToString());

			var result2 = parser.Parse(input2);
			var groups2 = result2.CreateErrorGroups();

			Assert.NotEmpty(groups2);
			Assert.NotEmpty(groups2.RelevantGroups);
			Assert.Equal(2, groups2.RelevantGroups.Count);

			var lastRelevant21 = groups2.ReversedRelevant[0];
			var lastRelevant22 = groups2.ReversedRelevant[1];

			Assert.Equal(1, lastRelevant21.Line);
			Assert.Equal(52, lastRelevant21.Column);
			Assert.Contains("literal ';'", lastRelevant21.Expected.ToString());

			Assert.Equal(1, lastRelevant22.Line);
			Assert.Equal(22, lastRelevant22.Column);
			Assert.Contains("literal ';'", lastRelevant22.Expected.ToString());
		}
	}
}