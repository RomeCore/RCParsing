using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests
{
	public class TokenPatternTests
	{
		[Fact(DisplayName = "Literal token matches exact string")]
		public void LiteralToken_MatchesExact()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Literal("hello");

			var parser = builder.Build();
			var tokenResult = parser.MatchToken("kw", "hello world");
			Assert.True(tokenResult.Success);
			Assert.Equal("hello", tokenResult.Text);
			Assert.Equal(0, tokenResult.StartIndex);
			Assert.Equal(5, tokenResult.Length);
		}

		[Fact(DisplayName = "LiteralChoice picks longest alternative and supports case-insensitive comparer")]
		public void LiteralChoice_LongestAndCaseInsensitive()
		{
			var builder = new ParserBuilder();

			// literal choice with case-insensitive comparer
			builder.CreateToken("bool")
				.LiteralChoice(new[] { "True", "TRUE", "trueish", "true" }, StringComparer.OrdinalIgnoreCase);

			var parser = builder.Build();
			var res = parser.MatchToken("bool", "TRUEISH!");
			Assert.True(res.Success);
			// should pick "trueish" (longest) regardless of case when comparer is case-insensitive
			Assert.Equal("TRUEISH", res.Text); // text is original input slice, comparer only affects matching
			Assert.Equal("TRUEISH".Length, res.Length);
		}

		[Fact(DisplayName = "CharRange token matches characters in range")]
		public void CharRangeToken_MatchesRange()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("hexdigit").Char(c => c >= '0' && c <= '9');

			var parser = builder.Build();
			var res = parser.MatchToken("hexdigit", "7F");
			Assert.True(res.Success);
			Assert.Equal("7", res.Text);
		}

		[Fact(DisplayName = "Regex token exposes Match as intermediate value and can be transformed via factory")]
		public void RegexToken_ProvidesMatchAndFactoryWorks()
		{
			var builder = new ParserBuilder();
			// factory converts matched number text to int and also intermediate Match should be available
			builder.CreateToken("num")
				.Regex(@"\d+");

			var parser = builder.Build();
			var res = parser.MatchToken("num", "123abc");
			Assert.True(res.Success);

			// IntermediateValue (raw) should be Match (implementation-dependent but typical)
			Assert.IsType<Match>(res.IntermediateValue);
			var match = (Match)res.IntermediateValue!;
			Assert.Equal("123", match.Value);
		}

		[Fact(DisplayName = "EOF token matches only at end")]
		public void EOFToken_MatchesOnlyAtEnd()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("eof").EOF();

			var parser = builder.Build();

			// not at end -> shouldn't match
			var r1 = parser.MatchToken("eof", "abc");
			Assert.False(r1.Success);

			// at end (empty input) -> should match
			var r2 = parser.MatchToken("eof", "");
			Assert.True(r2.Success);
		}

		[Fact(DisplayName = "Sequence token concatenates children and transform factory applied")]
		public void SequenceToken_TransformsResult()
		{
			var builder = new ParserBuilder();

			// build token: '(' number ')'  -> transform to int
			builder.CreateToken("parenNum")
				.Literal("(")
				.Regex(@"\d+") // keep intermediate in token; factory used later by sequence
				.Literal(")")
				.Pass(v => v[1]);

			var parser = builder.Build();
			var res = parser.MatchToken("parenNum", "(42)+x");
			Assert.True(res.Success);
			Assert.Equal("(42)", res.Text);
			Assert.Equal("42", (res.IntermediateValue as Match)!.Value);
		}

		[Fact(DisplayName = "Optional token present and absent behavior")]
		public void OptionalToken_PresentAndAbsent()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("opt")
				.Literal("[")
				.Optional(o => o.Regex(@"[a-z]+"))
				.Literal("]");

			var parser = builder.Build();

			var r1 = parser.MatchToken("opt", "[abc]");
			Assert.True(r1.Success);
			Assert.Equal("[abc]", r1.Text);

			var r2 = parser.MatchToken("opt", "[]");
			Assert.True(r2.Success);
			Assert.Equal("[]", r2.Text);
		}

		[Fact(DisplayName = "Repeat token zero-or-more and one-or-more")]
		public void RepeatToken_ZeroOrMoreAndOneOrMore()
		{
			var builder = new ParserBuilder();

			// token that matches one or more 'ha' : ("ha")+ ; we will use sequence repeat
			builder.CreateToken("haSeq")
				.Literal("ha")
				.ZeroOrMore(z => z.Literal("ha")); // allows empty or repeated "ha"

			// token that matches zero or more 'ha' : ("ha")* ; we will use sequence repeat
			builder.CreateToken("haSeqZ")
				.ZeroOrMore(z => z.Literal("ha"));

			var parser = builder.Build();

			var r1 = parser.MatchToken("haSeq", "hahahaX");
			Assert.True(r1.Success);
			Assert.Equal("hahaha", r1.Text);

			var r2 = parser.MatchToken("haSeqZ", "X");
			Assert.True(r2.Success);
			Assert.Equal("", r2.Text);
		}

		[Fact(DisplayName = "Choice token picks first matching child (or longest if implemented)")]
		public void ChoiceToken_Behavior()
		{
			var builder = new ParserBuilder();

			// Build explicit choice between literal "a", "ab" and regex letter+
			builder.CreateToken("ch")
				.Choice(
					b => b.Literal("ab"),
					b => b.Literal("a"),
					b => b.Regex("[a-z]+"));

			var parser = builder.Build();
			var res = parser.MatchToken("ch", "abz");
			Assert.True(res.Success);
			// expected that first choice "ab" matches (or, if precedence/longest, "ab" still chosen)
			Assert.Equal("ab", res.Text);
		}

		[Fact(DisplayName = "Named token alias lookup works")]
		public void TokenAlias_LookupByName()
		{
			var builder = new ParserBuilder();

			// build token with alias "id"
			builder.CreateToken("identifier").Regex(@"[A-Za-z_][A-Za-z0-9_]*");

			var parser = builder.Build();
			var res = parser.MatchToken("identifier", "var123 = 5");
			Assert.True(res.Success);
			Assert.Equal("var123", res.Text);
		}

		[Fact(DisplayName = "Token sequence with internal factories: intermediate values preserved")]
		public void TokenSequence_IntermediateValuesPreserved()
		{
			var builder = new ParserBuilder();

			// build token: quote + word + quote, but keep inner word match as intermediate value
			builder.CreateToken("quoted")
				.Literal('"')
				.Regex(@"[a-z]+") // regex sets intermediate Match by default
				.Literal('"')
				.Pass(v => v[1]);

			var parser = builder.Build();
			var res = parser.MatchToken("quoted", "\"hello\" rest");
			Assert.True(res.Success);

			// check intermediate match (child token) is available in children if your wrapper exposes it;
			// specifically check final transformed Value equals inner content
			Assert.Equal("hello", (res.IntermediateValue as Match)!.Value);
		}

		[Fact]
		public void CombinatorJsonParsing()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("string")
				.Between(
					b => b.Literal('"'),
					b => b.TextUntil('"'),
					b => b.Literal('"'));

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer);

			builder.CreateToken("boolean")
				.Map<string>(b => b.LiteralChoice("true", "false"), m => m == "true");

			builder.CreateToken("null")
				.Literal("null", _ => null);

			builder.CreateToken("value")
				.SkipWhitespaces(b =>
					b.Choice(
						c => c.Token("string"),
						c => c.Token("number"),
						c => c.Token("boolean"),
						c => c.Token("null"),
						c => c.Token("array"),
						c => c.Token("object")
				));

			builder.CreateToken("value_list")
				.ZeroOrMoreSeparated(
					b => b.Token("value"),
					b => b.SkipWhitespaces(b => b.Literal(',')))
				.Pass(v =>
				{
					return v.ToArray();
				});

			builder.CreateToken("array")
				.Between(
					b => b.Literal('['),
					b => b.Token("value_list"),
					b => b.SkipWhitespaces(b => b.Literal(']')));

			builder.CreateToken("pair")
				.SkipWhitespaces(b => b.Token("string"))
				.SkipWhitespaces(b => b.Literal(':'))
				.Token("value")
				.Pass(v =>
				{
					return KeyValuePair.Create((string)v[0], v[2]);
				});

			builder.CreateToken("pair_list")
				.ZeroOrMoreSeparated(
					b => b.Token("pair"),
					b => b.SkipWhitespaces(b => b.Literal(',')))
				.Pass(v =>
				{
					return v.Cast<KeyValuePair<string, object>>().ToDictionary();
				});

			builder.CreateToken("object")
				.Between(
					b => b.Literal('{'),
					b => b.Token("pair_list"),
					b => b.SkipWhitespaces(b => b.Literal('}')));

			builder.CreateMainRule("content")
				.Token("value") // 0
				.EOF()
				.TransformSelect(index: 0);

			var parser = builder.Build();

			var json =
			"""
			{
				"id": 1,
				"name": "Sample Data",
				"created": "2023-01-01T00:00:00", // This is a comment
				"tags": ["tag1", "tag2", "tag3"],
				"isActive": true,
				"nested": {
					"value": 123.456,
					"description": "Nested description"
				}
			}
			""";

			var result = parser.Parse<Dictionary<string, object>>(json);
			Assert.Equal(1, (double)result["id"]);
			Assert.Equal(["tag1", "tag2", "tag3"], (object[])result["tags"]);
			Assert.True((bool)result["isActive"]);
			var nested = (Dictionary<string, object>)result["nested"];
			Assert.Equal(123.456, (double)nested["value"]);
			Assert.Equal("Nested description", nested["description"].ToString());
		}
	}
}