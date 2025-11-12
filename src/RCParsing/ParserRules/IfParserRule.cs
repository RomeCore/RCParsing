using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that conditionally matches one of two rules based on parser parameter.
	/// </summary>
	public class IfParserRule : ParserRule
	{
		/// <summary>
		/// Gets the parser parameter condition function that determines which branch to take.
		/// </summary>
		public Func<object?, bool> Condition { get; }

		/// <summary>
		/// Gets the rule ID for the true branch.
		/// </summary>
		public int TrueBranch { get; }

		/// <summary>
		/// Gets the rule ID for the false branch. Can be -1 if not specified.
		/// </summary>
		public int FalseBranch { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="IfParserRule"/> class.
		/// </summary>
		/// <param name="condition">The parser parameter condition function that determines which branch to take.</param>
		/// <param name="trueBranch">The rule ID for the true branch.</param>
		/// <param name="falseBranch">The rule ID for the false branch.</param>
		public IfParserRule(Func<object?, bool> condition, int trueBranch, int falseBranch = -1)
		{
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
			TrueBranch = trueBranch;
			FalseBranch = falseBranch;
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				var set = new HashSet<char>(GetRule(TrueBranch).FirstChars);
				if (FalseBranch >= 0)
					set.UnionWith(GetRule(FalseBranch).FirstChars);
				return set;
			}
		}

		protected override bool IsFirstCharDeterministicCore => GetRule(TrueBranch).IsFirstCharDeterministic && (TryGetRule(FalseBranch)?.IsFirstCharDeterministic ?? true);
		protected override bool IsOptionalCore => GetRule(TrueBranch).IsOptional || (TryGetRule(FalseBranch)?.IsOptional ?? false);



		private ParseDelegate parseFunction;
		private ParserRule trueRule;
		private ParserRule? falseRule;
		private bool canTrueRuleBeInlined;
		private bool canFalseRuleBeInlined;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			trueRule = GetRule(TrueBranch);
			falseRule = TryGetRule(FalseBranch);
			canTrueRuleBeInlined = trueRule.CanBeInlined && (initFlags & ParserInitFlags.InlineRules) != 0;
			canFalseRuleBeInlined = falseRule?.CanBeInlined == true && (initFlags & ParserInitFlags.InlineRules) != 0;

			ParsedRule ParseConditional(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				var conditionResult = Condition(context.parserParameter);
				var branch = conditionResult ? trueRule : falseRule;

				if (branch != null)
				{
					ParsedRule result;
					if (branch == trueRule && canTrueRuleBeInlined)
						result = trueRule.Parse(context, childSettings, childSettings);
					else if (branch == falseRule && canFalseRuleBeInlined)
						result = falseRule.Parse(context, childSettings, childSettings);
					else
						result = TryParseRule(branch.Id, context, childSettings);

					if (result.success)
					{
						result.occurency = conditionResult ? 0 : 1;
						return new ParsedRule(Id,
							result.startIndex,
							result.length,
							result.passedBarriers,
							result.intermediateValue,
							result);
					}
				}

				return ParsedRule.Fail;
			}

			parseFunction = WrapParseFunction(ParseConditional, initFlags);
		}

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction(ref context, ref settings, ref childSettings);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			string ifBranch = GetRule(TrueBranch).ToString(remainingDepth - 1);
			string elseBranch = "| " + TryGetRule(FalseBranch)?.ToString(remainingDepth - 1) ?? "no else branch";
			return $"If:{Environment.NewLine}{ifBranch}{Environment.NewLine}{elseBranch.Indent("  ")}";
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			if (remainingDepth <= 0)
				return $"If...";

			string ifBranch = GetRule(TrueBranch).ToString(remainingDepth - 1);
			if (TrueBranch == childIndex)
				ifBranch += " <-- here";
			string elseBranch = "| " + TryGetRule(FalseBranch)?.ToString(remainingDepth - 1) ?? "no else branch";
			if (FalseBranch == childIndex && childIndex != -1)
				elseBranch += " <-- here";

			return $"If:{Environment.NewLine}{ifBranch}{Environment.NewLine}{elseBranch.Indent("  ")}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is IfParserRule rule &&
				   TrueBranch == rule.TrueBranch &&
				   FalseBranch == rule.FalseBranch &&
				   Equals(Condition, rule.Condition);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + TrueBranch.GetHashCode();
			hashCode = hashCode * 397 + FalseBranch.GetHashCode();
			hashCode = hashCode * 397 + Condition.GetHashCode();
			return hashCode;
		}
	}
}