using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Tests
{
	/// <summary>
	/// Tests based on samples in README.md
	/// </summary>
	public class ReadmeSamplesTests
	{
		[Fact]
		public void AplusB()
		{
			// First, you need to create a builder
			var builder = new ParserBuilder();

			// Enable and configure the auto-skip (you can replace `Whitespaces` with any parser rule)
			builder.Settings
				.Skip(b => b.Whitespaces().ConfigureForSkip());

			// Create the number token from regular expression that transforms to double
			builder.CreateToken("number")
				.Number<double>();

			// Create a main sequential expression rule
			builder.CreateMainRule("expression")
				.Token("number")
				.LiteralChoice("+", "-")
				.Token("number")
				.Transform(v => {
					var value1 = v.GetValue<double>(0);
					var op = v.Children[1].Text;
					var value2 = v.GetValue<double>(2);
					return op == "+" ? value1 + value2 : value1 - value2;
				});

			// Build the parser
			var parser = builder.Build();

			// Parse a string using 'expression' rule and get the raw AST (value currently not calculated)
			var parsedRule = parser.Parse("10 + 15");

			// We can now get the value from our 'Transform' functions (value calculates now)
			var transformedValue = parsedRule.GetValue<double>();

			Assert.Equal(25, transformedValue);
		}

		[Fact]
		public void JSON()
		{
			var builder = new ParserBuilder();

			// Configure whitespace and comment skip-rule
			builder.Settings
				.Skip(r => r.Rule("skip"), ParserSkippingStrategy.SkipBeforeParsingGreedy);

			builder.CreateRule("skip")
				.Choice(
					b => b.Whitespaces(),
					b => b.Literal("//").TextUntil('\n', '\r'))
				.ConfigureForSkip();

			builder.CreateToken("string")
				.Literal('"')
				.EscapedTextPrefix(prefix: '\\', '\\', '\"') // This sub-token automatically escapes the source string and puts it into intermediate value
				.Literal('"')
				.Pass(index: 1); // Pass the EscapedTextPrefix's intermediate value up (it will be used as token's result value)

			builder.CreateToken("number")
				.Number<double>();

			builder.CreateToken("boolean")
				.LiteralChoice("true", "false").Transform(v => v.Text == "true");

			builder.CreateToken("null")
				.Literal("null").Transform(v => null);

			builder.CreateRule("value")
				.Choice(
					c => c.Token("string"),
					c => c.Token("number"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Rule("array"),
					c => c.Rule("object")
				); // Choice rule propogates child's value by default

			builder.CreateRule("array")
				.Literal("[")
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","),
					allowTrailingSeparator: true, includeSeparatorsInResult: false)
					.TransformLast(v => v.SelectArray())
				.Literal("]")
				.TransformSelect(1); // Selects the Children[1]'s value

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","),
					allowTrailingSeparator: true, includeSeparatorsInResult: false)
					.TransformLast(v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary(k => k.Key, v => v.Value))
				.Literal("}")
				.TransformSelect(1);

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value")
				.Transform(v => KeyValuePair.Create(v.GetValue<string>(0), v.GetValue(2)));

			builder.CreateMainRule("content")
				.Rule("value")
				.EOF() // Sure that we captured all the input
				.TransformSelect(0);

			var jsonParser = builder.Build();

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

			// Get the result!
			var result = jsonParser.Parse<Dictionary<string, object>>(json);

			Assert.Equal("Sample Data", result["name"]);
		}

		[Fact]
		public void PythonLike()
		{
			var builder = new ParserBuilder();

			builder.Settings
				.SkipWhitespaces();

			// Add the 'INDENT' and 'DEDENT' barrier tokenizer
			// 'INDENT' is emitted when indentation grows
			// And 'DEDENT' is emitted when indentation cuts
			// They are indentation delta tokens
			builder.BarrierTokenizers
				.AddIndent(indentSize: 4, "INDENT", "DEDENT");

			// Create the statement rule
			builder.CreateRule("statement")
				.Choice(
				b => b
					.Literal("def")
					.Identifier()
					.Literal("():")
					.Rule("block"),
				b => b
					.Literal("if")
					.Identifier()
					.Literal(":")
					.Rule("block"),
				b => b
					.Identifier()
					.Literal("=")
					.Identifier()
					.Literal(";"));

			// Create the 'block' rule that matches our 'INDENT' and 'DEDENT' barrier tokens
			builder.CreateRule("block")
				.Token("INDENT")
				.OneOrMore(b => b.Rule("statement"))
				.Token("DEDENT");

			builder.CreateMainRule("program")
				.ZeroOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			string inputStr =
			"""
			def a():
				b = c;
				c = a;
			a = p;
			if c:
				h = i;
				if b:
					a = aa;
			""";

			// Get the optimized AST...
			var ast = parser.Parse(inputStr).Optimized();

			string part1 =
			"""
			def a():
				b = c;
				c = a;
			""";
			string part2 =
			"""
			a = p;
			""";
			string part3 =
			"""
			if c:
				h = i;
				if b:
					a = aa;
			""";

			Assert.Equal(part1, ast[0].Text);
			Assert.Equal(part2, ast[1].Text);
			Assert.Equal(part3, ast[2].Text);
		}

		[Fact]
		public void CustomToken()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("custom")
				.Custom((self, input, start, end, parameter) =>
				{
					var context = (char)parameter!;

					// Fail if input at current position is not equal to context character
					if (start >= end || input[start] != context)
						return ParsedElement.Fail;

					// Capture the character
					return new ParsedElement(
						startIndex: start,
						length: 1,
						intermediateValue: "my intermediate value!");
				});

			builder.CreateRule("custom_rule").Token("custom");
			var parser = builder.Build();

			var result = parser.ParseRule("custom_rule", "x", parameter: 'x').GetIntermediateValue<string>(); // "my intermediate value!"
			Assert.Equal("my intermediate value!", result);

			Assert.Throws<ParsingException>(() => parser.ParseRule("custom_rule", "y", parameter: 'x'));
		}

		[Fact]
		public void JSONTokenCombination()
		{
			var builder = new ParserBuilder();

			// Use lookahead for 'Choice' tokens
			builder.Settings.UseFirstCharacterMatch();

			builder.CreateToken("string")
				// 'Between' token pattern matches a sequence of three elements,
				// but calculates and propogates intermediate value of second element
				.Between(
					b => b.Literal('"'),
					b => b.TextUntil('"'),
					b => b.Literal('"'));

			builder.CreateToken("number")
				.Number<double>();

			builder.CreateToken("boolean")
				// 'Map' token pattern applies intermediate value transformer to child's value
				.Map<string>(b => b.LiteralChoice("true", "false"), m => m == "true");

			builder.CreateToken("null")
				// 'Return' does not calculates value for child element, just returns 'null' here
				.Return(b => b.Literal("null"), null);

			builder.CreateToken("value")
				// Skip whitespaces before value token
				.SkipWhitespaces(b =>
					// 'Choice' token selects the matched token's value
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
					b => b.SkipWhitespaces(b => b.Literal(',')),
					includeSeparatorsInResult: false)
				// You can apply passage function for tokens that
				// matches multiple and variable amount of child elements
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

			// Match the token directly and produce intermediate value
			var result = parser.MatchToken<Dictionary<string, object>>("value", json);
			Console.WriteLine(result["name"]); // Outputs: Sample data

			Assert.Equal(1.0, result["id"]);
			Assert.Equal(["tag1", "tag2", "tag3"], (object[])result["tags"]);
			Assert.Equal("Sample Data", result["name"]);
			Assert.True((bool)result["isActive"]);
			var nested = (Dictionary<string, object>)result["nested"];
			Assert.Equal(123.456, (double)nested["value"]);
			Assert.Equal("Nested description", nested["description"].ToString());
		}

		/// <summary>
		/// The Habr article test
		/// </summary>
		[Fact]
		public void SimplifiedYAML()
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespaces();
			builder.BarrierTokenizers.AddIndent(indentSize: 2, "INDENT", "DEDENT", "NEWLINE");

			builder.CreateToken("boolean")
				.LiteralChoice("true", "false").Transform(v => v.Text == "true");

			builder.CreateToken("number")
				.Number<double>();

			builder.CreateToken("string")
				.Literal("\"")                       // 0
				.EscapedTextPrefix('\\', '\\', '\"') // 1
				.Literal("\"")                       // 2
				.Pass(index: 1);

			builder.CreateToken("identifier")
				.UnicodeIdentifier();

			builder.CreateRule("value")
				.Choice(
					b => b.Token("boolean"),
					b => b.Token("number"),
					b => b.Token("string")
				);

			builder.CreateRule("object_pair")
				.Token("identifier")
				.Literal(":")
				.Rule("value")
				.Token("NEWLINE")
				.Transform(v =>
				{
					var key = v[0].Text;
					var value = v[2].GetValue();
					return new KeyValuePair<string, object>(key, value);
				});

			builder.CreateRule("object_child")
				.Choice(
					b => b.Rule("object_pair"),
					b => b.Rule("object")
				);

			builder.CreateRule("object")
				.Token("identifier")
				.Literal(":")
				.Token("NEWLINE")
				.Token("INDENT")
				.OneOrMore(b => b.Rule("object_child"))
					.TransformLast(v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary())
				.Token("DEDENT")
				.Transform(v =>
				{
					var key = v[0].Text;
					var value = v[4].GetValue();
					return new KeyValuePair<string, object>(key, value);
				});

			builder.CreateMainRule()
				.ZeroOrMore(b => b.Rule("object_child"))
					.TransformLast(v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary())
				.EOF()
				.TransformSelect(index: 0);

			var parser = builder.Build();

			string input =
			"""
			a_nested_map:
			  key: true
			  another_key: 0.9
			  another_nested_map:
			    hello: "Hello world!"
			  key_after: 999
			""";

			var value = parser.Parse<Dictionary<string, object>>(input);

			var a_nested_map = value["a_nested_map"] as Dictionary<string, object>;
			var key = (bool)a_nested_map!["key"];
			var another_key = (double)a_nested_map["another_key"];
			var another_nested_map = a_nested_map["another_nested_map"] as Dictionary<string, object>;
			var hello = another_nested_map!["hello"] as string;
			var key_after = (double)a_nested_map["key_after"];

			Assert.True(key);
			Assert.Equal(0.9, another_key);
			Assert.Equal("Hello world!", hello);
			Assert.Equal(999, key_after);
		}
	}
}