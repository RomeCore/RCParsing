using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests
{
	/// <summary>
	/// The tests for incremental parsing. These are useful when you need to reparse a part of changed input in the middle.
	/// </summary>
	public class IncrementalParsingTests
	{
		[Fact]
		public void SimpleIncrementalParsing()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces().UseLazyAST();

			builder.CreateMainRule()
				.Identifier()
				.Literal('=')
				.Identifier();

			var parser = builder.Build();

			var input = "a = b";
			var ast = parser.Parse(input);

			Assert.Equal(0, ast.Version);
			Assert.Equal(0, ast[0].Version);
			Assert.Equal(0, ast[1].Version);
			Assert.Equal(0, ast[2].Version);

			var changedInput1 = "a = c";
			var changedAst1 = ast.Reparsed(changedInput1);

			Assert.Equal(1, changedAst1.Version);
			Assert.Equal(0, changedAst1[0].Version);
			Assert.Equal(0, changedAst1[1].Version);
			Assert.Equal(1, changedAst1[2].Version);

			// Check versions of old AST again for immutability
			Assert.Equal(0, ast.Version);
			Assert.Equal(0, ast[0].Version);
			Assert.Equal(0, ast[1].Version);
			Assert.Equal(0, ast[2].Version);

			var changedInput2 = "abc = b";
			var changedAst2 = ast.Reparsed(changedInput2);

			Assert.Equal(1, changedAst2.Version);
			Assert.Equal(1, changedAst2[0].Version);
			Assert.Equal(0, changedAst2[1].Version);
			Assert.Equal(0, changedAst2[2].Version);

			var changedInput3 = "abc = c";
			var changedAst3 = changedAst1.Reparsed(changedInput3);

			Assert.Equal(2, changedAst3.Version);
			Assert.Equal(2, changedAst3[0].Version);
			Assert.Equal(0, changedAst3[1].Version);
			Assert.Equal(1, changedAst3[2].Version);
		}

		[Fact]
		public void JSONIncrementalParsing()
		{
			var builder = new ParserBuilder();

			builder.Settings
				.Skip(r => r.Rule("skip"), ParserSkippingStrategy.SkipBeforeParsingGreedy)
				.UseLazyAST();

			builder.CreateRule("skip")
				.Choice(
					b => b.Whitespaces(),
					b => b.Literal("//").TextUntil('\n', '\r'))
				.ConfigureForSkip();

			builder.CreateToken("string")
				.Literal('"')
				.EscapedTextPrefix(prefix: '\\', '\\', '\"')
				.Literal('"')
				.Pass(index: 1);

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
				);

			builder.CreateRule("array")
				.Literal("[")
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","),
					allowTrailingSeparator: true, includeSeparatorsInResult: false)
					.TransformLast(v => v.SelectArray())
				.Literal("]")
				.TransformSelect(1);

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
				.EOF()
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

			var ast = jsonParser.Parse(json);
			var value = ast.Value as Dictionary<string, object>;
			var tags = value!["tags"] as object[];
			var name = value!["name"] as string;
			var nestedObj = value!["nested"] as Dictionary<string, object>;

			var originalNestedObject = new Dictionary<string, object>
			{
				["value"] = 123.456,
				["description"] = "Nested description"
			};

			Assert.Equal(["tag1", "tag2", "tag3"], tags);
			Assert.Equal(originalNestedObject, nestedObj);

			var changedJson =
			"""
			{
				"id": 1,
				"name": "Sample Data",
				"created": "2023-01-01T00:00:00", // This is a comment
				"tags": { "nested": ["tag1", "tag2", "tag3"] },
				"isActive": true,
				"nested": {
					"value": 123.456,
					"description": "Nested description"
				}
			}
			""";

			var changedAst = ast.Reparsed(changedJson);
			var changedValue = changedAst.Value as Dictionary<string, object>;
			var changedTags = changedValue!["tags"] as Dictionary<string, object>;
			var changedName = changedValue!["name"] as string;
			var changedNestedObj = changedValue!["nested"] as Dictionary<string, object>;

			var originalTagsObject = new Dictionary<string, object>
			{
				["nested"] = new object[] { "tag1", "tag2", "tag3" }
			};

			Assert.Equal(originalTagsObject, changedTags);
			Assert.Same(name, changedName);
			Assert.Same(nestedObj, changedNestedObj);
		}
	}
}