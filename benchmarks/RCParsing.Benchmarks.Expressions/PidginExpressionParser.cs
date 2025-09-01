using Pidgin;
using Pidgin.Expression;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace RCParsing.Benchmarks.Expressions
{
	public static class PidginExpressionParser
	{
		private static readonly Parser<char, int> Expr;

		static PidginExpressionParser()
		{
			var operators = new[]
			{
				Operator.Prefix(Char('-').Before(SkipWhitespaces).ThenReturn<Func<int, int>>(x => -x)),
				Operator.InfixL(Char('*').Before(SkipWhitespaces).ThenReturn<Func<int, int, int>>((x, y) => x * y)
					.Or(Char('/').Before(SkipWhitespaces).ThenReturn<Func<int, int, int>>((x, y) => x / y))),
				Operator.InfixL(Char('+').Before(SkipWhitespaces).ThenReturn<Func<int, int, int>>((x, y) => x + y)
					.Or(Char('-').Before(SkipWhitespaces).ThenReturn<Func<int, int, int>>((x, y) => x - y)))
			};
			Expr = ExpressionParser.Build(
				expr => Num.Before(SkipWhitespaces).Or(expr.Between(Char('('), Char(')'))),
				operators
			);
		}

		public static int Parse(string expression)
		{
			return Expr.ParseOrThrow(expression);
		}
	}
}