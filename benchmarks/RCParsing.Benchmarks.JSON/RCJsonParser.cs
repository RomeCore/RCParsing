using System;
using System.Collections;
using System.Collections.Generic;
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
		static Parser optimizedParser, defaultParser, debugParser, slowParser;

		private static void FillWithRules(ParserBuilder builder)
		{
			builder.Settings
				.SkipWhitespaces();

			builder.CreateToken("string")
				.Literal('"')
				.TextUntil('"') // 1
				.Literal('"')
				.Pass(1);

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer); // Match integer, without convertation

			builder.CreateToken("boolean")
				.LiteralChoice(["true", "false"],
				v => v.GetIntermediateValue<string>() == "true"); // Intermediate value is matched string in choice

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
				.Rule("value") // 0
				.EOF()
				.TransformSelect(index: 0);
		}

		static RCJsonParser()
		{
			var builder = new ParserBuilder();
			builder.Settings.UseInlining().IgnoreErrors().UseLightAST();
			FillWithRules(builder);
			optimizedParser = builder.Build();

			builder = new ParserBuilder();
			FillWithRules(builder);
			defaultParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.DetailedErrors().WriteStackTrace();
			FillWithRules(builder);
			debugParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.DetailedErrors().WriteStackTrace().UseCaching().UseLazyAST();
			FillWithRules(builder);
			slowParser = builder.Build();
		}

		public static object ParseInlinedNoValue(string text)
		{
			return optimizedParser.Parse(text); // Just AST
		}

		// The parser used in benchmarks for comparison with other libraries
		public static object ParseInlined(string text)
		{
			return optimizedParser.Parse<object>(text);
		}

		public static object Parse(string text)
		{
			return defaultParser.Parse<object>(text);
		}

		public static object ParseDebug(string text)
		{
			return debugParser.Parse<object>(text);
		}

		public static object ParseDebugMemoized(string text)
		{
			return slowParser.Parse<object>(text);
		}
	}
}