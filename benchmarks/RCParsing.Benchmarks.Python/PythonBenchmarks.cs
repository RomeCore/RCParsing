using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace RCParsing.Benchmarks.Python
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net80, iterationCount: 7, warmupCount: 2)]
	[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
	public class PythonBenchmarks
	{
		private readonly Parser rcDefaultParser;
		private readonly Parser rcOptimizedParser;
		private readonly Parser rcMemoizedParser;
		private readonly Parser rcMemoizedOptimizedParser;

		public PythonBenchmarks()
		{
			rcDefaultParser = RCPythonParser.CreateParser();
			rcOptimizedParser = RCPythonParser.CreateParser(b => b.Settings.UseInlining().IgnoreErrors().UseFirstCharacterMatch());
			rcMemoizedParser = RCPythonParser.CreateParser(b => b.Settings.UseCaching());
			rcMemoizedOptimizedParser = RCPythonParser.CreateParser(b => b.Settings.UseCaching().UseInlining().IgnoreErrors().UseFirstCharacterMatch());
		}

		// SHORT

		[Benchmark(Baseline = true), BenchmarkCategory("short")]
		public void PythonShort_RCParsing_Default()
		{
			rcDefaultParser.Parse(TestInputs.shortPython);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void PythonShort_RCParsing_Optimized()
		{
			rcOptimizedParser.Parse(TestInputs.shortPython);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void PythonShort_RCParsing_Memoized()
		{
			rcMemoizedParser.Parse(TestInputs.shortPython);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void PythonShort_RCParsing_MemoizedOptimized()
		{
			rcMemoizedOptimizedParser.Parse(TestInputs.shortPython);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void PythonShort_ANTLR()
		{
			ANTLRPythonParser.Parse(TestInputs.shortPython);
		}

		// BIG

		[Benchmark(Baseline = true), BenchmarkCategory("big")]
		public void PythonBig_RCParsing_Default()
		{
			rcDefaultParser.Parse(TestInputs.bigPython);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void PythonBig_RCParsing_Optimized()
		{
			rcOptimizedParser.Parse(TestInputs.bigPython);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void PythonBig_RCParsing_Memoized()
		{
			rcMemoizedParser.Parse(TestInputs.bigPython);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void PythonBig_RCParsing_MemoizedOptimized()
		{
			rcMemoizedOptimizedParser.Parse(TestInputs.bigPython);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void PythonBig_ANTLR()
		{
			ANTLRPythonParser.Parse(TestInputs.bigPython);
		}
	}
}