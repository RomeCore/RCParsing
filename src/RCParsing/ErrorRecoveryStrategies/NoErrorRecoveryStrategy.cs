using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.ErrorRecoveryStrategies
{
	/// <summary>
	/// Represents a recovery strategy that does not attempt any recovery.
	/// </summary>
	public class NoErrorRecoveryStrategy : ErrorRecoveryStrategy
	{
		public override ParsedRule TryRecover(ParserContext context, ParserSettings inheritedSettings, ParserRule rule, ParserContext advancedContext, ParserSettings localSettings, ParserSettings childSettings)
		{
			return ParsedRule.Fail;
		}
	}
}