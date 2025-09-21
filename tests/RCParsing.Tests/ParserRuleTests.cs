using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RCParsing;
using RCParsing.Building;

namespace RCParsing.Tests
{
	/// <summary>
	/// The first set of tests here, may contain lots of 'Regex' tokens.
	/// </summary>
	public class ParserRuleTests
	{
		[Fact]
		public void SimpleBuilding()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("identifier")
				.Regex("[a-zA-Z_][a-zA-Z0-9_]*");

			builder.CreateRule("expression")
				.Token("identifier");

			var parser = builder.Build();
		}
		
		[Fact]
		public void SimpleExpressionParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(r => r.Token("whitespace"));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

			builder.CreateToken("identifier")
				.Regex("[a-zA-Z_][a-zA-Z0-9_]*");

			builder.CreateRule("expression")
				.Token("identifier")
				.Choice(
					c => c.Literal("+"),
					c => c.Literal("-"))
				.Token("identifier");

			var parser = builder.Build();

			var testString = "a + b";
			var parsed = parser.ParseRule("expression", testString);

			var a = parsed.Children[0].Text;
			Assert.Equal("a", a);
			var op = parsed.Children[1].Text;
			Assert.Equal("+", op);
			var b = parsed.Children[2].Text;
			Assert.Equal("b", b);
		}

		[Fact]
		public void CommaSeparatedIdentifiers()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(r => r.Token("whitespace"));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

			builder.CreateToken("identifier")
				.Regex(@"[a-zA-Z_][a-zA-Z0-9_]*");

			builder.CreateRule("id_list")
				.Token("identifier")
				.ZeroOrMore(z => z.Literal(",").Token("identifier"));

			var parser = builder.Build();
			var input = "x, y, z";
			var result = parser.ParseRule("id_list", input);

			Assert.Equal("x", result.Children[0].Text);
			Assert.Equal("y", result.Children[1].Children[0].Children[1].Text);
			Assert.Equal("z", result.Children[1].Children[1].Children[1].Text);
		}

		[Fact]
		public void EscapedStringList()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(r => r.Token("whitespace"));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

			builder.CreateToken("string")
				.Regex(@"""(?:\\""|[^""])*""")
				.Transform(v => v.GetIntermediateValue<Match>().Value[1..^1].Replace("\\\"", "\""));

			builder.CreateRule("string_list")
				.Token("string")
				.ZeroOrMore(z => z.Literal(",").Token("string"));

			var parser = builder.Build();
			var input = @"""hello"", ""world\n"", ""\""escaped\""""";
			var result = parser.ParseRule("string_list", input);

			Assert.Equal("hello", result.Children[0].Value);
			Assert.Equal("world\\n", result.Children[1].Children[0].Children[1].Value);
			Assert.Equal("\"escaped\"", result.Children[1].Children[1].Children[1].Value);
		}

		[Fact]
		public void SimpleMathExpressions()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(r => r.Token("whitespace"));

			builder.CreateToken("whitespace")
				.Regex(@"\s+");

			builder.CreateToken("number")
				.Regex(@"\d+").Transform(v => int.Parse(v.Text));

			builder.CreateRule("expression")
				.Token("number")
				.ZeroOrMore(z => z
					.Choice(
						c => c.Literal("+"),
						c => c.Literal("-"))
					.Token("number"));

			var parser = builder.Build();
			var input = "10 + 20 - 5";
			var result = parser.ParseRule("expression", input);

			var joined = result.GetJoinedChildren(maxDepth: 999).ToList();

			Assert.Equal(5, joined.Count);
			Assert.Equal("+", joined[1].Text);
			Assert.Equal("20", joined[2].Text);
		}

		[Fact]
		public void SimpleJSONParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings
				.Skip(r => r.Rule("skip"), ParserSkippingStrategy.SkipBeforeParsingGreedy);

			// Parser will always try to skip the skip-rules until they are not capturing anything
			// So we don't need to create another .OneOrMore(c => c.Choice(...))
			builder.CreateRule("skip")
				.Choice(
					b => b.Whitespaces(),
					b => b.Literal("//").TextUntil('\n', '\r'))
				.ConfigureForSkip();

			builder.CreateToken("string")
				.Literal("\"")
				.EscapedTextPrefix(prefix: '\\', '\\', '\"') // This sub-token automatically escapes the source string and puts it into intermediate value
				.Literal("\"")
				.Pass(1); // Pass the EscapedTextPrefix's intermediate value up

			builder.CreateToken("number")
				.Number<double>(TokenPatterns.NumberFlags.StrictScientific);

			builder.CreateToken("boolean")
				.LiteralChoice(["true", "false"]).Transform(v => v.Text == "true"); // LiteralChoice uses Trie

			builder.CreateToken("null")
				.Literal("null").Transform(v => null);

			builder.CreateRule("value")
				.Choice(
					c => c.Token("string"),
					c => c.Token("number"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Rule("array"),
					c => c.Rule("object"));

			builder.CreateRule("array")
				.Literal("[") // The string with one character automatically converts to LiteralCharTokenPattern
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","))
					.TransformLast(v => v.SelectArray())
				.Literal("]")
				.TransformSelect(1);

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value")
				.Transform<string, Ignored, object>((k, _, v) => KeyValuePair.Create(k, v));

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","))
					.TransformLast(v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary())
				.Literal("}")
				.TransformSelect(1);

			builder.CreateMainRule()
				.Rule("value")
				.EOF() // Sure that we captured all the input
				.TransformSelect(0);

			var jsonParser = builder.Build(); // <-- Here is the horryfying deduplication algorithm that builds the parser!

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

			var result = jsonParser.Parse<Dictionary<string, object>>(json);
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
				"created": "2023-01-01T00:00:00", // This is a comment
				"tags": ["tag1", "tag2", "tag3"],, // Extra comma here
				"isActive": true,
				"nested": {
					"value": 123.456,
					"description": "Nested description"
				}
			}
			""";

			var exception = Assert.Throws<ParsingException>(() => jsonParser.Parse(invalidJson));
			var lastGroup = exception.Groups.Last!;
			Assert.Equal(5, lastGroup.Line);
			Assert.Equal(35, lastGroup.Column);
			Assert.Equal("string", lastGroup.Expected.Tokens[0].Alias);
		}

		[Fact]
		public void AdvancedExpressionParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(r => r.Whitespaces());

			// Values

			builder.CreateToken("identifier")
				.Identifier();

			builder.CreateToken("methodName")
				.Identifier();

			builder.CreateToken("fieldName")
				.Identifier();

			builder.CreateToken("string")
				.Literal('"')
				.EscapedTextDoubleChars('"')
				.Literal('"')
				.Pass(1);

			builder.CreateToken("number")
				.Number<double>(TokenPatterns.NumberFlags.StrictScientific);

			builder.CreateToken("boolean")
				.LiteralChoice("true", "false").Transform(v => v.Text == "true");

			builder.CreateToken("null")
				.Literal("null").Transform(v => null);

			// Operators

			builder.CreateRule("primary")
				.Choice(
					c => c.Token("number"),
					c => c.Token("identifier"),
					c => c.Token("string"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Literal('(').Rule("expression").Literal(')'));

			builder.CreateRule("postfix_member")
				.Rule("primary")
				.ZeroOrMore(b => b.Choice(
					b => b.Literal('.') // Method call
						  .Token("methodName")
						  .Literal('(')
						  .ZeroOrMoreSeparated(a => a.Rule("expression"), s => s.Literal(','))
						  .Literal(')'),

					b => b.Literal('.') // Field access
						  .Token("fieldName"),

					b => b.Literal('[') // Index access
						  .Rule("expression")
						  .Literal(']')
					));

			builder.CreateRule("postfix_operator")
				.Rule("postfix_member")
				.Optional(b => b.LiteralChoice("++", "--"));

			builder.CreateRule("prefix")
				.ZeroOrMore(b => b.LiteralChoice("++", "--", "+", "-", "!", "~"))
				.Rule("postfix_operator");

			builder.CreateRule("multiplicative_operator")
				.OneOrMoreSeparated(b => b.Rule("prefix"), o => o.LiteralChoice("*", "/"), includeSeparatorsInResult: true);

			builder.CreateRule("additive_operator")
				.OneOrMoreSeparated(b => b.Rule("multiplicative_operator"), o => o.LiteralChoice("+", "-"), includeSeparatorsInResult: true);

			builder.CreateRule("expression")
				.Rule("additive_operator");

			builder.CreateMainRule()
				.Rule("expression")
				.EOF();

			var parser = builder.Build();

			var result = parser.Parse(
				@"!1 + abc.def(1 + ""string"""""", ++test.index[1 + 5]) * (3 - ~4 * a.b[0]) / 5");
		}

		[Fact]
		public void ParserParameterPassage()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("id")
				.Identifier()
				.Transform(v =>
				{
					return v.GetParsingParameter<string>() + v.Text;
				});

			var parser = builder.Build();

			var result = parser.ParseRule("id", "test", "prefix_");
			Assert.Equal("prefix_test", result.Value);
		}
	}
}