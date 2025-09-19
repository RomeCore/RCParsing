using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// A parser rule that repeats a specified rule the specified range of times.
	/// </summary>
	public class RepeatParserRule : ParserRule
	{
		/// <summary>
		/// Gets the rule ID to repeat.
		/// </summary>
		public int Rule { get; }

		/// <summary>
		/// Gets the minimum inclusive number of times the rule must repeat.
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// Gets the maximum inclusive number of times the rule can repeat. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepeatParserRule"/> class.
		/// </summary>
		/// <param name="ruleId">The token pattern ID to repeat.</param>
		/// <param name="minCount">The minimum number of times the token pattern must repeat.</param>
		/// <param name="maxCount">The maximum number of times the token pattern can repeat. -1 indicates no upper limit.</param>
		public RepeatParserRule(int ruleId, int minCount, int maxCount)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be greater than or equal to 0");

			if (maxCount < minCount && maxCount != -1)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be greater than or equal to minCount or be -1 if no maximum is specified.");

			Rule = ruleId;
			MinCount = minCount;
			MaxCount = maxCount;
		}

		protected override HashSet<char>? FirstCharsCore => MinCount == 0 ? null :
			GetRule(Rule).FirstChars;



		private ParseDelegate parseFunction;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			ParsedRule Parse(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				var rules = new List<ParsedRule>();
				var initialPosition = context.position;
				ParsedRule parsedRule = default;

				for (int i = 0; i < this.MaxCount || this.MaxCount == -1; i++)
				{
					parsedRule = TryParseRule(Rule, context, childSettings);
					if (!parsedRule.success)
						break;

					context.position = parsedRule.startIndex + parsedRule.length;
					context.passedBarriers = parsedRule.passedBarriers;
					parsedRule.occurency = i;
					rules.Add(parsedRule);
				}

				if (rules.Count < MinCount)
				{
					RecordError(ref context, ref settings, $"Expected at least {MinCount} repetitions of child rule, but found {rules.Count}.");
					return ParsedRule.Fail;
				}

				return ParsedRule.Rule(Id, initialPosition, context.position - initialPosition, context.passedBarriers, rules, null);
			};

			parseFunction = Parse;

			parseFunction = WrapParseFunction(parseFunction, initFlags);
		}

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction(ref context, ref settings, ref childSettings);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}' " : string.Empty;

			if (remainingDepth <= 0)
				return $"Repeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]...";
			return $"Repeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]: " +
				$"{GetRule(Rule).ToString(remainingDepth - 1)}";
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}' " : string.Empty;

			if (remainingDepth <= 0)
				return $"Repeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]...";
			return $"Repeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]: " +
				$"{GetRule(Rule).ToString(remainingDepth - 1)} <-- here";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is RepeatParserRule rule &&
				   Rule == rule.Rule &&
				   MinCount == rule.MinCount &&
				   MaxCount == rule.MaxCount;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			hashCode = hashCode * -1521134295 + MinCount.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxCount.GetHashCode();
			return hashCode;
		}
	}
}