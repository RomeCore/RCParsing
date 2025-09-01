using BenchmarkDotNet.Running;

namespace RCParsing.Benchmarks.Expressions
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<ParserCombinatorExpressionBenchmarks>();
		}
	}
}