using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.ErrorRecoveryStrategies;

namespace RCParsing
{
	/// <summary>
	/// Represents a recovery action that can be taken when an error occurs during parsing a specific rule.
	/// </summary>
	public abstract class ErrorRecoveryStrategy
	{
		/// <summary>
		/// Gets an error recovery strategy that does not attempt any recovery.
		/// </summary>
		public static ErrorRecoveryStrategy NoRecovery { get; } = new NoErrorRecoveryStrategy();

		/// <summary>
		/// Gets an error recovery strategy that attempts to find the next valid rule.
		/// </summary>
		public static ErrorRecoveryStrategy FindNext { get; } = new FindNextErrorRecoveryStrategy();

		/// <summary>
		/// Tries to recover from an error during parsing a specific rule.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="settings">The settings that got passed from parent.</param>
		/// <param name="rule">The target rule that was failed during parsing and needed to be recovered.</param>
		/// <param name="ruleContext">The parser context that was advanced to use for parsing target rule.</param>
		/// <param name="ruleSettings">The settings to use for parsing this element.</param>
		/// <param name="ruleChildSettings">The settings to use for parsing this element's children.</param>
		/// <returns>The parsed result of the rule. If parsing fails, returns <see cref="ParsedRule.Fail"/>.</returns>
		public abstract ParsedRule TryRecover(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings);
	}
}