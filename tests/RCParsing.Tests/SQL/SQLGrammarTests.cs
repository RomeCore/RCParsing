using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.SQL
{
	public class SQLGrammarTests
	{
		[Fact]
		public void AdvancedSelectQuery()
		{
			var parser = SQLParser.CreateParser(b => b.Settings
				.RecordWalkTrace().SetMaxStepsToDisplay(50));

			string sql =
			"""
			SELECT 
				u.name,
				COUNT(o.id) AS order_count,
				SUM(o.amount) AS total_amount
			FROM users u
			LEFT JOIN orders o ON u.id = o.user_id
			WHERE u.active = TRUE 
				AND u.created_date >= '2023-01-01'
				AND o.status IN ('completed', 'shipped')
			GROUP BY u.id, u.name
			HAVING COUNT(o.id) > 5
			ORDER BY total_amount DESC, u.name ASC
			""";

			var result = parser.Parse<SqlSelectStatement>(sql);
			Assert.NotNull(result);

			var columns = result.Select;
			Assert.Equivalent(new SqlPropertyExpression { Expression = "u", PropertyName = "name" }, columns[0].Expression);
			
			Assert.Equivalent(new SqlFunctionCall { FunctionName = "COUNT", Arguments = new List<object> { new SqlPropertyExpression { Expression = "o", PropertyName = "id" } } }, columns[1].Expression);
			Assert.Equivalent(new SqlFunctionCall { FunctionName = "SUM", Arguments = new List<object> { new SqlPropertyExpression { Expression = "o", PropertyName = "amount" } } }, columns[2].Expression);

			var from = result.From;
			Assert.Equivalent(new SqlTableSource { Source = "users", Alias = "u" }, from.MainTable);
			Assert.Equivalent(new SqlJoin { JoinType = "LEFT", Table = new SqlTableSource { Source = "orders", Alias = "o" }, Condition = new SqlBinaryExpression { Left = new SqlPropertyExpression { Expression = "u", PropertyName = "id" }, Operator = "=", Right = new SqlPropertyExpression { Expression = "o", PropertyName = "user_id" } } }, from.Joins[0]);

			var where = result.Where;
			Assert.Equivalent(new SqlBinaryExpression { Left = new SqlBinaryExpression { Left = new SqlBinaryExpression { Left = new SqlPropertyExpression { Expression = "u", PropertyName = "active" }, Operator = "=", Right = true }, Operator = "AND", Right = new SqlBinaryExpression { Left = new SqlPropertyExpression { Expression = "u", PropertyName = "created_date" }, Operator = ">=", Right = "2023-01-01" } }, Operator = "AND", Right = new SqlInExpression { Column = new SqlPropertyExpression { Expression = "o", PropertyName = "status" }, Values = new List<object> { "completed", "shipped" } } }, where);

			var groupBy = result.GroupBy;
			Assert.Equivalent(new List<object> { new SqlPropertyExpression { Expression = "u", PropertyName = "id" }, new SqlPropertyExpression { Expression = "u", PropertyName = "name" } }, groupBy);

			var having = result.Having;
			Assert.Equivalent(new SqlBinaryExpression { Left = new SqlFunctionCall { FunctionName = "COUNT", Arguments = new List<object> { new SqlPropertyExpression { Expression = "o", PropertyName = "id" } } }, Operator = ">", Right = 5 }, having);

			var orderBy = result.OrderBy;
			Assert.Equivalent(new SqlOrderByItem { Column = "total_amount", Direction = "DESC" }, orderBy[0]);
			Assert.Equivalent(new SqlOrderByItem { Column = new SqlPropertyExpression { Expression = "u", PropertyName = "name" }, Direction = "ASC" }, orderBy[1]);
		}

		[Fact]
		public void ComplexNestedExpressions()
		{
			var parser = SQLParser.CreateParser(b => b.Settings
				.DetailedErrors().RecordWalkTrace().SetMaxStepsToDisplay(150));

			string sql =
			"""
			SELECT 
				users.age AS a,
				(SELECT MAX(salary) FROM employees e WHERE department = dept.name) AS max_salary
			FROM departments dept
			WHERE emp.department_id = dept.id 
				AND emp.salary > (SELECT AVG(salary) FROM employees)
			""";

			var result = parser.Parse<SqlSelectStatement>(sql);
			Assert.NotNull(result);

			var columns = result.Select;
			Assert.Equal(2, columns.Count);

			var from = result.From;
			Assert.Equivalent(new SqlTableSource { Source = "departments", Alias = "dept" }, from.MainTable);
		}

		[Fact]
		public void SubqueryInFromClause()
		{
			var parser = SQLParser.CreateParser();

			string sql =
			"""
			SELECT 
				sub.total_sales,
				sub.region
			FROM (
				SELECT 
					region,
					SUM(sales) AS total_sales
				FROM sales_data data
				GROUP BY region
				HAVING SUM(sales) > 100000
			) AS sub
			WHERE sub.total_sales > 500000
			ORDER BY sub.total_sales DESC
			""";

			var result = parser.Parse<SqlSelectStatement>(sql);
			Assert.NotNull(result);

			var from = result.From;
			Assert.NotNull(from.MainTable);
			// Subquery parsing would need additional AST types
		}

		[Fact]
		public void MultipleJoinsWithComplexConditions()
		{
			var parser = SQLParser.CreateParser();

			string sql =
			"""
			SELECT 
				c.name,
				o.order_date,
				p.product_name
			FROM customers c
			INNER JOIN orders o ON c.id = o.customer_id 
				AND o.status = 'completed'
				AND o.order_date >= DATEADD(month, -1, GETDATE())
			LEFT JOIN order_items oi ON o.id = oi.order_id
			INNER JOIN products p ON oi.product_id = p.id 
				AND p.category IN ('Electronics', 'Computers')
			WHERE c.country = 'USA'
				AND (c.membership_level = 'Premium' OR c.total_orders > 10)
			""";

			var result = parser.Parse<SqlSelectStatement>(sql);
			Assert.NotNull(result);

			var from = result.From;
			Assert.Equal(3, from.Joins.Count);

			var firstJoin = from.Joins[0];
			Assert.Equal("INNER", firstJoin.JoinType);

			var secondJoin = from.Joins[1];
			Assert.Equal("LEFT", secondJoin.JoinType);

			var thirdJoin = from.Joins[2];
			Assert.Equal("INNER", thirdJoin.JoinType);
		}

		[Fact]
		public void SetOperations()
		{
			var parser = SQLParser.CreateParser();

			string sql =
			"""
			SELECT name, department FROM employees WHERE salary > 50000
			UNION
			SELECT name, department FROM contractors WHERE hourly_rate > 50
			ORDER BY name
			""";

			// This would require UNION support in the grammar
			Assert.Throws<ParsingException>(() => parser.Parse<SqlSelectStatement>(sql));
		}

		[Fact]
		public void WindowFunctions()
		{
			var parser = SQLParser.CreateParser();

			string sql =
			"""
			SELECT 
				name,
				department,
				salary,
				RANK() OVER (PARTITION BY department ORDER BY salary DESC) as dept_rank,
				AVG(salary) OVER (PARTITION BY department) as avg_dept_salary
			FROM employees
			WHERE hire_date > '2020-01-01'
			""";

			// Window functions would need additional grammar support
			Assert.Throws<ParsingException>(() => parser.Parse<SqlSelectStatement>(sql));
		}
	}
}