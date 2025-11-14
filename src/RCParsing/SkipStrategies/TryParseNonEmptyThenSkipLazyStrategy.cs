namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that tries to parse non-empty content first, then alternates between skipping and parsing until non-empty parsing succeeds.
	/// </summary>
	public class TryParseNonEmptyThenSkipLazyStrategy : SkipStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the ID of the rule to be skipped after failed non-empty parsing attempt.
		/// </summary>
		public int SkipRuleId { get; }

		/// <summary>
		/// Gets the rule to be skipped after failed non-empty parsing attempt.
		/// </summary>
		public ParserRule SkipRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TryParseNonEmptyThenSkipLazyStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be skipped after failed non-empty parsing attempt.</param>
		public TryParseNonEmptyThenSkipLazyStrategy(int skipRuleId)
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

			// First try parse non-empty content
			var firstResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			if (firstResult.success && firstResult.length > 0)
				return firstResult;

			// Then alternate Skip -> TryParse -> Skip -> TryParse ... until non-empty success or nothing consumes
			var lastResult = ParsedRule.Fail;
			while (true)
			{
				var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
				if (parsedSkip.success)
				{
					ruleContext.position = context.position = parsedSkip.endIndex;

					lastResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
					if (lastResult.success && lastResult.length > 0)
						return lastResult;

					continue;
				}

				// If skip failed but we had a successful (but empty) parse, return that
				if (lastResult.success)
					return lastResult;

				// Otherwise return failure
				return ParsedRule.Fail;
			}
		}
	}
}