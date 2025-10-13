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
using RCParsing.Building;

namespace RCParsing.Benchmarks.DifferentConfigurations
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net80, iterationCount: 7, warmupCount: 3)]
	[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
	public class DifferentConfigurationsBenchmarks
	{
		private readonly Parser
			defaultParser,
			whitespaceOptParser,
			inlinedParser,
			lookaheadParser,
			ignoreErrorsParser,
			stackTraceParser,
			walkTraceParser,
			lazyAstParser,
			recordSkippedParser,
			memoizedParser,
			fastestParser,
			slowestParser;

		public DifferentConfigurationsBenchmarks()
		{
			var builder = new ParserBuilder();
			RCJsonParser.FillWithRules(builder);
			defaultParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.UseInlining();
			RCJsonParser.FillWithRules(builder);
			inlinedParser = builder.Build();
			
			builder = new ParserBuilder();
			builder.Settings.SkipWhitespacesOptimized();
			RCJsonParser.FillWithRules(builder);
			whitespaceOptParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.UseFirstCharacterMatch();
			RCJsonParser.FillWithRules(builder);
			lookaheadParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.IgnoreErrors();
			RCJsonParser.FillWithRules(builder);
			ignoreErrorsParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.WriteStackTrace();
			RCJsonParser.FillWithRules(builder);
			stackTraceParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.RecordWalkTrace();
			RCJsonParser.FillWithRules(builder);
			walkTraceParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.UseLazyAST();
			RCJsonParser.FillWithRules(builder);
			lazyAstParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.RecordSkippedRules();
			RCJsonParser.FillWithRules(builder);
			recordSkippedParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.UseCaching();
			RCJsonParser.FillWithRules(builder);
			memoizedParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.UseInlining().UseFirstCharacterMatch().IgnoreErrors().SkipWhitespacesOptimized();
			RCJsonParser.FillWithRules(builder);
			fastestParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.WriteStackTrace().RecordWalkTrace().UseLazyAST().RecordSkippedRules().UseCaching();
			RCJsonParser.FillWithRules(builder);
			slowestParser = builder.Build();
		}

		[Benchmark(Baseline = true), BenchmarkCategory("json")]
		public void Default()
		{
			var value = defaultParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void NoValue()
		{
			var ast = defaultParser.Parse(TestJSONs.bigJson); // Do not calculate value from AST
		}

		[Benchmark, BenchmarkCategory("json")]
		public void OptimizedWhitespaces()
		{
			var value = whitespaceOptParser.Parse<object>(TestJSONs.bigJson);
		}
		
		[Benchmark, BenchmarkCategory("json")]
		public void Inlined()
		{
			var value = inlinedParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void FirstCharacterMatch()
		{
			var value = lookaheadParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void IgnoreErrors()
		{
			var value = ignoreErrorsParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void StackTrace()
		{
			var value = stackTraceParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void WalkTrace()
		{
			var value = walkTraceParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void LazyAST()
		{
			var value = lazyAstParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void RecordSkipped()
		{
			var value = recordSkippedParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void Memoized()
		{
			var value = memoizedParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void FastestNoValue()
		{
			var ast = fastestParser.Parse(TestJSONs.bigJson); // Do not calculate value from AST
		}

		[Benchmark, BenchmarkCategory("json")]
		public void Fastest()
		{
			var value = fastestParser.Parse<object>(TestJSONs.bigJson);
		}

		[Benchmark, BenchmarkCategory("json")]
		public void Slowest()
		{
			var value = slowestParser.Parse<object>(TestJSONs.bigJson);
		}
	}
}