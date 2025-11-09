using System.Collections.Generic;

namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that alternates between skipping and parsing until parsing succeeds.
	/// </summary>
	public class SkipBeforeParsingLazyStrategy : SkipStrategy, IInitializeAfterBuild
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
		/// Initializes a new instance of the <see cref="SkipBeforeParsingLazyStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be skipped before parsing.</param>
		public SkipBeforeParsingLazyStrategy(int skipRuleId)
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
			settings.skippingStrategy = null;
			SkipRule.AdvanceContext(ref context, ref settings, out var childSkipSettings);

			// Alternate: Skip -> TryParse -> Skip -> TryParse ... until TryParse succeeds
			while (true)
			{
				var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
				if (parsedSkip.endIndex > context.position)
				{
					ruleContext.position = context.position = parsedSkip.endIndex;

					var parseResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
					if (parseResult.success)
						return parseResult;

					continue;
				}

				return rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			}
		}

		public override IEnumerable<ParsedRule> FindAllMatches(ParserContext context, ParserSettings settings, bool overlap,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			settings.skippingStrategy = null;
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