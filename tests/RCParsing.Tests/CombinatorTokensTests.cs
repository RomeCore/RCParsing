using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests
{
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
		}
	}
}