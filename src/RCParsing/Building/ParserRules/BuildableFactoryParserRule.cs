using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.Building.ParserRules
{
	/// <summary>
	/// Represents a buildable parser rule that used for easy construction of custom rule implementations.
	/// </summary>
	public class BuildableFactoryParserRule : BuildableParserRule
	{
		/// <summary>
		/// Gets or sets the factory function to create parser rule.
		/// </summary>
		public Func<List<int>, ParserRule> Factory { get; set; }

		/// <summary>
		/// The child rules of the rule.
		/// </summary>
		public List<Or<string, BuildableParserRule>> Children { get; } = new();

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => Children;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			if (Factory == null)
				throw new NullReferenceException($"{nameof(Factory)} property is not set.");
			return Factory.Invoke(ruleChildren)
				?? throw new NullReferenceException($"{nameof(Factory)} returned a null rule.");
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableFactoryParserRule other &&
				   Children.SequenceEqual(other.Children) &&
				   Factory == other.Factory;
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode = hashCode * 397 ^ Children.GetSequenceHashCode();
			hashCode = hashCode * 397 ^ (Factory?.GetHashCode() ?? 0);
			return hashCode;
		}
	}
}