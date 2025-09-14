using BenchmarkDotNet.Running;

namespace RCParsing.Benchmarks.Expressions
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var rcShortExpr = RCExpressionParser.ParseOptimized(TestExpressions.shortExpression);
			var rcBigExpr = RCExpressionParser.ParseOptimized(TestExpressions.bigExpression);
			var pidginShortExpr = PidginExpressionParser.Parse(TestExpressions.shortExpression);
			var pidginBigExpr = PidginExpressionParser.Parse(TestExpressions.bigExpression);
			var parlotShortExpr = ParlotExpressionParser.Parse(TestExpressions.shortExpression);
			var parlotBigExpr = ParlotExpressionParser.Parse(TestExpressions.bigExpression);

			if (rcShortExpr != pidginShortExpr || pidginShortExpr != parlotShortExpr)
			{
				Console.WriteLine($"rcShortExpr:{rcShortExpr}, pidginShortExpr:{pidginShortExpr} and parlotShortExpr:{parlotShortExpr} not equal!");
				return;
			}

			if (rcBigExpr != pidginBigExpr || pidginBigExpr != parlotBigExpr)
			{
				Console.WriteLine($"rcBigExpr:{rcBigExpr}, pidginBigExpr:{pidginBigExpr} and parlotBigExpr:{parlotBigExpr} not equal!");
				return;
			}

			Console.WriteLine($"rcShortExpr:{rcShortExpr}, pidginShortExpr:{pidginShortExpr}, parlotShortExpr:{parlotShortExpr}");
			Console.WriteLine($"rcBigExpr:{rcBigExpr}, pidginBigExpr:{pidginBigExpr}, parlotBigExpr:{parlotBigExpr}");
			Console.WriteLine("All results valid!");

			var summary = BenchmarkRunner.Run<ParserCombinatorExpressionBenchmarks>();
		}
	}
}