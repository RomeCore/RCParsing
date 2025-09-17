using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Benchmarks.JSON
{
	public class RCCombinatorJsonParser
	{
		static Parser parser;

		public static void FillWithRules(ParserBuilder builder)
		{
			builder.CreateToken("string")
				.Between(
					b => b.Literal('"'),
					b => b.TextUntil('"'),
					b => b.Literal('"'));

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer);

			builder.CreateToken("boolean")
				.Map<string>(b => b.LiteralChoice("true", "false"), m => m == "true");

			builder.CreateToken("null")
				.Literal("null", _ => null);

			builder.CreateToken("value")
				.SkipWhitespaces(b =>
					b.Choice(
						c => c.Token("string"),
						c => c.Token("number"),
						c => c.Token("true"),
						c => c.Token("false"),
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
					b => b.SkipWhitespaces(b => b.Literal('[')),
					b => b.Token("value_list"),
					b => b.SkipWhitespaces(b => b.Literal(']')));

			builder.CreateToken("pair")
				.Token("string")
				.SkipWhitespaces(b => b.Literal(':'))
				.Token("value")
				.Pass(v =>
				{
					return KeyValuePair.Create((string)v[0], v[2]);
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
					b => b.SkipWhitespaces(b => b.Literal('{')),
					b => b.Token("pair_list"),
					b => b.SkipWhitespaces(b => b.Literal('}')));

			builder.CreateMainRule("content")
				.Rule("value") // 0
				.EOF()
				.TransformSelect(index: 0);
		}

		static RCCombinatorJsonParser()
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			parser = builder.Build();
		}

		public static object ParseInlined(string text)
		{
			return parser.Parse<object>(text);
		}
	}
}