using BenchmarkDotNet.Running;

namespace RCParsing.Benchmarks.JSON
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var astSummary = BenchmarkRunner.Run<JSONASTBenchmarks>();
			var combinatorSummary = BenchmarkRunner.Run<JSONCombinatorBenchmarks>();
		}
	}
}