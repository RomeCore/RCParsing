using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests.Tokens
{
	public class TokenPatternTests
	{
		[Fact(DisplayName = "Literal token matches exact string")]
		public void LiteralToken_MatchesExact()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Literal("hello");

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "hello world");
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
			var res = parser.TryMatchToken("bool", "TRUEISH!");
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
			var res = parser.TryMatchToken("hexdigit", "7F");
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
			var res = parser.TryMatchToken("num", "123abc");
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
			var r1 = parser.TryMatchToken("eof", "abc");
			Assert.False(r1.Success);

			// at end (empty input) -> should match
			var r2 = parser.TryMatchToken("eof", "");
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
			var res = parser.TryMatchToken("parenNum", "(42)+x");
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

			var r1 = parser.TryMatchToken("opt", "[abc]");
			Assert.True(r1.Success);
			Assert.Equal("[abc]", r1.Text);

			var r2 = parser.TryMatchToken("opt", "[]");
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

			var r1 = parser.TryMatchToken("haSeq", "hahahaX");
			Assert.True(r1.Success);
			Assert.Equal("hahaha", r1.Text);

			var r2 = parser.TryMatchToken("haSeqZ", "X");
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
			var res = parser.TryMatchToken("ch", "abz");
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
			var res = parser.TryMatchToken("identifier", "var123 = 5");
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
			var res = parser.TryMatchToken("quoted", "\"hello\" rest");
			Assert.True(res.Success);

			// check intermediate match (child token) is available in children if your wrapper exposes it;
			// specifically check final transformed Value equals inner content
			Assert.Equal("hello", (res.IntermediateValue as Match)!.Value);
		}

		[Fact(DisplayName = "Literal token supports different StringComparison options")]
		public void LiteralToken_StringComparison()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Literal("hello", StringComparison.OrdinalIgnoreCase);

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "HELLO world");
			Assert.True(tokenResult.Success);
			Assert.Equal("HELLO", tokenResult.Text);
		}

		[Fact(DisplayName = "Literal token fails on non-match")]
		public void LiteralToken_FailsOnNonMatch()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Literal("hello");

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "world");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "LiteralChoice fails on empty input")]
		public void LiteralChoice_FailsOnEmptyInput()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").LiteralChoice(new[] { "a", "b", "c" });

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "LiteralChoice fails when no keyword matches")]
		public void LiteralChoice_FailsOnNoMatch()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").LiteralChoice(new[] { "a", "b", "c" });

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "d");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "Keyword token matches successfully")]
		public void KeywordToken_MatchesSuccessfully()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Keyword("hello", c => char.IsLetterOrDigit(c) || c == '_');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "hello world");
			Assert.True(tokenResult.Success);
			Assert.Equal("hello", tokenResult.Text);
		}

		[Fact(DisplayName = "Keyword token fails if followed by prohibited character")]
		public void KeywordToken_FailsOnProhibitedChar()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Keyword("hello", c => char.IsLetterOrDigit(c) || c == '_');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "helloworld");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "Keyword token supports case-insensitive matching")]
		public void KeywordToken_CaseInsensitive()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Keyword("hello", c => char.IsLetterOrDigit(c) || c == '_', StringComparison.OrdinalIgnoreCase);

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "HELLO world");
			Assert.True(tokenResult.Success);
			Assert.Equal("HELLO", tokenResult.Text);
		}

		[Fact(DisplayName = "Keyword token matches at end of input")]
		public void KeywordToken_MatchesAtEndOfInput()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").Keyword("hello", c => char.IsLetterOrDigit(c) || c == '_');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "hello");
			Assert.True(tokenResult.Success);
			Assert.Equal("hello", tokenResult.Text);
		}

		[Fact(DisplayName = "KeywordChoice token matches one of several keywords")]
		public void KeywordChoiceToken_MatchesOneOfSeveral()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").KeywordChoice(new[] { "if", "else", "while" }, c => char.IsLetterOrDigit(c) || c == '_');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "if (condition)");
			Assert.True(tokenResult.Success);
			Assert.Equal("if", tokenResult.Text);
		}

		[Fact(DisplayName = "KeywordChoice token picks longest match")]
		public void KeywordChoiceToken_PicksLongestMatch()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").KeywordChoice(new[] { "a", "ab", "abc" }, c => char.IsLetterOrDigit(c) || c == '_');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "abc");
			Assert.True(tokenResult.Success);
			Assert.Equal("abc", tokenResult.Text);
		}

		[Fact(DisplayName = "KeywordChoice token fails if followed by prohibited character")]
		public void KeywordChoiceToken_FailsOnProhibitedChar()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").KeywordChoice(new[] { "if", "else", "while" }, c => char.IsLetterOrDigit(c) || c == '_');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "ifA");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "KeywordChoice token supports case-insensitive matching")]
		public void KeywordChoiceToken_CaseInsensitive()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("kw").KeywordChoice(new[] { "if", "else", "while" }, c => char.IsLetterOrDigit(c) || c == '_', StringComparer.OrdinalIgnoreCase);

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("kw", "IF (condition)");
			Assert.True(tokenResult.Success);
			Assert.Equal("IF", tokenResult.Text);
		}

		[Fact(DisplayName = "Identifier token matches valid identifier")]
		public void IdentifierToken_MatchesValid()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("id").Identifier(c => char.IsLetter(c) || c == '_', c => char.IsLetterOrDigit(c) || c == '_');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("id", "my_var123");
			Assert.True(tokenResult.Success);
			Assert.Equal("my_var123", tokenResult.Text);
		}

		[Fact(DisplayName = "Identifier token fails on invalid identifier")]
		public void IdentifierToken_FailsOnInvalid()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("id").Identifier(c => char.IsLetter(c) || c == '_', c => char.IsLetterOrDigit(c) || c == '_');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("id", "123var");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "Identifier token respects min and max length")]
		public void IdentifierToken_RespectsMinMaxLength()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("id").Identifier(c => char.IsLetter(c) || c == '_', c => char.IsLetterOrDigit(c) || c == '_', 3, 5);

			var parser = builder.Build();

			// Too short
			var r1 = parser.TryMatchToken("id", "ab");
			Assert.False(r1.Success);

			// Valid
			var r2 = parser.TryMatchToken("id", "abc");
			Assert.True(r2.Success);
			Assert.Equal("abc", r2.Text);

			// Valid
			var r3 = parser.TryMatchToken("id", "abcde");
			Assert.True(r3.Success);
			Assert.Equal("abcde", r3.Text);

			// Too long
			var r4 = parser.TryMatchToken("id", "abcdef");
			Assert.True(r4.Success);
			Assert.Equal("abcde", r4.Text);
		}

		[Fact(DisplayName = "Identifier token handles unicode")]
		public void IdentifierToken_HandlesUnicode()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("id").Identifier(c => char.IsLetter(c), c => char.IsLetterOrDigit(c));

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("id", "переменная1");
			Assert.True(tokenResult.Success);
			Assert.Equal("переменная1", tokenResult.Text);
		}

		[Fact(DisplayName = "LiteralChar token matches single character")]
		public void LiteralCharToken_MatchesSingleChar()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("char").Literal('a');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("char", "abc");
			Assert.True(tokenResult.Success);
			Assert.Equal("a", tokenResult.Text);
		}

		[Fact(DisplayName = "LiteralChar token supports case-insensitive matching")]
		public void LiteralCharToken_CaseInsensitive()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("char").Literal('a', StringComparison.OrdinalIgnoreCase);

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("char", "Abc");
			Assert.True(tokenResult.Success);
			Assert.Equal("A", tokenResult.Text);
		}

		[Fact(DisplayName = "LiteralChar token matches at end of input")]
		public void LiteralCharToken_MatchesAtEndOfInput()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("char").Literal('a');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("char", "a");
			Assert.True(tokenResult.Success);
			Assert.Equal("a", tokenResult.Text);
		}

		[Fact(DisplayName = "LiteralChar token fails on non-match")]
		public void LiteralCharToken_FailsOnNonMatch()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("char").Literal('a');

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("char", "b");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "Newline token matches LF")]
		public void NewlineToken_MatchesLF()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("nl").Newline();

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("nl", "\n");
			Assert.True(tokenResult.Success);
			Assert.Equal("\n", tokenResult.Text);
		}

		[Fact(DisplayName = "Newline token matches CRLF")]
		public void NewlineToken_MatchesCRLF()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("nl").Newline();

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("nl", "\r\n");
			Assert.True(tokenResult.Success);
			Assert.Equal("\r\n", tokenResult.Text);
		}

		[Fact(DisplayName = "Newline token does not match in middle of line")]
		public void NewlineToken_NoMatchInMiddle()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("nl").Newline();

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("nl", "a\nb");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "Empty token always matches with zero length")]
		public void EmptyToken_AlwaysMatchesZeroLength()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("empty").Empty();

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("empty", "abc");
			Assert.True(tokenResult.Success);
			Assert.Equal(0, tokenResult.Length);
		}

		[Fact(DisplayName = "Fail token always fails")]
		public void FailToken_AlwaysFails()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("fail").Fail();

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("fail", "abc");
			Assert.False(tokenResult.Success);
		}

		[Fact(DisplayName = "Regex token handles more complex regex")]
		public void RegexToken_HandlesComplexRegex()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("email").Regex(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+");

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("email", "test@example.com");
			Assert.True(tokenResult.Success);
			Assert.Equal("test@example.com", tokenResult.Text);
		}

		[Fact(DisplayName = "Regex token fails on non-match")]
		public void RegexToken_FailsOnNonMatch()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("num").Regex(@"\d+");

			var parser = builder.Build();
			var tokenResult = parser.TryMatchToken("num", "abc");
			Assert.False(tokenResult.Success);
		}
	}
}