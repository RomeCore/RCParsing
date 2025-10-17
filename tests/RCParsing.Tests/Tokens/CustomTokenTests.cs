using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests.Tokens
{
	public class CustomTokenTests
	{
		[Fact]
		public void OneChildMatching()
		{
			var builder = new ParserBuilder();

			string intermediateValue = "test";

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				var res = children[0].Match(input, start, end, parameter, calc, ref ferr);
				res.intermediateValue = intermediateValue;
				return res;
			}

			builder.CreateToken("custom")
				.Custom(
					Match,
					b => b.Identifier()
				);

			var parser = builder.Build();

			var match = parser.TryMatchToken("custom", "ID");

			Assert.True(match.Success);
			Assert.Equal("test", match.IntermediateValue);
		}

		[Fact]
		public void TwoChildren_MatchFirstOnly()
		{
			var builder = new ParserBuilder();

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				var first = children[0].Match(input, start, end, parameter, calc, ref ferr);
				if (!first.success) return ParsedElement.Fail;
				return new ParsedElement(first.startIndex, first.length, first.GetText(input)!.ToUpperInvariant());
			}

			builder.CreateToken("custom")
				.Custom(Match,
					b => b.Literal("ok"),
					b => b.Identifier());

			var parser = builder.Build();

			var match = parser.TryMatchToken("custom", "ok");
			Assert.True(match.Success);
			Assert.Equal("OK", match.IntermediateValue);
		}

		[Fact]
		public void NoChildren_AlwaysFails()
		{
			var builder = new ParserBuilder();

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				return ParsedElement.Fail;
			}

			builder.CreateToken("failToken").Custom(Match);

			var parser = builder.Build();
			var match = parser.TryMatchToken("failToken", "abc");
			Assert.False(match.Success);
		}

		[Fact]
		public void ChildMatch_UsesExternalParameter()
		{
			var builder = new ParserBuilder();

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				var res = children[0].Match(input, start, end, parameter, calc, ref ferr);
				if (parameter is string suffix)
					res.intermediateValue = res.GetText(input) + suffix;
				return res;
			}

			builder.CreateToken("paramToken")
				.Custom(Match, b => b.Identifier());

			var parser = builder.Build();
			var match = parser.TryMatchToken("paramToken", "name", parameter: "_123");

			Assert.True(match.Success);
			Assert.Equal("name_123", match.IntermediateValue);
		}

		[Fact]
		public void CustomToken_ReturnsChildIntermediateValue()
		{
			var builder = new ParserBuilder();

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				var res = children[0].Match(input, start, end, parameter, calc, ref ferr);
				var match = (System.Text.RegularExpressions.Match)res.intermediateValue!;
				res.intermediateValue = match.Value.ToLower();
				return res;
			}

			builder.CreateToken("upper")
				.Custom(Match,
					b => b.Regex("[A-Z]+"));

			var parser = builder.Build();

			var match = parser.TryMatchToken("upper", "HELLO");
			Assert.True(match.Success);
			Assert.Equal("hello", match.IntermediateValue);
			Assert.Equal("HELLO", match.Text);
		}

		[Fact]
		public void CustomToken_FailsIfChildFails()
		{
			var builder = new ParserBuilder();

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				var res = children[0].Match(input, start, end, parameter, calc, ref ferr);
				return res.success ? res : ParsedElement.Fail;
			}

			builder.CreateToken("failIfNotDigit")
				.Custom(Match,
					b => b.Regex("\\d+"));

			var parser = builder.Build();

			var success = parser.TryMatchToken("failIfNotDigit", "12345");
			Assert.True(success.Success);

			var fail = parser.TryMatchToken("failIfNotDigit", "abc");
			Assert.False(fail.Success);
		}

		[Fact]
		public void CustomToken_MultipleChildren_CombinesResults()
		{
			var builder = new ParserBuilder();

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				var part1 = children[0].Match(input, start, end, parameter, calc, ref ferr);
				if (!part1.success) return ParsedElement.Fail;
				var nextStart = part1.startIndex + part1.length;

				var part2 = children[1].Match(input, nextStart, end, parameter, calc, ref ferr);
				if (!part2.success) return ParsedElement.Fail;

				var combinedText = input.Substring(part1.startIndex, part1.length)
					+ input.Substring(part2.startIndex, part2.length);
				return new ParsedElement(start, combinedText.Length, combinedText);
			}

			builder.CreateToken("combined")
				.Custom(Match,
					b => b.Regex(@"[A-Z]+"),
					b => b.Regex(@"\d+"));

			var parser = builder.Build();
			var match = parser.TryMatchToken("combined", "ABC123");

			Assert.True(match.Success);
			Assert.Equal("ABC123", match.IntermediateValue);
		}

		[Fact]
		public void CustomToken_RegexTrim()
		{
			var builder = new ParserBuilder();

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				var first = children[0].Match(input, start, end, parameter, calc, ref ferr);
				var text = input.Substring(first.startIndex, first.length).Trim();
				return new ParsedElement(first.startIndex, first.length, text);
			}

			builder.CreateToken("trimmed")
				.Custom(Match, b => b.Regex(@"[\sA-Za-z\s]+"));

			var parser = builder.Build();
			var match = parser.TryMatchToken("trimmed", "  Hello  ");
			Assert.True(match.Success);
			Assert.Equal("Hello", match.IntermediateValue);
		}
	}
}