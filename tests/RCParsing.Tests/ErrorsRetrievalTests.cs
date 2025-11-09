using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.ParserRules;

namespace RCParsing.Tests
{
	/// <summary>
	/// The tests that ensures error API and grouping is working correctly.
	/// </summary>
	public class ErrorsRetrievalTests
	{
		private static void FillWithJSON(ParserBuilder builder)
		{
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
		}

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
			Assert.Equal("Unknown error.", exception.Message);
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
			FillWithJSON(builder);
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

			builder.Settings.SkipWhitespaces();

			builder.CreateMainRule()
				.Choice(
					b => b
						.KeywordChoice("if", "for", "while")
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
			exception = Assert.Throws<ParsingException>(() => parser.Parse("if 1 a"));
			var group = exception.Groups.First(g => g.Column == 4);
			Assert.Contains("identifier", group.Expected.ToString());
			Assert.Contains("=", group.Expected.ToString());
		}

		[Fact]
		public void VariousTabSizes()
		{
			var builder = new ParserBuilder();
			builder.Settings.SetTabSize(4);
			FillWithJSON(builder);
			var parser = builder.Build();

			var invalidInput = """
			{
				"key1": [1 2],
				"key2": "value"
			}
			""";
			var exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));
			Assert.Equal(16, exception.Groups.Last!.VisualColumn);

			// NEXT

			builder = new ParserBuilder();
			builder.Settings.SetTabSize(2);
			FillWithJSON(builder);
			parser = builder.Build();

			invalidInput = """
			{
				"key1": [1 2],
				"key2": "value"
			}
			""";
			exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));
			Assert.Equal(14, exception.Groups.Last!.VisualColumn);

			// NEXT

			builder = new ParserBuilder();
			builder.Settings.SetTabSize(2);
			FillWithJSON(builder);
			parser = builder.Build();

			// Added space in second line
			invalidInput = """
			{
			 	"key1": [1 2],
				"key2": "value"
			}
			""";
			exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));
			Assert.Equal(14, exception.Groups.Last!.VisualColumn);

			// NEXT

			builder = new ParserBuilder();
			builder.Settings.SetTabSize(2);
			FillWithJSON(builder);
			parser = builder.Build();

			// Added another space in second line
			invalidInput = """
			{
			  	"key1": [1 2],
				"key2": "value"
			}
			""";
			exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));
			Assert.Equal(16, exception.Groups.Last!.VisualColumn);
		}

		[Fact]
		public void StackTraceWriting()
		{
			var builder = new ParserBuilder();
			builder.Settings.WriteStackTrace();
			FillWithJSON(builder);
			var parser = builder.Build();

			var invalidInput = """
			{
				"key1": [1 2],
				"key2": "value"
			}
			""";

			// Should fail in the array, after the first number
			var exception = Assert.Throws<ParsingException>(() => parser.Parse(invalidInput));

			// It expects the ',' first, then ']' (that will not be checked here)
			var stackTrace = exception.Groups.Last!.Expected[0].StackTrace!;

			var frame0 = stackTrace[0]; // Token rule: literal ','
			var frame1 = stackTrace[1]; // The list of values
			var frame2 = stackTrace[2]; // 'array'
			var frame3 = stackTrace[3]; // 'value'
			var frame4 = stackTrace[4]; // 'pair'
			var frame5 = stackTrace[5]; // The list of pairs
			var frame6 = stackTrace[6]; // 'object'
			var frame7 = stackTrace[7]; // Our main rule!

			Assert.True(frame0.Rule is TokenParserRule);
			Assert.True(frame1.Rule is SeparatedRepeatParserRule);
			Assert.True(frame2.Rule is SequenceParserRule && frame2.Rule.Alias == "array");
			Assert.True(frame3.Rule is ChoiceParserRule && frame3.Rule.Alias == "value");
			Assert.True(frame4.Rule is SequenceParserRule && frame4.Rule.Alias == "pair");
			Assert.True(frame5.Rule is SeparatedRepeatParserRule);
			Assert.True(frame6.Rule is SequenceParserRule && frame6.Rule.Alias == "object");
			Assert.True(frame7.Rule is SequenceParserRule && frame7.RuleId == parser.MainRuleId);
		}
	}
}