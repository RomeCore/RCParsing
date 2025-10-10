﻿using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that performs lookahead (positive or negative), without consuming input.
	/// </summary>
	public class LookaheadParserRule : ParserRule
	{
		/// <summary>
		/// Gets the rule ID to look ahead.
		/// </summary>
		public int Rule { get; }

		/// <summary>
		/// Gets a value indicating whether this is a positive lookahead.
		/// </summary>
		public bool IsPositive { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LookaheadParserRule"/> class.
		/// </summary>
		/// <param name="rule">The rule ID to look ahead.</param>
		/// <param name="isPositive">A value indicating whether this is a positive lookahead.</param>
		public LookaheadParserRule(int rule, bool isPositive)
		{
			Rule = rule;
			IsPositive = isPositive;
		}

		protected override HashSet<char> FirstCharsCore => GetRule(Rule).FirstChars;
		protected override bool IsFirstCharDeterministicCore => GetRule(Rule).IsFirstCharDeterministic;
		protected override bool IsOptionalCore => true;



		private ParseDelegate parseFunction;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			ParsedRule ParsePositive(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				var start = context.position;
				var result = TryParseRule(Rule, context, childSettings);

				if (result.success)
					return ParsedRule.Rule(Id, start, 0, context.passedBarriers, ParsedRuleChildUtils.empty, null);
				else
					return ParsedRule.Fail;
			}

			ParsedRule ParseNegative(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				var start = context.position;
				var result = TryParseRule(Rule, context, childSettings);

				if (!result.success)
					return ParsedRule.Rule(Id, start, 0, context.passedBarriers, ParsedRuleChildUtils.empty, null);
				else
					return ParsedRule.Fail;
			}

			parseFunction = WrapParseFunction(IsPositive ? ParsePositive : ParseNegative, initFlags);
		}

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction(ref context, ref settings, ref childSettings);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;
			string prefix = IsPositive ? "&" : "!";
			if (remainingDepth <= 0)
				return $"{prefix}{alias}...";
			return $"{prefix}{alias}: {GetRule(Rule).ToString(remainingDepth - 1)}";
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;
			string prefix = IsPositive ? "&" : "!";
			if (remainingDepth <= 0)
				return $"{prefix}{alias}...";
			return $"{prefix}{alias}: {GetRule(Rule).ToString(remainingDepth - 1)} <-- here";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is LookaheadParserRule rule &&
				   Rule == rule.Rule &&
				   IsPositive == rule.IsPositive;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			hashCode = hashCode * -1521134295 + IsPositive.GetHashCode();
			return hashCode;
		}
	}

}