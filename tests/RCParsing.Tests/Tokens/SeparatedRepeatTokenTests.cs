using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.Tokens
{
	public class SeparatedRepeatTokenTests
	{
		[Fact]
		public void ZeroOrMore_Default()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("0_inf")
				.ZeroOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','));

			var parser = builder.Build();

			Assert.Equal(0, parser.MatchToken("0_inf", "").Length); // Empty input
			Assert.Equal(0, parser.MatchToken("0_inf", ",").Length); // Just a separator
			Assert.Equal(2, parser.MatchToken("0_inf", "10").Length); // Just one element
			Assert.Equal(2, parser.MatchToken("0_inf", "10,").Length); // Do not catch trailing separator
			Assert.Equal(4, parser.MatchToken("0_inf", "10,1").Length); // Two elements
			Assert.Equal(4, parser.MatchToken("0_inf", "10,1,").Length); // Two elements, without trailing
			Assert.Equal(8, parser.MatchToken("0_inf", "10,1,999").Length); // Three elements
		}

		[Fact]
		public void ZeroOrMore_Default_ValueCalc()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("0_inf")
				.ZeroOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','))
				.Pass(v =>
				{
					var numbers = v.Cast<int>();
					var sum = numbers.Sum();
					return sum;
				});

			var parser = builder.Build();

			void Check(string token, int length, int value, string input)
			{
				var result = parser.MatchToken(token, input);
				Assert.Equal(length, result.Length);
				Assert.Equal(value, result.IntermediateValue);
			}

			Check("0_inf", 0, 0, "");
			Check("0_inf", 0, 0, ",");
			Check("0_inf", 2, 10, "10");
			Check("0_inf", 2, 10, "10,");
			Check("0_inf", 4, 11, "10,1");
			Check("0_inf", 4, 11, "10,1,");
			Check("0_inf", 8, 1010, "10,1,999");
		}

		[Fact]
		public void OneOrMore_Default()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("1_inf")
				.OneOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','));

			var parser = builder.Build();

			Assert.Throws<ParsingException>(() => parser.MatchToken("1_inf", ""));
			Assert.Throws<ParsingException>(() => parser.MatchToken("1_inf", ","));
			Assert.Equal(2, parser.MatchToken("1_inf", "10").Length);
			Assert.Equal(2, parser.MatchToken("1_inf", "10,").Length);
			Assert.Equal(4, parser.MatchToken("1_inf", "10,1").Length);
			Assert.Equal(4, parser.MatchToken("1_inf", "10,1,").Length);
			Assert.Equal(8, parser.MatchToken("1_inf", "10,1,999").Length);
		}

		[Fact]
		public void OneOrMore_ValueCalc()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("1_inf")
				.OneOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','))
				.Pass(v => v.Cast<int>().Aggregate((a, b) => a * b));

			var parser = builder.Build();

			void Check(string token, int length, int value, string input)
			{
				var result = parser.MatchToken(token, input);
				Assert.Equal(length, result.Length);
				Assert.Equal(value, result.IntermediateValue);
			}

			Assert.Throws<ParsingException>(() => parser.MatchToken("1_inf", ""));
			Check("1_inf", 1, 3, "3");
			Check("1_inf", 3, 3 * 7, "3,7");
			Check("1_inf", 5, 3 * 7 * 2, "3,7,2");
		}

		[Fact]
		public void Limited_Default()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("0_1")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 0, max: 1);
			builder.CreateToken("1_1")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 1, max: 1);
			builder.CreateToken("2_3")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 2, max: 3);

			var parser = builder.Build();

			Assert.Equal(0, parser.MatchToken("0_1", "").Length);
			Assert.Equal(2, parser.MatchToken("0_1", "42").Length);
			Assert.Equal(2, parser.MatchToken("0_1", "42,").Length);
			Assert.Equal(2, parser.MatchToken("0_1", "42,99").Length);

			Assert.Throws<ParsingException>(() => parser.MatchToken("1_1", ""));
			Assert.Equal(1, parser.MatchToken("1_1", "5").Length);
			Assert.Equal(1, parser.MatchToken("1_1", "5,").Length);
			Assert.Equal(1, parser.MatchToken("1_1", "5,7").Length);

			Assert.Throws<ParsingException>(() => parser.MatchToken("2_3", ""));
			Assert.Throws<ParsingException>(() => parser.MatchToken("2_3", "10"));
			Assert.Equal(5, parser.MatchToken("2_3", "10,20").Length);
			Assert.Equal(8, parser.MatchToken("2_3", "10,20,30").Length);
			Assert.Equal(8, parser.MatchToken("2_3", "10,20,30,40").Length);
		}

		[Fact]
		public void Limited_ValueCalc()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("2_3")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 2, max: 3)
				.Pass(v =>
				{
					var nums = v.Cast<int>().ToArray();
					return nums.Average();
				});

			var parser = builder.Build();

			void Check(string token, int length, double value, string input)
			{
				var result = parser.MatchToken(token, input);
				Assert.Equal(length, result.Length);
				Assert.Equal(value, (double)result.IntermediateValue!);
			}

			Assert.Throws<ParsingException>(() => parser.MatchToken("2_3", "5"));
			Check("2_3", 4, (5 + 10) / 2.0, "5,10 5");
			Check("2_3", 5, (2 + 4 + 6) / 3.0, "2,4,6 aaa");
			Check("2_3", 5, (2 + 4 + 6) / 3.0, "2,4,6,8");
		}

		[Fact]
		public void ZeroOrMore_AllowTrailing()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("0_inf_trail")
				.ZeroOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','), allowTrailingSeparator: true);

			var parser = builder.Build();

			Assert.Equal(0, parser.MatchToken("0_inf_trail", "").Length);
			Assert.Equal(0, parser.MatchToken("0_inf_trail", ",").Length);
			Assert.Equal(2, parser.MatchToken("0_inf_trail", "10").Length);
			Assert.Equal(3, parser.MatchToken("0_inf_trail", "10,").Length);
			Assert.Equal(4, parser.MatchToken("0_inf_trail", "10,1").Length);
			Assert.Equal(5, parser.MatchToken("0_inf_trail", "10,1,").Length);
			Assert.Equal(8, parser.MatchToken("0_inf_trail", "10,1,999 22").Length);
		}

		[Fact]
		public void OneOrMore_AllowTrailing()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("1_inf_trail")
				.OneOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','), allowTrailingSeparator: true);

			var parser = builder.Build();

			Assert.Throws<ParsingException>(() => parser.MatchToken("1_inf_trail", ""));
			Assert.Throws<ParsingException>(() => parser.MatchToken("1_inf_trail", ","));
			Assert.Equal(2, parser.MatchToken("1_inf_trail", "10").Length);
			Assert.Equal(3, parser.MatchToken("1_inf_trail", "10,").Length);
			Assert.Equal(4, parser.MatchToken("1_inf_trail", "10,1").Length);
			Assert.Equal(5, parser.MatchToken("1_inf_trail", "10,1,").Length);
			Assert.Equal(8, parser.MatchToken("1_inf_trail", "10,1,999 22").Length);
		}

		[Fact]
		public void Limited_AllowTrailing()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("0_2_trailing")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 0, max: 2, allowTrailingSeparator: true);
			builder.CreateToken("1_1_trailing")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 1, max: 1, allowTrailingSeparator: true);
			builder.CreateToken("2_5_trailing")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 2, max: 5, allowTrailingSeparator: true);

			var parser = builder.Build();

			Assert.Equal(0, parser.MatchToken("0_2_trailing", "").Length);
			Assert.Equal(2, parser.MatchToken("0_2_trailing", "10").Length);
			Assert.Equal(3, parser.MatchToken("0_2_trailing", "10,").Length);
			Assert.Equal(5, parser.MatchToken("0_2_trailing", "10,10").Length);
			Assert.Equal(6, parser.MatchToken("0_2_trailing", "10,10,").Length);
			Assert.Equal(6, parser.MatchToken("0_2_trailing", "10,10,10").Length);

			Assert.Throws<ParsingException>(() => parser.MatchToken("1_1_trailing", ""));
			Assert.Equal(2, parser.MatchToken("1_1_trailing", "10").Length);
			Assert.Equal(3, parser.MatchToken("1_1_trailing", "10,").Length);
			Assert.Equal(3, parser.MatchToken("1_1_trailing", "10,10").Length);

			Assert.Throws<ParsingException>(() => parser.MatchToken("2_5_trailing", "10"));
			Assert.Equal(5, parser.MatchToken("2_5_trailing", "10,10").Length);
			Assert.Equal(6, parser.MatchToken("2_5_trailing", "10,10,").Length);
			Assert.Equal(8, parser.MatchToken("2_5_trailing", "10,10,10").Length);
			Assert.Equal(9, parser.MatchToken("2_5_trailing", "10,10,10,").Length);
			Assert.Equal(14, parser.MatchToken("2_5_trailing", "10,10,10,10,10").Length);
			Assert.Equal(15, parser.MatchToken("2_5_trailing", "10,10,10,10,10,").Length);
			Assert.Equal(15, parser.MatchToken("2_5_trailing", "10,10,10,10,10,10").Length);
		}

		[Fact]
		public void Arithmetic_Expression_SumSequence()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("expr")
				.OneOrMoreSeparated(
					b => b.Number<int>(),
					b => b.LiteralChoice("+", "-"),
					includeSeparatorsInResult: true
				)
				.Pass(v =>
				{
					var seq = v.ToArray();
					int acc = Convert.ToInt32(seq[0]);
					for (int i = 1; i < seq.Length; i += 2)
					{
						var op = (string)seq[i]!;
						var num = Convert.ToInt32(seq[i + 1]);
						acc = op switch
						{
							"+" => acc + num,
							"-" => acc - num,
							_ => acc
						};
					}
					return acc;
				});

			var parser = builder.Build();

			void Check(string token, int length, int value, string input)
			{
				var result = parser.MatchToken(token, input);
				Assert.Equal(length, result.Length);
				Assert.Equal(value, result.IntermediateValue);
			}

			Check("expr", 1, 3, "3");               // single number
			Check("expr", 3, 8, "3+5");             // addition
			Check("expr", 3, -2, "3-5");            // subtraction
			Check("expr", 5, 6, "3+5-2");           // chained
			Check("expr", 6, 16, "10+5+1");         // multiple additions
			Check("expr", 8, 4, "10-5+1-2");        // mixed operations
		}

		[Fact]
		public void Arithmetic_Expression_WithSpaces_AndTrailingSeparator()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("expr_trailing")
				.OneOrMoreSeparated(
					b => b.Number<int>(),
					b => b.LiteralChoice("+", "-"),
					includeSeparatorsInResult: true,
					allowTrailingSeparator: true
				)
				.Pass(v =>
				{
					var seq = v.ToArray();
					int acc = Convert.ToInt32(seq[0]);
					for (int i = 1; i < seq.Length - 1; i += 2)
					{
						var op = (string)seq[i]!;
						var num = Convert.ToInt32(seq[i + 1]);
						acc = op == "+" ? acc + num : acc - num;
					}
					return acc;
				});

			var parser = builder.Build();

			void Check(string token, int length, int value, string input)
			{
				var result = parser.MatchToken(token, input);
				Assert.Equal(length, result.Length);
				Assert.Equal(value, result.IntermediateValue);
			}

			Check("expr_trailing", 1, 7, "7");
			Check("expr_trailing", 3, 12, "7+5");
			Check("expr_trailing", 4, 12, "7+5+");       // trailing separator
			Check("expr_trailing", 4, 12, "7+5+a");       // trailing separator with garbage
			Check("expr_trailing", 5, 10, "7+5-2");
			Check("expr_trailing", 6, 10, "7+5-2+");     // trailing separator after chain
			Check("expr_trailing", 7, 0, "10-5-5+");     // more trailing combos
			Check("expr_trailing", 9, 20, "10+5+3+2+"); // longer sequence with trailing
		}
	}
}