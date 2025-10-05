using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests
{
	public class MathExpressionsTests
	{
		private static void AssertEval(double expected, string expr)
		{
			var actual = MathExpr.MathParser.ParseExpression(expr);
			Assert.Equal(expected, actual, 0.001);
		}

		[Fact]
		public void BasicEvaluation()
		{
			AssertEval(9, "9.0");
			AssertEval(-5, "-5");
			AssertEval(3.14, "3.14");
			AssertEval(0, "0");
		}

		[Fact]
		public void ArithmeticOperations()
		{
			AssertEval(5, "2 + 3");
			AssertEval(-1, "2 - 3");
			AssertEval(6, "2 * 3");
			AssertEval(2.5, "5 / 2");
			AssertEval(8, "2 ^ 3");
			AssertEval(1, "5 % 2");
		}

		[Fact]
		public void OperatorPrecedence()
		{
			AssertEval(14, "2 + 3 * 4");
			AssertEval(20, "(2 + 3) * 4");
			AssertEval(10, "2 * 3 + 4");
			AssertEval(14, "2 * (3 + 4)");
			AssertEval(11, "2 + 3 ^ 2");
			AssertEval(25, "(2 + 3) ^ 2");
		}

		[Fact]
		public void ComplexExpressions()
		{
			AssertEval(17, "2 + 3 * 5");
			AssertEval(19, "2 * 3 + 5 * 2 + 3");
			AssertEval(26, "2 * (3 + 4) + 3 * 4");
			AssertEval(512, "2 ^ 3 ^ 2"); // Right associativity: 2^(3^2) = 2^9 = 512
			AssertEval(64, "(2 ^ 3) ^ 2"); // 8^2 = 64
		}

		[Fact]
		public void UnaryOperators()
		{
			AssertEval(-5, "-5");
			AssertEval(5, "+5");
			AssertEval(5, "--5");
			AssertEval(-5, "-+5");
			AssertEval(-5, "+-+5");
			AssertEval(5, "----5");
			AssertEval(-5, "---5");
		}

		[Fact]
		public void MathConstants()
		{
			AssertEval(Math.PI, "pi");
			AssertEval(Math.E, "e");
			AssertEval(double.PositiveInfinity, "inf");
			AssertEval(double.Epsilon, "eps");
			Assert.True(double.IsNaN(MathExpr.MathParser.ParseExpression("nan")));
		}

		[Fact]
		public void MathFunctions()
		{
			AssertEval(1, "sin(pi/2)");
			AssertEval(0, "cos(pi/2)");
			AssertEval(4, "sqrt(16)");
			AssertEval(1, "tan(pi/4)");
			AssertEval(0, "sin(0)");
			AssertEval(1, "cos(0)");
			AssertEval(2, "sqrt(4)");
		}

		[Fact]
		public void FunctionComposition()
		{
			AssertEval(2, "sin(pi/2) + cos(0)");
			AssertEval(2, "sqrt(4) * cos(0)");
			AssertEval(0, "sin(pi) * cos(pi/2)");
			AssertEval(1.414, "sqrt(sin(pi/2)^2 + cos(0)^2)");
		}

		[Fact]
		public void AbsoluteValue()
		{
			AssertEval(5, "|5|");
			AssertEval(5, "|-5|");
			AssertEval(0, "|0|");
			AssertEval(3.14, "|3.14|");
			AssertEval(3.14, "|-3.14|");
			AssertEval(10, "|2 * 5|");
			AssertEval(10, "|-2 * 5|");
		}

		[Fact]
		public void MixedOperations()
		{
			AssertEval(7, "| -3 | + 4");
			AssertEval(1, "sin(pi/2) * | -1 |");
			AssertEval(5, "sqrt(| -25 |)");
			AssertEval(2, "| -2 ^ 3 | / 4");
			AssertEval(3, "| -3 | * cos(0)");
		}

		[Fact]
		public void EdgeCases()
		{
			Assert.True(double.IsNaN(MathExpr.MathParser.ParseExpression("0 * inf")));
			Assert.True(double.IsNaN(MathExpr.MathParser.ParseExpression("0 / 0")));
			AssertEval(double.PositiveInfinity, "1 / 0");
			AssertEval(double.PositiveInfinity, "inf + 1");
			AssertEval(double.PositiveInfinity, "inf * 2");
		}

		[Fact]
		public void WhitespaceHandling()
		{
			AssertEval(5, "  2  +  3  ");
			AssertEval(10, "2 *  ( 3 + 2 ) ");
			AssertEval(1, "sin ( pi / 2 ) ");
			AssertEval(4, "sqrt ( 16 ) ");
		}

		[Fact]
		public void SignFunction()
		{
			AssertEval(1, "sign(5)");
			AssertEval(-1, "sign(-5)");
			AssertEval(0, "sign(0)");
			AssertEval(1, "sign(0.001)");
			AssertEval(-1, "sign(-0.001)");
		}

		[Fact]
		public void TrigonometricIdentities()
		{
			AssertEval(1, "sin(0)^2 + cos(0)^2");
			AssertEval(1, "sin(pi/4)^2 + cos(pi/4)^2");
			AssertEval(1, "sin(1)^2 + cos(1)^2");
			AssertEval(1, "cos(0)");
			AssertEval(0, "sin(0)");
		}

		[Fact]
		public void ExponentialAndLogarithmic()
		{
			AssertEval(Math.E, "e^1");
			AssertEval(1, "ln(e)");
			AssertEval(2, "e^ln(2)");
		}

		[Fact]
		public void ComplexNestedFunctions()
		{
			AssertEval(2, "sqrt(sin(pi/2)^2 + cos(pi/2)^2 + 3)");
			AssertEval(1, "| sin(pi/2) | * | cos(0) |");
			AssertEval(5, "sqrt(| -16 |) + | -1 |");
			AssertEval(2, "sign(sin(pi/2)) + sign(cos(0))");
		}

		[Fact]
		public void MultipleParentheses()
		{
			AssertEval(5, "((((5))))");
			AssertEval(20, "((2 + 3) * 4)");
			AssertEval(9, "((1 + 2) * (2 + 1))");
			AssertEval(1, "((sin((pi/2))))");
			AssertEval(2, "sqrt(((4)))");
		}

		[Fact]
		public void OperatorChains()
		{
			AssertEval(10, "2 + 3 + 5");
			AssertEval(30, "2 * 3 * 5");
			AssertEval(-6, "2 - 3 - 5");
			AssertEval(2, "16 / 4 / 2");
			AssertEval(256, "2 ^ 2 ^ 3");
		}
	}
}