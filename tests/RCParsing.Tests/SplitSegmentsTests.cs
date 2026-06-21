namespace RCParsing.Tests
{
	/// <summary>
	/// Tests for <see cref="Parser.SplitSegments"/> method.
	/// </summary>
	public class SplitSegmentsTests
	{
		[Fact]
		public void SplitSegments_Basic()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var input = "a 1 b 2 c";

			var segments = builder.Build().SplitSegments(input).ToList();

			Assert.Equal(3, segments.Count);
			Assert.Equal("a ", segments[0].Slice);
			Assert.Equal(" b ", segments[1].Slice);
			Assert.Equal(" c", segments[2].Slice);
		}

		[Fact]
		public void SplitSegments_NoMatches_ReturnsWholeInput()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var segments = builder.Build().SplitSegments("hello").ToList();

			Assert.Single(segments);
			Assert.Equal("hello", segments[0].Slice);
		}

		[Fact]
		public void SplitSegments_ByRuleAlias()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("n").Number<int>();
			builder.CreateRule("val").Token("n");
			builder.CreateMainRule().Rule("val");

			var segments = builder.Build().SplitSegments("val", "x 1 y").ToList();

			// One match => two segments: before and after
			Assert.Equal(2, segments.Count);
			Assert.Equal("x ", segments[0].Slice);
			Assert.Equal(" y", segments[1].Slice);
		}

		[Fact]
		public void SplitSegments_WithContext()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var parser = builder.Build();
			var context = parser.CreateContext("q 8 r");

			var segments = parser.SplitSegments(context).ToList();

			// One match => two segments: before and after
			Assert.Equal(2, segments.Count);
			Assert.Equal("q ", segments[0].Slice);
			Assert.Equal(" r", segments[1].Slice);
		}

		[Fact]
		public void SplitSegments_MainRuleNotSet_Throws()
		{
			var parser = new ParserBuilder().Build();

			Assert.Throws<InvalidOperationException>(() => parser.SplitSegments("test"));
		}

		[Fact]
		public void SplitSegments_InvalidAlias_Throws()
		{
			var builder = new ParserBuilder();
			builder.CreateMainRule().Literal("a");
			var parser = builder.Build();

			Assert.Throws<ArgumentException>(() => parser.SplitSegments("nonexistent", "test"));
		}

		[Fact]
		public void SplitSegments_EmptyInput_ReturnsEmpty()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number").Number<int>();
			builder.CreateMainRule().Token("number");

			var segments = builder.Build().SplitSegments("").ToList();

			Assert.Empty(segments);
		}
	}
}
