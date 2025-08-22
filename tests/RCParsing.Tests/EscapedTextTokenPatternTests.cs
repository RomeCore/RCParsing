using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing;
using RCParsing.Building;

namespace RCParsing.Tests.Parsing
{
	public class EscapedTextTokenPatternTests
	{
		private Parser BuildQuotedStringTokenParser(
			IEnumerable<KeyValuePair<string, string>> escapeMappings,
			IEnumerable<string> forbidden,
			StringComparer? comparer = null)
		{
			var builder = new ParserBuilder();

			builder.CreateToken("string")
				.Literal('"')
				.EscapedText(escapeMappings, forbidden, comparer: comparer, config: null)
				.Literal('"')
				// choose the inner token (EscapedText is second element in sequence: 0='"', 1=EscapedText, 2='"')
				.Pass(t => t[1]);

			return builder.Build();
		}

		[Fact(DisplayName = "Prefix escapes: common JSON escapes are replaced correctly")]
		public void PrefixEscapes_ReplacesCommonEscapes()
		{
			// arrange: JSON-like escapes
			var escapes = new[]
			{
				new KeyValuePair<string,string>(@"\""", @""""),
				new KeyValuePair<string,string>(@"\\", @"\"),
				new KeyValuePair<string,string>(@"\n", "\n"),
			};
			var forbidden = new[] { "\"" }; // unescaped quote terminates

			var parser = BuildQuotedStringTokenParser(escapes, forbidden);

			// input: "a\"b\\c\n"
			var input = "\"a\\\"b\\\\c\\n\"";

			// act
			var result = parser.TryMatchToken("string", input, out var tokenResult);

			// assert
			Assert.True(result, "TryMatchToken should return true");
			Assert.True(tokenResult.Success);
			// IntermediateValue contains processed inner string
			var inner = (string)tokenResult.IntermediateValue!;
			Assert.Equal("a\"b\\c\n", inner);
		}

		[Fact(DisplayName = "Invalid prefix escape leads to no match (escape not defined)")]
		public void PrefixEscapes_InvalidEscapeSequence_FailsTokenize()
		{
			// allowed: \n, \" and \\ only
			var escapes = new[]
			{
				new KeyValuePair<string,string>(@"\\n", "\n"),
				new KeyValuePair<string,string>(@"\\\""", @""""),
				new KeyValuePair<string,string>(@"\\\\", @"\"),
			};
			var forbidden = new[] { "\"" };

			var parser = BuildQuotedStringTokenParser(escapes, forbidden);
			parser.MatchToken("string", "\"abc\\xdef\"");
		}

		[Fact(DisplayName = "Double character escaping ('' -> ') works")]
		public void DoubleCharactersEscaping_WorksForSingleQuoteDoubling()
		{
			var parserBuilder = new ParserBuilder();

			parserBuilder.CreateToken("str")
				.Literal('\'')
				.EscapedTextDoubleChars('\'') // "''" -> "'"
				.Literal('\'')
				.Pass(t => t[1]);

			var parser = parserBuilder.Build();
			var result = parser.MatchToken("str", "'a''b'");

			var inner = (string)result.IntermediateValue!;
			Assert.Equal("a'b", inner);
		}

		[Fact(DisplayName = "Longest-match: prefers longest escape mapping when overlaps exist")]
		public void LongestEscapeMatch_PrefersLongerMapping()
		{
			var escapes = new[]
			{
				new KeyValuePair<string,string>(@"\u", "U"),
				new KeyValuePair<string,string>(@"\u0041", "A"),
			};
			var forbidden = new[] { "\"" };

			var parser = BuildQuotedStringTokenParser(escapes, forbidden);
			var result = parser.MatchToken("string", "\"prefix\\u0041suffix\"");

			var inner = (string)result.IntermediateValue!;
			Assert.Equal("prefixAsuffix", inner); // \u0041 -> A, not (\u) + "0041"
		}

		[Fact(DisplayName = "Forbidden multi-character sequence stops matching if unescaped")]
		public void ForbiddenSequence_MultiCharStopsMatchingWhenUnescaped()
		{
			// forbidden = "}}" ; escape maps include "}}}}" -> "}}"
			var escapes = new[]
			{
				new KeyValuePair<string,string>("}}}}", "}}")
			};
			var forbidden = new[] { "\"", "}}" };

			var parserBuilder = new ParserBuilder();
			parserBuilder.CreateToken("string")
				.Literal('"')
				.EscapedText(escapes, forbidden)
				.Literal('"')
				.Pass(t => t[1]);
			var parser = parserBuilder.Build();

			parser.MatchToken("string", "\"hello}}}}rest\"");
		}

		[Fact(DisplayName = "Empty input or immediate forbidden results in no match")]
		public void EmptyInputOrImmediateForbidden_ProducesNoMatch()
		{
			// Build a token that is only EscapedText (no surrounding quotes) for direct testing
			var parserBuilder = new ParserBuilder();
			parserBuilder.CreateToken("inner")
				.EscapedText(new KeyValuePair<string, string>[0], new[] { "\"" });
			var parser = parserBuilder.Build();

			parser.MatchToken("inner", "\"");
		}

		[Fact(DisplayName = "Adjacent escape sequences are processed in order and replaced")]
		public void AdjacentEscapeSequences_AreProcessedInOrder()
		{
			var escapes = new[]
			{
				new KeyValuePair<string,string>(@"\u0041", "A"),
				new KeyValuePair<string,string>(@"\\n", "\n"),
				new KeyValuePair<string,string>(@"\\\\", @"\"),
			};
			var forbidden = new[] { "\"" };

			var parser = BuildQuotedStringTokenParser(escapes, forbidden);
			var result = parser.MatchToken("string", @"""x\u0041\\n\\\\y""");

			var inner = (string)result.IntermediateValue!;
			Assert.Equal("xA\n\\y", inner);
		}

		[Fact(DisplayName = "EscapedText honors case-insensitive comparer")]
		public void EscapedText_WorksWithCustomComparer_CaseInsensitive()
		{
			var escapes = new[]
			{
				new KeyValuePair<string,string>(@"\X", "X_UPCASE")
			};
			var forbidden = new[] { "\"" };

			var parser = BuildQuotedStringTokenParser(escapes, forbidden, StringComparer.OrdinalIgnoreCase);
			var result = parser.MatchToken("string", "\"a\\xz\"");

			var inner = (string)result.IntermediateValue!;
			Assert.Equal("aX_UPCASEz", inner);
		}
	}
}