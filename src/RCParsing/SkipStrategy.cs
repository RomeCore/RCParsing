using System.Collections.Generic;
using RCParsing.SkipStrategies;

namespace RCParsing
{
	/// <summary>
	/// Represents a strategy for skipping rules during parsing.
	/// </summary>
	public abstract class SkipStrategy
	{
		/// <summary>
		/// Gets a skip strategy that just directly parsed rule without any skipping.
		/// </summary>
		public static SkipStrategy NoSkipping { get; } = new NoSkipStrategy();

		/// <summary>
		/// Gets a skip strategy that skips whitespaces before parsing the target rule.
		/// </summary>
		public static SkipStrategy Whitespaces { get; } = new SkipWhitespacesStrategy();

		/// <summary>
		/// Tries to parse given rule using the skip strategy.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="settings">The settings that got passed from parent.</param>
		/// <param name="rule">The target rule to parse.</param>
		/// <param name="ruleContext">The parser context that was advanced to use for parsing target rule.</param>
		/// <param name="ruleSettings">The settings to use for parsing target rule.</param>
		/// <param name="ruleChildSettings">The settings to use for parsing target rule's children.</param>
		/// <returns>The parsed result of the rule. If parsing fails, returns <see cref="ParsedRule.Fail"/>.</returns>
		public abstract ParsedRule ParseWithSkip(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings);

		/// <summary>
		/// Tries to find all matches of given rule using the skip strategy.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="settings">The settings that got passed from parent.</param>
		/// <param name="overlap">A value indicating whether to allow overlapping matches.</param>
		/// <param name="rule">The target rule to parse.</param>
		/// <param name="ruleContext">The parser context that was advanced to use for parsing target rule.</param>
		/// <param name="ruleSettings">The settings to use for parsing target rule.</param>
		/// <param name="ruleChildSettings">The settings to use for parsing target rule's children.</param>
		/// <returns>The parsed result of the rule. If parsing fails, returns <see cref="ParsedRule.Fail"/>.</returns>
		public virtual IEnumerable<ParsedRule> FindAllMatches(ParserContext context, ParserSettings settings, bool overlap,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			while (ruleContext.position < ruleContext.maxPosition)
			{
				var result = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
				if (result.success)
				{
					yield return result;
					if (overlap)
						ruleContext.position++;
					else
						ruleContext.position = result.endIndex;
				}
				else
					ruleContext.position++;
			}
		}
	}
}