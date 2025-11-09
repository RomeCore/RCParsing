using System.Collections.Generic;
using RCParsing.ErrorRecoveryStrategies;
using RCParsing.Utils;

namespace RCParsing.Building.ErrorRecoveryStrategies
{
	/// <summary>
	/// A class that can be built into an error recovery strategy that skips input after an anchor rule is found, with an optional stop rule.
	/// </summary>
	public class BuildableSkipAfterAnchorErrorRecoveryStrategy : BuildableErrorRecoveryStrategy
	{
		/// <summary>
		/// Gets or sets the anchor rule that will be used to determine where to start skipping from.
		/// </summary>
		public Or<string, BuildableParserRule> AnchorRule { get; set; }

		/// <summary>
		/// Gets or sets the stop rule that will be used to determine when to stop the recovery process.
		/// </summary>
		public Or<string, BuildableParserRule>? StopRule { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the skip should be repeated if parsing fails after finding the anchor.
		/// </summary>
		public bool RepeatSkip { get; set; }

		public override IEnumerable<Or<string, BuildableParserRule>> RuleChildren => new[] { AnchorRule, StopRule ?? default };

		public override ErrorRecoveryStrategy BuildTyped(List<int>? ruleChildren, List<int>? tokenChildren, List<object?>? elementChildren)
		{
			if (ruleChildren[1] == -1)
				return new SkipAfterAnchorErrorRecoveryStrategy(ruleChildren[0], RepeatSkip);
			return new SkipAfterAnchorWithStopErrorRecoveryStrategy(ruleChildren![0], ruleChildren[1], RepeatSkip);
		}
	}
}