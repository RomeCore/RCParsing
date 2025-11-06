using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.Rules
{
	public class SeparatedRepeatRuleTests
	{
		[Fact]
		public void ZeroOrMore_Default()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();
			builder.CreateMainRule()
				.ZeroOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','));
			var parser = builder.Build();

			void Check(string input, int length, int childCount)
			{
				var ast = parser.Parse(input);
				Assert.Equal(length, ast.Length);
				Assert.Equal(childCount, ast.Count);
			}

			Check("", 0, 0); // Empty input
			Check(",", 0, 0); // Just a separator
			Check("10", 2, 1); // Just one element
			Check("10,", 2, 1); // Do not catch trailing separator
			Check("10,1", 4, 2); // Two elements
			Check("10,1,", 4, 2); // Two elements, without trailing
			Check("10,1,999", 8, 3); // Three elements
		}

		[Fact]
		public void OneOrMore_Default()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();
			builder.CreateMainRule()
				.OneOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','));
			var parser = builder.Build();

			void Check(string input, int length, int childCount)
			{
				var ast = parser.Parse(input);
				Assert.Equal(length, ast.Length);
				Assert.Equal(childCount, ast.Count);
			}

			Assert.Throws<ParsingException>(() => parser.Parse("")); // Empty input
			Assert.Throws<ParsingException>(() => parser.Parse(",")); // Just a separator

			Check("10", 2, 1); // Just one element
			Check("10,", 2, 1); // Do not catch trailing separator
			Check("10,1", 4, 2); // Two elements
			Check("10,1,", 4, 2); // Two elements, without trailing
			Check("10,1,999", 8, 3); // Three elements
		}

		[Fact]
		public void OneOrMore_NewlineSeparators()
		{
			var builder = new ParserBuilder();
			builder.Settings.Skip(b => b.Spaces().ConfigureForSkip());
			builder.CreateMainRule()
				.OneOrMoreSeparated(b => b.Number<int>(), b => b.OneOrMore(b => b.Newline()));
			var parser = builder.Build();

			void Check(string input, int length, int childCount)
			{
				var ast = parser.Parse(input);
				Assert.Equal(length, ast.Length);
				Assert.Equal(childCount, ast.Count);
			}

			Assert.Throws<ParsingException>(() => parser.Parse("")); // Empty input
			Assert.Throws<ParsingException>(() => parser.Parse("\n")); // Just a separator

			Check(
			"""
			90
			""", 2, 1); // Just one element
			Check(
			"""
			90


			""", 2, 1); // Do not catch trailing separators
			Check(
			"""
			90

			1
			""", 7, 2); // Two elements
			Check(
			"""
			90

			1



			""", 7, 2); // Two elements, without trailing
			Check(
			"""
			90
			1
			999


			""", 10, 3); // Three elements
		}

		[Fact]
		public void OneOrMore_NewlineSeparators_WithinSequence()
		{
			var builder = new ParserBuilder();
			builder.Settings.Skip(b => b.Spaces().ConfigureForSkip());
			builder.CreateMainRule()
				.OneOrMoreSeparated(b => b.Number<int>(), b => b.OneOrMore(b => b.Newline()))
				.Optional(b => b.Whitespaces())
				.EOF();
			var parser = builder.Build();

			void Check(string input, int length, int childCount)
			{
				var ast = parser.Parse(input)[0];
				Assert.Equal(length, ast.Length);
				Assert.Equal(childCount, ast.Count);
			}

			Assert.Throws<ParsingException>(() => parser.Parse("")); // Empty input
			Assert.Throws<ParsingException>(() => parser.Parse("\n")); // Just a separator

			Check(
			"""
			90
			""", 2, 1); // Just one element
			Check(
			"""
			90


			""", 2, 1); // Do not catch trailing separators
			Check(
			"""
			90

			1
			""", 7, 2); // Two elements
			Check(
			"""
			90

			1



			""", 7, 2); // Two elements, without trailing
			Check(
			"""
			90
			1
			999


			""", 10, 3); // Three elements
		}

		[Fact]
		public void Limited_Default()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("0_0")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 0, max: 0);
			builder.CreateRule("0_1")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 0, max: 1);
			builder.CreateRule("0_2")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 0, max: 2);
			builder.CreateRule("1_1")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 1, max: 1);
			builder.CreateRule("2_2")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 2, max: 2);
			builder.CreateRule("2_5")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 2, max: 5);

			var parser = builder.Build();

			// 0 to 0
			Assert.Equal(0, parser.ParseRule("0_0", "").Length);
			Assert.Equal(0, parser.ParseRule("0_0", ",").Length);
			Assert.Equal(0, parser.ParseRule("0_0", "10").Length);
			Assert.Equal(0, parser.ParseRule("0_0", "10,").Length);
			
			// 0 to 1
			Assert.Equal(0, parser.ParseRule("0_1", "").Length);
			Assert.Equal(0, parser.ParseRule("0_1", ",").Length);
			Assert.Equal(2, parser.ParseRule("0_1", "10").Length);
			Assert.Equal(2, parser.ParseRule("0_1", "10,").Length);
			Assert.Equal(2, parser.ParseRule("0_1", "10,10").Length);
			
			// 0 to 2
			Assert.Equal(0, parser.ParseRule("0_2", "").Length);
			Assert.Equal(0, parser.ParseRule("0_2", ",").Length);
			Assert.Equal(2, parser.ParseRule("0_2", "10").Length);
			Assert.Equal(2, parser.ParseRule("0_2", "10,").Length);
			Assert.Equal(5, parser.ParseRule("0_2", "10,10").Length);
			Assert.Equal(5, parser.ParseRule("0_2", "10,10,").Length);
			Assert.Equal(5, parser.ParseRule("0_2", "10,10,10").Length);

			// 1 to 1
			Assert.Throws<ParsingException>(() => parser.ParseRule("1_1", ""));
			Assert.Throws<ParsingException>(() => parser.ParseRule("1_1", ","));
			Assert.Equal(2, parser.ParseRule("1_1", "10").Length);
			Assert.Equal(2, parser.ParseRule("1_1", "10,").Length);
			Assert.Equal(2, parser.ParseRule("1_1", "10,10").Length);

			// 2 to 2
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_2", ""));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_2", ","));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_2", "10"));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_2", "10,"));
			Assert.Equal(5, parser.ParseRule("2_2", "10,10").Length);
			Assert.Equal(5, parser.ParseRule("2_2", "10,10,").Length);
			Assert.Equal(5, parser.ParseRule("2_2", "10,10,10").Length);

			// 2 to 5
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_5", ""));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_5", ","));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_5", "10"));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_5", "10,"));
			Assert.Equal(5, parser.ParseRule("2_5", "10,10").Length);
			Assert.Equal(5, parser.ParseRule("2_5", "10,10,").Length);
			Assert.Equal(8, parser.ParseRule("2_5", "10,10,10").Length);
			Assert.Equal(8, parser.ParseRule("2_5", "10,10,10,").Length);
			Assert.Equal(11, parser.ParseRule("2_5", "10,10,10,10").Length);
			Assert.Equal(11, parser.ParseRule("2_5", "10,10,10,10,").Length);
			Assert.Equal(14, parser.ParseRule("2_5", "10,10,10,10,10").Length);
			Assert.Equal(14, parser.ParseRule("2_5", "10,10,10,10,10,").Length);
			Assert.Equal(14, parser.ParseRule("2_5", "10,10,10,10,10,10").Length);

			// Whitespaces
			Assert.Equal(0, parser.ParseRule("0_0", " ").Length);
			Assert.Equal(0, parser.ParseRule("0_1", " ").Length);
			Assert.Equal(2, parser.ParseRule("0_1", " 10 ").Length);
			Assert.Equal(7, parser.ParseRule("0_2", " 10 , 10 ").Length);
			Assert.Equal(8, parser.ParseRule("0_2", " 10 , 10 ").EndIndex);
			Assert.Equal(7, parser.ParseRule("2_2", " 10 , 10 ").Length);
			Assert.Equal(8, parser.ParseRule("2_2", " 10 , 10 ").EndIndex);
			Assert.Equal(12, parser.ParseRule("2_5", " 10 , 10 , 10 ").Length);
			Assert.Equal(13, parser.ParseRule("2_5", " 10 , 10 , 10 ").EndIndex);

			// Different numbers
			Assert.Equal(3, parser.ParseRule("2_2", "1,2").Length);
			Assert.Equal(5, parser.ParseRule("2_5", "1,2,3").Length);
			Assert.Equal(15, parser.ParseRule("2_5", "123,456,789,999").Length);

			// Edge cases with min and max counts
			Assert.Equal(0, parser.ParseRule("0_1", "").Length); // min = 0
			Assert.Equal(1, parser.ParseRule("0_1", "1").Length); // max = 1
			Assert.Equal(9, parser.ParseRule("2_5", "1,2,3,4,5").Length); // max = 5
			Assert.Equal(9, parser.ParseRule("2_5", "1,2,3,4,5,6").Length); // > max = 5
		}

		[Fact]
		public void ZeroOrMore_AllowTrailing()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();
			builder.CreateMainRule()
				.ZeroOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','), allowTrailingSeparator: true);
			var parser = builder.Build();

			Assert.Equal(0, parser.Parse("").Length); // Empty input
			Assert.Equal(0, parser.Parse(",").Length); // Just a separator
			Assert.Equal(2, parser.Parse("10").Length); // Just one element
			Assert.Equal(3, parser.Parse("10,").Length); // Catch trailing separator
			Assert.Equal(4, parser.Parse("10,1").Length); // Two elements
			Assert.Equal(5, parser.Parse("10,1,").Length); // Two elements, with trailing
			Assert.Equal(8, parser.Parse("10,1,999 22").Length); // Three elements with garbage ahead
		}

		[Fact]
		public void OneOrMore_AllowTrailing()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();
			builder.CreateMainRule()
				.OneOrMoreSeparated(b => b.Number<int>(), b => b.Literal(','), allowTrailingSeparator: true);
			var parser = builder.Build();

			Assert.Throws<ParsingException>(() => parser.Parse("")); // Empty input
			Assert.Throws<ParsingException>(() => parser.Parse(",")); // Just a separator
			Assert.Equal(2, parser.Parse("10").Length); // Just one element
			Assert.Equal(3, parser.Parse("10,").Length); // Catch trailing separator
			Assert.Equal(4, parser.Parse("10,1").Length); // Two elements
			Assert.Equal(5, parser.Parse("10,1,").Length); // Two elements, with trailing
			Assert.Equal(8, parser.Parse("10,1,999 22").Length); // Three elements with garbage ahead
		}

		[Fact]
		public void Limited_AllowTrailing()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("0_0_trailing")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 0, max: 0, allowTrailingSeparator: true);
			builder.CreateRule("0_1_trailing")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 0, max: 1, allowTrailingSeparator: true);
			builder.CreateRule("0_2_trailing")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 0, max: 2, allowTrailingSeparator: true);
			builder.CreateRule("1_1_trailing")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 1, max: 1, allowTrailingSeparator: true);
			builder.CreateRule("2_5_trailing")
				.RepeatSeparated(b => b.Number<int>(), b => b.Literal(','), min: 2, max: 5, allowTrailingSeparator: true);

			var parser = builder.Build();

			// 0 to 0 with trailing
			Assert.Equal(0, parser.ParseRule("0_0_trailing", "").Length);
			Assert.Equal(0, parser.ParseRule("0_0_trailing", ",").Length);
			Assert.Equal(0, parser.ParseRule("0_0_trailing", "10").Length);

			// 0 to 1 with trailing
			Assert.Equal(0, parser.ParseRule("0_1_trailing", "").Length);
			Assert.Equal(0, parser.ParseRule("0_1_trailing", ",").Length);
			Assert.Equal(2, parser.ParseRule("0_1_trailing", "10").Length);
			Assert.Equal(3, parser.ParseRule("0_1_trailing", "10,").Length); // Trailing included
			Assert.Equal(3, parser.ParseRule("0_1_trailing", "10,10").Length);
			Assert.Equal(3, parser.ParseRule("0_1_trailing", "10,10,").Length);
			
			// 0 to 2 with trailing
			Assert.Equal(0, parser.ParseRule("0_2_trailing", "").Length);
			Assert.Equal(0, parser.ParseRule("0_2_trailing", ",").Length);
			Assert.Equal(2, parser.ParseRule("0_2_trailing", "10").Length);
			Assert.Equal(3, parser.ParseRule("0_2_trailing", "10,").Length); // Trailing included
			Assert.Equal(5, parser.ParseRule("0_2_trailing", "10,10").Length);
			Assert.Equal(6, parser.ParseRule("0_2_trailing", "10,10,").Length); // Trailing included
			Assert.Equal(6, parser.ParseRule("0_2_trailing", "10,10,10").Length); // Max 2 elements with trailing

			// 1 to 1 with trailing
			Assert.Throws<ParsingException>(() => parser.ParseRule("1_1_trailing", ""));
			Assert.Throws<ParsingException>(() => parser.ParseRule("1_1_trailing", ","));
			Assert.Equal(2, parser.ParseRule("1_1_trailing", "10").Length);
			Assert.Equal(3, parser.ParseRule("1_1_trailing", "10,").Length); // Trailing included
			Assert.Equal(3, parser.ParseRule("1_1_trailing", "10,10").Length); // Only one element with trailing

			// 2 to 5 with trailing
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_5_trailing", ""));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_5_trailing", ","));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_5_trailing", "10"));
			Assert.Throws<ParsingException>(() => parser.ParseRule("2_5_trailing", "10,"));
			Assert.Equal(5, parser.ParseRule("2_5_trailing", "10,10").Length);
			Assert.Equal(6, parser.ParseRule("2_5_trailing", "10,10,").Length); // Trailing included
			Assert.Equal(8, parser.ParseRule("2_5_trailing", "10,10,10").Length);
			Assert.Equal(9, parser.ParseRule("2_5_trailing", "10,10,10,").Length); // Trailing included
			Assert.Equal(14, parser.ParseRule("2_5_trailing", "10,10,10,10,10").Length);
			Assert.Equal(15, parser.ParseRule("2_5_trailing", "10,10,10,10,10,").Length); // Trailing included
			Assert.Equal(15, parser.ParseRule("2_5_trailing", "10,10,10,10,10,10").Length); // Max 5 elements with trailing
		}
	}
}