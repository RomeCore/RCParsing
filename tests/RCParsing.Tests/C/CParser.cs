using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.C
{
	/// <summary>
	/// The C parser based on ANTLR grammar, not completed yet due to NFA algorithm used in ANTLR
	/// </summary>
	public class CParser
	{
		public static void FillWithRules(ParserBuilder builder)
		{
			builder.CreateToken("skip")
				.Choice(
					b => b.Whitespaces(),
					b => b.Literal("//").ZeroOrMoreChars(c => c != '\n' && c != '\r'),
					b => b.Literal("#define").ZeroOrMoreChars(c => c != '\n' && c != '\r'),
					b => b.Literal("#include").ZeroOrMoreChars(c => c != '\n' && c != '\r'),
					b => b.Literal("/*").TextUntil("*/").Literal("*/")
				).Configure(c => c.IgnoreErrors());

			builder.Settings.UseCaching().RecordWalkTrace().SetMaxStepsToDisplay(350).Skip(b => b.Token("skip"), ParserSkippingStrategy.SkipBeforeParsingGreedy);

			builder.CreateToken("Identifier").Identifier();
			builder.CreateToken("Constant").Number<double>();
			builder.CreateToken("StringLiteral").Literal('"').EscapedTextPrefix('\\', '"').Literal('"');
			builder.CreateToken("DigitSequence").Number<uint>();

			builder.CreateRule("primaryExpression")
				.LongestChoice(
					b => b.Token("Identifier"),
					b => b.Token("Constant"),
					b => b.OneOrMore(b => b.Token("StringLiteral")),
					b => b.Literal("(").Rule("expression").Literal(")"),
					b => b.Rule("genericSelection"),
					b => b.Optional(b => b.Literal("__extension__"))
						.Literal("(").Rule("compoundStatement").Literal(")"),
					b => b.Keyword("__builtin_va_arg")
						.Literal("(").Rule("unaryExpression").Literal(",").Rule("typeName").Literal(")"),
					b => b.Keyword("__builtin_offsetof")
						.Literal("(").Rule("typeName").Literal(",").Rule("unaryExpression").Literal(")")
				);

			builder.CreateRule("genericSelection")
				.Keyword("_Generic")
				.Literal("(").Rule("assignmentExpression").Literal(",").Rule("genericAssocList").Literal(")");

			builder.CreateRule("genericAssocList")
				.OneOrMoreSeparated(b => b.Rule("genericAssociation"), b => b.Literal(","));

			builder.CreateRule("genericAssociation")
				.LongestChoice(
					b => b.Rule("typeName"),
					b => b.Keyword("default")
				)
				.Literal(":").Rule("assignmentExpression");

			builder.CreateRule("postfixExpression")
				.LongestChoice(
					b => b.Rule("primaryExpression"),
					b => b.Optional(b => b.Keyword("__extension__"))
						.Literal("(").Rule("typeName").Literal(")")
						.Literal("{").Rule("initializerList").Optional(b => b.Literal(",")).Literal("}")
				)
				.ZeroOrMore(b => b.LongestChoice(
					b => b.Literal("[").Rule("expression").Literal("]"),
					b => b.Literal("(").Optional(b => b.Rule("argumentExpressionList")).Literal(")"),
					b => b.LiteralChoice(".", "->").Token("Identifier"),
					b => b.LiteralChoice("++", "--")
				));

			builder.CreateRule("argumentExpressionList")
				.OneOrMoreSeparated(b => b.Rule("assignmentExpression"), b => b.Literal(","));

			builder.CreateRule("unaryExpression")
				.ZeroOrMore(b => b.LiteralChoice("++", "--", "sizeof"))
				.LongestChoice(
					b => b.Rule("postfixExpression"),
					b => b.Token("unaryOperator").Rule("castExpression"),
					b => b.KeywordChoice("sizeof", "_Alignof")
						.Literal("(").Rule("typeName").Literal(")"),
					b => b.Literal("&&").Token("Identifier")
				);

			builder.CreateToken("unaryOperator")
				.LiteralChoice("&", "*", "+", "-", "~", "!");

			builder.CreateRule("castExpression")
				.LongestChoice(
					b => b.Optional(b => b.Keyword("__extension__"))
						.Literal("(").Rule("typeName").Literal(")").Rule("castExpression"),
					b => b.Rule("unaryExpression"),
					b => b.Token("DigitSequence")
				);

			builder.CreateRule("multiplicativeExpression")
				.OneOrMoreSeparated(
					b => b.Rule("castExpression"),
					b => b.LiteralChoice("*", "/", "%")
				);

			builder.CreateRule("additiveExpression")
				.OneOrMoreSeparated(
					b => b.Rule("multiplicativeExpression"),
					b => b.LiteralChoice("+", "-")
				);

			builder.CreateRule("shiftExpression")
				.OneOrMoreSeparated(
					b => b.Rule("additiveExpression"),
					b => b.LiteralChoice("<<", ">>")
				);

			builder.CreateRule("relationalExpression")
				.OneOrMoreSeparated(
					b => b.Rule("shiftExpression"),
					b => b.LiteralChoice("<", ">", "<=", ">=")
				);

			builder.CreateRule("equalityExpression")
				.OneOrMoreSeparated(
					b => b.Rule("relationalExpression"),
					b => b.LiteralChoice("==", "!=")
				);

			builder.CreateRule("andExpression")
				.OneOrMoreSeparated(b => b.Rule("equalityExpression"), b => b.Literal("&"));

			builder.CreateRule("exclusiveOrExpression")
				.OneOrMoreSeparated(b => b.Rule("andExpression"), b => b.Literal("^"));

			builder.CreateRule("inclusiveOrExpression")
				.OneOrMoreSeparated(b => b.Rule("exclusiveOrExpression"), b => b.Literal("|"));

			builder.CreateRule("logicalAndExpression")
				.OneOrMoreSeparated(b => b.Rule("inclusiveOrExpression"), b => b.Literal("&&"));

			builder.CreateRule("logicalOrExpression")
				.OneOrMoreSeparated(b => b.Rule("logicalAndExpression"), b => b.Literal("||"));

			builder.CreateRule("conditionalExpression")
				.Rule("logicalOrExpression")
				.Optional(b => b.Literal("?").Rule("expression").Literal(":").Rule("conditionalExpression"));

			builder.CreateRule("assignmentExpression")
				.LongestChoice(
					b => b.Rule("unaryExpression").Token("assignmentOperator").Rule("assignmentExpression"),
					b => b.Rule("conditionalExpression"),
					b => b.Token("DigitSequence")
				);

			builder.CreateToken("assignmentOperator")
				.LiteralChoice("=", "*=", "/=", "%=", "+=", "-=", "<<=", ">>=", "&=", "^=", "|=");

			builder.CreateRule("expression")
				.OneOrMoreSeparated(b => b.Rule("assignmentExpression"), b => b.Literal(","));

			builder.CreateRule("constantExpression")
				.Rule("conditionalExpression");

			builder.CreateRule("declaration")
				.LongestChoice(
					b => b.Rule("declarationSpecifiers")
						.Optional(b => b.Rule("initDeclaratorList"))
						.Literal(";"),
					b => b.Rule("staticAssertDeclaration")
				);

			builder.CreateRule("declarationSpecifiers")
				.OneOrMore(b => b.Rule("declarationSpecifier"));

			builder.CreateRule("declarationSpecifier")
				.Choice(
					b => b.Token("storageClassSpecifier"),
					b => b.Rule("typeSpecifier"),
					b => b.Token("typeQualifier"),
					b => b.Rule("functionSpecifier"),
					b => b.Rule("alignmentSpecifier")
				);

			builder.CreateRule("initDeclaratorList")
				.OneOrMoreSeparated(b => b.Rule("initDeclarator"), b => b.Literal(","));

			builder.CreateRule("initDeclarator")
				.Rule("declarator")
				.Optional(b => b.Literal("=").Rule("initializer"));

			builder.CreateToken("storageClassSpecifier")
				.KeywordChoice("typedef", "extern", "static", "_Thread_local", "auto", "register");

			builder.CreateRule("typeSpecifier")
				.LongestChoice(
					b => b.KeywordChoice("void", "char", "short", "int", "long", "float", "double",
										"signed", "unsigned", "_Bool", "_Complex",
										"__m128", "__m128d", "__m128i"),
					b => b.Keyword("__extension__")
						.Literal("(")
						.KeywordChoice("__m128", "__m128d", "__m128i")
						.Literal(")"),
					b => b.Rule("atomicTypeSpecifier"),
					b => b.Rule("structOrUnionSpecifier"),
					b => b.Rule("enumSpecifier"),
					b => b.Rule("typedefName"),
					b => b.Keyword("__typeof__")
						.Literal("(").Rule("constantExpression").Literal(")")
				);

			builder.CreateRule("structOrUnionSpecifier")
				.LongestChoice(
					b => b.Token("structOrUnion")
						.Optional(b => b.Token("Identifier"))
						.Literal("{").Rule("structDeclarationList").Literal("}"),
					b => b.Token("structOrUnion").Token("Identifier")
				);

			builder.CreateToken("structOrUnion")
				.KeywordChoice("struct", "union");

			builder.CreateRule("structDeclarationList")
				.OneOrMore(b => b.Rule("structDeclaration"));

			builder.CreateRule("structDeclaration")
				.LongestChoice(
					b => b.Rule("specifierQualifierList").Optional(b => b.Rule("structDeclaratorList")).Literal(";"),
					b => b.Rule("staticAssertDeclaration")
				);

			builder.CreateRule("specifierQualifierList")
				.OneOrMore(b => b.LongestChoice(
					b => b.Token("typeQualifier"),
					b => b.Rule("typeSpecifier")
				));

			builder.CreateToken("typeQualifier")
				.KeywordChoice("const", "restrict", "volatile", "_Atomic");

			// 17. Struct Declarator List
			builder.CreateRule("structDeclaratorList")
				.OneOrMoreSeparated(b => b.Rule("structDeclarator"), b => b.Literal(","));

			builder.CreateRule("structDeclarator")
				.LongestChoice(
					b => b.Optional(b => b.Rule("declarator"))
						.Literal(":").Rule("constantExpression"),
					b => b.Rule("declarator")
				);

			// 18. Enum Specifier
			builder.CreateRule("enumSpecifier")
				.Keyword("enum")
				.LongestChoice(
					b => b.Optional(b => b.Token("Identifier"))
						.Literal("{").Rule("enumeratorList").Optional(b => b.Literal(",")).Literal("}"),
					b => b.Token("Identifier")
				);

			builder.CreateRule("enumeratorList")
				.OneOrMoreSeparated(b => b.Rule("enumerator"), b => b.Literal(","));

			builder.CreateRule("enumerator")
				.Token("enumerationConstant")
				.Optional(b => b.Literal("=").Rule("constantExpression"));

			builder.CreateToken("enumerationConstant")
				.Token("Identifier");

			// 19. Atomic and Qualifiers
			builder.CreateRule("atomicTypeSpecifier")
				.Keyword("_Atomic").Literal("(").Rule("typeName").Literal(")");

			builder.CreateRule("functionSpecifier")
				.LongestChoice(
					b => b.KeywordChoice("inline", "_Noreturn", "__inline__", "__stdcall"),
					b => b.Rule("gccAttributeSpecifier"),
					b => b.Keyword("__declspec").Literal("(").Token("Identifier").Literal(")")
				);

			builder.CreateRule("alignmentSpecifier")
				.Keyword("_Alignas").Literal("(")
				.LongestChoice(b => b.Rule("typeName"), b => b.Rule("constantExpression"))
				.Literal(")");

			builder.CreateRule("declarator")
				.Optional(b => b.Rule("pointer"))
				.Rule("directDeclarator")
				.ZeroOrMore(b => b.Rule("gccDeclaratorExtension"));

			builder.CreateRule("directDeclarator")
				.LongestChoice(
					b => b.Literal("(").Optional(b => b.Token("vcSpecificModifer")).Rule("declarator").Literal(")"),
					b => b.Token("Identifier").Literal(":").Token("DigitSequence"), // bit field
					b => b.Optional(b => b.Token("vcSpecificModifer")).Token("Identifier") // VC Extension
				)
				.ZeroOrMore(b => b.LongestChoice(
					b => b.Literal("[").Optional(b => b.Rule("typeQualifierList"))
						.Optional(b => b.Rule("assignmentExpression")).Literal("]"),
					b => b.Literal("[").Keyword("static")
						.Optional(b => b.Rule("typeQualifierList"))
						.Rule("assignmentExpression").Literal("]"),
					b => b.Literal("[").Rule("typeQualifierList").Keyword("static")
						.Rule("assignmentExpression").Literal("]"),
					b => b.Literal("[").Literal("*").Literal("]"),
					b => b.Literal("(").Rule("parameterTypeList").Literal(")"),
					b => b.Literal("(").Optional(b => b.Rule("identifierList")).Literal(")")
				));

			builder.CreateToken("vcSpecificModifer")
				.KeywordChoice("__cdecl", "__clrcall", "__stdcall", "__fastcall",
							  "__thiscall", "__vectorcall");

			builder.CreateRule("gccDeclaratorExtension")
				.LongestChoice(
					b => b.Keyword("__asm").Literal("(").OneOrMore(b => b.Token("StringLiteral")).Literal(")"),
					b => b.Rule("gccAttributeSpecifier")
				);

			builder.CreateRule("gccAttributeSpecifier")
				.Keyword("__attribute__").Literal("(").Literal("(")
				.Rule("gccAttributeList").Literal(")").Literal(")");

			builder.CreateRule("gccAttributeList")
				.ZeroOrMoreSeparated(b => b.Optional(b => b.Rule("gccAttribute")), b => b.Literal(","));

			builder.CreateRule("gccAttribute")
				.Regex(@"[^,()]+") // relaxed def
				.Optional(b => b.Literal("(").Optional(b => b.Rule("argumentExpressionList")).Literal(")"));

			builder.CreateRule("pointer")
				.OneOrMore(b => b.LongestChoice(b => b.Literal("*"), b => b.Literal("^")) // ^ - Blocks extension
					.Optional(b => b.Rule("typeQualifierList")));

			builder.CreateRule("typeQualifierList")
				.OneOrMore(b => b.Token("typeQualifier"));

			builder.CreateRule("parameterTypeList")
				.Rule("parameterList")
				.Optional(b => b.Literal(",").Literal("..."));

			builder.CreateRule("parameterList")
				.OneOrMoreSeparated(b => b.Rule("parameterDeclaration"), b => b.Literal(","));

			builder.CreateRule("parameterDeclaration")
				.LongestChoice(
					b => b.Rule("declarationSpecifiers").Rule("declarator"),
					b => b.Rule("declarationSpecifiers").Optional(b => b.Rule("abstractDeclarator"))
				);

			builder.CreateRule("identifierList")
				.OneOrMoreSeparated(b => b.Token("Identifier"), b => b.Literal(","));

			builder.CreateRule("typeName")
				.Rule("specifierQualifierList")
				.Optional(b => b.Rule("abstractDeclarator"));

			builder.CreateRule("abstractDeclarator")
				.LongestChoice(
					b => b.Optional(b => b.Rule("pointer"))
						.Rule("directAbstractDeclarator")
						.ZeroOrMore(b => b.Rule("gccDeclaratorExtension")),
					b => b.Rule("pointer")
				);

			builder.CreateRule("directAbstractDeclarator")
				.LongestChoice(
					b => b.Literal("(").Rule("abstractDeclarator").Literal(")").ZeroOrMore(b => b.Rule("gccDeclaratorExtension")),
					b => b.Literal("[").Optional(b => b.Rule("typeQualifierList"))
						.Optional(b => b.Rule("assignmentExpression")).Literal("]"),
					b => b.Literal("[").Keyword("static").Optional(b => b.Rule("typeQualifierList"))
						.Rule("assignmentExpression").Literal("]"),
					b => b.Literal("[").Rule("typeQualifierList").Keyword("static")
						.Rule("assignmentExpression").Literal("]"),
					b => b.Literal("[").Literal("*").Literal("]"),
					b => b.Literal("(").Optional(b => b.Rule("parameterTypeList")).Literal(")").ZeroOrMore(b => b.Rule("gccDeclaratorExtension"))
				)
				.ZeroOrMore(b => b.LongestChoice(
					b => b.Literal("[").Optional(b => b.Rule("typeQualifierList"))
						.Optional(b => b.Rule("assignmentExpression")).Literal("]"),
					b => b.Literal("[").Keyword("static").Optional(b => b.Rule("typeQualifierList"))
						.Rule("assignmentExpression").Literal("]"),
					b => b.Literal("[").Rule("typeQualifierList").Keyword("static")
						.Rule("assignmentExpression").Literal("]"),
					b => b.Literal("[").Literal("*").Literal("]"),
					b => b.Literal("(").Optional(b => b.Rule("parameterTypeList")).Literal(")")
						.ZeroOrMore(b => b.Rule("gccDeclaratorExtension"))
				));

			builder.CreateRule("typedefName")
				.Token("Identifier");

			builder.CreateRule("initializer")
				.LongestChoice(
					b => b.Rule("assignmentExpression"),
					b => b.Literal("{").Rule("initializerList").Optional(b => b.Literal(",")).Literal("}")
				);

			builder.CreateRule("initializerList")
				.OneOrMoreSeparated(
					b => b.Optional(b => b.Rule("designation")).Rule("initializer"),
					b => b.Literal(",")
				);

			builder.CreateRule("designation")
				.Rule("designatorList").Literal("=");

			builder.CreateRule("designatorList")
				.OneOrMore(b => b.Rule("designator"));

			builder.CreateRule("designator")
				.LongestChoice(
					b => b.Literal("[").Rule("constantExpression").Literal("]"),
					b => b.Literal(".").Token("Identifier")
				);

			builder.CreateRule("staticAssertDeclaration")
				.Keyword("_Static_assert").Literal("(").Rule("constantExpression").Literal(",")
				.OneOrMore(b => b.Token("StringLiteral")).Literal(")").Literal(";");

			builder.CreateRule("statement")
				.LongestChoice(
					b => b.Rule("labeledStatement"),
					b => b.Rule("compoundStatement"),
					b => b.Rule("expressionStatement"),
					b => b.Rule("selectionStatement"),
					b => b.Rule("iterationStatement"),
					b => b.Rule("jumpStatement"),
					b => b.KeywordChoice("__asm", "__asm__")
						.Optional(b => b.KeywordChoice("volatile", "__volatile__"))
						.Literal("(")
						.Optional(b => b.OneOrMoreSeparated(b => b.Rule("logicalOrExpression"), b => b.Literal(",")))
						.ZeroOrMore(b => b.Literal(":")
							.Optional(b => b.OneOrMoreSeparated(b => b.Rule("logicalOrExpression"), b => b.Literal(","))))
						.Literal(")").Literal(";")
				);

			builder.CreateRule("labeledStatement")
				.LongestChoice(
					b => b.Token("Identifier").Literal(":").Optional(b => b.Rule("statement")),
					b => b.Keyword("case").Rule("constantExpression").Literal(":").Rule("statement"),
					b => b.Keyword("default").Literal(":").Rule("statement")
				);

			builder.CreateRule("compoundStatement")
				.Literal("{").Optional(b => b.Rule("blockItemList")).Literal("}");

			builder.CreateRule("blockItemList")
				.OneOrMore(b => b.Rule("blockItem"));

			builder.CreateRule("blockItem")
				.LongestChoice(
					b => b.Rule("statement"),
					b => b.Rule("declaration")
				);

			builder.CreateRule("expressionStatement")
				.Optional(b => b.Rule("expression")).Literal(";");

			builder.CreateRule("selectionStatement")
				.LongestChoice(
					b => b.Keyword("if").Literal("(").Rule("expression").Literal(")")
						.Rule("statement")
						.Optional(b => b.Keyword("else").Rule("statement")),
					b => b.Keyword("switch").Literal("(").Rule("expression").Literal(")").Rule("statement")
				);

			builder.CreateRule("iterationStatement")
				.LongestChoice(
					b => b.Keyword("while").Literal("(").Rule("expression").Literal(")").Rule("statement"),
					b => b.Keyword("do").Rule("statement").Keyword("while")
						.Literal("(").Rule("expression").Literal(")").Literal(";"),
					b => b.Keyword("for").Literal("(").Rule("forCondition").Literal(")").Rule("statement")
				);

			builder.CreateRule("forCondition")
				.LongestChoice(
					b => b.Rule("forDeclaration"),
					b => b.Optional(b => b.Rule("expression"))
				)
				.Literal(";").Optional(b => b.Rule("forExpression"))
				.Literal(";").Optional(b => b.Rule("forExpression"));

			builder.CreateRule("forDeclaration")
				.Rule("declarationSpecifiers").Optional(b => b.Rule("initDeclaratorList"));

			builder.CreateRule("forExpression")
				.OneOrMoreSeparated(b => b.Rule("assignmentExpression"), b => b.Literal(","));

			builder.CreateRule("jumpStatement")
				.LongestChoice(
					b => b.Keyword("goto").Token("Identifier"),
					b => b.Keyword("continue"),
					b => b.Keyword("break"),
					b => b.Keyword("return").Optional(b => b.Rule("expression")),
					b => b.Keyword("goto").Rule("unaryExpression") // GCC extension
				)
				.Literal(";");

			builder.CreateMainRule("compilationUnit")
				.Optional(b => b.Rule("translationUnit"))
				.EOF();

			builder.CreateRule("translationUnit")
				.OneOrMore(b => b.Rule("externalDeclaration"));

			builder.CreateRule("externalDeclaration")
				.LongestChoice(
					b => b.Rule("declaration"),
					b => b.Rule("functionDefinition"),
					b => b.Literal(";") // stray semicolon
				);

			builder.CreateRule("functionDefinition")
				.OneOrMore(b => b.LongestChoice(
					b => b.Rule("declarationSpecifier"),
					b => b.Rule("declarator")
				))
				.Optional(b => b.Rule("declarationList"))
				.Rule("compoundStatement");

			builder.CreateRule("declarationList")
				.OneOrMore(b => b.Rule("declaration"));
		}

		public static Parser CreateParser()
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			return builder.Build();
		}
	}
}