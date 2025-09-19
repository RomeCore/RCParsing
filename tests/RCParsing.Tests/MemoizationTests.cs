using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Tests
{
	/// <summary>
	/// The tests for the memoization functionality. Memoization kills exponential complexity and left recursion.
	/// </summary>
	public class MemoizationTests
	{
		/*[Fact]
		public void LeftRecursion_PlusExpression()
		{
			var builder = new ParserBuilder();
			builder.Settings.UseCaching();

			builder.CreateMainRule("expr")
				.Choice(
					b => b.Rule("expr").Literal("+").Rule("expr").Transform<double, Ignored, double>((a, _, b) => a + b),
					b => b.Number<double>()
				);

			var parser = builder.Build();

			var result = parser.Parse("1+2+3");
			Assert.Equal(6.0, result.Value);
		}

		[Fact]
		public void LeftRecursion_PlusMinusExpression()
		{
			var builder = new ParserBuilder();
			builder.Settings.UseCaching();

			builder.CreateMainRule("expr")
				.Choice(
					b => b.Rule("expr").Literal("+").Rule("expr").Transform<double, Ignored, double>((a, _, b) => a + b),
					b => b.Rule("expr").Literal("-").Rule("expr").Transform<double, Ignored, double>((a, _, b) => a - b),
					b => b.Number<double>()
				);

			var parser = builder.Build();

			var result = parser.Parse("1+2+3-4");
			Assert.Equal(2.0, result.Value);
		}*/
	}
}