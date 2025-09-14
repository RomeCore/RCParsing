using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing;
using RCParsing.Building;

namespace SyntaxColorizer
{
	public static class ParserProvider
	{
		private static void DeclareValues(ParserBuilder builder)
		{
			builder.CreateToken("identifier")
				.Identifier()
				.Transform(v => v.Text);

			builder.CreateToken("method_name")
				.Identifier()
				.Transform(v => v.Text);

			builder.CreateToken("field_name")
				.Identifier()
				.Transform(v => v.Text);

			builder.CreateToken("number")
				.Regex(@"\d+(?:\.\d+)?");

			builder.CreateToken("string")
				.Literal('\'')
				.EscapedTextDoubleChars('\'')
				.Literal('\'');

			builder.CreateToken("raw_string")
				.Literal('\'')
				.EscapedTextDoubleChars('\'')
				.Literal('\'');

			builder.CreateToken("boolean")
				.LiteralChoice("true", "false");

			builder.CreateToken("null")
				.Literal("null");

			// Constants //

			builder.CreateRule("constant")
				.Choice(
					c => c.Token("number"),
					c => c.Token("string"),
					c => c.Token("boolean"),
					c => c.Token("null"),
					c => c.Rule("constant_array"),
					c => c.Rule("constant_object"));

			builder.CreateRule("constant_pair")
				.Token("identifier")
				.Literal(":")
				.Rule("constant");

			builder.CreateRule("constant_array")
				.Literal("[")
				.ZeroOrMoreSeparated(b => b.Rule("constant"), b => b.Literal(","), allowTrailingSeparator: true)
				.Literal("]");

			builder.CreateRule("constant_object")
				.Literal("{")
				.ZeroOrMoreSeparated(b => b.Rule("constant_pair"), b => b.Literal(","), allowTrailingSeparator: true)
				.Literal("}");

			// Expression values //

			builder.CreateRule("function_access")
				.Identifier()
				.Literal('(')
				.ZeroOrMoreSeparated(b => b
					.Rule("expression"), b => b.Literal(','))
				.Literal(')');

			builder.CreateRule("context_access")
				.Choice(
					b => b.Literal("ctx"),
					b => b.Identifier());

			builder.CreateRule("primary")
				.Choice(
					c => c.Rule("constant"),
					c => c.Rule("function_access"),
					c => c.Rule("context_access"),
					c => c.Literal('(').Rule("expression").Literal(')'));

			builder.CreateRule("value")
				.Rule("primary"); // Add alias for 'primary' rule
		}

		private static void DeclareExpressions(ParserBuilder builder)
		{
			builder.CreateRule("postfix_member")
				.Rule("primary")
				.ZeroOrMore(b => b.Choice(
					b => b.Literal('.') // Method call
						  .Token("method_name")
						  .Literal('(')
						  .ZeroOrMoreSeparated(a => a.Rule("expression"), s => s.Literal(','))
						  .Literal(')'),

					b => b.Literal('.') // Field access
						  .Token("field_name"),

					b => b.Literal('[') // Index access
						  .Rule("expression")
						  .Literal(']')
					));

			builder.CreateRule("prefix_operator")
				.ZeroOrMore(b => b.LiteralChoice("+", "-", "!"))
				.Rule("postfix_member");

			builder.CreateRule("nop_expression")
				.Rule("prefix_operator");

			// Operators //

			builder.CreateRule("multiplicative_operator") // multiplicative
				.OneOrMoreSeparated(b => b.Rule("prefix_operator"),
					o => o.LiteralChoice("*", "/", "%"),
					includeSeparatorsInResult: true);

			builder.CreateRule("additive_operator") // additive
				.OneOrMoreSeparated(b => b.Rule("multiplicative_operator"),
					o => o.LiteralChoice("+", "-"),
					includeSeparatorsInResult: true);

			builder.CreateRule("relational_operator") // relational (<, <=, >, >=)
				.OneOrMoreSeparated(b => b.Rule("additive_operator"),
					o => o.LiteralChoice("<", "<=", ">", ">="),
					includeSeparatorsInResult: true);

			builder.CreateRule("equality_operator") // equality (==, !=)
				.OneOrMoreSeparated(b => b.Rule("relational_operator"),
					o => o.LiteralChoice("==", "!="),
					includeSeparatorsInResult: true);

			builder.CreateRule("logical_and_operator") // logical AND
				.OneOrMoreSeparated(b => b.Rule("equality_operator"),
					o => o.Literal("&&"),
					includeSeparatorsInResult: true);

			builder.CreateRule("logical_or_operator") // logical OR
				.OneOrMoreSeparated(b => b.Rule("logical_and_operator"),
					o => o.Literal("||"),
					includeSeparatorsInResult: true);

			builder.CreateRule("ternary_operator") // ternary (? :)
				.Rule("logical_or_operator")
				.Optional(b => b
					.Literal('?')
					.Rule("expression")
					.Literal(':')
					.Rule("expression"));

			// Final expression = lowest precedence

			builder.CreateRule("expression")
				.Rule("ternary_operator");
		}

