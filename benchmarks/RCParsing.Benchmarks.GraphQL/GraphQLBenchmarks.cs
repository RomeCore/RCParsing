using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace RCParsing.Benchmarks.GraphQL
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net80, iterationCount: 7, warmupCount: 2)]
	[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
	public class GraphQLBenchmarks
	{
		private readonly Parser rcDefaultParser;
		private readonly Parser rcOptimizedParser;
		private readonly Parser rcMemoizedParser;
		private readonly Parser rcMemoizedOptimizedParser;

		public GraphQLBenchmarks()
		{
			rcDefaultParser = RCGraphQLParser.CreateParser();
			rcOptimizedParser = RCGraphQLParser.CreateParser(b => b.Settings.UseInlining().IgnoreErrors().UseFirstCharacterMatch());
			rcMemoizedParser = RCGraphQLParser.CreateParser(b => b.Settings.UseCaching());
			rcMemoizedOptimizedParser = RCGraphQLParser.CreateParser(b => b.Settings.UseCaching().UseInlining().IgnoreErrors().UseFirstCharacterMatch());
		}

		// SHORT

		[Benchmark(Baseline = true), BenchmarkCategory("short")]
		public void QueryShort_RCParsing_Default()
		{
			rcDefaultParser.Parse(TestInputs.shortGraphQLQuery);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void QueryShort_RCParsing_Optimized()
		{
			rcOptimizedParser.Parse(TestInputs.shortGraphQLQuery);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void QueryShort_RCParsing_Memoized()
		{
			rcMemoizedParser.Parse(TestInputs.shortGraphQLQuery);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void QueryShort_RCParsing_MemoizedOptimized()
		{
			rcMemoizedOptimizedParser.Parse(TestInputs.shortGraphQLQuery);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void QueryShort_ANTLR()
		{
			ANTLRGraphQLParser.Parse(TestInputs.shortGraphQLQuery);
		}

		// BIG

		[Benchmark(Baseline = true), BenchmarkCategory("big")]
		public void QueryBig_RCParsing_Default()
		{
			rcDefaultParser.Parse(TestInputs.bigGraphQLQuery);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void QueryBig_RCParsing_Optimized()
		{
			rcOptimizedParser.Parse(TestInputs.bigGraphQLQuery);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void QueryBig_RCParsing_Memoized()
		{
			rcMemoizedParser.Parse(TestInputs.bigGraphQLQuery);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void QueryBig_RCParsing_MemoizedOptimized()
		{
			rcMemoizedOptimizedParser.Parse(TestInputs.bigGraphQLQuery);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void QueryBig_ANTLR()
		{
			ANTLRGraphQLParser.Parse(TestInputs.bigGraphQLQuery);
		}
	}
}