using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.ParserRules;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests
{
	/// <summary>
	/// Tests for matching patterns in a string.
	/// </summary>
	public class FindAllMatchesTests
	{
		[Fact]
		public void SimpleStringMatch()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("simple_string").Regex("hello world");
			builder.CreateMainRule().Token("simple_string");

			var input =
			"""
			hello world

			abcdefg
			1234567890

			hello world

			aaa hello world bbb

			hell!!! hello world
			""";

			var matches = builder.Build().FindAllMatches(input);
			Assert.Equal(4, matches.Count()); // 4 occurrences of "hello world" in the input string.
		}

		[Fact]
		public void Transformation()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateMainRule()

				.Literal("Price:")
				.Number<double>()
				.LiteralChoice(["USD", "EUR"])

				.Transform(v =>
				{
					var number = v[1].Value;
					var currency = v[2].Value;
					return new { Amount = number, Currency = currency };
				});

			var input =
			"""
			Some log entries.
			Price: 42.99 USD
			Error: something happened.
			Price: 99.50 EUR
			Another line.
			Price: 2.50 USD
			""";

			var prices = builder.Build().FindAllMatches<dynamic>(input).ToList();

			Assert.Equal(3, prices.Count);
			Assert.Equal(42.99, prices[0].Amount);
			Assert.Equal("USD", prices[0].Currency);
			Assert.Equal("EUR", prices[1].Currency);
		}

		[Fact]
		public void ErrorsInInput()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("key_value")
				.Identifier()
				.Literal("=")
				.Number<int>()
				.Transform(v =>
				{
					var key = v[0].Text;
					var value = v[2].Value;
					return $"{key}={value}";
				});

			builder.CreateMainRule().Rule("key_value");

			var input = "a=1 broken!! c=3 d=invalid e=5 f=6";

			var validPairs = builder.Build().FindAllMatches<string>(input).ToList();

			Assert.Equal(4, validPairs.Count); // a=1, c=3, e=5, f=6
			Assert.Contains("a=1", validPairs);
			Assert.Contains("f=6", validPairs);
			Assert.DoesNotContain("broken!!", validPairs);
			Assert.DoesNotContain("d=invalid", validPairs);
		}

		[Fact]
		public void SkippingComments()
		{
			var builder = new ParserBuilder();

			builder.Settings
				.Skip(r => r.Rule("skip"), ParserSkippingStrategy.SkipBeforeParsingGreedy);

			builder.CreateRule("skip")
				.Choice(
					b => b.Whitespaces(),
					b => b.Literal("//").TextUntil('\n', '\r'),
					b => b.Literal("/*").TextUntil("*/").Literal("*/"))
				.ConfigureForSkip();

			builder.CreateToken("string")
				.Literal('"')
				.TextUntil('"')
				.Literal('"')
				.Pass(1);

			builder.CreateRule("log_statement")
				.Literal("log")
				.Literal("(")
				.Token("string")
				.Literal(")")
				.TransformSelect(2);

			builder.CreateMainRule().Rule("log_statement");

			var input =
			"""
			log("start");
			var a = 1;
			// log("ignore_me");
			log("continue");
			/*
			log("never_reach");
			*/
			log("end");
			""";

			var logCalls = builder.Build().FindAllMatches<string>(input).ToList();

			Assert.Equal(3, logCalls.Count);
			Assert.Equal("start", logCalls[0]);
			Assert.Equal("continue", logCalls[1]);
			Assert.Equal("end", logCalls[2]);
		}

		[Fact]
		public void OverlappingPatterns()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("abc_rule").Literal("abc");
			builder.CreateRule("bcd_rule").Literal("bcd");

			builder.CreateMainRule()
				.Choice(
					c => c.Rule("abc_rule"),
					c => c.Rule("bcd_rule")
				);

			var input = "abcd";

			var parser = builder.Build();
			var matches = parser.FindAllMatches(input).ToList();

			Assert.Single(matches);
			Assert.Equal("abc", matches[0].Text);

			var overlappingMatches = parser.FindAllMatches(input, overlap: true).ToList();

			Assert.Equal(2, overlappingMatches.Count);
			Assert.Equal("abc", overlappingMatches[0].Text);
			Assert.Equal("bcd", overlappingMatches[1].Text);
		}
	}
}