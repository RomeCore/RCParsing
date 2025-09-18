using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Newtonsoft.Json.Linq;

namespace RCParsing.Benchmarks.JSON
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net80, iterationCount: 7, warmupCount: 2)]
	[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
	public class ParserCombinatorJSONBenchmarks
	{
		public ParserCombinatorJSONBenchmarks()
		{
		}

		// ====== Short JSON (~20 lines) ======

		[Benchmark(Baseline = true), BenchmarkCategory("short")]
		public void JsonShort_RCParsing()
		{
			var value = RCJsonParser.Parse(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_RCParsing_Optimized()
		{
			var value = RCJsonParser.ParseOptimized(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_RCParsing_TokenCombination()
		{
			var value = RCCombinatorJsonParser.Parse(TestJSONs.shortJson);
		}

		/*[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_SystemTextJson()
		{
			var value = JsonNode.Parse(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_NewtonsoftJson()
		{
			var value = JToken.Parse(TestJSONs.shortJson);
		}*/

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_Parlot()
		{
			var value = ParlotJsonParser.Parse(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_Pidgin()
		{
			var value = PidginJsonParser.Parse(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_Superpower()
		{
			var value = SuperpowerJsonParser.ParseJson(TestJSONs.shortJson);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void JsonShort_Sprache()
		{
			var value = SpracheJsonParser.ParseJson(TestJSONs.shortJson);
		}

		// ====== Big JSON (~200 lines) ======

		[Benchmark(Baseline = true), BenchmarkCategory("big")]
		public void JsonBig_RCParsing()
		{
			var value = RCJsonParser.Parse(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_RCParsing_Optimized()
		{
			var value = RCJsonParser.ParseOptimized(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_RCParsing_TokenCombination()
		{
			var value = RCCombinatorJsonParser.Parse(TestJSONs.bigJson);
		}

		/*[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_SystemTextJson()
		{
			var value = JsonNode.Parse(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_NewtonsoftJson()
		{
			var value = JToken.Parse(TestJSONs.bigJson);
		}*/

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_Parlot()
		{
			var value = ParlotJsonParser.Parse(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_Pidgin()
		{
			var value = PidginJsonParser.Parse(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_Superpower()
		{
			var value = SuperpowerJsonParser.ParseJson(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void JsonBig_Sprache()
		{
			var value = SpracheJsonParser.ParseJson(TestJSONs.bigJson);
		}
	}
}