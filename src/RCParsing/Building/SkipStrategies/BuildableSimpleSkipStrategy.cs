using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using RCParsing.SkipStrategies;
using RCParsing.Utils;

namespace RCParsing.Building.SkipStrategies
{
	/// <summary>
	/// A class that can be built into one of several built-in skip strategies.
	/// </summary>
	public class BuildableSimpleSkipStrategy : BuildableSkipStrategy
	{
		/// <summary>
		/// Gets or sets the builtin skip strategy type.
		/// </summary>
		public ParserSkippingStrategy Strategy { get; set; }

		/// <summary>
		/// Gets or sets the skip rule.
		/// </summary>
		public Or<string, BuildableParserRule>? SkipRule { get; set; }



		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren =>
			Strategy == ParserSkippingStrategy.Default || Strategy == ParserSkippingStrategy.Whitespaces
				? null
				: new[] { SkipRule ?? default };

		public override SkipStrategy BuildTyped(List<int>? ruleChildren, List<int>? tokenChildren, List<object?>? elementChildren)
		{
			switch (Strategy)
			{
				case ParserSkippingStrategy.Default:
					return SkipStrategy.NoSkipping;
				case ParserSkippingStrategy.Whitespaces:
					return SkipStrategy.Whitespaces;
				case ParserSkippingStrategy.SkipBeforeParsing:
					return new SkipBeforeParsingStrategy(ruleChildren[0]);
				case ParserSkippingStrategy.SkipBeforeParsingGreedy:
					return new SkipBeforeParsingGreedyStrategy(ruleChildren[0]);
				case ParserSkippingStrategy.SkipBeforeParsingLazy:
					return new SkipBeforeParsingLazyStrategy(ruleChildren[0]);
				case ParserSkippingStrategy.TryParseThenSkip:
					return new TryParseThenSkipStrategy(ruleChildren[0]);
				case ParserSkippingStrategy.TryParseThenSkipGreedy:
					return new TryParseThenSkipGreedyStrategy(ruleChildren[0]);
				case ParserSkippingStrategy.TryParseThenSkipLazy:
					return new TryParseThenSkipLazyStrategy(ruleChildren[0]);
				case ParserSkippingStrategy.TryParseNonEmptyThenSkip:
					return new TryParseNonEmptyThenSkipStrategy(ruleChildren[0]);
				case ParserSkippingStrategy.TryParseNonEmptyThenSkipGreedy:
					return new TryParseNonEmptyThenSkipGreedyStrategy(ruleChildren[0]);
				case ParserSkippingStrategy.TryParseNonEmptyThenSkipLazy:
					return new TryParseNonEmptyThenSkipLazyStrategy(ruleChildren[0]);
				default:
					throw new InvalidEnumArgumentException(nameof(Strategy), (int)Strategy, typeof(ParserSkippingStrategy));
			}
		}
	}
}