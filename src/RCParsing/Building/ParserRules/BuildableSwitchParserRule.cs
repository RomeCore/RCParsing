using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable switch parser rule that chooses between multiple branches based on parser parameter.
	/// </summary>
	public class BuildableSwitchParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the selector function that determines which branch to take.
		/// </summary>
		public Func<object?, int> Selector { get; set; } = null!;

		/// <summary>
		/// Gets or sets the rules for the branches.
		/// </summary>
		public List<Or<string, BuildableParserRule>> Branches { get; set; } = new();

		/// <summary>
		/// Gets or sets the rule for the default branch.
		/// </summary>
		public Or<string, BuildableParserRule> DefaultBranch { get; set; }

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren =>
			Branches.Append(DefaultBranch);

		public BuildableSwitchParserRule()
		{
			ParsedValueFactory = v => v.Count > 0 ? v[0].Value : null;
		}

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			var branches = ruleChildren.Take(ruleChildren.Count - 1);
			var defaultBranch = ruleChildren[ruleChildren.Count - 1];
			return new SwitchParserRule(Selector, branches, defaultBranch);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSwitchParserRule other &&
				   Equals(Selector, other.Selector) &&
				   Branches.SequenceEqual(other.Branches) &&
				   DefaultBranch == other.DefaultBranch;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + (Selector?.GetHashCode() ?? 0);
			foreach (var branch in Branches)
			{
				hashCode = hashCode * 397 + branch.GetHashCode();
			}
			hashCode = hashCode * 397 + DefaultBranch.GetHashCode();
			return hashCode;
		}
	}
}