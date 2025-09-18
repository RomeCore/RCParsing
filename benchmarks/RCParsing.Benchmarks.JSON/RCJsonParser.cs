using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Benchmarks.JSON
{
	public static class RCJsonParser
	{
		static Parser parser, optimizedParser;

		public static void FillWithRules(ParserBuilder builder)
		{
			builder.Settings
				.SkipWhitespaces();

			builder.CreateToken("string")
				.Between(
					b => b.Literal('"'),
					b => b.TextUntil('"'),
					b => b.Literal('"'));

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer); // Match integer, without convertation

			builder.CreateToken("true")
				.Literal("true", _ => true);

			builder.CreateToken("false")
				.Literal("false", _ => false);

			builder.CreateToken("null")
				.Literal("null", _ => null);

			builder.CreateRule("value")
				.Choice(
					c => c.Token("string"),
					c => c.Token("number"),
					c => c.Token("true"),
					c => c.Token("false"),
					c => c.Token("null"),
					c => c.Rule("array"),
					c => c.Rule("object")
				);

			builder.CreateRule("array")
				.Literal('[')
				.List(v => v.Rule("value")) // 1
				.Literal(']')
				.TransformSelect(index: 1);

			builder.CreateRule("object")
				.Literal('{')
				.List(v => v.Rule("pair"))
					.TransformLast(v =>
					{
						var dict = new Dictionary<string, object>(v.Count);
						foreach (var child in v)
						{
							var kvp = child.GetValue<KeyValuePair<string, object>>();
							dict.Add(kvp.Key, kvp.Value);
						}
						return dict;
					})
				.Literal('}')
				.TransformSelect(index: 1);

			builder.CreateRule("pair")
				.Token("string")
				.Literal(':')
				.Rule("value")
				.Transform<string, Ignored, object>((k, _, v) => new KeyValuePair<string, object>(k, v));

			builder.CreateMainRule("content")
				.Rule("value");
		}

		static RCJsonParser()
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			parser = builder.Build();
			
			builder = new ParserBuilder();
			builder.Settings
				.UseInlining() // Reduces abstraction level, redirecting calls from Parser directly to rule
				.UseFirstCharacterMatch() // Enables lookahead based on first-character sets
				.IgnoreErrors() // Disables error recording
				.UseLightAST() // Enables a more lightweight version of AST, reducing allocations
				.SkipWhitespacesOptimized(); // Enables direct whitespace skipping in Parser
			FillWithRules(builder);
			optimizedParser = builder.Build();
		}

		public static object Parse(string text)
		{
			return parser.Parse<object>(text);
		}

		public static object ParseOptimized(string text)
		{
			return optimizedParser.Parse<object>(text);
		}
	}
}