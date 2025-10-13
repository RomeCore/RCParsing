using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.Python
{
	/// <summary>
	/// The Python 3.13 parser. Grammar took from https://github.com/antlr/grammars-v4/tree/master/python/python3_13 <br/>
	/// And https://docs.python.org/3.13/reference/grammar.html
	/// <para/>
	/// 
	/// Notes:
	/// fstrings are not fully supported, but works;
	/// Arguments, lists, dicts and other separated things must be in one line, for example:
	/// [a, 2, "string"]
	/// but not these:
	/// [a, *newline*
	/// 2, *newline*
	/// "string"]
	/// </summary>
	public static class PythonParser
	{
		public static void FillWithRules(ParserBuilder builder)
		{
			builder.BarrierTokenizers
				.AddIndent(indentSize: 4, "INDENT", "DEDENT");

			builder.CreateMainRule("file_input")
				.Optional(b => b.Rule("statements"))
				.EOF();

			builder.CreateRule("interactive")
				.Rule("statement_newline");

			builder.CreateRule("eval")
				.Rule("expressions")
				.ZeroOrMore(b => b.Token("NEWLINE"))
				.EOF();

			builder.CreateRule("func_type")
				.Literal('(')
				.Optional(b => b.Rule("type_expressions"))
				.Literal(')')
				.Literal("->")
				.Rule("expression")
				.ZeroOrMore(b => b.Token("NEWLINE"))
				.EOF();

			builder.CreateRule("statements")
				.OneOrMore(b => b.Rule("statement"));

			builder.CreateRule("statement")
				.Choice(
					b => b.Token("NEWLINE"),
					b => b.Rule("compound_stmt"),
					b => b.Rule("simple_stmts")
				);

			builder.CreateRule("statement_newline")
				.Choice(
					b => b
						.Rule("compound_stmt")
						.Token("NEWLINE"),
					b => b.Rule("simple_stmts"),
					b => b.Token("NEWLINE"),
					b => b.EOF()
				);

			builder.CreateRule("simple_stmts")
				.Rule("simple_stmt")
				.ZeroOrMore(b => b.Literal(";")
					.Rule("simple_stmt"))
					.Optional(b => b.Literal(";"))
				.Token("NEWLINE");

			builder.CreateRule("simple_stmt")
				.Choice(
					b => b.Rule("import_stmt"),
					b => b.Rule("return_stmt"),
					b => b.Rule("raise_stmt"),
					b => b.Rule("del_stmt"),
					b => b.Rule("yield_stmt"),
					b => b.Rule("assert_stmt"),
					b => b.Keyword("pass"),
					b => b.Keyword("break"),
					b => b.Keyword("continue"),
					b => b.Rule("global_stmt"),
					b => b.Rule("nonlocal_stmt"),
					b => b.Rule("assignment"),
					b => b.Rule("type_alias"),
					b => b.Rule("star_expressions")
				);

			builder.CreateRule("compound_stmt")
				.Choice(
					b => b.Rule("function_def"),
					b => b.Rule("if_stmt"),
					b => b.Rule("class_def"),
					b => b.Rule("with_stmt"),
					b => b.Rule("for_stmt"),
					b => b.Rule("try_stmt"),
					b => b.Rule("while_stmt"),
					b => b.Rule("match_stmt")
				);

			builder.CreateRule("assignment")
				.Choice(
					b => b
						.Rule("name")
						.Literal(':')
						.Rule("expression")
						.Optional(b => b.Literal('=')
						.Rule("annotated_rhs")),
					b => b
						.Choice(
							b => b
								.Literal('(')
								.Rule("single_target")
								.Literal(')'),
							b => b.Rule("single_subscript_attribute_target")
						)
						.Literal(':')
						.Rule("expression")
						.Optional(b => b.Literal('=')
						.Rule("annotated_rhs")),
					b => b
						.OneOrMore(b => b.Rule("star_targets")
							.Literal('='))
						.Choice(
							b => b.Rule("yield_expr"),
							b => b.Rule("star_expressions")
						)
						.Optional(b => b.Token("TYPE_COMMENT")),
					b => b
						.Rule("single_target")
						.Rule("augassign")
						.Choice(
							b => b.Rule("yield_expr"),
							b => b.Rule("star_expressions")
						)
				);

			builder.CreateRule("annotated_rhs")
				.Choice(
					b => b.Rule("yield_expr"),
					b => b.Rule("star_expressions")
				);

			builder.CreateRule("augassign")
				.Token("augassign");

			builder.CreateToken("augassign")
				.LiteralChoice("+=", "-=", "*=", "@=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", "**=", "//=");

			builder.CreateRule("return_stmt")
				.Keyword("return")
				.Optional(b => b.Rule("star_expressions"));

			builder.CreateRule("raise_stmt")
				.Keyword("raise")
				.Optional(b => b.Rule("expression")
				.Optional(b => b.Keyword("from")
				.Rule("expression")));

			builder.CreateRule("global_stmt")
				.Keyword("global")
				.Rule("name")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("name"));

			builder.CreateRule("nonlocal_stmt")
				.Keyword("nonlocal")
				.Rule("name")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("name"));

			builder.CreateRule("del_stmt")
				.Keyword("del")
				.Rule("del_targets");

			builder.CreateRule("yield_stmt")
				.Rule("yield_expr");

			builder.CreateRule("assert_stmt")
				.Keyword("assert")
				.Rule("expression")
				.Optional(b => b.Literal(',')
				.Rule("expression"));

			builder.CreateRule("import_stmt")
				.Choice(
					b => b.Rule("import_name"),
					b => b.Rule("import_from")
				);

			builder.CreateRule("import_name")
				.Keyword("import")
				.Rule("dotted_as_names");

			builder.CreateRule("import_from")
				.Choice(
					b => b
						.Keyword("from")
						.ZeroOrMore(b => b.LiteralChoice(".", "..."))
						.Rule("dotted_name")
						.Keyword("import")
						.Rule("import_from_targets"),
					b => b
						.Keyword("from")
						.OneOrMore(b => b.LiteralChoice(".", "..."))
						.Keyword("import")
						.Rule("import_from_targets")
				);

			builder.CreateRule("import_from_targets")
				.Choice(
					b => b
						.Literal('(')
						.Rule("import_from_as_names")
						.Optional(b => b.Literal(','))
						.Literal(')'),
					b => b.Rule("import_from_as_names"),
					b => b.Literal("*")
				);

			builder.CreateRule("import_from_as_names")
				.Rule("import_from_as_name")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("import_from_as_name"));

			builder.CreateRule("import_from_as_name")
				.Rule("name")
				.Optional(b => b.Keyword("as")
				.Rule("name"));

			builder.CreateRule("dotted_as_names")
				.Rule("dotted_as_name")
				.ZeroOrMore(b => b.Literal(',')
					.Rule("dotted_as_name"));

			builder.CreateRule("dotted_as_name")
				.Rule("dotted_name")
				.Optional(b => b.Keyword("as")
				.Rule("name"));

			builder.CreateRule("dotted_name")
				.Rule("name")
				.Rule("dotted_name_tail");

			builder.CreateRule("dotted_name_tail")
				.Choice(
					b => b
						.Literal(".")
						.Rule("name")
						.Rule("dotted_name_tail"),
					b => b.Empty()
				);

			builder.CreateRule("block")
				.Choice(
					b => b
						.Token("NEWLINE")
						.Token("INDENT")
						.Rule("statements")
						.Token("DEDENT"),
					b => b.Rule("simple_stmts")
				);

			builder.CreateRule("decorators")
				.OneOrMore(b => b.Literal("@")
				.Rule("named_expression")
				.Token("NEWLINE"));

			builder.CreateRule("class_def")
				.Choice(
					b => b
						.Rule("decorators")
						.Rule("class_def_raw"),
					b => b.Rule("class_def_raw")
				);

			builder.CreateRule("class_def_raw")
				.Keyword("class")
				.Rule("name")
				.Optional(b => b.Rule("type_params"))
				.Optional(b => b.Literal('(')
				.Optional(b => b.Rule("arguments"))
				.Literal(')'))
				.Literal(':')
				.Rule("block");

			builder.CreateRule("function_def")
				.Choice(
					b => b
						.Rule("decorators")
						.Rule("function_def_raw"),
					b => b.Rule("function_def_raw")
				);

			builder.CreateRule("function_def_raw")
				.Optional(b => b.Keyword("async"))
				.Keyword("def")
				.Rule("name")
				.Optional(b => b.Rule("type_params"))
				.Literal('(')
				.Optional(b => b.Rule("params"))
				.Literal(')')
				.Optional(b => b.Literal("->")
				.Rule("expression"))
				.Literal(':')
				.Optional(b => b.Rule("func_type_comment"))
				.Rule("block");

			builder.CreateRule("params")
				.Rule("parameters");

			builder.CreateRule("parameters")
				.Choice(
					b => b
						.Rule("slash_no_default")
						.ZeroOrMore(b => b.Rule("param_no_default"))
						.ZeroOrMore(b => b.Rule("param_with_default"))
						.Optional(b => b.Rule("star_etc")),
					b => b
						.Rule("slash_with_default")
						.ZeroOrMore(b => b.Rule("param_with_default"))
						.Optional(b => b.Rule("star_etc")),
					b => b
						.OneOrMore(b => b.Rule("param_no_default"))
						.ZeroOrMore(b => b.Rule("param_with_default"))
						.Optional(b => b.Rule("star_etc")),
					b => b
						.OneOrMore(b => b.Rule("param_with_default"))
						.Optional(b => b.Rule("star_etc")),
					b => b.Rule("star_etc")
				);

			builder.CreateRule("slash_no_default")
				.OneOrMore(b => b.Rule("param_no_default"))
				.Literal("/")
				.Optional(b => b.Literal(','))
				.Choice(
					b => b.Literal(','),
					b => b.PositiveLookahead(b => b.Literal(')'))
				);

			builder.CreateRule("slash_with_default")
				.ZeroOrMore(b => b.Rule("param_no_default"))
				.OneOrMore(b => b.Rule("param_with_default"))
				.Literal("/")
				.Choice(
					b => b.Literal(','),
					b => b.PositiveLookahead(b => b.Literal(')'))
				);

			builder.CreateRule("star_etc")
				.Choice(
					b => b
						.Literal("*")
						.Rule("param_no_default")
						.ZeroOrMore(b => b.Rule("param_maybe_default"))
						.Optional(b => b.Rule("kwds")),
					b => b
						.Literal("*")
						.Rule("param_no_default_star_annotation")
						.ZeroOrMore(b => b.Rule("param_maybe_default"))
						.Optional(b => b.Rule("kwds")),
					b => b
						.Literal("*")
						.Literal(',')
						.OneOrMore(b => b.Rule("param_maybe_default"))
						.Optional(b => b.Rule("kwds")),
					b => b.Rule("kwds")
				);

			builder.CreateRule("kwds")
				.Literal("**")
				.Rule("param_no_default");

			builder.CreateRule("param_no_default")
				.Rule("param")
				.Choice(
					b => b
						.Literal(',')
						.Optional(b => b.Token("TYPE_COMMENT")),
					b => b
						.Optional(b => b.Token("TYPE_COMMENT"))
						.PositiveLookahead(b => b.Literal(')'))
				);

			builder.CreateRule("param_no_default_star_annotation")
				.Rule("param_star_annotation")
				.Choice(
					b => b
						.Literal(',')
						.Optional(b => b.Token("TYPE_COMMENT")),
					b => b
						.Optional(b => b.Token("TYPE_COMMENT"))
						.PositiveLookahead(b => b.Literal(')'))
				);

			builder.CreateRule("param_with_default")
				.Rule("param")
				.Rule("default_assignment")
				.Choice(
					b => b
						.Literal(',')
						.Optional(b => b.Token("TYPE_COMMENT")),
					b => b
						.Optional(b => b.Token("TYPE_COMMENT"))
						.PositiveLookahead(b => b.Literal(')'))
				);

			builder.CreateRule("param_maybe_default")
				.Rule("param")
				.Optional(b => b.Rule("default_assignment"))
				.Choice(
					b => b
						.Literal(',')
						.Optional(b => b.Token("TYPE_COMMENT")),
					b => b
						.Optional(b => b.Token("TYPE_COMMENT"))
						.PositiveLookahead(b => b.Literal(')'))
				);

			builder.CreateRule("param")
				.Rule("name")
				.Optional(b => b.Rule("annotation"));

			builder.CreateRule("param_star_annotation")
				.Rule("name")
				.Rule("star_annotation");

			builder.CreateRule("annotation")
				.Literal(':')
				.Rule("expression");

			builder.CreateRule("star_annotation")
				.Literal(':')
				.Rule("star_expression");

			builder.CreateRule("default_assignment")
				.Literal('=')
				.Rule("expression");

			builder.CreateRule("if_stmt")
				.Keyword("if")
				.Rule("named_expression")
				.Literal(':')
				.Rule("block")
				.Choice(
					b => b.Rule("elif_stmt"),
					b => b.Optional(b => b.Rule("else_block"))
				);

			builder.CreateRule("elif_stmt")
				.Keyword("elif")
				.Rule("named_expression")
				.Literal(':')
				.Rule("block")
				.Choice(
					b => b.Rule("elif_stmt"),
					b => b.Optional(b => b.Rule("else_block"))
				);

			builder.CreateRule("else_block")
				.Keyword("else")
				.Literal(':')
				.Rule("block");

			builder.CreateRule("while_stmt")
				.Keyword("while")
				.Rule("named_expression")
				.Literal(':')
				.Rule("block")
				.Optional(b => b.Rule("else_block"));

			builder.CreateRule("for_stmt")
				.Optional(b => b.Keyword("async"))
				.Keyword("for")
				.Rule("star_targets")
				.Keyword("in")
				.Rule("star_expressions")
				.Literal(':')
				.Optional(b => b.Token("TYPE_COMMENT"))
				.Rule("block")
				.Optional(b => b.Rule("else_block"));

			builder.CreateRule("with_stmt")
				.Choice(
					b => b
						.Keyword("with")
						.Literal('(')
						.Rule("with_item")
						.ZeroOrMore(b => b.Literal(',')
						.Rule("with_item"))
						.Optional(b => b.Literal(','))
						.Literal(')')
						.Literal(':')
						.Optional(b => b.Token("TYPE_COMMENT"))
						.Rule("block"),
					b => b
						.Keyword("async")
						.Keyword("with")
						.Literal('(')
						.Rule("with_item")
						.ZeroOrMore(b => b.Literal(',')
						.Rule("with_item"))
						.Optional(b => b.Literal(','))
						.Literal(')')
						.Literal(':')
						.Rule("block"),
					b => b
						.Optional(b => b.Keyword("async"))
						.Keyword("with")
						.Rule("with_item")
						.ZeroOrMore(b => b.Literal(',')
						.Rule("with_item"))
						.Literal(':')
						.Optional(b => b.Token("TYPE_COMMENT"))
						.Rule("block")
				);

			builder.CreateRule("with_item")
				.Rule("expression")
				.Optional(b => b.Keyword("as")
				.Rule("star_target"));

			builder.CreateRule("try_stmt")
				.Choice(
					b => b
						.Keyword("try")
						.Literal(':')
						.Rule("block")
						.Rule("finally_block"),
					b => b
						.Keyword("try")
						.Literal(':')
						.Rule("block")
						.OneOrMore(b => b.Rule("except_block"))
						.Optional(b => b.Rule("else_block"))
						.Optional(b => b.Rule("finally_block")),
					b => b
						.Keyword("try")
						.Literal(':')
						.Rule("block")
						.OneOrMore(b => b.Rule("except_star_block"))
						.Optional(b => b.Rule("else_block"))
						.Optional(b => b.Rule("finally_block"))
				);

			builder.CreateRule("except_block")
				.Keyword("except")
				.Optional(b => b.Rule("expression")
				.Optional(b => b.Keyword("as")
				.Rule("name")))
				.Literal(':')
				.Rule("block");

			builder.CreateRule("except_star_block")
				.Keyword("except")
				.Literal("*")
				.Rule("expression")
				.Optional(b => b.Keyword("as")
				.Rule("name"))
				.Literal(':')
				.Rule("block");

			builder.CreateRule("finally_block")
				.Keyword("finally")
				.Literal(':')
				.Rule("block");

			builder.CreateRule("match_stmt")
				.Keyword("match")
				.Rule("subject_expr")
				.Literal(':')
				.Token("NEWLINE")
				.Token("INDENT")
				.OneOrMore(b => b.Rule("case_block"))
				.Token("DEDENT");

			builder.CreateRule("subject_expr")
				.Choice(
					b => b
						.Rule("star_named_expression")
						.Literal(',')
						.Optional(b => b.Rule("star_named_expressions")),
					b => b.Rule("named_expression")
				);

			builder.CreateRule("case_block")
				.Keyword("case")
				.Rule("patterns")
				.Optional(b => b.Rule("guard"))
				.Literal(':')
				.Rule("block");

			builder.CreateRule("guard")
				.Keyword("if")
				.Rule("named_expression");

			builder.CreateRule("patterns")
				.Choice(
					b => b.Rule("open_sequence_pattern"),
					b => b.Rule("pattern")
				);

			builder.CreateRule("pattern")
				.Choice(
					b => b.Rule("as_pattern"),
					b => b.Rule("or_pattern")
				);

			builder.CreateRule("as_pattern")
				.Rule("or_pattern")
				.Keyword("as")
				.Rule("pattern_capture_target");

			builder.CreateRule("or_pattern")
				.Rule("closed_pattern")
				.ZeroOrMore(b => b.Literal("|")
				.Rule("closed_pattern"));

			builder.CreateRule("closed_pattern")
				.Choice(
					b => b.Rule("literal_pattern"),
					b => b.Rule("capture_pattern"),
					b => b.Rule("wildcard_pattern"),
					b => b.Rule("value_pattern"),
					b => b.Rule("group_pattern"),
					b => b.Rule("sequence_pattern"),
					b => b.Rule("mapping_pattern"),
					b => b.Rule("class_pattern")
				);

			builder.CreateRule("literal_pattern")
				.Choice(
					b => b.Rule("signed_number"),
					b => b.Rule("complex_number"),
					b => b.Rule("strings"),
					b => b.Keyword("None"),
					b => b.Keyword("True"),
					b => b.Keyword("False")
				);

			builder.CreateRule("literal_expr")
				.Choice(
					b => b.Rule("signed_number"),
					b => b.Rule("complex_number"),
					b => b.Rule("strings"),
					b => b.Keyword("None"),
					b => b.Keyword("True"),
					b => b.Keyword("False")
				);

			builder.CreateRule("complex_number")
				.Rule("signed_real_number")
				.LiteralChoice("+", "-")
				.Rule("imaginary_number");

			builder.CreateRule("signed_number")
				.Optional(b => b.Literal("-"))
				.Token("NUMBER");

			builder.CreateRule("signed_real_number")
				.Optional(b => b.Literal("-"))
				.Rule("real_number");

			builder.CreateRule("real_number")
				.Token("NUMBER");

			builder.CreateRule("imaginary_number")
				.Token("NUMBER");

			builder.CreateRule("capture_pattern")
				.Rule("pattern_capture_target");

			builder.CreateRule("pattern_capture_target")
				.Rule("name_except_underscore");

			builder.CreateRule("wildcard_pattern")
				.Keyword("_");

			builder.CreateRule("value_pattern")
				.Rule("attr");

			builder.CreateRule("attr")
				.Rule("name")
				.OneOrMore(b => b.Literal(".")
				.Rule("name"));

			builder.CreateRule("name_or_attr")
				.Rule("name")
				.ZeroOrMore(b => b.Literal(".")
				.Rule("name"));

			builder.CreateRule("group_pattern")
				.Literal('(')
				.Rule("pattern")
				.Literal(')');

			builder.CreateRule("sequence_pattern")
				.Choice(
					b => b
						.Literal('[')
						.Optional(b => b.Rule("maybe_sequence_pattern"))
						.Literal(']'),
					b => b
						.Literal('(')
						.Optional(b => b.Rule("open_sequence_pattern"))
						.Literal(')')
				);

			builder.CreateRule("open_sequence_pattern")
				.Rule("maybe_star_pattern")
				.Literal(',')
				.Optional(b => b.Rule("maybe_sequence_pattern"));

			builder.CreateRule("maybe_sequence_pattern")
				.Rule("maybe_star_pattern")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("maybe_star_pattern"))
				.Optional(b => b.Literal(','));

			builder.CreateRule("maybe_star_pattern")
				.Choice(
					b => b.Rule("star_pattern"),
					b => b.Rule("pattern")
				);

			builder.CreateRule("star_pattern")
				.Literal("*")
				.Rule("name");

			builder.CreateRule("mapping_pattern")
				.Choice(
					b => b
						.Literal('{')
						.Literal('}'),
					b => b
						.Literal('{')
						.Rule("double_star_pattern")
						.Optional(b => b.Literal(','))
						.Literal('}'),
					b => b
						.Literal('{')
						.Rule("items_pattern")
						.Optional(b => b.Literal(',')
						.Rule("double_star_pattern"))
						.Optional(b => b.Literal(','))
						.Literal('}')
				);

			builder.CreateRule("items_pattern")
				.Rule("key_value_pattern")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("key_value_pattern"));

			builder.CreateRule("key_value_pattern")
				.Choice(
					b => b.Rule("literal_expr"),
					b => b.Rule("attr")
				)
				.Literal(':')
				.Rule("pattern");

			builder.CreateRule("double_star_pattern")
				.Literal("**")
				.Rule("pattern_capture_target");

			builder.CreateRule("class_pattern")
				.Rule("name_or_attr")
				.Literal('(')
				.Optional(b => b.Choice(
					b => b
						.Rule("positional_patterns")
						.Optional(b => b.Literal(',')
						.Rule("keyword_patterns")),
					b => b.Rule("keyword_patterns")
				)
				.Optional(b => b.Literal(',')))
				.Literal(')');

			builder.CreateRule("positional_patterns")
				.Rule("pattern")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("pattern"));

			builder.CreateRule("keyword_patterns")
				.Rule("keyword_pattern")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("keyword_pattern"));

			builder.CreateRule("keyword_pattern")
				.Rule("name")
				.Literal('=')
				.Rule("pattern");

			builder.CreateRule("type_alias")
				.Keyword("type")
				.Rule("name")
				.Optional(b => b.Rule("type_params"))
				.Literal('=')
				.Rule("expression");

			builder.CreateRule("type_params")
				.Literal('[')
				.Rule("type_param_seq")
				.Literal(']');

			builder.CreateRule("type_param_seq")
				.Rule("type_param")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("type_param"))
				.Optional(b => b.Literal(','));

			builder.CreateRule("type_param")
				.Choice(
					b => b
						.Rule("name")
						.Optional(b => b.Rule("type_param_bound"))
						.Optional(b => b.Rule("type_param_default")),
					b => b
						.Literal("*")
						.Rule("name")
						.Optional(b => b.Rule("type_param_starred_default")),
					b => b
						.Literal("**")
						.Rule("name")
						.Optional(b => b.Rule("type_param_default"))
				);

			builder.CreateRule("type_param_bound")
				.Literal(':')
				.Rule("expression");

			builder.CreateRule("type_param_default")
				.Literal('=')
				.Rule("expression");

			builder.CreateRule("type_param_starred_default")
				.Literal('=')
				.Rule("star_expression");

			builder.CreateRule("expressions")
				.Rule("expression")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("expression"))
				.Optional(b => b.Literal(','));

			builder.CreateRule("expression")
				.Choice(
					b => b.Rule("lambdef"),
					b => b
						.Rule("disjunction")
						.Optional(b => b.Keyword("if")
						.Rule("disjunction")
						.Keyword("else")
						.Rule("expression"))
				);

			builder.CreateRule("yield_expr")
				.Keyword("yield")
				.Choice(
					b => b
						.Keyword("from")
						.Rule("expression"),
					b => b.Optional(b => b.Rule("star_expressions"))
				);

			builder.CreateRule("star_expressions")
				.Rule("star_expression")
				.ZeroOrMore(b => b.Literal(',')
					.Rule("star_expression"))
				.Optional(b => b.Literal(','));

			builder.CreateRule("star_expression")
				.Choice(
					b => b
						.Literal("*")
						.Rule("bitwise_or"),
					b => b.Rule("expression")
				);

			builder.CreateRule("star_named_expressions")
				.Rule("star_named_expression")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("star_named_expression"))
				.Optional(b => b.Literal(','));

			builder.CreateRule("star_named_expression")
				.Choice(
					b => b
						.Literal("*")
						.Rule("bitwise_or"),
					b => b.Rule("named_expression")
				);

			builder.CreateRule("assignment_expression")
				.Rule("name")
				.Literal(":=")
				.Rule("expression");

			builder.CreateRule("named_expression")
				.Choice(
					b => b.Rule("assignment_expression"),
					b => b.Rule("expression")
				);

			builder.CreateRule("disjunction")
				.Rule("conjunction")
				.ZeroOrMore(b => b.Keyword("or")
				.Rule("conjunction"));

			builder.CreateRule("conjunction")
				.Rule("inversion")
				.ZeroOrMore(b => b.Keyword("and")
				.Rule("inversion"));

			builder.CreateRule("inversion")
				.Choice(
					b => b
						.Keyword("not")
						.Rule("inversion"),
					b => b.Rule("comparison")
				);

			builder.CreateRule("comparison")
				.Rule("bitwise_or")
				.ZeroOrMore(b => b.Rule("compare_op_bitwise_or_pair"));

			builder.CreateRule("compare_op_bitwise_or_pair")
				.Choice(
					b => b.Rule("eq_bitwise_or"),
					b => b.Rule("noteq_bitwise_or"),
					b => b.Rule("lte_bitwise_or"),
					b => b.Rule("lt_bitwise_or"),
					b => b.Rule("gte_bitwise_or"),
					b => b.Rule("gt_bitwise_or"),
					b => b.Rule("notin_bitwise_or"),
					b => b.Rule("in_bitwise_or"),
					b => b.Rule("isnot_bitwise_or"),
					b => b.Rule("is_bitwise_or")
				);

			builder.CreateRule("eq_bitwise_or")
				.Literal("==")
				.Rule("bitwise_or");

			builder.CreateRule("noteq_bitwise_or")
				.Literal("!=")
				.Rule("bitwise_or");

			builder.CreateRule("lte_bitwise_or")
				.Literal("<=")
				.Rule("bitwise_or");

			builder.CreateRule("lt_bitwise_or")
				.Literal("<")
				.Rule("bitwise_or");

			builder.CreateRule("gte_bitwise_or")
				.Literal(">=")
				.Rule("bitwise_or");

			builder.CreateRule("gt_bitwise_or")
				.Literal(">")
				.Rule("bitwise_or");

			builder.CreateRule("notin_bitwise_or")
				.Keyword("not")
				.Keyword("in")
				.Rule("bitwise_or");

			builder.CreateRule("in_bitwise_or")
				.Keyword("in")
				.Rule("bitwise_or");

			builder.CreateRule("isnot_bitwise_or")
				.Keyword("is")
				.Keyword("not")
				.Rule("bitwise_or");

			builder.CreateRule("is_bitwise_or")
				.Keyword("is")
				.Rule("bitwise_or");

			builder.CreateRule("bitwise_or")
				.OneOrMoreSeparated(
					b => b.Rule("bitwise_xor"),
					s => s.Token(b => b.NegativeLookahead(b => b.Token("augassign")).Literal("|"))
				);

			builder.CreateRule("bitwise_or_tail")
				.Choice(
					b => b
						.Literal("|")
						.Rule("bitwise_xor")
						.Rule("bitwise_or_tail"),
					b => b.Empty()
				);

			builder.CreateRule("bitwise_xor")
				.OneOrMoreSeparated(
					b => b.Rule("bitwise_and"),
					s => s.Token(b => b.NegativeLookahead(b => b.Token("augassign")).Literal("^"))
				);

			builder.CreateRule("bitwise_and")
				.OneOrMoreSeparated(
					b => b.Rule("shift_expr"),
					s => s.Token(b => b.NegativeLookahead(b => b.Token("augassign")).Literal("&"))
				);

			builder.CreateRule("shift_expr")
				.OneOrMoreSeparated(
					b => b.Rule("sum"),
					s => s.Token(b => b.NegativeLookahead(b => b.Token("augassign")).LiteralChoice("<<", ">>"))
				);

			builder.CreateRule("sum")
				.OneOrMoreSeparated(
					b => b.Rule("term"),
					s => s.Token(b => b.NegativeLookahead(b => b.Token("augassign")).LiteralChoice("+", "-"))
				);

			builder.CreateRule("term")
				.OneOrMoreSeparated(
					b => b.Rule("factor"),
					s => s.Token(b => b.NegativeLookahead(b => b.Token("augassign")).LiteralChoice("*", "/", "//", "%", "@"))
				);

			builder.CreateRule("factor")
				.ZeroOrMore(b => b.Token(b => b.NegativeLookahead(b => b.Token("augassign")).LiteralChoice("+", "-", "~")))
				.Rule("power");

			builder.CreateRule("power")
				.Rule("await_primary")
				.Optional(b => b.Literal("**")
				.Rule("factor"));

			builder.CreateRule("await_primary")
				.Choice(
					b => b
						.Keyword("await")
						.Rule("primary"),
					b => b.Rule("primary")
				);

			builder.CreateRule("primary")
				.Rule("atom")
				.Rule("primary_tail");

			builder.CreateRule("primary_tail")
				.Choice(
					b => b
						.Choice(
							b => b
								.Literal(".")
								.Rule("name"),
							b => b.Rule("genexp"),
							b => b
								.Literal('(')
								.Optional(b => b.Rule("arguments"))
								.Literal(')'),
							b => b
								.Literal('[')
								.Rule("slices")
								.Literal(']')
						)
						.Rule("primary_tail"),
					b => b.Empty()
				);

			builder.CreateRule("slices")
				.Choice(
					b => b.Rule("slice"),
					b => b
						.Choice(
							b => b.Rule("slice"),
							b => b.Rule("starred_expression")
						)
						.ZeroOrMore(b => b.Literal(',')
						.Choice(
							b => b.Rule("slice"),
							b => b.Rule("starred_expression")
						))
						.Optional(b => b.Literal(','))
				);

			builder.CreateRule("slice")
				.Choice(
					b => b
						.Optional(b => b.Rule("expression"))
						.Literal(':')
						.Optional(b => b.Rule("expression"))
						.Optional(b => b.Literal(':')
						.Optional(b => b.Rule("expression"))),
					b => b.Rule("named_expression")
				);

			builder.CreateRule("atom")
				.Choice(
					b => b.Keyword("True"),
					b => b.Keyword("False"),
					b => b.Keyword("None"),
					b => b.Token("NUMBER"),
					b => b.Rule("strings"),
					b => b.Rule("name"),
					b => b.Rule("tuple"),
					b => b.Rule("group"),
					b => b.Rule("genexp"),
					b => b.Rule("list"),
					b => b.Rule("listcomp"),
					b => b.Rule("dict"),
					b => b.Rule("set"),
					b => b.Rule("dictcomp"),
					b => b.Rule("setcomp"),
					b => b.Literal("...")
				);

			builder.CreateRule("group")
				.Literal('(')
				.Choice(
					b => b.Rule("yield_expr"),
					b => b.Rule("named_expression")
				)
				.Literal(')');

			builder.CreateRule("lambdef")
				.Keyword("lambda")
				.Optional(b => b.Rule("lambda_params"))
				.Literal(':')
				.Rule("expression");

			builder.CreateRule("lambda_params")
				.Rule("lambda_parameters");

			builder.CreateRule("lambda_parameters")
				.Choice(
					b => b
						.Rule("lambda_slash_no_default")
						.ZeroOrMore(b => b.Rule("lambda_param_no_default"))
						.ZeroOrMore(b => b.Rule("lambda_param_with_default"))
						.Optional(b => b.Rule("lambda_star_etc")),
					b => b
						.Rule("lambda_slash_with_default")
						.ZeroOrMore(b => b.Rule("lambda_param_with_default"))
						.Optional(b => b.Rule("lambda_star_etc")),
					b => b
						.OneOrMore(b => b.Rule("lambda_param_no_default"))
						.ZeroOrMore(b => b.Rule("lambda_param_with_default"))
						.Optional(b => b.Rule("lambda_star_etc")),
					b => b
						.OneOrMore(b => b.Rule("lambda_param_with_default"))
						.Optional(b => b.Rule("lambda_star_etc")),
					b => b.Rule("lambda_star_etc")
				);

			builder.CreateRule("lambda_slash_no_default")
				.OneOrMore(b => b.Rule("lambda_param_no_default"))
				.Literal("/")
				.Optional(b => b.Literal(','));

			builder.CreateRule("lambda_slash_with_default")
				.ZeroOrMore(b => b.Rule("lambda_param_no_default"))
				.OneOrMore(b => b.Rule("lambda_param_with_default"))
				.Literal("/")
				.Optional(b => b.Literal(','));

			builder.CreateRule("lambda_star_etc")
				.Choice(
					b => b
						.Literal("*")
						.Rule("lambda_param_no_default")
						.ZeroOrMore(b => b.Rule("lambda_param_maybe_default"))
						.Optional(b => b.Rule("lambda_kwds")),
					b => b
						.Literal("*")
						.Literal(',')
						.OneOrMore(b => b.Rule("lambda_param_maybe_default"))
						.Optional(b => b.Rule("lambda_kwds")),
					b => b.Rule("lambda_kwds")
				);

			builder.CreateRule("lambda_kwds")
				.Literal("**")
				.Rule("lambda_param_no_default");

			builder.CreateRule("lambda_param_no_default")
				.Rule("lambda_param")
				.Optional(b => b.Literal(','));

			builder.CreateRule("lambda_param_with_default")
				.Rule("lambda_param")
				.Rule("default_assignment")
				.Optional(b => b.Literal(','));

			builder.CreateRule("lambda_param_maybe_default")
				.Rule("lambda_param")
				.Optional(b => b.Rule("default_assignment"))
				.Optional(b => b.Literal(','));

			builder.CreateRule("lambda_param")
				.Rule("name");

			builder.CreateRule("list")
				.Literal('[')
				.Optional(b => b.Rule("star_named_expressions"))
				.Literal(']');

			builder.CreateRule("tuple")
				.Literal('(')
				.Optional(b => b.Rule("star_named_expression")
				.Literal(',')
				.Optional(b => b.Rule("star_named_expressions")))
				.Literal(')');

			builder.CreateRule("set")
				.Literal('{')
				.Rule("star_named_expressions")
				.Literal('}');

			builder.CreateRule("dict")
				.Literal('{')
				.Optional(b => b.Rule("double_starred_kvpairs"))
				.Literal('}');

			builder.CreateRule("double_starred_kvpairs")
				.Rule("double_starred_kvpair")
				.ZeroOrMore(b => b.Literal(',')
				.Rule("double_starred_kvpair"))
				.Optional(b => b.Literal(','));

			builder.CreateRule("double_starred_kvpair")
				.Choice(
					b => b
						.Literal("**")
						.Rule("bitwise_or"),
					b => b.Rule("kvpair")
				);

			builder.CreateRule("kvpair")
				.Rule("expression")
				.Literal(':')
				.Rule("expression");

			builder.CreateRule("for_if_clauses")
				.OneOrMore(b => b.Rule("for_if_clause"));

			builder.CreateRule("for_if_clause")
				.Optional(b => b.Keyword("async"))
				.Keyword("for")
				.Rule("star_targets")
				.Keyword("in")
				.Rule("disjunction")
				.ZeroOrMore(b => b.Keyword("if")
				.Rule("disjunction"));

			builder.CreateRule("listcomp")
				.Literal('[')
				.Rule("named_expression")
				.Rule("for_if_clauses")
				.Literal(']');

			builder.CreateRule("setcomp")
				.Literal('{')
				.Rule("named_expression")
				.Rule("for_if_clauses")
				.Literal('}');

			builder.CreateRule("genexp")
				.Literal('(')
				.Choice(
					b => b.Rule("assignment_expression"),
					b => b.Rule("expression")
				)
				.Rule("for_if_clauses")
				.Literal(')');

			builder.CreateRule("dictcomp")
				.Literal('{')
				.Rule("kvpair")
				.Rule("for_if_clauses")
				.Literal('}');

			builder.CreateRule("arguments")
				.Rule("args")
				.Optional(b => b.Literal(','))
				.PositiveLookahead(b => b.Literal(')'));

			builder.CreateRule("args")
				.Choice(
					b => b.Rule("kwargs"),
					b => b
						.OneOrMoreSeparated(
							b => b.Choice(
								b => b.Rule("starred_expression"),
								b => b
									.Choice(
										b => b.Rule("assignment_expression"),
										b => b.Rule("expression").NegativeLookahead(b => b.Literal(":="))
									)
									.NegativeLookahead(b => b.Literal('='))),
							b => b.Literal(',')
						)
						.Optional(b => b
							.Literal(',')
							.Rule("kwargs"))
				);

			builder.CreateRule("kwargs")
				.Choice(
					b => b
						.Rule("kwarg_or_starred")
						.ZeroOrMore(b => b.Literal(',')
							.Rule("kwarg_or_starred"))
						.Optional(b => b.Literal(',')
							.Rule("kwarg_or_double_starred")
							.ZeroOrMore(b => b.Literal(',')
								.Rule("kwarg_or_double_starred"))),
					b => b
						.Rule("kwarg_or_double_starred")
						.ZeroOrMore(b => b.Literal(',')
							.Rule("kwarg_or_double_starred"))
				);

			builder.CreateRule("starred_expression")
				.Literal("*")
				.Rule("expression");

			builder.CreateRule("kwarg_or_starred")
				.Choice(
					b => b
						.Rule("name")
						.Literal('=')
						.Rule("expression"),
					b => b.Rule("starred_expression")
				);

			builder.CreateRule("kwarg_or_double_starred")
				.Choice(
					b => b
						.Rule("name")
						.Literal('=')
						.Rule("expression"),
					b => b
						.Literal("**")
						.Rule("expression")
				);

			builder.CreateRule("star_targets")
				.OneOrMoreSeparated(b => b.Rule("star_target"), b => b.Literal(','),
				allowTrailingSeparator: true);

			builder.CreateRule("star_targets_list_seq")
				.OneOrMoreSeparated(b => b.Rule("star_target"), b => b.Literal(','),
				allowTrailingSeparator: true);

			builder.CreateRule("star_targets_tuple_seq")
				.Rule("star_target")
				.Choice(
					b => b.Literal(','),
					b => b
						.OneOrMore(b => b.Literal(',')
							.Rule("star_target"))
						.Optional(b => b.Literal(','))
				);

			builder.CreateRule("star_target")
				.Choice(
					b => b
						.Literal("*")
						.Rule("star_target"),
					b => b.Rule("target_with_star_atom")
				);

			builder.CreateRule("target_with_star_atom")
				.Choice(
					b => b.Rule("t_primary"),
					b => b.Rule("star_atom")
				);

			builder.CreateRule("star_atom")
				.Choice(
					b => b.Rule("name"),
					b => b
						.Literal('(')
						.Rule("target_with_star_atom")
						.Literal(')'),
					b => b
						.Literal('(')
						.Optional(b => b.Rule("star_targets_tuple_seq"))
						.Literal(')'),
					b => b
						.Literal('[')
						.Optional(b => b.Rule("star_targets_list_seq"))
						.Literal(']')
				);

			builder.CreateRule("single_target")
				.Choice(
					b => b.Rule("single_subscript_attribute_target"),
					b => b.Rule("name"),
					b => b
						.Literal('(')
						.Rule("single_target")
						.Literal(')')
				);

			builder.CreateRule("single_subscript_attribute_target")
				.Rule("t_primary")
				.Optional(
					b => b
						.Literal('[')
						.Rule("slices")
						.Literal(']')
				);

			builder.CreateRule("t_primary")
				.Rule("atom")
				.ZeroOrMore(b => b
					.Choice(
						b => b
							.Literal(".")
							.Rule("name"),
						b => b
							.Literal('[')
							.Rule("slices")
							.Literal(']'),
						b => b.Rule("genexp"),
						b => b
							.Literal('(')
							.Optional(b => b.Rule("arguments"))
							.Literal(')')
				));

			builder.CreateRule("del_targets")
				.OneOrMoreSeparated(b => b.Rule("del_target"), b => b.Literal(','));

			builder.CreateRule("del_target")
				.Choice(
					b => b
						.Rule("t_primary")
						.Optional(
							b => b
								.Literal('[')
								.Rule("slices")
								.Literal(']')
						),
					b => b.Rule("del_t_atom")
				);

			builder.CreateRule("del_t_atom")
				.Choice(
					b => b.Rule("name"),
					b => b
						.Literal('(')
						.Optional(b => b.Rule("del_targets"))
						.Literal(')'),
					b => b
						.Literal('[')
						.Optional(b => b.Rule("del_targets"))
						.Literal(']')
				);

			builder.CreateRule("type_expressions")
				.Choice(
					b => b
						.Rule("expression")
						.ZeroOrMore(b => b.Literal(',')
						.Rule("expression"))
						.Optional(b => b.Literal(',')
						.Choice(
							b => b
								.Literal("*")
								.Rule("expression")
								.Optional(b => b.Literal(',')
								.Literal("**")
								.Rule("expression")),
							b => b
								.Literal("**")
								.Rule("expression")
						)),
					b => b
						.Literal("*")
						.Rule("expression")
						.Optional(b => b.Literal(',')
						.Literal("**")
						.Rule("expression")),
					b => b
						.Literal("**")
						.Rule("expression")
				);

			builder.CreateRule("func_type_comment")
				.Choice(
					b => b
						.Token("NEWLINE")
						.Token("TYPE_COMMENT"),
					b => b.Token("TYPE_COMMENT")
				);

			builder.CreateRule("name_except_underscore")
				.Choice(
					b => b.Token("NAME"),
					b => b.Token("NAME_OR_TYPE"),
					b => b.Token("NAME_OR_MATCH"),
					b => b.Token("NAME_OR_CASE")
				);

			builder.CreateRule("name")
				.Choice(
					b => b.Token("NAME"),
					b => b.Token("NAME_OR_TYPE"),
					b => b.Token("NAME_OR_MATCH"),
					b => b.Token("NAME_OR_CASE"),
					b => b.Token("NAME_OR_WILDCARD")
				);

			// TOKENS

			builder.CreateToken("TYPE_COMMENT")
				.Empty();

			builder.CreateToken("NAME_OR_TYPE")
				.Keyword("type");

			builder.CreateToken("NAME_OR_MATCH")
				.Keyword("match");

			builder.CreateToken("NAME_OR_CASE")
				.Keyword("case");

			builder.CreateToken("NAME_OR_WILDCARD")
				.Keyword("_");

			builder.CreateToken("NAME")
				.UnicodeIdentifier();

			builder.CreateToken("NUMBER")
				.Number<float>().Optional(b => b.LiteralChoice("j", "J"));

			// STRINGS

			builder.CreateToken("SHORT_STRING_SQ_CONTENT")
				.EscapedText(new Dictionary<string, string>
				{
					["\\t"] = "\t",
					["\\n"] = "\n",
					["\\r"] = "\r",
					["\\'"] = "'",
					["\\\\"] = "\\"
				}, ["\\", "'", "\n", "\r"]);

			builder.CreateToken("SHORT_STRING_DQ_CONTENT")
				.EscapedText(new Dictionary<string, string>
				{
					["\\t"] = "\t",
					["\\n"] = "\n",
					["\\r"] = "\r",
					["\\'"] = "'",
					["\\\""] = "\"",
					["\\\\"] = "\\"
				}, ["\\", "\"", "\n", "\r"]);

			builder.CreateToken("STRING_LITERAL")
				.Optional(b => b.LiteralChoiceIgnoreCase("r", "u"))
				.Choice(
					b => b.Literal("'''").TextUntil("'''").Literal("'''"),
					b => b.Literal("\"\"\"").TextUntil("\"\"\"").Literal("\"\"\""),
					b => b.Literal('\'').Token("SHORT_STRING_SQ_CONTENT").Literal('\''),
					b => b.Literal('"').Token("SHORT_STRING_DQ_CONTENT").Literal('"')
				);

			builder.CreateToken("BYTES_LITERAL")
				.LiteralChoiceIgnoreCase("b", "rb", "br")
				.Choice(
					b => b.Literal("'''").TextUntil("'''").Literal("'''"),
					b => b.Literal("\"\"\"").TextUntil("\"\"\"").Literal("\"\"\""),
					b => b.Literal('\'').Token("SHORT_STRING_SQ_CONTENT").Literal('\''),
					b => b.Literal('"').Token("SHORT_STRING_DQ_CONTENT").Literal('"')
				);

			builder.CreateToken("FSTRING_MIDDLE")
				.TextUntil("{}\"", allowsEmpty: false);

			builder.CreateToken("STRING")
				.Choice(
					b => b.Token("STRING_LITERAL"),
					b => b.Token("BYTES_LITERAL")
				);

			builder.CreateRule("fstring_middle")
				.Choice(
					b => b.Rule("fstring_replacement_field"),
					b => b.Token("FSTRING_MIDDLE")
				);

			builder.CreateRule("fstring_replacement_field")
				.Literal('{')
				.Rule("annotated_rhs")
				.Optional(b => b.Literal('='))
				.Optional(b => b.Rule("fstring_conversion"))
				.Optional(b => b.Rule("fstring_full_format_spec"))
				.Literal('}');

			builder.CreateRule("fstring_conversion")
				.Literal("!")
				.Rule("name");

			builder.CreateRule("fstring_full_format_spec")
				.Literal(':')
				.ZeroOrMore(b => b.Rule("fstring_format_spec"));

			builder.CreateRule("fstring_format_spec")
				.Choice(
					b => b.Token("FSTRING_MIDDLE"),
					b => b.Rule("fstring_replacement_field")
				);

			builder.CreateRule("fstring_middles")
				.ZeroOrMore(b => b.Rule("fstring_middle"));

			builder.CreateRule("fstring")
				.LiteralChoiceIgnoreCase("f", "fr", "rf")
				.Choice(
					b => b.Literal("'''").Rule("fstring_middles").Literal("'''"),
					b => b.Literal("\"\"\"").Rule("fstring_middles").Literal("\"\"\""),
					b => b.Literal('\'').Rule("fstring_middles").Literal('\''),
					b => b.Literal('\"').Rule("fstring_middles").Literal('\"')
				);

			builder.CreateRule("string")
				.Token("STRING");

			builder.CreateRule("strings")
				.OneOrMore(b => b.Choice(
						b => b.Rule("fstring"),
						b => b.Rule("string")
					).Configure(c => c.IgnoreBarriers())
				);

			// SKIP TOKENS

			builder.CreateToken("NEWLINE")
				.Newline();

			builder.CreateToken("BACKSLASH_NEWLINE")
				.Literal("\\").Newline();

			builder.CreateToken("WS")
				.Spaces();

			builder.CreateToken("COMMENT")
				.Literal("#").ZeroOrMoreChars(c => c != '\n' && c != '\r');

			builder.CreateToken("EXPLICIT_LINE_JOINING")
				.Token("BACKSLASH_NEWLINE");

			builder.CreateToken("SKIP")
				.Choice(
					b => b.Token("WS"),
					b => b.Token("COMMENT"),
					b => b.Token("EXPLICIT_LINE_JOINING")
				);

			builder.Settings.Skip(b => b.Token("SKIP"),
				ParserSkippingStrategy.SkipBeforeParsingGreedy);
		}

		public static Parser CreateParser(Action<ParserBuilder>? buildingAction = null)
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			buildingAction?.Invoke(builder);
			return builder.Build();
		}
	}
}