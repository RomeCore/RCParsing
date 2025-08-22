using System;
using System.Linq;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Running;

namespace BenchmarkJSON
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<ParserCombinatorJSONBenchmarks>();
		}
	}
}