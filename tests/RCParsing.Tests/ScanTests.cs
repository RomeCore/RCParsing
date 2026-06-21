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
	/// <summary>
	/// Tests for <see cref="Parser.Scan"/> method.
	/// </summary>
	public class ScanTests
	{
		[Fact]
		public void Scan_BasicInterleaving()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var input = "abc 123 def 456 ghi";

			var results = builder.Build().Scan(input).ToList();

			// Expected: [raw("abc "), match(123), raw(" def "), match(456), raw(" ghi")]
			Assert.Equal(5, results.Count);

			Assert.Equal(0, results[0].VariantIndex); // raw
			Assert.Equal("abc ", results[0].AsT1().Slice);

			Assert.Equal(1, results[1].VariantIndex); // match
			Assert.Equal(123, results[1].AsT2().GetValue<int>());

			Assert.Equal(0, results[2].VariantIndex); // raw
			Assert.Equal(" def ", results[2].AsT1().Slice);

			Assert.Equal(1, results[3].VariantIndex); // match
			Assert.Equal(456, results[3].AsT2().GetValue<int>());

			Assert.Equal(0, results[4].VariantIndex); // raw
			Assert.Equal(" ghi", results[4].AsT1().Slice);
		}

		[Fact]
		public void Scan_NoMatches_ReturnsSingleRawSegment()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var input = "hello world";

			var results = builder.Build().Scan(input).ToList();

			Assert.Single(results);
			Assert.Equal(0, results[0].VariantIndex); // raw
			Assert.Equal("hello world", results[0].AsT1().Slice);
		}

		[Fact]
		public void Scan_EntireInputMatches_ReturnsEmptyRawBeforeMatch()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var input = "12345";

			var results = builder.Build().Scan(input).ToList();

			// Even if whole input matches, there's an empty raw segment before the match
			Assert.Equal(2, results.Count);
			Assert.Equal(0, results[0].VariantIndex); // raw (empty)
			Assert.Empty(results[0].AsT1().Slice);
			Assert.Equal(1, results[1].VariantIndex); // match
			Assert.Equal(12345, results[1].AsT2().GetValue<int>());
		}

		[Fact]
		public void Scan_WithTransformation_ReturnsTransformedValues()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateMainRule()
				.Literal("Price:")
				.Number<double>()
				.LiteralChoice("USD", "EUR")
				.Transform(v =>
				{
					var number = v.GetValue<double>(1);
					var currency = v.GetValue<string>(2);
					return $"{number.ToString(CultureInfo.InvariantCulture)} {currency}";
				});

			var input = "Text Price: 42.99 USD more Price: 99.50 EUR end";

			var results = builder.Build().Scan(input).ToList();

			// Expected: raw("Text "), match("42.99 USD"), raw(" more "), match("99.50 EUR"), raw(" end")
			Assert.Equal(5, results.Count);

			Assert.Equal("Text ", results[0].AsT1().Slice);
			Assert.Equal("42.99 USD", results[1].AsT2().GetValue<string>());
			Assert.Equal(" more ", results[2].AsT1().Slice);
			Assert.Equal("99.5 EUR", results[3].AsT2().GetValue<string>());
			Assert.Equal(" end", results[4].AsT1().Slice);
		}

		[Fact]
		public void Scan_TypedOr_WithFactories_ReturnsOrUnion()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var input = "a 5 b";

			// Use different types for T1 and T2 (Or disallows same types)
			var results = builder.Build().Scan<StringSegment, int>(
				input,
				rawFactory: seg => seg,
				matchFactory: match => match.GetValue<int>()
			).ToList();

			Assert.Equal(3, results.Count);

			Assert.Equal(0, results[0].VariantIndex); // raw
			Assert.Equal("a ", results[0].AsT1().Slice);

			Assert.Equal(1, results[1].VariantIndex); // match
			Assert.Equal(5, results[1].AsT2());

			Assert.Equal(0, results[2].VariantIndex); // raw
			Assert.Equal(" b", results[2].AsT1().Slice);
		}

		[Fact]
		public void Scan_ByRuleAlias()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("num").Number<int>();
			builder.CreateRule("value").Token("num");
			builder.CreateMainRule().Rule("value");

			var input = "abc 42 def";

			var results = builder.Build().Scan("value", input).ToList();

			Assert.Equal(3, results.Count);
			Assert.Equal("abc ", results[0].AsT1().Slice);
			Assert.Equal(1, results[1].VariantIndex);
			Assert.Equal(42, results[1].AsT2().GetValue<int>());
			Assert.Equal(" def", results[2].AsT1().Slice);
		}

		[Fact]
		public void Scan_WithContext()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var parser = builder.Build();
			var context = parser.CreateContext("x 7 y");

			var results = parser.Scan(context).ToList();

			Assert.Equal(3, results.Count);
			Assert.Equal("x ", results[0].AsT1().Slice);
			Assert.Equal(7, results[1].AsT2().GetValue<int>());
			Assert.Equal(" y", results[2].AsT1().Slice);
		}

		[Fact]
		public void Scan_MainRuleNotSet_Throws()
		{
			var parser = new ParserBuilder().Build();

			Assert.Throws<InvalidOperationException>(() => parser.Scan("test"));
		}

		[Fact]
		public void Scan_WrongContext_Throws()
		{
			var builder1 = new ParserBuilder();
			builder1.CreateMainRule().Literal("a");
			var parser1 = builder1.Build();

			var builder2 = new ParserBuilder();
			builder2.CreateMainRule().Literal("b");
			var parser2 = builder2.Build();

			var ctx = parser2.CreateContext("test");

			Assert.Throws<InvalidOperationException>(() => parser1.Scan(ctx));
		}

		[Fact]
		public void Scan_InvalidRuleAlias_Throws()
		{
			var builder = new ParserBuilder();
			builder.CreateMainRule().Literal("a");
			var parser = builder.Build();

			Assert.Throws<ArgumentException>(() => parser.Scan("nonexistent", "test"));
		}

		[Fact]
		public void Scan_EmptyInput_ReturnsEmpty()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var results = builder.Build().Scan("").ToList();

			// Empty input yields no segments at all (nothing to scan)
			Assert.Empty(results);
		}

		[Fact]
		public void Scan_TypedOr_FromUntypedScan_ProducesCorrectSequence()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var input = "x 10 y 20 z";

			var rawResults = builder.Build().Scan(input).ToList();

			// Convert manually to check the pattern
			var typed = rawResults.Select(seg =>
			{
				if (seg.VariantIndex == 0)
					return $"RAW:{seg.AsT1().Slice}";
				else
					return $"NUM:{seg.AsT2().GetValue<int>()}";
			}).ToList();

			Assert.Equal(5, typed.Count);
			Assert.Equal("RAW:x ", typed[0]);
			Assert.Equal("NUM:10", typed[1]);
			Assert.Equal("RAW: y ", typed[2]);
			Assert.Equal("NUM:20", typed[3]);
			Assert.Equal("RAW: z", typed[4]);
		}
	}
}
