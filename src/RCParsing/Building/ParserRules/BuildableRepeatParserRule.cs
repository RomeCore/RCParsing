using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	public class BuildableRepeatParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the child of this parser rule.
		/// </summary>
		public Or<string, BuildableParserRule> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the minimum number of times the child pattern can be repeated.
		/// </summary>
		public int MinCount { get; set; } = 0;

		/// <summary>
		/// Gets or sets the maximum number of times the child pattern can be repeated. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; set; } = -1;

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Child.WrapIntoEnumerable();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			return new RepeatParserRule(ruleChildren[0], MinCount, MaxCount);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableRepeatParserRule other &&
				   Child == other.Child &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode ^= Child.GetHashCode() * 23;
			hashCode ^= MinCount.GetHashCode() * 29;
			hashCode ^= MaxCount.GetHashCode() * 31;
			return hashCode;
		}
	}
}