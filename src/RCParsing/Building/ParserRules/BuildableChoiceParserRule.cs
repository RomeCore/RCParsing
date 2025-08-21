using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents a parser rule that can be built into a choice of multiple rules.
	/// </summary>
	public class BuildableChoiceParserRule : BuildableParserRule
	{
		/// <summary>
		/// The choices of this parser rule.
		/// </summary>
		public List<Or<string, BuildableParserRule>> Choices { get; } = new List<Or<string, BuildableParserRule>>();
		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Choices;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new ChoiceParserRule(ruleChildren);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableChoiceParserRule other &&
				   Choices.SequenceEqual(other.Choices);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode ^= Choices.GetSequenceHashCode() * 23;
			return hashCode;
		}
	}
}