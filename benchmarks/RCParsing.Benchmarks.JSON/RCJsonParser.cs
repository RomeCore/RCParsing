using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing;
using RCParsing.Building;

namespace RCParsing.Benchmarks.JSON
{
	public static class RCJsonParser
	{
		static Parser parser;

		static RCJsonParser()
		{
			var builder = new ParserBuilder();

			builder.Settings
				.Skip(r => r.Whitespaces().ConfigureForSkip(), ParserSkippingStrategy.SkipBeforeParsing)
				.UseInitFlags(ParserInitFlags.InlineRules);

			builder.CreateToken("string")
				.Literal("\"")
				.TextUntil("\"")
				.Literal("\"")
				.Pass(v => v[1])
				.Transform(v => v.IntermediateValue);
			
			builder.CreateToken("number")
				.OneOrMoreChars(char.IsAsciiDigit, v => double.Parse(v.Text));

			builder.CreateToken("boolean")
				.LiteralChoice(["true", "false"], v => v.Text == "true");

			builder.CreateToken("null")
				.Literal("null", _ => null);

			builder.CreateRule("value")
				.Choice(
					c => c.Token("string"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Token("number"),
					c => c.Rule("array"),
					c => c.Rule("object")
				);

			builder.CreateRule("array")
				.Literal("[")
				.ZeroOrMoreSeparated(v => v.Rule("value"), s => s.Literal(","),
					allowTrailingSeparator: true, includeSeparatorsInResult: false,
					factory: v => v.Children.Select(a => a.Value).ToArray())
				.Literal("]")
				.Transform(v => v.GetValue(1));

			builder.CreateRule("object")
				.Literal("{")
				.ZeroOrMoreSeparated(v => v.Rule("pair"), s => s.Literal(","), allowTrailingSeparator: true,
					factory: v => v.SelectValues<KeyValuePair<string, object>>().ToDictionary(k => k.Key, v => v.Value))
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

			parser = builder.Build();
		}

		public static object Parse(string text)
		{
			return parser.Parse<object>(text);
		}
	}
}