using System.Collections.Generic;

namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that skips whitespaces before parsing the rule.
	/// </summary>
	public class SkipWhitespacesStrategy : SkipStrategy
	{
		public override ParsedRule ParseWithSkip(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			while (ruleContext.position < ruleContext.maxPosition && char.IsWhiteSpace(ruleContext.input[ruleContext.position]))
				ruleContext.position++;
			return rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
		}

		public override IEnumerable<ParsedRule> FindAllMatches(ParserContext context, ParserSettings settings, bool overlap,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			while (ruleContext.position < ruleContext.maxPosition)
			{
				if (char.IsWhiteSpace(ruleContext.input[ruleContext.position]))
				{
					ruleContext.position++;
					continue;
				}

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