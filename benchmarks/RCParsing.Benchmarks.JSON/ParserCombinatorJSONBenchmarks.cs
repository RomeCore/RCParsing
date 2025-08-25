using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace RCParsing.Benchmarks.JSON
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net80, iterationCount: 3, warmupCount: 2)]
	public class ParserCombinatorJSONBenchmarks
	{
		public ParserCombinatorJSONBenchmarks()
		{
		}

		[Benchmark]
		public void RCParseJsonShort()
		{
			var value = RCJsonParser.Parse(TestJSONs.shortJson);
		}

		[Benchmark]
		public void PidginParseJsonShort()
		{
			var value = PidginJsonParser.Parse(TestJSONs.shortJson);
		}

		[Benchmark]
		public void SuperpowerParseJsonShort()
		{
			var value = SuperpowerJsonParser.ParseJson(TestJSONs.shortJson);
		}

		[Benchmark]
		public void RCParseJsonBig()
		{
			var value = RCJsonParser.Parse(TestJSONs.bigJson);
		}

		[Benchmark]
		public void PidginParseJsonBig()
		{
			var value = PidginJsonParser.Parse(TestJSONs.bigJson);
		}

		[Benchmark]
		public void SuperpowerParseJsonBig()
		{
			var value = SuperpowerJsonParser.ParseJson(TestJSONs.bigJson);
		}
	}
}