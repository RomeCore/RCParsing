using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable custom parser rule.
	/// </summary>
	public class BuildableCustomParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the match function for this custom parser rule.
		/// </summary>
		public CustomRuleParseFunction ParseFunction { get; set; }

		/// <summary>
		/// Gets or sets the custom parser rule string representation.
		/// </summary>
		public string StringRepresentation { get; set; } = "Custom";

		/// <summary>
		/// The child rules of the custom rule.
		/// </summary>
		public List<Or<string, BuildableParserRule>> Children { get; } = new();

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Children;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new CustomParserRule(ParseFunction, ruleChildren, StringRepresentation);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableCustomParserRule other &&
				   Children.SequenceEqual(other.Children) &&
				   ParseFunction == other.ParseFunction &&
				   StringRepresentation == other.StringRepresentation;
		}

		public override int GetHashCode()
		{
			int hashCode = 1930700721;
			hashCode = hashCode * -1521134295 + Children.GetSequenceHashCode();
			hashCode = hashCode * -1521134295 + (ParseFunction?.GetHashCode() ?? 0);
			hashCode = hashCode * -1521134295 + (StringRepresentation?.GetHashCode() ?? 0);
			return hashCode;
		}
	}

}