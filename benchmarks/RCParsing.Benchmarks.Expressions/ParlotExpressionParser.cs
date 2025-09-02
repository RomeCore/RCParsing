using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace RCParsing.Benchmarks.Expressions
{
	public static class ParlotExpressionParser
	{
		private static readonly Parser<int> Expr;

		static ParlotExpressionParser()
		{
			var defExpr = new Deferred<int>();

			var term = SkipWhiteSpace(Terms.Integer().Then<int>())
				.Or(Between(SkipWhiteSpace(Terms.Char('(')), SkipWhiteSpace(defExpr), SkipWhiteSpace(Terms.Char(')'))));

			var factor = term.LeftAssociative((SkipWhiteSpace(Terms.Char('*')), (l, r) => l * r),
				(SkipWhiteSpace(Terms.Char('/')), (l, r) => l / r));

			var expr = factor.LeftAssociative((SkipWhiteSpace(Terms.Char('+')), (l, r) => l + r),
				(SkipWhiteSpace(Terms.Char('-')), (l, r) => l - r));

			defExpr.Parser = expr;
			Expr = expr.Compile();
		}

		public static int Parse(string expression)
		{
			return Expr.Parse(expression);
		}
	}
}