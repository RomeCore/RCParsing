using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.ParserRules;

namespace RCParsing.Tests.Rules
{
	public class SequenceLabelRuleTests
	{
		[Fact]
		public void SimpleLabelsTest()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.LiteralChoice("Hello", "Hola", "Bonjour").Label("greeting")
				.Spaces() // No label
				.Number<double>().Label("number")
				.EOF()

				.Transform(v =>
				{
					return $"Greeting: {v.GetValue("greeting")}, Number: {v.GetValue("number")}";
				});

			var parser = builder.Build();

			var value1 = parser.Parse<string>("Hello 123");
			var value2 = parser.Parse<string>("Hola 456");
			var value3 = parser.Parse<string>("Bonjour 789");

			Assert.Equal("Greeting: Hello, Number: 123", value1);
			Assert.Equal("Greeting: Hola, Number: 456", value2);
			Assert.Equal("Greeting: Bonjour, Number: 789", value3);
		}

		[Fact]
		public void NoLabelFound_ThrowsException()
		{
			var builder = new ParserBuilder();

			builder.CreateMainRule()
				.LiteralChoice("Hello", "Hola", "Bonjour").Label("greeting")
				.Spaces() // No label
				.Number<double>().Label("number")
				.EOF()

				.Transform(v =>
				{
					return v.GetValue("unknown");
				});

			var parser = builder.Build();

			Assert.Throws<SemanticException>(() => parser.Parse<string>("Hello 123"));
		}

		[Fact]
		public void EmptySequence_ThrowsException()
		{
			Assert.Throws<ParserBuildingException>(() =>
			{
				var builder = new ParserBuilder();

				builder.CreateMainRule()
					.Label("empty");
			});
		}

		[Fact]
		public void MultipleSameLabels_ThrowsException()
		{
			Assert.Throws<ParserBuildingException>(() =>
			{
				var builder = new ParserBuilder();

				builder.CreateMainRule()
					.Number<double>().Label("float")
					.Spaces()
					.Number<float>().Label("float");
			});
		}

		[Fact]
		public void SingleRule_CreatesSequence()
		{
			var builder = new ParserBuilder();

			builder.CreateRule("rule")
				.Literal("Hello").Label("greeting");

			var parser = builder.Build();

			var rule = parser.GetRule("rule");
			Assert.IsType<SequenceParserRule>(rule);
			Assert.Equal(2, parser.Rules.Count); // sequence "rule" and token rule that wraps literal token "Hello"
		}
	}
}