		private static void DeclareMessagesTemplates(ParserBuilder builder)
		{
			builder.CreateRule("messages_template")
				.Literal('@')
				.Literal("messages")
				.Whitespaces()
				.Literal("template")
				.Whitespaces()
				.Optional(b => b.Token("identifier"))
				.Literal('{')
				.Optional(b =>
					b.Rule("metadata_block"))
				.Rule("message_statements")
				.Literal('}');

			builder.CreateRule("messages_template_block")
				.Literal('{')
				.Rule("message_statements")
				.Literal('}');

			builder.CreateRule("message_statements")
				.ZeroOrMore(b => b
					.Literal('@')
					.Choice(
						c => c.Rule("message_block"),
						c => c.Rule("messages_if"),
						c => c.Rule("messages_foreach"),
						c => c.Rule("messages_while"),
						c => c.Rule("messages_render"),
						c => c.Rule("messages_var_assignment")));

			builder.CreateRule("message_block")
				.Choice(
					b => b.Rule("message_block_explicit_role"),
					b => b.Rule("message_block_variable_role"));

			builder.CreateRule("message_block_explicit_role")
				.LiteralChoice("system", "user", "assistant", "tool")
				.Whitespaces()
				.Literal("message")
				.Rule("text_template_block");

			builder.CreateRule("message_block_variable_role")
				.Literal("message")
				.Literal('{')
				.Literal('@').Literal("role").Whitespaces().Rule("expression")
				.Rule("text_statements")
				.Literal('}');

			builder.CreateRule("messages_if")
				.Literal("if")
				.Whitespaces()
				.Rule("expression")
				.Rule("messages_template_block")
				.Optional(
					b => b
						.Literal("else")
						.Choice(
							b => b.Rule("messages_if"),
							b => b.Rule("messages_template_block")));

			builder.CreateRule("messages_foreach")
				.Literal("foreach")
				.Whitespaces()
				.Token("identifier")
				.Literal("in")
				.Whitespaces()
				.Rule("expression")
				.Rule("messages_template_block");

			builder.CreateRule("messages_while")
				.Literal("while")
				.Rule("expression")
				.Rule("messages_template_block");

			builder.CreateRule("messages_render")
				.Literal("render")
				.Whitespaces()
				.Rule("nop_expression")
				.Optional(b => b
					.Literal("with")
					.Rule("nop_expression")
					.Transform(v => v.GetValue(1)));

			builder.CreateRule("messages_var_assignment")
				.Choice(
				b => b
					.Literal("let")
					.Whitespaces()
					.Token("identifier")
					.Literal("=")
					.Rule("expression"),
				b => b
					.Token("identifier")
					.Literal("=")
					.Rule("expression"));
		}

