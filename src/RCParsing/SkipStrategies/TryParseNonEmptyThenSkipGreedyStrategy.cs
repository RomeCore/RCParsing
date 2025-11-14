namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that tries to parse non-empty content first, then greedily skips if non-empty parsing fails.
	/// </summary>
	public class TryParseNonEmptyThenSkipGreedyStrategy : SkipStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the ID of the rule to be greedily skipped after failed non-empty parsing attempt.
		/// </summary>
		public int SkipRuleId { get; }

		/// <summary>
		/// Gets the rule to be greedily skipped after failed non-empty parsing attempt.
		/// </summary>
		public ParserRule SkipRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TryParseNonEmptyThenSkipGreedyStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be greedily skipped after failed non-empty parsing attempt.</param>
		public TryParseNonEmptyThenSkipGreedyStrategy(int skipRuleId)
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

			// Try parse non-empty content first
			var firstResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			if (firstResult.success && firstResult.length > 0)
				return firstResult;

			// If non-empty parsing failed, greedily skip then parse once
			while (true)
			{
				var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
				if (parsedSkip.length > 0)
				{
					ruleContext.position = context.position = parsedSkip.endIndex;
				}
				else
				{
					break;
				}
			}

			return rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
		}
	}
}