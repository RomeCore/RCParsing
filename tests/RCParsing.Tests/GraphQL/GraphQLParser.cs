using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.GraphQL
{
	/// <summary>
	/// The GraphQL parser. Grammar took from https://github.com/antlr/grammars-v4/blob/master/graphql/GraphQL.g4
	/// </summary>
	public static class GraphQLParser
	{
		public static void FillWithRules(ParserBuilder builder)
		{
			builder.CreateMainRule("document")
				.OneOrMore(b => b.Rule("definition"))
				.EOF();

			builder.CreateRule("definition")
				.Choice(
					b => b.Rule("executableDocument"),
					b => b.Rule("typeSystemDocument"),
					b => b.Rule("typeSystemExtensionDocument")
				);

			builder.CreateRule("executableDocument")
				.OneOrMore(b => b.Rule("executableDefinition"));

			builder.CreateRule("executableDefinition")
				.Choice(
					b => b.Rule("operationDefinition"),
					b => b.Rule("fragmentDefinition")
				);

			builder.CreateRule("operationDefinition")
				.Choice(
					b => b.Rule("operationType")
						.Optional(b => b.Rule("name"))
						.Optional(b => b.Rule("variableDefinitions"))
						.Optional(b => b.Rule("directives"))
						.Rule("selectionSet"),
					b => b.Rule("selectionSet")
				);

			builder.CreateRule("operationType")
				.KeywordChoice("query", "mutation", "subscription");

			builder.CreateRule("selectionSet")
				.Literal('{').OneOrMore(b => b.Rule("selection")).Literal('}');

			builder.CreateRule("selection")
				.Choice(
					b => b.Rule("field"),
					b => b.Rule("fragmentSpread"),
					b => b.Rule("inlineFragment")
				);

			builder.CreateRule("field")
				.Optional(b => b.Rule("alias"))
				.Rule("name")
				.Optional(b => b.Rule("arguments"))
				.Optional(b => b.Rule("directives"))
				.Optional(b => b.Rule("selectionSet"));

			builder.CreateRule("arguments")
				.Literal('(')
				.OneOrMore(b => b.Rule("argument"))
				.Literal(')');

			builder.CreateRule("argument")
				.Rule("name")
				.Literal(':')
				.Rule("value");

			builder.CreateRule("alias")
				.Rule("name")
				.Literal(':');

			builder.CreateRule("fragmentSpread")
				.Literal("...")
				.Rule("fragmentName")
				.Optional(b => b.Rule("directives"));

			builder.CreateRule("fragmentDefinition")
				.Keyword("fragment")
				.Rule("fragmentName")
				.Rule("typeCondition")
				.Optional(b => b.Rule("directives"))
				.Rule("selectionSet");

			builder.CreateRule("fragmentName")
				.Rule("name");

			builder.CreateRule("typeCondition")
				.Keyword("on")
				.Rule("namedType");

			builder.CreateRule("inlineFragment")
				.Literal("...")
				.Optional(b => b.Rule("typeCondition"))
				.Optional(b => b.Rule("directives"))
				.Rule("selectionSet");

			builder.CreateRule("value")
				.Choice(
					b => b.Rule("variable"),
					b => b.Rule("floatValue"),
					b => b.Rule("intValue"),
					b => b.Rule("stringValue"),
					b => b.Rule("booleanValue"),
					b => b.Rule("nullValue"),
					b => b.Rule("enumValue"),
					b => b.Rule("listValue"),
					b => b.Rule("objectValue")
				);

			builder.CreateRule("intValue")
				.Token("INT");

			builder.CreateRule("floatValue")
				.Token("FLOAT");

			builder.CreateRule("booleanValue")
				.KeywordChoice("true", "false");

			builder.CreateRule("stringValue")
				.Choice(
					b => b.Token("STRING"),
					b => b.Token("BLOCK_STRING")
				);

			builder.CreateRule("nullValue")
				.Keyword("null");

			builder.CreateRule("enumValue")
				.Rule("name");

			builder.CreateRule("listValue")
				.Literal('[').ZeroOrMore(b => b.Rule("value")).Literal(']');

			builder.CreateRule("objectValue")
				.Literal('{')
				.ZeroOrMore(b => b.Rule("objectField"))
				.Literal('}');

			builder.CreateRule("objectField")
				.Rule("name")
				.Literal(':')
				.Rule("value");

			builder.CreateRule("variable")
				.Literal('$')
				.Rule("name");

			builder.CreateRule("variableDefinitions")
				.Literal('(')
				.OneOrMore(b => b.Rule("variableDefinition"))
				.Literal(')');

			builder.CreateRule("variableDefinition")
				.Rule("variable")
				.Literal(':')
				.Rule("type_")
				.Optional(b => b.Rule("defaultValue"));

			builder.CreateRule("defaultValue")
				.Literal('=')
				.Rule("value");

			builder.CreateRule("type_")
				.Choice(
					b => b.Rule("namedType").Optional(b => b.Literal('!')),
					b => b.Rule("listType").Optional(b => b.Literal('!'))
				);

			builder.CreateRule("namedType")
				.Rule("name");

			builder.CreateRule("listType")
				.Literal('[')
				.Rule("type_")
				.Literal(']');

			builder.CreateRule("directives")
				.OneOrMore(b => b.Rule("directive"));

			builder.CreateRule("directive")
				.Literal('@')
				.Rule("name")
				.Optional(b => b.Rule("arguments"));

			builder.CreateRule("typeSystemDocument")
				.OneOrMore(b => b.Rule("typeSystemDefinition"));

			builder.CreateRule("typeSystemDefinition")
				.Choice(
					b => b.Rule("schemaDefinition"),
					b => b.Rule("typeDefinition"),
					b => b.Rule("directiveDefinition")
				);

			builder.CreateRule("typeSystemExtensionDocument")
				.OneOrMore(b => b.Rule("typeSystemExtension"));

			builder.CreateRule("typeSystemExtension")
				.Choice(
					b => b.Rule("schemaExtension"),
					b => b.Rule("typeExtension")
				);

			builder.CreateRule("schemaDefinition")
				.Keyword("schema")
				.Optional(b => b.Rule("directives"))
				.Literal('{')
				.OneOrMore(b => b.Rule("rootOperationTypeDefinition"))
				.Literal('}');

			builder.CreateRule("rootOperationTypeDefinition")
				.Rule("operationType")
				.Literal(':')
				.Rule("namedType");

			builder.CreateRule("schemaExtension")
				.Choice(
					b => b.Keyword("extend")
						  .Keyword("schema")
						  .Optional(b => b.Rule("directives"))
						  .Literal('{')
						  .OneOrMore(b => b.Rule("rootOperationTypeDefinition"))
						  .Literal('}'),
					b => b.Keyword("extend")
						  .Keyword("schema")
						  .Rule("directives")
				);

			builder.CreateRule("description")
				.Rule("stringValue");

			builder.CreateRule("typeDefinition")
				.Choice(
					b => b.Rule("scalarTypeDefinition"),
					b => b.Rule("objectTypeDefinition"),
					b => b.Rule("interfaceTypeDefinition"),
					b => b.Rule("unionTypeDefinition"),
					b => b.Rule("enumTypeDefinition"),
					b => b.Rule("inputObjectTypeDefinition")
				);

			builder.CreateRule("typeExtension")
				.Choice(
					b => b.Rule("scalarTypeExtension"),
					b => b.Rule("objectTypeExtension"),
					b => b.Rule("interfaceTypeExtension"),
					b => b.Rule("unionTypeExtension"),
					b => b.Rule("enumTypeExtension"),
					b => b.Rule("inputObjectTypeExtension")
				);

			builder.CreateRule("scalarTypeDefinition")
				.Optional(b => b.Rule("description"))
				.Keyword("scalar")
				.Rule("name")
				.Optional(b => b.Rule("directives"));

			builder.CreateRule("scalarTypeExtension")
				.Keyword("extend")
				.Keyword("scalar")
				.Rule("name")
				.Rule("directives");

			builder.CreateRule("objectTypeDefinition")
				.Optional(b => b.Rule("description"))
				.Keyword("type")
				.Rule("name")
				.Optional(b => b.Rule("implementsInterfaces"))
				.Optional(b => b.Rule("directives"))
				.Optional(b => b.Rule("fieldsDefinition"));

			builder.CreateRule("implementsInterfaces")
				.Keyword("implements")
				.Optional(b => b.Literal('&'))
				.OneOrMoreSeparated(b => b.Rule("namedType"), b => b.Literal('&'));

			builder.CreateRule("fieldsDefinition")
				.Literal('{')
				.OneOrMore(b => b.Rule("fieldDefinition"))
				.Literal('}');

			builder.CreateRule("fieldDefinition")
				.Optional(b => b.Rule("description"))
				.Rule("name")
				.Optional(b => b.Rule("argumentsDefinition"))
				.Literal(':')
				.Rule("type_")
				.Optional(b => b.Rule("directives"));

			builder.CreateRule("argumentsDefinition")
				.Literal('(')
				.OneOrMore(b => b.Rule("inputValueDefinition"))
				.Literal(')');

			builder.CreateRule("inputValueDefinition")
				.Optional(b => b.Rule("description"))
				.Rule("name")
				.Literal(':')
				.Rule("type_")
				.Optional(b => b.Rule("defaultValue"))
				.Optional(b => b.Rule("directives"));

			builder.CreateRule("objectTypeExtension")
				.Keyword("extend")
				.Keyword("type")
				.Rule("name")
				.Optional(b => b.Rule("implementsInterfaces"))
				.Choice(
					b => b.Optional(b => b.Rule("directives")).Rule("fieldsDefinition"),
					b => b.Rule("directives"),
					b => b.Empty() // implementsInterfaces only
				);

			builder.CreateRule("interfaceTypeDefinition")
				.Optional(b => b.Rule("description"))
				.Keyword("interface")
				.Rule("name")
				.Optional(b => b.Rule("implementsInterfaces"))
				.Optional(b => b.Rule("directives"))
				.Optional(b => b.Rule("fieldsDefinition"));

			builder.CreateRule("interfaceTypeExtension")
				.Keyword("extend")
				.Keyword("interface")
				.Rule("name")
				.Optional(b => b.Rule("implementsInterfaces"))
				.Choice(
					b => b.Optional(b => b.Rule("directives")).Rule("fieldsDefinition"),
					b => b.Rule("directives"),
					b => b.Empty() // implementsInterfaces only
				);

			builder.CreateRule("unionTypeDefinition")
				.Optional(b => b.Rule("description"))
				.Keyword("union")
				.Rule("name")
				.Optional(b => b.Rule("directives"))
				.Optional(b => b.Rule("unionMemberTypes"));

			builder.CreateRule("unionMemberTypes")
				.Literal('=')
				.Optional(b => b.Literal('|'))
				.Rule("namedType")
				.ZeroOrMore(b => b.Literal('|').Rule("namedType"));

			builder.CreateRule("unionTypeExtension")
				.Keyword("extend")
				.Keyword("union")
				.Rule("name")
				.Choice(
					b => b.Optional(b => b.Rule("directives")).Rule("unionMemberTypes"),
					b => b.Rule("directives")
				);

			builder.CreateRule("enumTypeDefinition")
				.Optional(b => b.Rule("description"))
				.Keyword("enum")
				.Rule("name")
				.Optional(b => b.Rule("directives"))
				.Optional(b => b.Rule("enumValuesDefinition"));

			builder.CreateRule("enumValuesDefinition")
				.Literal('{')
				.OneOrMore(b => b.Rule("enumValueDefinition"))
				.Literal('}');

			builder.CreateRule("enumValueDefinition")
				.Optional(b => b.Rule("description"))
				.Rule("enumValue")
				.Optional(b => b.Rule("directives"));

			builder.CreateRule("enumTypeExtension")
				.Keyword("extend")
				.Keyword("enum")
				.Rule("name")
				.Choice(
					b => b.Optional(b => b.Rule("directives")).Rule("enumValuesDefinition"),
					b => b.Rule("directives")
				);

			builder.CreateRule("inputObjectTypeDefinition")
				.Optional(b => b.Rule("description"))
				.Keyword("input")
				.Rule("name")
				.Optional(b => b.Rule("directives"))
				.Optional(b => b.Rule("inputFieldsDefinition"));

			builder.CreateRule("inputFieldsDefinition")
				.Literal('{')
				.OneOrMore(b => b.Rule("inputValueDefinition"))
				.Literal('}');

			builder.CreateRule("inputObjectTypeExtension")
				.Keyword("extend")
				.Keyword("input")
				.Rule("name")
				.Choice(
					b => b.Optional(b => b.Rule("directives")).Rule("inputFieldsDefinition"),
					b => b.Rule("directives")
				);

			builder.CreateRule("directiveDefinition")
				.Optional(b => b.Rule("description"))
				.Keyword("directive")
				.Literal('@')
				.Rule("name")
				.Optional(b => b.Rule("argumentsDefinition"))
				.Optional(b => b.Keyword("repeatable"))
				.Keyword("on")
				.Rule("directiveLocations");

			builder.CreateRule("directiveLocations")
				.Rule("directiveLocation")
				.ZeroOrMore(b => b.Literal('|').Rule("directiveLocation"));

			builder.CreateRule("directiveLocation")
				.Choice(
					b => b.Rule("executableDirectiveLocation"),
					b => b.Rule("typeSystemDirectiveLocation")
				);

			builder.CreateRule("executableDirectiveLocation")
				.KeywordChoice(
					"QUERY",
					"MUTATION",
					"SUBSCRIPTION",
					"FIELD",
					"FRAGMENT_DEFINITION",
					"FRAGMENT_SPREAD",
					"INLINE_FRAGMENT",
					"VARIABLE_DEFINITION"
				);

			builder.CreateRule("typeSystemDirectiveLocation")
				.KeywordChoice(
					"SCHEMA",
					"SCALAR",
					"OBJECT",
					"FIELD_DEFINITION",
					"ARGUMENT_DEFINITION",
					"INTERFACE",
					"UNION",
					"ENUM",
					"ENUM_VALUE",
					"INPUT_OBJECT",
					"INPUT_FIELD_DEFINITION"
				);

			builder.CreateRule("name")
				.Token("NAME");

			// TOKENS

			builder.CreateToken("NAME")
				.Identifier();

			builder.CreateToken("CHARACTER")
				.Choice(
					b => b.Token("ESC"),
					b => b.Char(c => c != '"' && c != '\\')
				);

			builder.CreateToken("ESC")
				.Literal('\\')
				.Choice(
					b => b.LiteralChoice("\\", "/", "b", "f", "n", "r", "t"),
					b => b.Token("UNICODE")
				);

			builder.CreateToken("STRING")
				.Literal('"')
				.ZeroOrMore(t => t.Token("CHARACTER"))
				.Literal('"');

			builder.CreateToken("BLOCK_STRING")
				.Literal("\"\"\"")
				.TextUntil("\"\"\"")
				.Literal("\"\"\"");

			builder.CreateToken("ID")
				.Token("STRING");

			builder.CreateToken("UNICODE")
				.Literal('u').Token("HEX").Token("HEX").Token("HEX").Token("HEX");

			builder.CreateToken("HEX")
				.Char(c => ('0' <= c && c <= '9') ||
						   ('a' <= c && c <= 'f') ||
						   ('A' <= c && c <= 'F'));

			builder.CreateToken("NONZERO_DIGIT")
				.Char(c => ('1' <= c && c <= '9'));

			builder.CreateToken("DIGIT")
				.Char(c => ('0' <= c && c <= '9'));

			builder.CreateToken("FRACTIONAL_PART")
				.Literal('.').OneOrMore(b => b.Token("DIGIT"));

			builder.CreateToken("EXPONENTIAL_PART")
				.Token("EXPONENT_INDICATOR").Optional(b => b.Token("SIGN")).OneOrMore(b => b.Token("DIGIT"));

			builder.CreateToken("EXPONENT_INDICATOR")
				.Char(c => ('e' == c || 'E' == c));

			builder.CreateToken("SIGN")
				.Char(c => ('+' == c || '-' == c));

			builder.CreateToken("NEGATIVE_SIGN")
				.Literal('-');

			builder.CreateToken("FLOAT")
				.Token("INT")
				.Optional(b => b.Token("FRACTIONAL_PART"))
				.Optional(b => b.Token("EXPONENTIAL_PART"));

			builder.CreateToken("INT")
				.Optional(b => b.Token("NEGATIVE_SIGN"))
				.OneOrMore(b => b.Token("DIGIT"));

			// SETTINGS

			builder.Settings.Skip(b => b.Choice(
					b => b.Whitespaces(),
					b => b.Literal(','),
					b => b.Token(b => b.Literal("#").ZeroOrMoreChars(c => c != '\n' && c != '\r'))
				).ConfigureForSkip(), ParserSkippingStrategy.SkipBeforeParsingGreedy);
		}

		public static Parser CreateParser(Action<ParserBuilder>? builderAction = null)
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			builderAction?.Invoke(builder);
			return builder.Build();
		}
	}
}