using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.ErrorRecoveryStrategies
{
	/// <summary>
	/// Represents an error recovery strategy that tries to find the next valid rule.
	/// </summary>
	public class FindNextErrorRecoveryStrategy : ErrorRecoveryStrategy
	{
		public override ParsedRule TryRecover(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			context.RecordErrorRecoveryIndex();

			ruleSettings.errorHandling = ParserErrorHandlingMode.NoRecord;
			ruleChildSettings.errorHandling = ParserErrorHandlingMode.NoRecord;

			ruleContext.position++;
			while (ruleContext.position <= ruleContext.maxPosition)
			{
				var result = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
				if (result.success) return result;
				ruleContext.position++;
			}
			return ParsedRule.Fail;
		}
	}
}