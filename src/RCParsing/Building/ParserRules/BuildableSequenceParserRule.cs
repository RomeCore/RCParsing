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

		/// <summary>
		/// Labels for the elements of the sequence parser rule.
		/// </summary>
		public Dictionary<string, int> Labels { get; set; } = new Dictionary<string, int>();

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Elements;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		public BuildableSequenceParserRule()
		{
			ParsedValueFactory = v => v.Count > 0 ? v[0].Value : null;
		}

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new SequenceParserRule(ruleChildren, Labels);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSequenceParserRule other &&
				   Elements.SequenceEqual(other.Elements) &&
				   Labels.SequenceEqual(other.Labels);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Elements.GetSequenceHashCode() * 17;
			hashCode = hashCode * 397 + Labels.GetSequenceHashCode() * 17;
			return hashCode;
		}
	}
}