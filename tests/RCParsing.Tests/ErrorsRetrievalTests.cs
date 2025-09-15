using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Tests
{
	/// <summary>
	/// The tests that ensures error API and grouping is working correctly.
	/// </summary>
	public class ErrorsRetrievalTests
	{
		[Fact]
		public void NoGroupsWhenIgnoring()
		{
			var builder = new ParserBuilder();
			builder.Settings.IgnoreErrors();

			builder.CreateMainRule()
				.Identifier();

			var parser = builder.Build();

			var exception = Assert.Throws<ParsingException>(() => parser.Parse("1a"));
			Assert.Empty(exception.Groups);
			Assert.Null(exception.Groups.Last);
		}

		[Fact]
		public void SimpleArglistError()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateMainRule()
				.Identifier()
				.Literal("(")
				.List(b => b.Identifier())
				.Literal(")");

			var parser = builder.Build();

			// Should fail after trailing comma, on ')'
			var exception = Assert.Throws<ParsingException>(() => parser.Parse("id1 (id2, )"));
			var lastGroup = exception.Groups.Last!;
			Assert.Equal(11, lastGroup.Column);
			Assert.Contains("identifier", lastGroup.Expected.Select(e => e.ToString()));

			// Should fail on first character
			exception = Assert.Throws<ParsingException>(() => parser.Parse("1id (id2)"));
			lastGroup = exception.Groups.Last!;
			Assert.Equal(1, lastGroup.Column);
			Assert.Contains("identifier", lastGroup.Expected.Select(e => e.ToString()));

			// Should fail on '('
			exception = Assert.Throws<ParsingException>(() => parser.Parse("id {id2}"));
			lastGroup = exception.Groups.Last!;
			Assert.Equal(4, lastGroup.Column);
			Assert.Contains("literal '('", lastGroup.Expected.Select(e => e.ToString()));
		}

		[Fact]
		public void NestedErrorInComplexStructure()
		{
			// We use simplified JSON here
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","))
				.Literal("}");

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value");

			builder.CreateRule("value")
				.Choice(
					c => c.Token("number"),
					c => c.Token("string"),
					c => c.Rule("array"),
					c => c.Rule("object")
				);

			builder.CreateRule("array")
				.Literal("[")
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","))
				.Literal("]");

			builder.CreateToken("string").Literal("\"").EscapedTextPrefix('\\', '\"').Literal("\"").Pass(1);
			builder.CreateToken("number").Number<double>();

			builder.CreateMainRule().Rule("object").EOF();

			var parser = builder.Build();

			var invalidInput = """
			{
				"key1": [1 2],
				"key2": "value"
			}
			""";

			// Should fail in the array, after the first number
			var exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));
			var expected = exception.Groups.Last!.Expected.ToString();
			Assert.Contains("literal ','", expected);
			Assert.Contains("literal ']'", expected);
			Assert.Equal(2, exception.Groups.Last!.Line);
			Assert.Equal(13, exception.Groups.Last!.Column);

			invalidInput =
			"""
			{
				"key1": [1, 2],
				"key2": value
			}
			""";

			exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));
			expected = exception.Groups.Last!.Expected.ToString();
			Assert.Contains("number", expected);
			Assert.Contains("string", expected);
			Assert.Contains("array", expected);
			Assert.Contains("object", expected);
			Assert.Contains("value", expected);
			Assert.Equal(3, exception.Groups.Last!.Line);
			Assert.Equal(10, exception.Groups.Last!.Column);
		}

		[Fact]
		public void UnexpectedBarrierTokenizer()
		{
			// バリアトークンを使用したインデント解析のエラー回復テスト
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();
			builder.BarrierTokenizers.AddIndent(4, "INDENT", "DEDENT");

			builder.CreateRule("block")
				.Token("INDENT")
				.OneOrMore(b => b.Identifier())
				.Token("DEDENT");

			builder.CreateMainRule()
				.Identifier()
				.Literal(":")
				.Rule("block")
				.EOF();

			var parser = builder.Build();

			// 不正なインデント（深すぎる）
			var invalidInput =
			"""
			test:
					too_deep_indent
			""";

			// 
			var exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));
			var barrierError = exception.Groups.Last!.UnexpectedBarrier;
			Assert.NotNull(barrierError);
			Assert.Equal("INDENT", barrierError.Alias);
			Assert.Equal(2, exception.Groups.Last!.Line);
		}

		[Fact]
		public void CustomTokenErrorWithParameter()
		{
			// カスタムトークンでのパラメータ付きエラーテスト
			var builder = new ParserBuilder();

			builder.CreateToken("custom_delimiter")
				.Custom((self, input, start, end, parameter) =>
				{
					var expectedChar = (char)parameter!;
					if (start >= end || input[start] != expectedChar)
						return ParsedElement.Fail;

					return new ParsedElement(self.Id, start, 1);
				});

			builder.CreateMainRule()
				.Identifier()
				.Token("custom_delimiter") // セミコロンを期待
				.EOF();

			var parser = builder.Build();

			// Помогите перевести эти ебучие комментарии сверху
			var exception = Assert.Throws<ParsingException>(() => parser.Parse("test,", parameter: ';'));
			var lastGroup = exception.Groups.Last!;
			Assert.Contains("custom_delimiter", lastGroup.Expected.ToString());
			Assert.Equal(5, lastGroup.Column);
		}

		// Ладно, пора слезать с саке...
		// Переходим обратно на English.
		// Let's fuck these tests!!!

		[Fact]
		public void AmbigousChoices()
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespaces(ParserSkippingStrategy.TryParseThenSkip);

			builder.CreateMainRule()
				.Choice(
					b => b
						.LiteralChoice("if", "for", "while")
						.Whitespaces()
						.Identifier(),
					b => b
						.Identifier()
						.Literal("=")
						.Number<int>()
				);

			var parser = builder.Build();

			// It will fail on '=', then fail on 'a', making two groups of errors
			var exception = Assert.Throws<ParsingException>(() => parser.Parse("if = a"));
			var first = exception.Groups.First(g => g.Column == 4);
			var second = exception.Groups.First(g => g.Column == 6);
			Assert.Contains("identifier", first.Expected.ToString());
			Assert.Contains("number", second.Expected.ToString());

			// It requires whitespaces after 'if' and fails on 'a', then fail on EOF in the second choice
			exception = Assert.Throws<ParsingException>(() => parser.Parse("ifa"));
			first = exception.Groups.First(g => g.Column == 3);
			second = exception.Groups.First(g => g.Column == 4);
			Assert.Contains("whitespaces", first.Expected.ToString());
			Assert.Contains("=", second.Expected.ToString());
		}
	}
}