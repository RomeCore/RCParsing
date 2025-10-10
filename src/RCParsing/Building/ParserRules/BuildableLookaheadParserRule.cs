using System.Collections.Generic;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable lookahead parser rule (positive or negative).
	/// </summary>
	public class BuildableLookaheadParserRule : BuildableParserRule
	{
		/// <summary>
		/// The child of this parser rule.
		/// </summary>
		public Or<string, BuildableParserRule> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether this is a positive lookahead.
		/// </summary>
		public bool IsPositive { get; set; } = true;

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Child.WrapIntoEnumerable();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new LookaheadParserRule(ruleChildren[0], IsPositive);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableLookaheadParserRule other &&
				   Child == other.Child &&
				   IsPositive == other.IsPositive;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode() * 23;
			hashCode = hashCode * 397 + IsPositive.GetHashCode();
			return hashCode;
		}
	}

}