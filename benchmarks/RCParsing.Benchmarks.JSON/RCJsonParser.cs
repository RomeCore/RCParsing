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
				.Skip(r => r.Whitespaces().ConfigureForSkip(), ParserSkippingStrategy.SkipBeforeParsing);

			builder.CreateToken("string")
				.Literal('"')
				.TextUntil('"')
				.Literal('"')
				.Pass(1);

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer);

			builder.CreateToken("boolean")
				.LiteralChoice(["true", "false"], v => v.GetIntermediateValue<string>() == "true");

			builder.CreateToken("null")
				.Literal("null", _ => null);

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
					allowTrailingSeparator: true, includeSeparatorsInResult: false,
					factory: v => v.SelectArray())
				.Literal("]")
				.Transform(v => v.GetValue(1));

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","), allowTrailingSeparator: true,
					factory: v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary())
				.Literal("}")
				.Transform(v => v.GetValue(1));

			builder.CreateRule("pair")
				.Token("string")
				.Literal(":")
				.Rule("value")
				.Transform(v => new KeyValuePair<string, object>(v.GetValue<string>(0), v.GetValue(2)));

			builder.CreateMainRule("content")
				.Rule("value")
				.EOF()
				.Transform(v => v.GetValue(0));
		}

		static RCJsonParser()
		{
			var builder = new ParserBuilder();
			builder.Settings.UseInlining().IgnoreErrors();
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
			builder.Settings.DetailedErrors().WriteStackTrace().UseCaching();
			FillWithRules(builder);
			slowParser = builder.Build();
		}

		public static object ParseInlinedNoValue(string text)
		{
			return optimizedParser.Parse(text); // Just AST
		}

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