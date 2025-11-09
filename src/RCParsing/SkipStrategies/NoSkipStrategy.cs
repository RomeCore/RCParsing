using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that just directly parsed rule without any skipping.
	/// </summary>
	public class NoSkipStrategy : SkipStrategy
	{
		public override ParsedRule ParseWithSkip(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			return rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
		}

		public override IEnumerable<ParsedRule> FindAllMatches(ParserContext context, ParserSettings settings, bool overlap,
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