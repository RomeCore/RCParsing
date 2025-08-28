using BenchmarkDotNet.Running;
using RCParsing.Benchmarks.JSON;

namespace RCParsing.Benchmarks.DifferentConfigurations
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<DifferentConfigurationsBenchmarks>();
		}
	}
}