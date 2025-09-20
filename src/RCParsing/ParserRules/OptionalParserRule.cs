using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that is optional.
	/// </summary>
	public class OptionalParserRule : ParserRule
	{
		/// <summary>
		/// Gets the rule ID that is optional.
		/// </summary>
		public int Rule { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionalParserRule"/> class.
		/// </summary>
		/// <param name="rule">The rule ID that is optional.</param>
		public OptionalParserRule(int rule)
		{
			Rule = rule;
		}

		protected override HashSet<char>? FirstCharsCore => GetRule(Rule).FirstChars;



		private ParseDelegate parseFunction;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			ParsedRule Parse(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				var result = TryParseRule(Rule, context, childSettings);
				if (result.success)
				{
					return ParsedRule.Rule(Id, result.startIndex, result.length, result.passedBarriers, ParsedRuleChildUtils.Single(ref result), result.intermediateValue);
				}
				else
				{
					return ParsedRule.Rule(Id, context.position, 0, context.passedBarriers, ParsedRuleChildUtils.empty, null);
				}
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
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;

			if (remainingDepth <= 0)
				return $"Optional{alias}...";
			return $"Optional{alias}: {GetRule(Rule).ToString(remainingDepth - 1)}";
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;

			if (remainingDepth <= 0)
				return $"Optional{alias}...";
			return $"Optional{alias}: {GetRule(Rule).ToString(remainingDepth - 1)} <-- here";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is OptionalParserRule rule &&
				   Rule == rule.Rule;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			return hashCode;
		}
	}
}