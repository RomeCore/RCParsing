using System.Collections.Generic;
using RCParsing.ErrorRecoveryStrategies;
using RCParsing.Utils;

namespace RCParsing.Building.ErrorRecoveryStrategies
{
	/// <summary>
	/// A class that can be built into an error recovery strategy that searches for the next valid rule until a specified stop rule is encountered.
	/// </summary>
	public class BuildableFindNextErrorRecoveryStrategy : BuildableErrorRecoveryStrategy
	{
		/// <summary>
		/// Gets or sets the stop rule that will be used to determine when to stop searching for the next valid rule.
		/// </summary>
		public Or<string, BuildableParserRule>? StopRule { get; set; }

		public override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren =>
			new[] { StopRule ?? default };

		public override ErrorRecoveryStrategy BuildTyped(List<int>? ruleChildren, List<int>? tokenChildren, List<object?>? elementChildren)
		{
			if (ruleChildren[0] == -1)
				return ErrorRecoveryStrategy.FindNext;
			return new FindNextUntilErrorRecoveryStrategy(ruleChildren[0]);
		}
	}
}