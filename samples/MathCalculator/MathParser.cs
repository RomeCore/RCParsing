using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RCParsing;
using RCParsing.Building;
using RCParsing.Building.ParserRules;
using RCParsing.ParserRules;

namespace MathCalculator
{
	public static class MathParser
	{
		private static Parser parser = CreateParser();

		public static RuleBuilder CreateFunctionRule(Delegate @delegate, string funcName, string expressionName)
		{
			var builder = new RuleBuilder();

			var argCount = @delegate.Method.GetParameters().Length;

			builder
				.Keyword(funcName)
				.Literal("(")
				.RepeatSeparated(r => r.Rule(expressionName), r => r.Literal(","), min: argCount, max: argCount)
				.Literal(")")

				.Transform(v =>
				{
					var values = v.SelectArray<object>(index: 2);
					var value = @delegate.DynamicInvoke(values)!;
					return (double)value;
				});

			return builder;
		}

		public static Parser CreateParser(Action<ParserBuilder>? builderAction = null)
		{
			var builder = new ParserBuilder();

			builder.Settings.SkipWhitespaces()
				// .UseInitFlagsOn(ParserInitFlags.FirstCharacterMatch, e => e is ChoiceParserRule)
				;

			// Basic terms
			
			builder.CreateRule("number")
				.Number<double>();

			builder.CreateRule("const")
				.KeywordChoice("pi", "inf", "eps", "e", "nan")

				.Transform(v =>
				{
					return v.GetIntermediateValue<string>() switch
					{
						"pi" => Math.PI,
						"inf" => double.PositiveInfinity,
						"eps" => double.Epsilon,
						"e" => double.E,
						"nan" => double.NaN,
						_ => throw new Exception() // Will not be thrown
					};
				});

			// Functions

			var funcChoiceRuleHolder = builder.CreateRule("func");
			var funcChoiceRule = new BuildableChoiceParserRule();
			funcChoiceRule.ParsedValueFactory = v => v.GetValue(0);
			funcChoiceRuleHolder.BuildingRule = funcChoiceRule;

			void AddFunction(Delegate @delegate, string name)
			{
				funcChoiceRule.Choices.Add(CreateFunctionRule(@delegate, name, "expr").BuildingRule!.Value);
			}

			AddFunction(Math.Sin, "sin");
			AddFunction(Math.Cos, "cos");
			AddFunction(Math.Sqrt, "sqrt");
			AddFunction(Math.Tan, "tan");
			AddFunction(Math.Tanh, "tanh");
			AddFunction(Math.Sinh, "sinh");
			AddFunction(Math.Cosh, "cosh");
			AddFunction((double value) => (double)Math.Sign(value), "sign");

			// The expressions

			builder.CreateRule("term")
				.Choice(
					b => b.Rule("number"),
					b => b.Rule("const"),

					// Expression in parenthesis
					b => b.Literal("(").Rule("expr").Literal(")")
						.Transform(v => v.GetValue<double>(index: 1)),

					// Absolute operator
					b => b.Literal("|").Rule("expr").Literal("|")
						.Transform(v => Math.Abs(v.GetValue<double>(index: 1))),

					b => b.Rule("func")
				);

			builder.CreateRule("op_pre")
				.ZeroOrMore(b => b.LiteralChoice("+", "-"))
				.Rule("term")

				.Transform(v =>
				{
					var operators = v.SelectArray<string>(index: 0);
					var value = v.GetValue<double>(index: 1);

					for (int i = operators.Length - 1; i >= 0; i--)
					{
						var op = operators[i];
						value = op switch
						{
							"-" => -value,
							_ => value
						};
					}

					return value;
				});

			builder.CreateRule("op_pow")
				.OneOrMoreSeparated(b => b.Rule("op_pre"), b => b.Literal("^"))

				.TransformFoldLeft<double, double>((l, r) =>
				{
					return Math.Pow(l, r);
				});

			builder.CreateRule("op_mul")
				.OneOrMoreSeparated(b => b.Rule("op_pow"), b => b.LiteralChoice("*", "/"), includeSeparatorsInResult: true)

				.TransformFoldLeft<double, string, double>((l, op, r) =>
				{
					return op == "*" ? l * r : l / r;
				});

			builder.CreateRule("op_add")
				.OneOrMoreSeparated(b => b.Rule("op_mul"), b => b.LiteralChoice("+", "-"), includeSeparatorsInResult: true)

				.TransformFoldLeft<double, string, double>((l, op, r) =>
				{
					return op == "+" ? l + r : l - r;
				});

			builder.CreateRule("expr")
				.Rule("op_add");

			builder.CreateMainRule()
				.Rule("expr").EOF().TransformSelect(0);

			builderAction?.Invoke(builder);
			return builder.Build();
		}

		public static double ParseExpression(string expression)
		{
			return parser.Parse<double>(expression);
		}
	}
}