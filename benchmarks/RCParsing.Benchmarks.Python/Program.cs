using BenchmarkDotNet.Running;

namespace RCParsing.Benchmarks.Python
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<PythonBenchmarks>();
		}
	}
}