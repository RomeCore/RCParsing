using System;
using System.Collections.Generic;
using System.Xml.Linq;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents the buildable entry from parser rule to token pattern.
	/// </summary>
	public sealed class BuildableTokenParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the child of this token parser rule. This can be a name reference or a buildable token pattern.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => null;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Child.WrapIntoEnumerable();

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new TokenParserRule(tokenChildren[0]);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableTokenParserRule other &&
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