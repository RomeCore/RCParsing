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
				.Regex(@"""(?:\\""|[^""])*""", match =>
					(match.Result.intermediateValue as Match)!.Value[1..^1].Replace("\\\"", "\""));

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
				.Regex(@"\d+", match => int.Parse(match.Text));

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
				.Regex(@"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?", v => double.Parse(v.Text.Replace('.', ','))); // Hehe, here you can use Regex

			builder.CreateToken("boolean")
				.LiteralChoice(["true", "false"], v => v.Text == "true"); // LiteralChoice uses Trie

			builder.CreateToken("null")
				.Literal("null", _ => null);

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
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","),
					allowTrailingSeparator: true, includeSeparatorsInResult: false)
					.TransformLast(v => v.SelectArray())
				.Literal("]")
				.TransformSelect(1);

			builder.CreateRule("object")
				.Literal("{") // And chained calling with builder converts the rule into SequenceParserRule by default
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","),
					allowTrailingSeparator: true, includeSeparatorsInResult: false)
					.TransformLast(v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary(k => k.Key, v => v.Value))
				.Literal("}")
				.TransformSelect(1);

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value")
				.Transform<string, Ignored, object>((k, _, v) => KeyValuePair.Create(k, v));

			builder.CreateMainRule("content")
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
			var invalidJson = "{ \"name\": \"Test\", \"age\": }";

			var result = jsonParser.Parse<Dictionary<string, object>>(json);
			Assert.Throws<ParsingException>(() => jsonParser.ParseRule("content", invalidJson));
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
				.Pass(v => v[1])
				.Transform(v => v.IntermediateValue);

			builder.CreateToken("number")
				.Regex("-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?",
					v => double.Parse(v.Text, CultureInfo.InvariantCulture));

			builder.CreateToken("boolean")
				.LiteralChoice(["true", "false"],
					v => v.Text == "true");

			builder.CreateToken("null")
				.Literal("null",
					_ => null);

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

			builder.CreateMainRule("content")
				.Rule("expression")
				.EOF();

			var parser = builder.Build();

			var result = parser.Parse(
				@"!1 + abc.def(1 + ""string"""""", ++test.index[1 + 5]) * (3 - ~4 * a.b[0]) / 5");
		}

		[Fact]
		public void SelfReferenceRule()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("Loop").Rule("Loop");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void RuleCircularReference()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("A")
				.Rule("B");

			builder.CreateRule("B")
				.Rule("A");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void TokenCircularReference()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Token("integer");

			builder.CreateToken("integer")
				.Token("number");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void IndirectCircularReferenceDeep()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("A").Rule("B");
			builder.CreateRule("B").Rule("C");
			builder.CreateRule("C").Rule("A");

			Assert.Throws<ParserBuildingException>(() => builder.Build());
		}

		[Fact]
		public void SameReferenceRule()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Regex(@"\d+");

			builder.CreateToken("int").Token("number");
			builder.CreateToken("integer").Token("int");
			builder.CreateToken("double").Token("number");

			var parser = builder.Build();

			Assert.Single(parser.TokenPatterns);
			Assert.True(parser.GetTokenPattern("number").Id == parser.GetTokenPattern("integer").Id);
			Assert.True(parser.GetTokenPattern("number").Id == parser.GetTokenPattern("int").Id);
			Assert.True(parser.GetTokenPattern("double").Id == parser.GetTokenPattern("int").Id);
		}

		[Fact]
		public void AliasesOrderRule()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Regex(@"\d+");

			builder.CreateToken("int").Token("number");
			builder.CreateToken("integer").Token("int");
			builder.CreateToken("double").Token("number");

			var parser = builder.Build();

			Assert.Equal(["number", "int", "integer", "double"], parser.GetTokenPattern("number").Aliases);
		}

		[Fact]
		public void RuleDeduplication()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("rule1")
				.Literal("abc");

			builder.CreateRule("rule2")
				.Literal("abc");

			var parser = builder.Build();

			Assert.True(parser.GetRule("rule1").Id == parser.GetRule("rule2").Id);
		}
	}
}