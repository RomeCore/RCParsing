using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.ParserRules;

namespace RCParsing.Tests.Rules
{
	public class CustomRuleTests
	{
		[Fact]
		public void OneChildMatching()
		{
			var builder = new ParserBuilder();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				var res = self.ParseRule(childrenIds[0], ctx, childSettings);
				res.length -= (int)ctx.parserParameter!;
				return new ParsedRule(self.Id, res);
			}

			builder.CreateRule("custom")
				.Custom(
					Parse,
					b => b.Identifier()
				);

			var parser = builder.Build();

			var result = parser.ParseRule("custom", "IDD", parameter: 1);

			Assert.Equal(2, result.Length);
			Assert.Equal("ID", result.Text);
		}

		[Fact]
		public void TwoChildren_ConcatenateResults()
		{
			var builder = new ParserBuilder();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				var left = self.ParseRule(childrenIds[0], ctx, childSettings);
				ctx.position = left.endIndex;
				var right = self.ParseRule(childrenIds[1], ctx, childSettings);

				return new ParsedRule(self.Id,
					new ParsedElement(left.startIndex, right.endIndex - left.startIndex,
						left.GetText(ctx) + right.GetText(ctx) + "C"));
			}

			builder.CreateRule("customConcat")
				.Custom(Parse,
					b => b.Literal("A"),
					b => b.Literal("B"));

			var parser = builder.Build();

			var result = parser.ParseRule("customConcat", "AB");
			Assert.True(result.Success);
			Assert.Equal("AB", result.Text);
			Assert.Equal("ABC", result.IntermediateValue);
		}

		[Fact]
		public void FailsIfChildFails()
		{
			var builder = new ParserBuilder();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				var res = self.ParseRule(childrenIds[0], ctx, childSettings);
				if (!res.success)
					return ParsedRule.Fail;
				return new ParsedRule(self.Id, res);
			}

			builder.CreateRule("mustMatchId")
				.Custom(Parse, b => b.Identifier());

			var parser = builder.Build();

			var ok = parser.ParseRule("mustMatchId", "abc");
			Assert.True(ok.Success);

			var ex = Assert.Throws<ParsingException>(() => parser.ParseRule("mustMatchId", "123"));
			Assert.NotNull(ex);
		}

		[Fact]
		public void CustomRule_ParameterAffectsResult()
		{
			var builder = new ParserBuilder();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				var res = self.ParseRule(childrenIds[0], ctx, childSettings);
				var suffix = (string)ctx.parserParameter!;
				var combinedText = res.GetText(ctx) + suffix;

				return new ParsedRule(self.Id,
					new ParsedElement(res.startIndex, combinedText.Length, combinedText));
			}

			builder.CreateRule("paramRule")
				.Custom(Parse, b => b.Identifier());

			var parser = builder.Build();

			var result = parser.ParseRule("paramRule", "Test", parameter: "_X");
			Assert.Equal("Test_X", result.IntermediateValue);
		}

		[Fact]
		public void CustomRule_SelectsShortestChild()
		{
			var builder = new ParserBuilder();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				ParsedRule best = ParsedRule.Fail;
				foreach (var id in childrenIds)
				{
					var r = self.ParseRule(id, ctx, childSettings);
					if (!best.success || r.length < best.length)
						best = r;
				}

				return new ParsedRule(self.Id, best);
			}

			builder.CreateRule("shortest")
				.Custom(Parse,
					b => b.Literal("HELLO"),
					b => b.Literal("HE"));

			var parser = builder.Build();

			var result = parser.ParseRule("shortest", "HELLO");
			Assert.Equal(2, result.Length);
			Assert.Equal("HE", result.Text);
		}

		[Fact]
		public void CustomRule_ReusesChildResultsWithOffset()
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespaces();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				var first = self.ParseRule(childrenIds[0], ctx, childSettings);
				ctx.position = first.endIndex;
				var second = self.ParseRule(childrenIds[0], ctx, childSettings);

				return new ParsedRule(self.Id,
					new ParsedElement(first.startIndex, second.endIndex - first.startIndex,
						first.GetText(ctx) + second.GetText(ctx)));
			}

			builder.CreateRule("doubleId")
				.Custom(Parse, b => b.Identifier());

			var parser = builder.Build();

			var result = parser.ParseRule("doubleId", "id id");
			Assert.Equal("id id", result.Text);
			Assert.Equal("idid", result.IntermediateValue);
		}

		[Fact]
		public void CustomRule_WithMultipleChildrenAndSkip()
		{
			var builder = new ParserBuilder();
			builder.Settings.Skip(r => r.Whitespaces());

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				var left = self.ParseRule(childrenIds[0], ctx, childSettings);
				ctx.position = left.endIndex;
				var right = self.ParseRule(childrenIds[1], ctx, childSettings);

				return new ParsedRule(self.Id, new ParsedElement(left.startIndex,
					right.endIndex - left.startIndex, $"{left.GetText(ctx)}-{right.GetText(ctx)}"));
			}

			builder.CreateRule("combine")
				.Custom(Parse,
					b => b.Identifier(),
					b => b.Identifier());

			var parser = builder.Build();
			var result = parser.ParseRule("combine", "x y");

			Assert.True(result.Success);
			Assert.Equal("x y", result.Text);
			Assert.Equal("x-y", result.IntermediateValue);
		}

		[Fact]
		public void CustomRule_CanReturnTransformedIntermediateValue()
		{
			var builder = new ParserBuilder();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				var res = self.ParseRule(childrenIds[0], ctx, childSettings);
				return new ParsedRule(self.Id, res) { intermediateValue = res.length };
			}

			builder.CreateRule("lengthRule")
				.Custom(Parse, b => b.Identifier());

			var parser = builder.Build();
			var result = parser.ParseRule("lengthRule", "abcd");
			Assert.Equal(4, result.IntermediateValue);
		}

		[Fact]
		public void CustomRule_MultipleChildren_FailIfMismatch()
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespaces();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, ParserRule[] children, int[] childrenIds)
			{
				var first = self.ParseRule(childrenIds[0], ctx, childSettings);
				ctx.position = first.endIndex;
				var second = self.ParseRule(childrenIds[1], ctx, childSettings);

				if (first.GetText(ctx) != second.GetText(ctx))
					return ParsedRule.Fail;

				return new ParsedRule(self.Id, first, second);
			}

			builder.CreateRule("mustMatchTwice")
				.Custom(Parse,
					b => b.Identifier(),
					b => b.Identifier());

			var parser = builder.Build();

			Assert.Throws<ParsingException>(() => parser.ParseRule("mustMatchTwice", "foo bar"));

			var ok = parser.ParseRule("mustMatchTwice", "abc abc");
			Assert.True(ok.Success);
		}
	}
}