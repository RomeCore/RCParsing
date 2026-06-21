using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.ParserRules;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests
{
	public class ReplaceAllMatchesTests
	{
		[Fact]
		public void Replace_WithCustomSelector()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("hello").Literal("hello");
			builder.CreateMainRule().Token("hello");

			var result = builder.Build().ReplaceAllMatches("hello world hello",
				replacementSelector: r => "X");

			Assert.Equal("X world X", result);
		}

		[Fact]
		public void Replace_NoMatches_ReturnsOriginal()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var result = builder.Build().ReplaceAllMatches("hello world");
			Assert.Equal("hello world", result);
		}

		[Fact]
		public void Replace_ByRuleAlias()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("num").Number<int>();
			builder.CreateRule("value").Token("num");
			builder.CreateMainRule().Rule("value");

			var result = builder.Build().ReplaceAllMatches("value", "a 1 b 2 c",
				replacementSelector: r => (r.GetValue<int>() * 12).ToString(CultureInfo.InvariantCulture));

			Assert.Equal("a 12 b 24 c", result);
		}

		[Fact]
		public void Replace_SequenceWithTransform_RemovesSpaces()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateMainRule()
				.Literal("(")
				.Number<int>()
				.Literal(")")
				.Transform(v =>
				{
					var num = v.GetValue<int>(1);
					return (num * 10).ToString(CultureInfo.InvariantCulture);
				});

			// SkipWhitespaces removes spaces between tokens
			var result = builder.Build().ReplaceAllMatches("( 1 ) text ( 2 ) more ( 3 )",
				replacementSelector: r => r.GetValue<string>());

			Assert.Equal("10 text 20 more 30", result);
		}

		[Fact]
		public void Replace_WithContext()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("num").Number<int>();
			builder.CreateMainRule().Token("num");

			var parser = builder.Build();
			var context = parser.CreateContext("x 5 y");

			var result = parser.ReplaceAllMatches(context,
				r => r.GetValue<int>().ToString(CultureInfo.InvariantCulture));

			Assert.Equal("x 5 y", result);
		}

		[Fact]
		public void Replace_WithParameter()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Literal("x")
				.Transform(v =>
				{
					var param = v.GetParsingParameter<int>();
					return $"x{param}";
				});

			var result = builder.Build().ReplaceAllMatches("x x", parameter: 42,
				replacementSelector: r => r.GetValue<string>());

			Assert.Equal("x42 x42", result);
		}

		[Fact]
		public void Replace_NoOverlap()
		{
			var builder = new ParserBuilder();
			builder.CreateMainRule().Literal("aa");

			var parser = builder.Build();

			var result = parser.ReplaceAllMatches("aaa",
				replacementSelector: r => "X");

			Assert.Equal("Xa", result);
		}

		[Fact]
		public void Replace_MainRuleNotSet_Throws()
		{
			var parser = new ParserBuilder().Build();
			Assert.Throws<InvalidOperationException>(() => parser.ReplaceAllMatches("test"));
		}

		[Fact]
		public void Replace_InvalidAlias_Throws()
		{
			var builder = new ParserBuilder();
			builder.CreateMainRule().Literal("a");
			var parser = builder.Build();

			Assert.Throws<ArgumentException>(() =>
				parser.ReplaceAllMatches("nonexistent", "test", replacementSelector: r => "X"));
		}

		[Fact]
		public void Replace_EmptyInput_ReturnsEmpty()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var result = builder.Build().ReplaceAllMatches("", replacementSelector: r => "X");
			Assert.Empty(result);
		}

		[Fact]
		public void Replace_WithFallbackReplacement_NullValue()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Literal("x")
				.Transform(v => null); // explicitly return null

			var result = builder.Build().ReplaceAllMatches("x y x",
				fallbackReplacement: "NULL");

			// Value is null -> fallbackReplacement kicks in
			Assert.Equal("NULL y NULL", result);
		}
	}
}
