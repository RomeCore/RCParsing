using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	public class BuildableSeparatedRepeatParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the child pattern to repeat.
		/// </summary>
		public Or<string, BuildableParserRule> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the separator pattern.
		/// </summary>
		public Or<string, BuildableParserRule> Separator { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the minimum number of repetitions.
		/// </summary>
		public int MinCount { get; set; } = 0;

		/// <summary>
		/// Gets or sets the maximum number of repetitions. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; set; } = -1;

		/// <summary>
		/// Gets or sets a value indicating whether trailing separator is allowed.
		/// </summary>
		public bool AllowTrailingSeparator { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether separators should be included in the result children rules.
		/// </summary>
		public bool IncludeSeparatorsInResult { get; set; } = false;

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren
			=> Child.WrapIntoEnumerable().Concat(Separator.WrapIntoEnumerable());

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new SeparatedRepeatParserRule(
				ruleChildren[0],
				ruleChildren[1],
				MinCount,
				MaxCount,
				AllowTrailingSeparator,
				IncludeSeparatorsInResult);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSeparatedRepeatParserRule other &&
				   Child == other.Child &&
				   Separator == other.Separator &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount &&
				   AllowTrailingSeparator == other.AllowTrailingSeparator &&
				   IncludeSeparatorsInResult == other.IncludeSeparatorsInResult;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode() * 23;
			hashCode = hashCode * 397 + Separator.GetHashCode() * 29;
			hashCode = hashCode * 397 + MinCount.GetHashCode() * 31;
			hashCode = hashCode * 397 + MaxCount.GetHashCode() * 37;
			hashCode = hashCode * 397 + AllowTrailingSeparator.GetHashCode() * 41;
			hashCode = hashCode * 397 + IncludeSeparatorsInResult.GetHashCode() * 50;
			return hashCode;
		}
	}

}