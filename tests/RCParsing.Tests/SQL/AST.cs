using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.SQL
{
#nullable disable

	public class SqlSelectStatement
	{
		public List<SqlSelectItem> Select { get; set; }
		public SqlFromClause From { get; set; }
		public object Where { get; set; }
		public List<object> GroupBy { get; set; }
		public object Having { get; set; }
		public List<SqlOrderByItem> OrderBy { get; set; }
	}

	public class SqlSelectItem
	{
		public object Expression { get; set; }
		public string Alias { get; set; }
	}

	public class SqlFromClause
	{
		public SqlTableSource MainTable { get; set; }
		public List<SqlJoin> Joins { get; set; }
	}

	public class SqlTableSource
	{
		public object Source { get; set; }
		public string Alias { get; set; }
	}

	public class SqlJoin
	{
		public string JoinType { get; set; }
		public SqlTableSource Table { get; set; }
		public object Condition { get; set; }
	}

	public class SqlFunctionCall
	{
		public string FunctionName { get; set; }
		public List<object> Arguments { get; set; }
	}

	public class SqlBinaryExpression
	{
		public object Left { get; set; }
		public string Operator { get; set; }
		public object Right { get; set; }
	}

	public class SqlPropertyExpression
	{
		public object Expression { get; set; }
		public string PropertyName { get; set; }
	}

	public class SqlUnaryExpression
	{
		public string Operator { get; set; }
		public object Operand { get; set; }
	}

	public class SqlInExpression
	{
		public object Column { get; set; }
		public List<object> Values { get; set; }
	}

	public class SqlOrderByItem
	{
		public object Column { get; set; }
		public string Direction { get; set; }
	}
}