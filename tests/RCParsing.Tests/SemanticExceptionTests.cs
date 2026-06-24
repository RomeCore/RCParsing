using Xunit.Abstractions;

namespace RCParsing.Tests
{
	public class SemanticExceptionTests(ITestOutputHelper output)
	{
		[Fact]
		public void SemanticExceptions_SimpleTest()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Literal("Hello!")

				.Transform(v =>
				{
					throw new SemanticException(v, "Invalid message!");
				});

			var parser = builder.Build();

			Assert.True(parser.TryParse("Hello!", out var result));
			var ex = Assert.Throws<SemanticException>(() => result.Value);
			output.WriteLine(ex.ToString());
		}

		[Fact]
		public void SemanticExceptions_SingleCharFormatting()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Literal("a")

				.Transform(v =>
				{
					throw new SemanticException(v, "Invalid message!");
				});

			var parser = builder.Build();

			Assert.True(parser.TryParse("a", out var result));
			var ex = Assert.Throws<SemanticException>(() => result.Value);
			var exstr = ex.ToString();
			output.WriteLine(exstr);

			Assert.Contains("Invalid message!", exstr);
			Assert.Contains("line 1", exstr);
			Assert.Contains("column 1", exstr);
			Assert.DoesNotContain("length", exstr);
		}

		[Fact]
		public void SemanticExceptions_MultipleCharsFormatting()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Literal("abc")

				.Transform(v =>
				{
					throw new SemanticException(v, "Invalid message!");
				});

			var parser = builder.Build();

			Assert.True(parser.TryParse("abc", out var result));
			var ex = Assert.Throws<SemanticException>(() => result.Value);
			var exstr = ex.ToString();
			output.WriteLine(exstr);

			Assert.Contains("Invalid message!", exstr);
			Assert.Contains("line 1", exstr);
			Assert.Contains("column 1", exstr);
			Assert.Contains("length 3", exstr);
		}

		[Fact]
		public void SemanticExceptions_DifferentLinesFormatting()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Literal("abc")
				.Newline()
				.Literal("def")

				.Transform(v =>
				{
					throw new SemanticException(v, "Invalid message!");
				});

			var parser = builder.Build();

			Assert.True(parser.TryParse("abc\ndef", out var result));
			var ex = Assert.Throws<SemanticException>(() => result.Value);
			var exstr = ex.ToString();
			output.WriteLine(exstr);

			Assert.Contains("Invalid message!", exstr);
			Assert.Contains("line 1", exstr);
			Assert.Contains("column 1", exstr);
			Assert.Contains("line 2", exstr);
			Assert.Contains("column 3", exstr);
			Assert.Contains("length 7", exstr);
		}

		[Fact]
		public void SemanticExceptions_DifferentLinesFormatting_WithLineGap()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.Literal("abc")
				.Newline()
				.Literal("def")
				.Newline()
				.Literal("ghi")

				.Transform(v =>
				{
					throw new SemanticException(v, "Invalid message!");
				});

			var parser = builder.Build();

			Assert.True(parser.TryParse("abc\ndef\nghi", out var result));
			var ex = Assert.Throws<SemanticException>(() => result.Value);
			var exstr = ex.ToString();
			output.WriteLine(exstr);

			Assert.Contains("Invalid message!", exstr);
			Assert.Contains("line 1", exstr);
			Assert.Contains("column 1", exstr);
			Assert.Contains("...", exstr);
			Assert.Contains("line 3", exstr);
			Assert.Contains("column 3", exstr);
			Assert.Contains("length 11", exstr);
		}
	}
}
