using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests
{
	public class ErrorRecoveryTests
	{
		[Fact]
		public void ActNormalWhenNoError()
		{
			var builder = new ParserBuilder();

			// Skip the whitespace characters.
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Literal("var")
				.RequiredWhitespaces() // Require whitespaces after 'var' keyword to prevent the 'varabc'-like matching
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';'))); // Skip until semicolon is found, then retry.

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			string input = "var a = b; var garbage = here; var c = d;";

			var result = parser.Parse(input);

			Assert.True(result.Success);
			var statements = result.Children[0]; // Get the 'OneorMore' rule's children
			Assert.Equal("var a = b;", statements[0].Text);
			Assert.Equal("var garbage = here;", statements[1].Text);
			Assert.Equal("var c = d;", statements[2].Text);
		}

		[Fact]
		public void SkipUntilSemicolon()
		{
			var builder = new ParserBuilder();

			// Skip the whitespace characters.
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Literal("var")
				.RequiredWhitespaces() // Require whitespaces after 'var' keyword to prevent the 'varabc'-like matching
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';'))); // Skip until semicolon is found, then retry.

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			string input = "var a = b var garbage = here; var c = d;";

			var result = parser.Parse(input);

			Assert.True(result.Success);
			var statements = result.Children[0]; // Get the 'OneorMore' rule's children
			Assert.Equal("var c = d;", statements[0].Text); // Then get the only matched statement, the 'var c = d;'
		}

		[Fact]
		public void ErrorRecovery_SkipAfterAnchor_RecoversToNextValidStatement()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			// Rule for a simple assignment statement: "id = id;"
			builder.CreateRule("statement")
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				// Recovery strategy: skip everything until we find the next semicolon
				.Recovery(r => r.SkipAfter(a => a.Literal(';')));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			// Input with error in the middle: missing semicolon and garbage content
			string input = "a = b; c = d garbage here; e = f;";

			var result = parser.Parse(input);
			var statements = result.Children[0];

			Assert.True(result.Success);
			// Should recover and find two valid statements: "a = b;" and "e = f;"
			// The corrupted statement "c = d garbage here;" should be skipped entirely
			Assert.Equal(2, statements.Children.Count);
			Assert.Equal("a = b;", statements.Children[0].Text);
			Assert.Equal("e = f;", statements.Children[1].Text);
		}

		[Fact]
		public void ErrorRecovery_SkipAfterAnchorWithStopRule_DoesNotSkipBeyondBoundary()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				// Recovery: skip until semicolon, but stop if we encounter "end"
				.Recovery(r => r.SkipAfter(
					a => a.Literal(';'),
					s => s.Literal("end"),
					repeat: false));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"));

			var parser = builder.Build();

			string input = "a = b; c = d garbage here ; e = f;";

			var result = parser.Parse(input);

			Assert.True(result.Success);
			// Without a stop rule, the parser would skip to the end of input
			Assert.Equal(2, result.Children.Count);
			Assert.Equal("a = b;", result.Children[0].Text);
			Assert.Equal("e = f;", result.Children[1].Text);

			string stoppingInput = "a = b; c = d garbage here end; e = f;";

			result = parser.Parse(stoppingInput);

			Assert.True(result.Success);
			// Should find only "a = b;" and stop at "end", not attempting to recover "e = f;"
			Assert.Single(result.Children);
			Assert.Equal("a = b;", result.Children[0].Text);
		}

		[Fact]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Assertions", "xUnit2013:Do not use equality check to check for collection size.")]
		public void ErrorRecovery_NestedStructuresWithDifferentStrategies_RecoversAppropriately()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			// Statement level recovery: skip to next semicolon
			builder.CreateRule("statement")
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';')));

			// Block level recovery: skip to next closing brace
			builder.CreateRule("block")
				.Literal('{')
				.OneOrMore(b => b.Rule("statement"))
				.Literal('}')
					.RecoveryLast(r => r.SkipUntil(a => a.Literal('}'))); // Place recovery strategy to the next closing brace

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("block"))
				.EOF();

			var parser = builder.Build();

			// Complex input with errors at different levels
			string input = @"
			{
				a = b;
				c = d garbage here;
				e = f;
			}
			{
				x = y;
				corrupted block content
			}
			{
				z = w;
			}";

			var result = parser.Parse(input);
			var blocks = result.Children[0];

			Assert.True(result.Success);
			// Should recover three blocks with appropriate statements
			Assert.Equal(3, blocks.Children.Count);

			// First block: "a = b;" and "e = f;"
			Assert.Equal(2, blocks.Children[0].Children[1].Count);

			// Second block: only "x = y;" (recovery at block level)
			Assert.Equal(1, blocks.Children[1].Children[1].Count);

			// Third block: normal
			Assert.Equal(1, blocks.Children[2].Children[1].Count);
		}

		[Fact]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Assertions", "xUnit2013:Do not use equality check to check for collection size.")]
		public void ErrorRecovery_SkipUntilAnchor_ResumesParsingUntilAnchor()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("function")
				.Literal("function")
				.Identifier()
				.Literal('(')
				.Literal(')')
				.Literal('{')
				.OneOrMore(b => b.Rule("statement"))
				.Literal('}')
				// Recovery: skip until 'function' keyword is found
				.Recovery(r => r.SkipUntil(a => a.Literal("function")));

			builder.CreateRule("statement")
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';');

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("function"));

			var parser = builder.Build();

			string input = @"
			function foo() {
				a = b;
				garbage content that breaks parsing
			}
			function bar() {
				c = d;
			}";

			var result = parser.Parse(input);

			Assert.True(result.Success);
			// Should recover and find the second 'bar' function
			Assert.Equal(1, result.Children.Count);
			Assert.Contains("function bar", result.Children[0].Text);
		}

		[Fact]
		public void ErrorRecovery_NoAnchor_ShouldNotRecover()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Literal("var")
				.RequiredWhitespaces()
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				.Recovery(r => r.SkipAfter(a => a.Literal(';')));

			builder.CreateMainRule("program")
				.ZeroOrMore(b => b.Rule("statement"));

			var parser = builder.Build();

			// Input with error in the middle: missing semicolon and garbage content
			string input = "var a = b garbage garbage garbage";

			var result = parser.Parse(input);

			// Yes it succeeds (it matches zero elements), but no recovery happens
			Assert.True(result.Success);
			Assert.Empty(result.Children);
		}

		[Fact]
		public void ErrorRecovery_WithRepeat_HandlesMultipleConsecutiveErrors()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Literal("var")
				.RequiredWhitespaces()
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				// Key: repeat = true to handle multiple errors in sequence
				.Recovery(r => r.SkipAfter(a => a.Literal(';'), repeat: true));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF();

			var parser = builder.Build();

			// Multiple garbage sections between valid statements
			string input = "var a = b; garbage1; var c = d; garbage2 garbage3; var e = f;";

			var result = parser.Parse(input);
			var statements = result.Children[0];

			Assert.True(result.Success);
			Assert.Equal(3, statements.Children.Count); // Should find all 3 valid statements
			Assert.Equal("var a = b;", statements.Children[0].Text);
			Assert.Equal("var c = d;", statements.Children[1].Text);
			Assert.Equal("var e = f;", statements.Children[2].Text);
		}

		[Fact]
		public void ErrorRecovery_StopRuleTakesPrecedence_WhenBothAnchorAndStopEncountered()
		{
			var builder = new ParserBuilder();
			builder.Settings.SkipWhitespaces();

			builder.CreateRule("statement")
				.Literal("var")
				.RequiredWhitespaces()
				.Identifier()
				.Literal('=')
				.Identifier()
				.Literal(';')
				// Recovery: look for semicolon but stop at EOF
				.Recovery(r => r.SkipAfter(
					a => a.Literal(';'),
					repeat: false));

			builder.CreateMainRule("program")
				.OneOrMore(b => b.Rule("statement"))
				.EOF()
					.RecoveryLast(r => r.FindNext());

			var parser = builder.Build();

			// Input where anchor would never be found
			string input = "var a = b; var c = d garbage until end";

			var result = parser.Parse(input);
			var statements = result.Children[0];

			Assert.True(result.Success);
			// Should recover only first statement and stop at EOF
			Assert.Single(statements.Children);
			Assert.Equal("var a = b;", statements.Children[0].Text);
		}
	}
}