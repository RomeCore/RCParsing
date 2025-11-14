using System.Collections.Generic;

namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that repeadetly skips a specific rule before parsing.
	/// </summary>
	public class SkipBeforeParsingGreedyStrategy : SkipStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the ID of the rule to be repeatedly skipped before parsing.
		/// </summary>
		public int SkipRuleId { get; }

		/// <summary>
		/// Gets the rule to be repeatedly skipped before parsing.
		/// </summary>
		public ParserRule SkipRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SkipBeforeParsingGreedyStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be skipped before parsing.</param>
		public SkipBeforeParsingGreedyStrategy(int skipRuleId)
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

			while (true)
			{
				var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
				if (parsedSkip.endIndex > context.position)
					ruleContext.position = context.position = parsedSkip.endIndex;
				else
					break;
			}

			return rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
		}

		public override IEnumerable<ParsedRule> FindAllMatches(ParserContext context, ParserSettings settings, bool overlap,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			SkipRule.AdvanceContext(ref context, ref settings, out var childSkipSettings);

			while (ruleContext.position < ruleContext.maxPosition)
			{
				while (true)
				{
					var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
					if (parsedSkip.endIndex > context.position)
						ruleContext.position = context.position = parsedSkip.endIndex;
					else
						break;
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