		private static void DeclareTextTemplates(ParserBuilder builder)
		{
			builder.CreateRule("text_template")
				.Literal('@')
				.Literal("template")
				.Whitespaces()
				.Optional(b => b.Token("identifier"))
				.Literal('{')
				.Optional(b => b.Rule("metadata_block"))
				.Rule("text_statements")
				.Literal('}');

			builder.CreateRule("text_content")
				.EscapedTextDoubleChars("@{}", allowsEmpty: false);

			builder.CreateRule("text_statements")
				.ZeroOrMore(b => b.Choice(
					c => c.Rule("text_content"),
					c => c.Rule("text_statement")));

			builder.CreateRule("text_template_block")
				.Literal('{')
				.Rule("text_statements")
				.Literal('}');

			builder.CreateRule("text_statement")
				.Literal('@')
				.Choice(
					b => b.Rule("text_if"),
					b => b.Rule("text_foreach"),
					b => b.Rule("text_render"),
					b => b.Rule("text_while"),
					b => b.Rule("text_var_assignment"),
					b => b.Rule("text_expression"));

			builder.CreateRule("text_expression")
				.Rule("nop_expression")
				.Optional(b => b
					.Literal(':')
					.Token("raw_string"));

			builder.CreateRule("text_if")
				.Literal("if")
				.Whitespaces()
				.Rule("expression")
				.Rule("text_template_block")
				.Optional(
					b => b
						.Literal("else")
						.Choice(
							b => b.Rule("text_if"),
							b => b.Rule("text_template_block")));

			builder.CreateRule("text_foreach")
				.Literal("foreach")
				.Whitespaces()
				.Token("identifier")
				.Literal("in")
				.Whitespaces()
				.Rule("expression")
				.Rule("text_template_block");

			builder.CreateRule("text_while")
				.Literal("while")
				.Rule("expression")
				.Rule("text_template_block");

			builder.CreateRule("text_render")
				.Literal("render")
				.Whitespaces()
				.Rule("prefix_operator")
				.Optional(b => b
					.Literal("with")
					.Rule("prefix_operator"));

			builder.CreateRule("text_var_assignment")
				.Choice(
					b => b
						.Literal("let")
						.Whitespaces()
						.Token("identifier")
						.Literal("=")
						.Rule("nop_expression"),
					b => b
						.Token("identifier")
						.Literal("=")
						.Rule("nop_expression"));
		}

		private static void DeclareMainRules(ParserBuilder builder)
		{
			builder.Settings
				.Skip(s => s.Choice(
					c => c.Whitespaces(),
					c => c.Literal("@//").TextUntil('\n', '\r'), // @// C#-like comments
					c => c.Literal("@*").TextUntil("*@").Literal("*@")) // @*...*@ comments
					.ConfigureForSkip(), // Ignore all errors when parsing comments and unnecessary whitespace
					ParserSkippingStrategy.TryParseThenSkipLazy) // Allows rules to capture skip-rules contents if can, such as whitespaces
				.UseCaching().UseInlining().DetailedErrors(); // If caching is disabled, prepare to wait for a long time (seconds) when encountering an error :P (you will also get a million of errors, seriously)

			builder.CreateRule("template")
				.Choice(
					b => b.Rule("text_template"),
					b => b.Rule("messages_template"));

			builder.CreateRule("metadata_block")
				.Literal('@')
				.Literal("metadata")
				.Rule("constant_object");

			builder.CreateMainRule("file_content")
				.ZeroOrMore(b => b.Rule("template"))
				.EOF();
		}

		public static Parser CreateParser()
		{
			var builder = new ParserBuilder();

			DeclareValues(builder);
			DeclareExpressions(builder);
			DeclareMessagesTemplates(builder);
			DeclareTextTemplates(builder);
			DeclareMainRules(builder);

			return builder.Build();
		}
	}
}