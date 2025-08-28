using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using RCParsing.Benchmarks.JSON;

namespace RCParsing.Benchmarks.DifferentConfigurations
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net80, iterationCount: 3, warmupCount: 2)]
	[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
	public class DifferentConfigurationsBenchmarks
	{
		public DifferentConfigurationsBenchmarks()
		{
		}

		// ====== Short JSON (~20 lines) ======

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_InlinedNoValue()
		{
			var value = RCJsonParser.ParseInlinedNoValue(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_Inlined()
		{
			var value = RCJsonParser.ParseInlined(TestJSONs.shortJson);
		}

		[Benchmark(Baseline = true), BenchmarkCategory("short")]
		public void JsonShort_Default()
		{
			var value = RCJsonParser.Parse(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_Debug()
		{
			var value = RCJsonParser.ParseDebug(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_DebugMemoized()
		{
			var value = RCJsonParser.ParseDebugMemoized(TestJSONs.shortJson);
		}

		// ====== Big JSON (~180 lines) ======

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_InlinedNoValue()
		{
			var value = RCJsonParser.ParseInlinedNoValue(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_Inlined()
		{
			var value = RCJsonParser.ParseInlined(TestJSONs.bigJson);
		}

		[Benchmark(Baseline = true), BenchmarkCategory("big")]
		public void JsonBig_Default()
		{
			var value = RCJsonParser.Parse(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_Debug()
		{
			var value = RCJsonParser.ParseDebug(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_DebugMemoized()
		{
			var value = RCJsonParser.ParseDebugMemoized(TestJSONs.bigJson);
		}
	}
}