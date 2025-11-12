using System;
using System.Collections.Generic;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable conditional parser rule that chooses between two branches based on parser parameter.
	/// </summary>
	public class BuildableIfParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the parser parameter condition function that determines which branch to take.
		/// </summary>
		public Func<object?, bool> Condition { get; set; } = null!;

		/// <summary>
		/// Gets or sets the rule for the true branch.
		/// </summary>
		public Or<string, BuildableParserRule> TrueBranch { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the rule for the false branch.
		/// </summary>
		public Or<string, BuildableParserRule> FalseBranch { get; set; } = string.Empty;

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren =>
			new[] { TrueBranch, FalseBranch };

		public BuildableIfParserRule()
		{
			ParsedValueFactory = v => v.Count > 0 ? v[0].Value : null;
		}

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new IfParserRule(Condition, ruleChildren[0], ruleChildren[1]);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableIfParserRule other &&
				   Equals(Condition, other.Condition) &&
				   TrueBranch == other.TrueBranch &&
				   FalseBranch == other.FalseBranch;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + (Condition?.GetHashCode() ?? 0);
			hashCode = hashCode * 397 + TrueBranch.GetHashCode();
			hashCode = hashCode * 397 + FalseBranch.GetHashCode();
			return hashCode;
		}
	}
}