using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable optional parser rule.
	/// </summary>
	public class BuildableOptionalParserRule : BuildableParserRule
	{
		/// <summary>
		/// The child of this parser rule.
		/// </summary>
		public Or<string, BuildableParserRule> Child { get; set; } = string.Empty;
		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Child.WrapIntoEnumerable();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new OptionalParserRule(ruleChildren[0]);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableOptionalParserRule other &&
				   Child == other.Child;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode() * 23;
			return hashCode;
		}
	}

}