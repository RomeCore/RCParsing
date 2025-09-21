using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using RCParsing.Building;

namespace RCParsing.Benchmarks.Regex
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net80, iterationCount: 5, warmupCount: 2)]
	[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
	public class RegexBenchmarks
	{
		private readonly Parser identifierParser;
		private readonly Parser optimizedIdentifierParser;
		private readonly System.Text.RegularExpressions.Regex identifierRegex;
		
		private readonly Parser emailParser;
		private readonly Parser optimizedEmailParser;
		private readonly System.Text.RegularExpressions.Regex emailRegex;

		public RegexBenchmarks()
		{
			var builder = new ParserBuilder();
			builder.Settings.IgnoreErrors();
			builder.CreateMainRule()
				.Identifier();
			identifierParser = builder.Build();
			
			builder = new ParserBuilder();
			builder.Settings.IgnoreErrors();
			builder.Settings.Skip(b => b.Token("skip"));
			builder.CreateToken("skip")
				.OneOrMoreChars(c => !char.IsAsciiLetterOrDigit(c) && c != '_');
			builder.CreateMainRule()
				.Identifier();
			optimizedIdentifierParser = builder.Build();

			identifierRegex = new(@"[a-zA-Z_][a-zA-Z0-9_]*", RegexOptions.Compiled);

			builder = new ParserBuilder();
			builder.Settings.UseInlining().UseFirstCharacterMatch().IgnoreErrors();
			builder.CreateToken("email")
				.OneOrMoreChars(char.IsAsciiLetterOrDigit)
				.Literal('@')
				.OneOrMoreChars(char.IsAsciiLetterOrDigit)
				.Literal('.')
				.OneOrMoreChars(char.IsAsciiLetterOrDigit);
			builder.CreateMainRule()
				.Token("email");
			emailParser = builder.Build();

			builder = new ParserBuilder();
			builder.Settings.IgnoreErrors();
			builder.Settings.Skip(b => b.Token("skip"), ParserSkippingStrategy.TryParseThenSkip);
			builder.CreateToken("skip")
				.OneOrMoreChars(char.IsAsciiLetterOrDigit);
			builder.CreateToken("email")
				.OneOrMoreChars(char.IsAsciiLetterOrDigit)
				.Literal('@')
				.OneOrMoreChars(char.IsAsciiLetterOrDigit)
				.Literal('.')
				.OneOrMoreChars(char.IsAsciiLetterOrDigit);
			builder.CreateMainRule()
				.Token("email");
			optimizedEmailParser = builder.Build();

			emailRegex = new(@"[a-zA-Z0-9]+@[a-zA-Z0-9]+\.[a-zA-Z0-9]+", RegexOptions.Compiled);
		}

		// Identifier

		[Benchmark(Baseline = true), BenchmarkCategory("id_short")]
		public int IdentifiersShort_RCParsing()
		{
			var matches = identifierParser.FindAllMatches(TestStrings.identifiersShort);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark, BenchmarkCategory("id_short")]
		public int IdentifiersShort_RCParsing_Optimized()
		{
			var matches = optimizedIdentifierParser.FindAllMatches(TestStrings.identifiersShort);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark, BenchmarkCategory("id_short")]
		public int IdentifiersShort_Regex()
		{
			var matches = identifierRegex.Matches(TestStrings.identifiersShort);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark(Baseline = true), BenchmarkCategory("id_big")]
		public int IdentifiersBig_RCParsing()
		{
			var matches = identifierParser.FindAllMatches(TestStrings.identifiersBig);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark, BenchmarkCategory("id_big")]
		public int IdentifiersBig_RCParsing_Optimized()
		{
			var matches = optimizedIdentifierParser.FindAllMatches(TestStrings.identifiersBig);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark, BenchmarkCategory("id_big")]
		public int IdentifiersBig_Regex()
		{
			var matches = identifierRegex.Matches(TestStrings.identifiersBig);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		// Email

		[Benchmark(Baseline = true), BenchmarkCategory("email_short")]
		public int EmailsShort_RCParsing()
		{
			var matches = emailParser.FindAllMatches(TestStrings.emailsShort);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark, BenchmarkCategory("email_short")]
		public int EmailsShort_RCParsing_Optimized()
		{
			var matches = optimizedEmailParser.FindAllMatches(TestStrings.emailsShort);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark, BenchmarkCategory("email_short")]
		public int EmailsShort_Regex()
		{
			var matches = emailRegex.Matches(TestStrings.emailsShort);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark(Baseline = true), BenchmarkCategory("email_big")]
		public int EmailsBig_RCParsing()
		{
			var matches = emailParser.FindAllMatches(TestStrings.emailsBig);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark, BenchmarkCategory("email_big")]
		public int EmailsBig_RCParsing_Optimized()
		{
			var matches = optimizedEmailParser.FindAllMatches(TestStrings.emailsBig);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}

		[Benchmark, BenchmarkCategory("email_big")]
		public int EmailsBig_Regex()
		{
			var matches = emailRegex.Matches(TestStrings.emailsBig);
			int count = 0;
			foreach (var match in matches)
			{
				count++;
			}
			return count;
		}
	}
}