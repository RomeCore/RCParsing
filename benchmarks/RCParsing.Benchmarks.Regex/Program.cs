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
			var rxShortIdCount = benchmarks.IdentifiersShort_Regex();
			var rcBigIdCount = benchmarks.IdentifiersBig_RCParsing();
			var rxBigIdCount = benchmarks.IdentifiersBig_Regex();
			
			var rcShortEmailCount = benchmarks.EmailsShort_RCParsing();
			var rxShortEmailCount = benchmarks.EmailsShort_Regex();
			var rcBigEmailCount = benchmarks.EmailsBig_RCParsing();
			var rxBigEmailCount = benchmarks.EmailsBig_Regex();

			if (rcShortIdCount != rxShortIdCount)
			{
				Console.WriteLine($"rcShortIdCount:{rcShortIdCount} and rxShortIdCount:{rxShortIdCount} not equal!");
				return;
			}
			
			if (rcBigIdCount != rxBigIdCount)
			{
				Console.WriteLine($"rcBigIdCount:{rcBigIdCount} and rxBigIdCount:{rxBigIdCount} not equal!");
				return;
			}
			
			if (rcShortEmailCount != rxShortEmailCount)
			{
				Console.WriteLine($"rcShortEmailCount:{rcShortEmailCount} and rxShortEmailCount:{rxShortEmailCount} not equal!");
				return;
			}
			
			if (rcBigEmailCount != rxBigEmailCount)
			{
				Console.WriteLine($"rcBigEmailCount:{rcBigEmailCount} and rxBigEmailCount:{rxBigEmailCount} not equal!");
				return;
			}

			Console.WriteLine($"rcShortIdCount:{rcShortIdCount}, rxShortIdCount:{rxShortIdCount}");
			Console.WriteLine($"rcBigIdCount:{rcBigIdCount}, rxBigIdCount:{rxBigIdCount}");
			Console.WriteLine($"rcShortEmailCount:{rcShortEmailCount}, rxShortEmailCount:{rxShortEmailCount}");
			Console.WriteLine($"rcBigEmailCount:{rcBigEmailCount}, rxBigEmailCount:{rxBigEmailCount}");
			Console.WriteLine("All results valid!");

			var summary = BenchmarkRunner.Run<RegexBenchmarks>();
		}
	}
}