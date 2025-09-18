using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Benchmarks.Expressions
{
	public static class RCCombinatorExpressionParser
	{
		static Parser parser;
		static TokenPattern exprToken;

		static void FillWithRules(ParserBuilder builder)
		{
			builder.CreateToken("term")
				.SkipWhitespaces(b => b.Choice(
					b => b.Number<int>(),
					b => b.Between(
						b => b.Literal('('),
						b => b.SkipWhitespaces(b => b.Token("expr")),
						b => b.SkipWhitespaces(b => b.Literal(')')))
				));

			builder.CreateToken("factor")
				.OneOrMoreSeparated(
					b => b.Token("term"),
					b => b.SkipWhitespaces(b => b.LiteralChoice("*", "/")), includeSeparatorsInResult: true
				).Pass(v =>
				{
					int result = (int)v[0];

					for (int i = 1; i < v.Count - 1; i += 2)
					{
						if ((string)v[i] == "*")
							result *= (int)v[i + 1];
						else
							result /= (int)v[i + 1];
					}

					return result;
				});

			builder.CreateToken("expr")
				.OneOrMoreSeparated(
					b => b.SkipWhitespaces(b => b.Token("factor")),
					b => b.SkipWhitespaces(b => b.LiteralChoice("+", "-")), includeSeparatorsInResult: true
				).Pass(v =>
				{
					int result = (int)v[0];

					for (int i = 1; i < v.Count - 1; i += 2)
					{
						if ((string)v[i] == "+")
							result += (int)v[i + 1];
						else
							result -= (int)v[i + 1];
					}

					return result;
				});
		}

		static RCCombinatorExpressionParser()
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			builder.Settings.UseFirstCharacterMatch();
			parser = builder.Build();
			exprToken = parser.GetTokenPattern("expr");
		}

		public static int Parse(string expression)
		{
			return (int)exprToken.Match(expression, 0, expression.Length, null, true).intermediateValue;
		}
	}
}