namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that tries to parse non-empty content first, then skips if non-empty parsing fails.
	/// </summary>
	public class TryParseNonEmptyThenSkipStrategy : SkipStrategy, IInitializeAfterBuild
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
		/// Initializes a new instance of the <see cref="TryParseNonEmptyThenSkipStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be skipped after failed non-empty parsing attempt.</param>
		public TryParseNonEmptyThenSkipStrategy(int skipRuleId)
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

			// Try to parse non-empty content first
			var lastResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			if (lastResult.success && lastResult.length > 0)
				return lastResult;

			// If non-empty parsing failed, try to skip then parse again
			var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
			if (parsedSkip.success)
			{
				ruleContext.position = context.position = parsedSkip.endIndex;
				return rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			}

			// If skip also failed but we had a successful (but empty) parse, return that
			if (lastResult.success)
				return lastResult;

			// Otherwise return failure
			return ParsedRule.Fail;
		}
	}
}