using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.SQL
{
	public static class SQLParser
	{
		public static void FillWithRules(ParserBuilder builder)
		{
			builder.Settings.Skip(b => b.Rule("skip"));

			builder.CreateRule("skip")
				.Choice(
					b => b.Whitespaces(),
					b => b.Literal("--").TextUntil('\n', '\r'), // Singleline comments
					b => b.Literal("/*").TextUntil("*/").Literal("*/")) // Multiline comments
				.ConfigureForSkip();

			builder.CreateToken("number")
				.Number<double>();

			builder.CreateToken("string")
				.Between(
					b => b.Literal('\''),
					b => b.EscapedTextDoubleChars('\''),
					b => b.Literal('\'')
				);

			builder.CreateToken("identifier")
				.UnicodeIdentifier()
				.Transform(v => v.Text);

			builder.CreateToken("boolean")
				.KeywordChoice("TRUE", "FALSE")
				.Transform(v => v.GetIntermediateValue<string>() == "TRUE");

			builder.CreateRule("function_call")
				.Token("identifier")
				.Literal("(")
				.ZeroOrMoreSeparated(
					b => b.Rule("expression"),
					b => b.Literal(","),
					allowTrailingSeparator: true,
					includeSeparatorsInResult: false)
				.Literal(")")
				.Transform(v => new SqlFunctionCall
				{
					FunctionName = v.GetValue<string>(0),
					Arguments = v.SelectValues<object>(2).ToList()
				});

			builder.CreateRule("primary_expression")
				.Choice(
					b => b.Token("number"),
					b => b.Token("string"),
					b => b.Token("boolean"),
					b => b.Rule("function_call"),
					b => b.Rule("subquery"),
					b => b.Token("identifier").Transform(v => v.Text),
					b => b.Literal('(').Rule("expression").Literal(')').TransformSelect(1));

			builder.CreateRule("prefix_expression")
				.ZeroOrMore(b => b.Choice(
					b => b.Keyword("NOT")
				))
				.Rule("primary_expression")
				.Transform(v =>
				{
					var operators = v.SelectValues<string>(0);
					var operand = v.GetValue<object>(1);
					foreach (var op in operators.Reverse())
						operand = new SqlUnaryExpression { Operator = op, Operand = operand };
					return operand;
				});

			builder.CreateRule("postfix_expression")
				.Rule("primary_expression")
				.ZeroOrMore(b => b.Choice(
					b => b
						.Literal('.')
						.UnicodeIdentifier()
						.Transform(v => new KeyValuePair<int, object>(0, v.GetText(1)))
				))
				.Transform(v =>
				{
					var operand = v.GetValue<object>(0);
					var operators = v.SelectValues<KeyValuePair<int, object>>(1);
					foreach (var op in operators.Reverse())
					{
						operand = op.Key switch
						{
							0 => new SqlPropertyExpression
								{ Expression = operand, PropertyName = (string)op.Value },
							_ => operand
						};
					}
					return operand;
				});

			builder.CreateRule("multiplicative_expression")
				.OneOrMoreSeparated(b => b.Rule("postfix_expression"),
					s => s.LiteralChoice("*", "/"), includeSeparatorsInResult: true)
				.TransformFoldLeft<object, string, object>((v, op, r) =>
					new SqlBinaryExpression { Left = v, Operator = op, Right = r });

			builder.CreateRule("additive_expression")
				.OneOrMoreSeparated(b => b.Rule("multiplicative_expression"),
					s => s.LiteralChoice("+", "-"), includeSeparatorsInResult: true)
				.TransformFoldLeft<object, string, object>((v, op, r) =>
					new SqlBinaryExpression { Left = v, Operator = op, Right = r });

			builder.CreateRule("in_expression")
				.Rule("additive_expression")
				.Optional(b => b
					.Keyword("IN")
					.Literal("(")
					.ZeroOrMoreSeparated(
						b => b.Rule("expression"),
						b => b.Literal(","))
					.Literal(")")
				)
				.Transform(v =>
				{
					var operand = v.GetValue<object>(0);
					var inExpression = v.Children[1];
					if (inExpression.Count > 0)
						operand = new SqlInExpression
						{
							Column = operand,
							Values = inExpression[0].SelectValues<object>(2).ToList()
						};
					return operand;
				});

			builder.CreateRule("comparison_expression")
				.OneOrMoreSeparated(b => b.Rule("in_expression"),
					s => s.LiteralChoice("=", "!=", "<>", "<", ">", "<=", ">=", "LIKE"), includeSeparatorsInResult: true)
				.TransformFoldLeft<object, string, object>((v, op, r) =>
					new SqlBinaryExpression { Left = v, Operator = op, Right = r });

			builder.CreateRule("logical_expression")
				.OneOrMoreSeparated(b => b.Rule("comparison_expression"),
					s => s.KeywordChoice("AND", "OR"), includeSeparatorsInResult: true)
				.TransformFoldLeft<object, string, object>((v, op, r) =>
					new SqlBinaryExpression { Left = v, Operator = op, Right = r });

			builder.CreateRule("expression")
				.Rule("logical_expression");

			// SELECT clause
			builder.CreateRule("select_clause")
				.Keyword("SELECT")
				.Choice(
					b => b.Literal("*").Transform(v => new List<SqlSelectItem> { new SqlSelectItem { Expression = "*", Alias = null } }),
					b => b.OneOrMoreSeparated(
						b => b.Rule("select_item"),
						b => b.Literal(","))
						.TransformLast(v => v.SelectValues<SqlSelectItem>().ToList()))
				.TransformSelect(1);

			builder.CreateRule("select_item")
				.Rule("expression")
				.Optional(b => b.Keyword("AS").Token("identifier"))
				.Transform(v => new SqlSelectItem
				{
					Expression = v.GetValue<object>(0),
					Alias = v.TryGetValue<string>(1)
				});

			// FROM clause with JOIN
			builder.CreateRule("from_clause")
				.Keyword("FROM")
				.Rule("table_source")
				.ZeroOrMore(b => b.Rule("join_clause"))
				.Transform(v => new SqlFromClause
				{
					MainTable = v.GetValue<SqlTableSource>(1),
					Joins = v.SelectValues<SqlJoin>(2).ToList()
				});

			builder.CreateRule("table_source")
				.Choice(
				b => b
					.Token("identifier")
					.Optional(b => b.Choice(
						b => b.Keyword("AS").Token("identifier").TransformSelect(1),
						b => b.Token("identifier")))
					.Transform(v => new SqlTableSource
					{
						Source = v.GetValue<string>(0),
						Alias = v.TryGetValue<string>(1)
					}),
				b => b
					.Rule("subquery")
					.Optional(b => b.Choice(
						b => b.Keyword("AS").Token("identifier").TransformSelect(1),
						b => b.Token("identifier")))
					.Transform(v => new SqlTableSource
					{
						Source = v.GetValue<object>(0),
						Alias = v.TryGetValue<string>(1)
					})
				);

			builder.CreateRule("join_clause")
				.Choice(
					b => b.Keywords("INNER", "JOIN"),
					b => b.Keywords("LEFT", "JOIN"),
					b => b.Keywords("RIGHT", "JOIN"))
				.Rule("table_source")
				.Keyword("ON")
				.Rule("expression")
				.Transform(v => new SqlJoin
				{
					JoinType = v.GetValue<string>(0),
					Table = v.GetValue<SqlTableSource>(1),
					Condition = v.GetValue<object>(3)
				});

			// WHERE clause
			builder.CreateRule("where_clause")
				.Keyword("WHERE")
				.Rule("expression")
				.TransformSelect(1);

			// GROUP BY and HAVING
			builder.CreateRule("group_by_clause")
				.Keywords("GROUP", "BY")
				.OneOrMoreSeparated(
					b => b.Rule("expression"),
					b => b.Literal(","))
					.TransformLast(v => v.SelectValues().ToList())
				.TransformSelect(2);

			builder.CreateRule("having_clause")
				.Keyword("HAVING")
				.Rule("expression")
				.TransformSelect(1);

			// ORDER BY
			builder.CreateRule("order_by_clause")
				.Keywords("ORDER", "BY")
				.OneOrMoreSeparated(
					b => b.Rule("order_by_item"),
					b => b.Literal(","))
					.TransformLast(v => v.SelectValues<SqlOrderByItem>().ToList())
				.TransformSelect(2);

			builder.CreateRule("order_by_item")
				.Rule("expression")
				.Optional(b => b.Choice(
					b => b.Keyword("ASC"),
					b => b.Keyword("DESC")))
				.Transform(v => new SqlOrderByItem
				{
					Column = v.GetValue<object>(0),
					Direction = v.TryGetValue<string>(1) ?? "ASC"
				});

			// Subquery
			builder.CreateRule("subquery")
				.Literal("(")
				.Rule("select_statement")
				.Literal(")")
				.TransformSelect(1);

			// The main SELECT statement rule
			var selectStatement = builder.CreateRule("select_statement");
			selectStatement
				.Rule("select_clause")
				.Rule("from_clause")
				.Optional(b => b.Rule("where_clause"))
				.Optional(b => b.Rule("group_by_clause"))
				.Optional(b => b.Rule("having_clause"))
				.Optional(b => b.Rule("order_by_clause"))
				.Transform(v => new SqlSelectStatement
				{
					Select = v.GetValue<List<SqlSelectItem>>(0),
					From = v.GetValue<SqlFromClause>(1),
					Where = v.TryGetValue<object>(2),
					GroupBy = v.TryGetValue<List<object>>(3),
					Having = v.TryGetValue<object>(4),
					OrderBy = v.TryGetValue<List<SqlOrderByItem>>(5)
				});

			builder.CreateMainRule("sql_query")
				.Rule("select_statement")
				.EOF()
				.TransformSelect(0);
		}

		public static Parser CreateParser(Action<ParserBuilder>? buildAction = null)
		{
			var builder = new ParserBuilder();
			FillWithRules(builder);
			buildAction?.Invoke(builder);
			return builder.Build();
		}
	}
}