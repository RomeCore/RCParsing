using BenchmarkDotNet.Running;

namespace RCParsing.Benchmarks.GraphQL
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<GraphQLBenchmarks>();
		}
	}
}