using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that skips a specific rule before parsing.
	/// </summary>
	public class SkipBeforeParsingStrategy : SkipStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the ID of the rule to be skipped before parsing.
		/// </summary>
		public int SkipRuleId { get; }

		/// <summary>
		/// Gets the rule to be skipped before parsing.
		/// </summary>
		public ParserRule SkipRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SkipBeforeParsingStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be skipped before parsing.</param>
		public SkipBeforeParsingStrategy(int skipRuleId)
		{
			SkipRuleId = skipRuleId;
		}

		public void Initialize(Parser parser)
		{
			SkipRule = parser.GetRule(SkipRuleId);
		}

		public override ParsedRule ParseWithSkip(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			SkipRule.AdvanceContext(ref context, ref settings, out var childSkipSettings);

			var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
			if (parsedSkip.success)
				ruleContext.position = parsedSkip.endIndex;
			return rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
		}

		public override IEnumerable<ParsedRule> FindAllMatches(ParserContext context, ParserSettings settings, bool overlap,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			SkipRule.AdvanceContext(ref context, ref settings, out var childSkipSettings);

			while (ruleContext.position < ruleContext.maxPosition)
			{
				var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
				if (parsedSkip.success)
					ruleContext.position = context.position = parsedSkip.endIndex;

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