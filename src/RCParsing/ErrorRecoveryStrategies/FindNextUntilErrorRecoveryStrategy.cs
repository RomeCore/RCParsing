using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.ErrorRecoveryStrategies
{
	/// <summary>
	/// Represents an error recovery strategy that attempts to find the next valid rule until a specified rule is encountered.
	/// </summary>
	public class FindNextUntilErrorRecoveryStrategy : ErrorRecoveryStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the rule ID that the recovery strategy should stop at.
		/// </summary>
		public int UntilRuleId { get; }

		/// <summary>
		/// Gets the rule that the recovery strategy should stop at.
		/// </summary>
		public ParserRule UntilRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="FindNextUntilErrorRecoveryStrategy"/> class.
		/// </summary>
		/// <param name="untilRuleId">The ID of the rule that the recovery strategy should stop at.</param>
		public FindNextUntilErrorRecoveryStrategy(int untilRuleId)
		{
			UntilRuleId = untilRuleId;
		}

		public void Initialize(Parser parser)
		{
			UntilRule = parser.GetRule(UntilRuleId);
		}

		public override ParsedRule TryRecover(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			context.RecordErrorRecoveryIndex();

			settings.errorHandling = ParserErrorHandlingMode.NoRecord;
			ruleSettings.errorHandling = ParserErrorHandlingMode.NoRecord;
			ruleChildSettings.errorHandling = ParserErrorHandlingMode.NoRecord;

			UntilRule.AdvanceContext(ref context, ref settings, out var childSettings);

			ruleContext.position++;
			while (ruleContext.position <= ruleContext.maxPosition)
			{
				var untilResult = UntilRule.Parse(context, settings, childSettings);
				if (untilResult.success) break;

				var result = rule.Parse(ruleContext, settings, ruleChildSettings);
				if (result.success) return result;

				ruleContext.position++;
			}
			return ParsedRule.Fail;
		}
	}
}