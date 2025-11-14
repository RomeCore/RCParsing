using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests.Tokens
{
	/// <summary>
	/// The token combinators tests. They can be used instead of rules for maximum performance.
	/// </summary>
	public class CombinatorTokensTests
	{
		[Fact]
		public void Token_String_Simple()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("string")
				.Between(
					b => b.Literal('"'),
					b => b.CaptureText(b => b.ZeroOrMoreChars(c => c != '"')),
					b => b.Literal('"'));

			var parser = builder.Build();
			var result = parser.MatchToken("string", "\"hello\"");
			Assert.True(result.Success);
			Assert.Equal("hello", result.IntermediateValue);
		}

		[Fact]
		public void Token_Number_Integer()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer);

			var parser = builder.Build();
			var result = parser.MatchToken("number", "42");
			Assert.True(result.Success);
			Assert.Equal(42.0, result.IntermediateValue);
		}

		[Fact]
		public void Token_Boolean_True()
		{
			var builder = new ParserBuilder();
			builder.CreateToken("boolean")
				.Map<string>(b => b.LiteralChoice("true", "false"), m => m == "true");

			var parser = builder.Build();
			var result = parser.MatchToken("boolean", "true");
			Assert.True(result.Success);
			Assert.True((bool)result.IntermediateValue!);
		}

		[Fact]
		public void Token_Value_String()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("string")
				.Between(
					b => b.Literal('"'),
					b => b.TextUntil('"'),
					b => b.Literal('"'));

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer);

			builder.CreateToken("value")
				.SkipWhitespaces(b => b.Choice(
					c => c.Token("string"),
					c => c.Token("number")
				));

			var parser = builder.Build();

			var result = parser.MatchToken("value", " \"hello\" ");
			Assert.True(result.Success);
			Assert.Equal("hello", result.IntermediateValue);

			result = parser.MatchToken("value", " 999 ");
			Assert.True(result.Success);
			Assert.Equal((double)999, result.IntermediateValue);

			Assert.True(parser.MatchesToken("value", " \"hello\" ", out var matchedLength));
			Assert.Equal(8, matchedLength);

			Assert.True(parser.MatchesToken("value", " 999 ", out matchedLength));
			Assert.Equal(4, matchedLength);
		}

		[Fact]
		public void Token_Array_Simple()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("string")
				.Between(
					b => b.Literal('"'),
					b => b.TextUntil('"'),
					b => b.Literal('"'));

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer);

			builder.CreateToken("value").SkipWhitespaces(b => b.Choice(
				b => b.Token("string"),
				b => b.Token("number")
				));

			// value_list
			builder.CreateToken("value_list")
				.ZeroOrMoreSeparated(
					b => b.Token("value"),
					b => b.SkipWhitespaces(b => b.Literal(',')))
				.Pass(v => v.ToArray());

			// array
			builder.CreateToken("array")
				.Between(
					b => b.SkipWhitespaces(b => b.Literal('[')),
					b => b.Token("value_list"),
					b => b.SkipWhitespaces(b => b.Literal(']')));

			var parser = builder.Build();
			var result = parser.MatchToken("array", "[1, 2, 3]");
			Assert.True(result.Success);
			var array = (object[])result.IntermediateValue!;
			Assert.Equal(3, array.Length);
			Assert.Equal(1.0, array[0]);
		}

		[Fact]
		public void CombinatorJsonParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings.UseFirstCharacterMatch();

			builder.CreateToken("string")
				.Between(
					b => b.Literal('"'),
					b => b.CaptureText(b => b.ZeroOrMoreChars(c => c != '"')),
					b => b.Literal('"'));

			builder.CreateToken("number")
				.Number<double>();

			builder.CreateToken("boolean")
				.Map<string>(b => b.LiteralChoice("true", "false"), m => m == "true");

			builder.CreateToken("null")
				.Return(b => b.Literal("null"), null);

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
					return KeyValuePair.Create((string)v[0]!, v[2]);
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
				"created": "2023-01-01T00:00:00",
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

			var invalidJson =
			"""
			{
				"id": 1,
				"name": "Sample Data",
				"created": "2023-01-01T00:00:00",
				"tags": ["tag1", "tag2", "tag3"],,
				"isActive": true,
				"nested": {
					"value": 123.456,
					"description": "Nested description"
				}
			}
			""";

			var error = parser.TryMatchToken("value", invalidJson).Context.CreateErrorGroups().Last!;
			Assert.Equal(35, error.Column);
			Assert.Equal(5, error.Line);

			Assert.True(parser.MatchesToken("value", "[90, 60, true, null]"));
		}

		[Fact]
		public void IfElse_General()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("ctx_int_morethan")
				.If<int>(
					i => i > 5,
					i => i.Identifier(),
					e => e.AllText()
				);
			
			builder.CreateToken("ctx_int_morethan_noelse")
				.If<int>(
					i => i > 5,
					i => i.Identifier()
				);

			var parser = builder.Build();

			var match = parser.TryMatchToken("ctx_int_morethan", "id 10", parameter: 10);
			Assert.True(match.Success);
			Assert.Equal(2, match.Length);

			match = parser.TryMatchToken("ctx_int_morethan", "id 10", parameter: 5);
			Assert.True(match.Success);
			Assert.Equal(5, match.Length);

			match = parser.TryMatchToken("ctx_int_morethan", "id 10", parameter: null);
			Assert.True(match.Success);
			Assert.Equal(5, match.Length);

			match = parser.TryMatchToken("ctx_int_morethan_noelse", "id 10", parameter: 10);
			Assert.True(match.Success);
			Assert.Equal(2, match.Length);

			match = parser.TryMatchToken("ctx_int_morethan_noelse", "id 10", parameter: 5);
			Assert.False(match.Success);

			match = parser.TryMatchToken("ctx_int_morethan_noelse", "id 10", parameter: null);
			Assert.False(match.Success);
		}

		[Fact]
		public void Optional_Fallback()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("optional_no_fallback")
				.Optional(
					b => b.Number<int>()
				);

			builder.CreateToken("optional_with_fallback")
				.Optional(
					b => b.Number<int>(),
					fallbackValue: +397
				);

			var parser = builder.Build();

			var match = parser.TryMatchToken("optional_no_fallback", "10");
			Assert.True(match.Success);
			Assert.Equal(10, match.IntermediateValue);

			match = parser.TryMatchToken("optional_no_fallback", "abc");
			Assert.True(match.Success);
			Assert.Null(match.IntermediateValue);

			match = parser.TryMatchToken("optional_with_fallback", "-397");
			Assert.True(match.Success);
			Assert.Equal(-397, match.IntermediateValue);

			match = parser.TryMatchToken("optional_with_fallback", "abc");
			Assert.True(match.Success);
			Assert.Equal(+397, match.IntermediateValue);
		}

		[Fact]
		public void TextUntil_General()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("simple")
				.TextUntil(b => b.Literal(';'));

			builder.CreateToken("faileof")
				.TextUntil(b => b.Literal(';'), failOnEof: true);

			builder.CreateToken("consume")
				.TextUntil(b => b.Literal(';'), consumeStop: true);

			builder.CreateToken("nonempty")
				.TextUntil(b => b.Literal(';'), allowEmpty: false);

			var parser = builder.Build();

			var match = parser.TryMatchToken("simple", "abc;a");
			Assert.True(match.Success);
			Assert.Equal(3, match.Length);
			Assert.Equal("abc", match.IntermediateValue);

			match = parser.TryMatchToken("simple", "abc");
			Assert.True(match.Success);
			Assert.Equal(3, match.Length);
			Assert.Equal("abc", match.IntermediateValue);


			match = parser.TryMatchToken("faileof", "abc;a");
			Assert.True(match.Success);
			Assert.Equal(3, match.Length);
			Assert.Equal("abc", match.IntermediateValue);

			match = parser.TryMatchToken("faileof", "abc");
			Assert.False(match.Success);


			match = parser.TryMatchToken("consume", "abc;a");
			Assert.True(match.Success);
			Assert.Equal(4, match.Length);
			Assert.Equal("abc;", match.IntermediateValue);

			match = parser.TryMatchToken("consume", "abc");
			Assert.True(match.Success);
			Assert.Equal(3, match.Length);
			Assert.Equal("abc", match.IntermediateValue);


			match = parser.TryMatchToken("nonempty", "abc;a");
			Assert.True(match.Success);
			Assert.Equal(3, match.Length);
			Assert.Equal("abc", match.IntermediateValue);

			match = parser.TryMatchToken("nonempty", "abc");
			Assert.True(match.Success);
			Assert.Equal(3, match.Length);
			Assert.Equal("abc", match.IntermediateValue);

			match = parser.TryMatchToken("nonempty", ";a");
			Assert.False(match.Success);

			match = parser.TryMatchToken("nonempty", "");
			Assert.False(match.Success);
		}

		[Fact]
		public void TextUntil_Combinations()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("consume_faileof")
				.TextUntil(b => b.Literal(';'), consumeStop: true, failOnEof: true);

			builder.CreateToken("consume_empty")
				.TextUntil(b => b.Literal(';'), consumeStop: true, allowEmpty: true);

			builder.CreateToken("nonempty_faileof")
				.TextUntil(b => b.Literal(';'), allowEmpty: false, failOnEof: true);

			builder.CreateToken("all_flags")
				.TextUntil(b => b.Literal(';'), allowEmpty: false, consumeStop: true, failOnEof: true);

			builder.CreateToken("complex_stop")
				.TextUntil(b => b.Choice(
					b2 => b2.Literal("END"),
					b2 => b2.Newline()
				), allowEmpty: false, consumeStop: true);

			builder.CreateToken("multi_char_stop")
				.TextUntil(b => b.Literal("stop"), consumeStop: true);

			builder.CreateToken("nonempty_noconsume")
				.TextUntil(b => b.Literal(';'), allowEmpty: false, consumeStop: false);

			var parser = builder.Build();

			var match = parser.TryMatchToken("consume_faileof", "text;rest");
			Assert.True(match.Success);
			Assert.Equal(5, match.Length);
			Assert.Equal("text;", match.IntermediateValue);

			match = parser.TryMatchToken("consume_faileof", "text");
			Assert.False(match.Success);

			match = parser.TryMatchToken("consume_empty", ";rest");
			Assert.True(match.Success);
			Assert.Equal(1, match.Length);
			Assert.Equal(";", match.IntermediateValue);

			match = parser.TryMatchToken("consume_empty", "text;rest");
			Assert.True(match.Success);
			Assert.Equal(5, match.Length);
			Assert.Equal("text;", match.IntermediateValue);

			match = parser.TryMatchToken("nonempty_faileof", "text;rest");
			Assert.True(match.Success);
			Assert.Equal(4, match.Length);
			Assert.Equal("text", match.IntermediateValue);

			match = parser.TryMatchToken("nonempty_faileof", ";rest");
			Assert.False(match.Success);

			match = parser.TryMatchToken("nonempty_faileof", "text");
			Assert.False(match.Success);

			match = parser.TryMatchToken("all_flags", "text;rest");
			Assert.True(match.Success);
			Assert.Equal(5, match.Length);
			Assert.Equal("text;", match.IntermediateValue);

			match = parser.TryMatchToken("all_flags", ";rest");
			Assert.False(match.Success);

			match = parser.TryMatchToken("all_flags", "text");
			Assert.False(match.Success);

			match = parser.TryMatchToken("complex_stop", "some textENDmore");
			Assert.True(match.Success);
			Assert.Equal(12, match.Length);
			Assert.Equal("some textEND", match.IntermediateValue);

			match = parser.TryMatchToken("complex_stop", "some text\nmore");
			Assert.True(match.Success);
			Assert.Equal(10, match.Length);
			Assert.Equal("some text\n", match.IntermediateValue);

			match = parser.TryMatchToken("multi_char_stop", "hello stop world");
			Assert.True(match.Success);
			Assert.Equal(10, match.Length); // "hello stop"
			Assert.Equal("hello stop", match.IntermediateValue);

			match = parser.TryMatchToken("nonempty_noconsume", "text;rest");
			Assert.True(match.Success);
			Assert.Equal(4, match.Length);
			Assert.Equal("text", match.IntermediateValue);

			match = parser.TryMatchToken("nonempty_noconsume", ";rest");
			Assert.False(match.Success);

			match = parser.TryMatchToken("consume_empty", "");
			Assert.True(match.Success);
			Assert.Equal(0, match.Length);
			Assert.Equal("", match.IntermediateValue);

			match = parser.TryMatchToken("nonempty_noconsume", "text");
			Assert.True(match.Success);
			Assert.Equal(4, match.Length);
			Assert.Equal("text", match.IntermediateValue);
		}
	}
}