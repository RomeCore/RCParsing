using BenchmarkDotNet.Running;

namespace RCParsing.Benchmarks.Expressions
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var rcShortExpr = RCExpressionParser.Parse(TestExpressions.shortExpression);
			var rcCombShortExpr = RCCombinatorExpressionParser.Parse(TestExpressions.shortExpression);
			var rcBigExpr = RCExpressionParser.Parse(TestExpressions.bigExpression);
			var rcCombBigExpr = RCCombinatorExpressionParser.Parse(TestExpressions.bigExpression);
			var pidginShortExpr = PidginExpressionParser.Parse(TestExpressions.shortExpression);
			var pidginBigExpr = PidginExpressionParser.Parse(TestExpressions.bigExpression);
			var parlotShortExpr = ParlotExpressionParser.Parse(TestExpressions.shortExpression);
			var parlotBigExpr = ParlotExpressionParser.Parse(TestExpressions.bigExpression);

			if (rcShortExpr != rcCombShortExpr || rcCombShortExpr != pidginShortExpr || pidginShortExpr != parlotShortExpr)
			{
				Console.WriteLine($"rcShortExpr:{rcShortExpr}, rcCombShortExpr:{rcCombShortExpr}, pidginShortExpr:{pidginShortExpr} and parlotShortExpr:{parlotShortExpr} not equal!");
				return;
			}

			if (rcBigExpr != rcCombBigExpr || rcCombBigExpr != pidginBigExpr || pidginBigExpr != parlotBigExpr)
			{
				Console.WriteLine($"rcBigExpr:{rcBigExpr}, rcCombBigExpr:{rcCombBigExpr}, pidginBigExpr:{pidginBigExpr} and parlotBigExpr:{parlotBigExpr} not equal!");
				return;
			}

			Console.WriteLine($"rcShortExpr:{rcShortExpr}, rcCombShortExpr:{rcCombShortExpr}, pidginShortExpr:{pidginShortExpr}, parlotShortExpr:{parlotShortExpr}");
			Console.WriteLine($"rcBigExpr:{rcBigExpr}, rcCombBigExpr:{rcCombBigExpr}, pidginBigExpr:{pidginBigExpr}, parlotBigExpr:{parlotBigExpr}");
			Console.WriteLine("All results valid!");

			var summary = BenchmarkRunner.Run<ParserCombinatorExpressionBenchmarks>();
		}
	}
}