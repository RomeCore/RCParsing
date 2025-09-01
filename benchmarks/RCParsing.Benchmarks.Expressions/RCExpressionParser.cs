using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Benchmarks.Expressions
{
	public static class RCExpressionParser
	{
		static Parser optimizedParser, defaultParser;

		static void FillWithRules(ParserBuilder builder)
		{
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("term")
				.Choice(
					b => b.Number<int>(),
					b => b.Literal('(').Rule("expr").Literal(')').TransformSelect(1)
				);

			builder.CreateRule("factor")
				.OneOrMoreSeparated(b => b.Rule("term"), b => b.LiteralChoice("*", "/"), includeSeparatorsInResult: true)
				.TransformFoldLeft<int, string, int>((v1, op, v2) => op == "*" ? v1 * v2 : v1 / v2);

			builder.CreateMainRule("expr")
				.OneOrMoreSeparated(b => b.Rule("factor"), b => b.LiteralChoice("+", "-"), includeSeparatorsInResult: true)
				.TransformFoldLeft<int, string, int>((v1, op, v2) => op == "+" ? v1 + v2 : v1 - v2);
		}

		static RCExpressionParser()
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			builder.Settings.UseInlining();
			optimizedParser = builder.Build();

			builder = new ParserBuilder();
			FillWithRules(builder);
			defaultParser = builder.Build();
		}

		public static int ParseOptimized(string expression)
		{
			return optimizedParser.Parse<int>(expression);
		}

		public static int Parse(string expression)
		{
			return defaultParser.Parse<int>(expression);
		}
	}
}