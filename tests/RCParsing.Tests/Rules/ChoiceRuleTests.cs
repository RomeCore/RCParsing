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
		public void First_Simple()
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
		public void First_OrderMatters()
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
		public void Longest_Operators()
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
		public void First_EmptyInput()
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
		public void First_NoMatchingAlternative()
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
		public void First_WithOptional()
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
		public void Longest_OverlappingPatterns()
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
		public void First_MixedRulesAndTokens()
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
		public void Shortest_Behavior()
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
		public void First_WithWhitespaceHandling()
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
		public void First_NestedChoices()
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

		[Fact]
		public void First_Lookahead()
		{
			var builder = new ParserBuilder();

			builder.Settings.UseFirstCharacterMatch().RecordWalkTrace();

			builder.CreateMainRule()
				.Choice(
					 b => b.Literal("{"),
					 b => b.Literal("["),
					 b => b.Regex("0"), // Non-deterministic
					 b => b.Literal("("),
					 b => b.Empty() // It's also non-deterministic
				);

			var parser = builder.Build();

			var ast1 = parser.Parse("{");
			Assert.Equal(1, ast1.Length);
			Assert.Equal(4, ast1.Context.walkTrace.Count);

			var ast2 = parser.Parse("[");
			Assert.Equal(1, ast2.Length);
			Assert.Equal(4, ast2.Context.walkTrace.Count);

			var ast3 = parser.Parse("0");
			Assert.Equal(1, ast3.Length);
			Assert.Equal(4, ast3.Context.walkTrace.Count);

			var ast4 = parser.Parse("");
			Assert.Equal(0, ast4.Length);
			Assert.Equal(6, ast4.Context.walkTrace.Count);

			var ast5 = parser.Parse("x");
			Assert.Equal(0, ast5.Length);
			// It will check all of non-deterministic choices and match empty choice
			Assert.Equal(6, ast5.Context.walkTrace.Count);

			// First it will check non-deterministic choice (our Regex), then will go to '('
			var ast6 = parser.Parse("(");
			Assert.Equal(1, ast6.Length);
			Assert.Equal(6, ast6.Context.walkTrace.Count);
		}
	}
}