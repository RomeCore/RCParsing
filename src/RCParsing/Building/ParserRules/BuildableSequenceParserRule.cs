using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable sequence parser rule.
	/// </summary>
	public class BuildableSequenceParserRule : BuildableParserRule
	{
		/// <summary>
		/// The elements of the sequence parser rule.
		/// </summary>
		public List<Or<string, BuildableParserRule>> Elements { get; } = new List<Or<string, BuildableParserRule>>();
		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Elements;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new SequenceParserRule(ruleChildren);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSequenceParserRule other &&
				   Elements.SequenceEqual(other.Elements);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode ^= Elements.GetSequenceHashCode() * 23;
			return hashCode;
		}
	}
}