using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.Rules
{
	public class FirstCharCalculation
	{
		[Fact]
		public void TokenCombinators_First()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("1")
				.First(
					b => b.Optional(b => b.Literal("Ab")),
					b => b.Literal("Bc")
				);

			builder.CreateToken("2")
				.First(
					b => b.Literal("Ab"),
					b => b.Literal("Bc")
				);

			builder.CreateToken("3")
				.First(
					b => b.Optional(b => b.Number<uint>()),
					b => b.Literal("Bc")
				);

			builder.CreateToken("4")
				.First(
					b => b.Optional(b => b.Literal("Ab", StringComparison.OrdinalIgnoreCase)),
					b => b.Literal("Bc")
				);

			var parser = builder.Build();

			Assert.Equal(new HashSet<char> ([ 'A', 'B']), parser.GetTokenPattern("1").FirstChars);
			Assert.Equal(new HashSet<char> ([ 'A' ]), parser.GetTokenPattern("2").FirstChars);
			Assert.Equal(new HashSet<char> ([ '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'B' ]), parser.GetTokenPattern("3").FirstChars);
			Assert.Equal(new HashSet<char>([ 'A', 'a', 'B' ]), parser.GetTokenPattern("4").FirstChars);
		}

		[Fact]
		public void Choice_FirstChars_Combination()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("choice1")
				.Choice(
					b => b.Literal("apple"),
					b => b.Literal("banana"),
					b => b.Literal("cherry")
				);

			builder.CreateToken("choice2")
				.Choice(
					b => b.Literal("123"),
					b => b.Literal("456"),
					b => b.Literal("abc")
				);

			builder.CreateToken("choice3")
				.Choice(
					b => b.Literal("+"),
					b => b.Literal("-"),
					b => b.Literal("*"),
					b => b.Literal("/")
				);

			var parser = builder.Build();

			Assert.Equal(new HashSet<char>(['a', 'b', 'c']), parser.GetTokenPattern("choice1").FirstChars);
			Assert.Equal(new HashSet<char>(['1', '4', 'a']), parser.GetTokenPattern("choice2").FirstChars);
			Assert.Equal(new HashSet<char>(['+', '-', '*', '/']), parser.GetTokenPattern("choice3").FirstChars);
		}

		[Fact]
		public void Optional_FirstChars_Inheritance()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("opt1")
				.Optional(b => b.Literal("hello"));

			builder.CreateToken("opt2")
				.Optional(b => b.Choice(
					b => b.Literal("x"),
					b => b.Literal("y")
				));

			builder.CreateToken("opt3")
				.Optional(b => b.Regex(@"\d+"));

			var parser = builder.Build();

			Assert.Equal(new HashSet<char>(['h']), parser.GetTokenPattern("opt1").FirstChars);
			Assert.Equal(new HashSet<char>(['x', 'y']), parser.GetTokenPattern("opt2").FirstChars);
			Assert.Equal(new HashSet<char>(), parser.GetTokenPattern("opt3").FirstChars); // Non-deterministic
		}

		[Fact]
		public void Sequence_FirstChars_Propagation()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("seq1")
				.Literal("a")
				.Literal("b");

			builder.CreateToken("seq2")
				.Optional(b => b.Literal("opt"))
				.Literal("req");

			builder.CreateToken("seq3")
				.Choice(
					b1 => b1.Literal("x"),
					b1 => b1.Literal("y")
				)
				.Literal("z");

			var parser = builder.Build();

			// FirstChars последовательности - это FirstChars первого элемента
			Assert.Equal(new HashSet<char>(['a']), parser.GetTokenPattern("seq1").FirstChars);
			Assert.Equal(new HashSet<char>(['o', 'r']), parser.GetTokenPattern("seq2").FirstChars); // 'o' от optional, 'r' от required
			Assert.Equal(new HashSet<char>(['x', 'y']), parser.GetTokenPattern("seq3").FirstChars);
		}

		[Fact]
		public void Repeat_FirstChars_Calculation()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("repeat1")
				.Repeat(b => b.Literal("a"), 1, 3);

			builder.CreateToken("repeat2")
				.Repeat(b => b.Choice(
					b => b.Literal("1"),
					b => b.Literal("2")
				), 0, 2);

			builder.CreateToken("repeat3")
				.Repeat(b => b.Optional(b => b.Literal("x")), 1, 1);

			var parser = builder.Build();

			Assert.Equal(new HashSet<char>(['a']), parser.GetTokenPattern("repeat1").FirstChars);
			Assert.Equal(new HashSet<char>(['1', '2']), parser.GetTokenPattern("repeat2").FirstChars);
			Assert.Equal(new HashSet<char>(['x']), parser.GetTokenPattern("repeat3").FirstChars);
		}

		[Fact]
		public void Complex_Nested_FirstChars()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("complex")
				.Optional(b => b.Literal("pre_"))
				.Choice(
					b1 => b1
						.Literal("func")
						.Regex(@"[a-zA-Z_][a-zA-Z0-9_]*"),
					b1 => b1
						.Literal("var")
						.Number<int>()
				)
				.Optional(b => b.Literal("_suffix"));

			var parser = builder.Build();

			// Первые символы: 'p' от optional "pre_", 'f' от "func", 'v' от "var"
			var expected = new HashSet<char>(['p', 'f', 'v']);
			var actual = parser.GetTokenPattern("complex").FirstChars;

			Assert.Equal(expected, actual);
		}

		[Fact]
		public void Empty_FirstChars()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("empty1")
				.Empty();

			builder.CreateToken("empty2")
				.Empty()
				.Literal("a");

			builder.CreateToken("empty3")
				.Choice(
					b => b.Empty(),
					b => b.Literal("b")
				);

			var parser = builder.Build();

			// Empty не добавляет символов в FirstChars
			Assert.Empty(parser.GetTokenPattern("empty1").FirstChars);
			Assert.Equal(new HashSet<char>(['a']), parser.GetTokenPattern("empty2").FirstChars);
			Assert.Equal(new HashSet<char>(['b']), parser.GetTokenPattern("empty3").FirstChars);
		}
	}
}