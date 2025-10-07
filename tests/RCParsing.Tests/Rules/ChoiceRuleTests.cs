using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;

namespace RCParsing.Tests.Rules
{
	public class ChoiceRuleTests
	{
		[Fact]
		public void FirstChoiceTest_Simple()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Choice(
					b => b.Keyword("a").Transform(_ => 1),
					b => b.Keyword("ab").Transform(_ => 2),
					b => b.Literal("a").Transform(_ => 3)
				);

			var parser = builder.Build();

			var value1 = parser.Parse<int>("a");
			Assert.Equal(1, value1);
			var value2 = parser.Parse<int>("ab");
			Assert.Equal(2, value2);
			var value3 = parser.Parse<int>("abc");
			Assert.Equal(3, value3);
			var value4 = parser.Parse<int>("ac");
			Assert.Equal(3, value4);
		}

		[Fact]
		public void FirstChoiceTest_OrderMatters()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Choice(
					b => b.Keyword("if").Transform(_ => "keyword"),
					b => b.Identifier().Transform(_ => "identifier")
				);

			var parser = builder.Build();

			var result = parser.Parse<string>("if");
			Assert.Equal("keyword", result);
		}

		[Fact]
		public void LongestChoiceTest_Operators()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.LongestChoice(
					b => b.Literal("+").Transform(_ => "plus"),
					b => b.Literal("++").Transform(_ => "increment"),
					b => b.Literal("+=").Transform(_ => "plus_assign")
				);

			var parser = builder.Build();

			var result1 = parser.Parse<string>("++");
			Assert.Equal("increment", result1);

			var result2 = parser.Parse<string>("+");
			Assert.Equal("plus", result2);

			var result3 = parser.Parse<string>("+=");
			Assert.Equal("plus_assign", result3);
		}

		[Fact]
		public void ChoiceTest_EmptyInput()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Choice(
					b => b.Keyword("a").Transform(_ => 1),
					b => b.Keyword("b").Transform(_ => 2)
				);

			var parser = builder.Build();

			Assert.Throws<ParsingException>(() => parser.Parse<int>(""));
		}

		[Fact]
		public void ChoiceTest_NoMatchingAlternative()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Choice(
					b => b.Keyword("apple").Transform(_ => 1),
					b => b.Keyword("banana").Transform(_ => 2)
				);

			var parser = builder.Build();

			Assert.Throws<ParsingException>(() => parser.Parse<int>("cherry"));
		}

		[Fact]
		public void ChoiceTest_WithOptional()
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespaces();

			builder.CreateMainRule()
				.Choice(
					b => b
						.Keyword("func")
						.Identifier()
						.Transform(ctx => $"function_{ctx[1]}"),
					b => b.Identifier().Transform(id => $"variable_{id}"),
					b => b.Number().Transform(num => $"number_{num}")
				);

			var parser = builder.Build();

			var result1 = parser.Parse<string>("func main");
			Assert.Equal("function_main", result1);

			var result2 = parser.Parse<string>("x");
			Assert.Equal("variable_x", result2);

			var result3 = parser.Parse<string>("42");
			Assert.Equal("number_42", result3);
		}

		[Fact]
		public void LongestChoiceTest_OverlappingPatterns()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.LongestChoice(
					b => b.Literal("[").Transform(_ => "single_bracket"),
					b => b.Literal("[[").Transform(_ => "double_bracket")
				);

			var parser = builder.Build();

			var result1 = parser.Parse<string>("[[");
			Assert.Equal("double_bracket", result1);

			var result2 = parser.Parse<string>("[");
			Assert.Equal("single_bracket", result2);
		}

		[Fact]
		public void ChoiceTest_MixedRulesAndTokens()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("string_literal")
				.Literal("\"")
				.Regex(@"[^\""]*")
				.Literal("\"")
				.Transform(ctx => $"string:{ctx[1]}");

			builder.CreateRule("number_literal")
				.Regex(@"\d+")
				.Transform(num => $"number:{num}");

			builder.CreateMainRule()
				.Choice(
					b => b.Rule("string_literal"),
					b => b.Rule("number_literal"),
					b => b.Identifier().Transform(id => $"id:{id}")
				);

			var parser = builder.Build();

			var result1 = parser.Parse<string>("\"hello\"");
			Assert.Equal("string:hello", result1);

			var result2 = parser.Parse<string>("123");
			Assert.Equal("number:123", result2);

			var result3 = parser.Parse<string>("variable");
			Assert.Equal("id:variable", result3);
		}

		[Fact]
		public void ShortestChoiceTest_Behavior()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Choice(ChoiceMode.Shortest,
					b => b.Regex(@"a+").Transform(_ => "long_a"),
					b => b.Regex(@"a").Transform(_ => "single_a")
				);

			var parser = builder.Build();

			var result = parser.Parse<string>("aaa");
			Assert.Equal("single_a", result);
		}

		[Fact]
		public void ChoiceTest_WithWhitespaceHandling()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateMainRule()
				.Choice(
					b => b.Keyword("let").Transform(_ => "declaration"),
					b => b.Keyword("set").Transform(_ => "assignment"),
					b => b.Keyword("get").Transform(_ => "access")
				);

			var parser = builder.Build();

			var result = parser.Parse<string>("  let  ");
			Assert.Equal("declaration", result);
		}

		[Fact]
		public void ChoiceTest_NestedChoices()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Choice(
					b => b.Choice(
						b1 => b1.Keyword("true").Transform(_ => true),
						b1 => b1.Keyword("false").Transform(_ => false)
					),
					b => b.Number().Transform(_ => "number")
				);

			var parser = builder.Build();

			var result1 = parser.Parse<object>("true");
			Assert.Equal(true, result1);

			var result2 = parser.Parse<object>("42");
			Assert.Equal("number", result2);
		}
	}
}