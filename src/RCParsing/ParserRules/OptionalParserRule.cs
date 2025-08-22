using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

		protected override HashSet<char>? FirstCharsCore => null;



		public override ParsedRule Parse(ParserContext context, ParserContext childContext)
		{
			var result = TryParseRule(Rule, childContext);
			if (result.success)
			{
				return ParsedRule.Rule(Id, result.startIndex, result.length, new List<ParsedRule> { result }, result.intermediateValue);
			}
			else
			{
				return ParsedRule.Rule(Id, context.position, 0, new List<ParsedRule>(), null);
			}
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "Optional...";
			return $"Optional: {GetRule(Rule).ToString(remainingDepth - 1)}";
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