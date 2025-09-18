using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

namespace RCParsing.Benchmarks.Expressions
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net80, iterationCount: 5, warmupCount: 2)]
	[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
	public class ParserCombinatorExpressionBenchmarks
	{
		[Benchmark(Baseline = true), BenchmarkCategory("short")]
		public void ExpressionShort_RCParsing()
		{
			var value = RCExpressionParser.Parse(TestExpressions.shortExpression);
		}
		
		[Benchmark, BenchmarkCategory("short")]
		public void ExpressionShort_RCParsing_Optimized()
		{
			var value = RCExpressionParser.ParseOptimized(TestExpressions.shortExpression);
		}
		
		[Benchmark, BenchmarkCategory("short")]
		public void ExpressionShort_RCParsing_TokenCombination()
		{
			var value = RCCombinatorExpressionParser.Parse(TestExpressions.shortExpression);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void ExpressionShort_Parlot()
		{
			var value = ParlotExpressionParser.Parse(TestExpressions.shortExpression);
		}

		[Benchmark, BenchmarkCategory("short")]
		public void ExpressionShort_Pidgin()
		{
			var value = PidginExpressionParser.Parse(TestExpressions.shortExpression);
		}



		[Benchmark(Baseline = true), BenchmarkCategory("big")]
		public void ExpressionBig_RCParsing()
		{
			var value = RCExpressionParser.Parse(TestExpressions.bigExpression);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void ExpressionBig_RCParsing_Optimized()
		{
			var value = RCExpressionParser.ParseOptimized(TestExpressions.bigExpression);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void ExpressionBig_RCParsing_TokenCombination()
		{
			var value = RCCombinatorExpressionParser.Parse(TestExpressions.bigExpression);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void ExpressionBig_Parlot()
		{
			var value = ParlotExpressionParser.Parse(TestExpressions.bigExpression);
		}

		[Benchmark, BenchmarkCategory("big")]
		public void ExpressionBig_Pidgin()
		{
			var value = PidginExpressionParser.Parse(TestExpressions.bigExpression);
		}
	}
}