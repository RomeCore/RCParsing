using BenchmarkDotNet.Running;

namespace RCParsing.Benchmarks.Regex
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Validating results...");

			var benchmarks = new RegexBenchmarks();

			var rcShortIdCount = benchmarks.IdentifiersShort_RCParsing();
			var rcShortOptIdCount = benchmarks.IdentifiersShort_RCParsing_Optimized();
			var rxShortIdCount = benchmarks.IdentifiersShort_Regex();
			var rcBigIdCount = benchmarks.IdentifiersBig_RCParsing();
			var rcBigIdOptCount = benchmarks.IdentifiersBig_RCParsing_Optimized();
			var rxBigIdCount = benchmarks.IdentifiersBig_Regex();
			
			var rcShortEmailCount = benchmarks.EmailsShort_RCParsing();
			var rcShortOptEmailCount = benchmarks.EmailsShort_RCParsing_Optimized();
			var rxShortEmailCount = benchmarks.EmailsShort_Regex();
			var rcBigEmailCount = benchmarks.EmailsBig_RCParsing();
			var rcBigOptEmailCount = benchmarks.EmailsBig_RCParsing_Optimized();
			var rxBigEmailCount = benchmarks.EmailsBig_Regex();

			if (rcShortIdCount != rxShortIdCount || rcShortOptIdCount != rxShortIdCount)
			{
				Console.WriteLine($"rcShortIdCount:{rcShortIdCount}, rcShortOptIdCount:{rcShortOptIdCount} and rxShortIdCount:{rxShortIdCount} not equal!");
				return;
			}
			
			if (rcBigIdCount != rxBigIdCount || rcBigIdOptCount != rxBigIdCount)
			{
				Console.WriteLine($"rcBigIdCount:{rcBigIdCount}, rcBigIdOptCount:{rcBigIdOptCount} and rxBigIdCount:{rxBigIdCount} not equal!");
				return;
			}
			
			if (rcShortEmailCount != rxShortEmailCount || rcShortOptEmailCount != rxShortEmailCount)
			{
				Console.WriteLine($"rcShortEmailCount:{rcShortEmailCount}, rcShortOptEmailCount:{rcShortOptEmailCount} and rxShortEmailCount:{rxShortEmailCount} not equal!");
				return;
			}
			
			if (rcBigEmailCount != rxBigEmailCount || rcBigOptEmailCount != rxBigEmailCount)
			{
				Console.WriteLine($"rcBigEmailCount:{rcBigEmailCount}, rcBigIdOptCount:{rcBigIdOptCount} and rxBigEmailCount:{rxBigEmailCount} not equal!");
				return;
			}

			Console.WriteLine($"rcShortIdCount:{rcShortIdCount}, rcShortOptIdCount:{rcShortOptIdCount}, rxShortIdCount:{rxShortIdCount}");
			Console.WriteLine($"rcBigIdCount:{rcBigIdCount}, rcBigIdOptCount:{rcBigIdOptCount}, rxBigIdCount:{rxBigIdCount}");
			Console.WriteLine($"rcShortEmailCount:{rcShortEmailCount}, rcShortOptEmailCount:{rcShortOptEmailCount}, rxShortEmailCount:{rxShortEmailCount}");
			Console.WriteLine($"rcBigEmailCount:{rcBigEmailCount}, rcBigIdOptCount:{rcBigIdOptCount}, rxBigEmailCount:{rxBigEmailCount}");
			Console.WriteLine("All results valid!");

			var summary = BenchmarkRunner.Run<RegexBenchmarks>();
		}
	}
}