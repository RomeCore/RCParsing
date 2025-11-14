namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that tries to parse first, then alternates between skipping and parsing until parsing succeeds.
	/// </summary>
	public class TryParseThenSkipLazyStrategy : SkipStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the ID of the rule to be skipped after failed parsing attempt.
		/// </summary>
		public int SkipRuleId { get; }

		/// <summary>
		/// Gets the rule to be skipped after failed parsing attempt.
		/// </summary>
		public ParserRule SkipRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TryParseThenSkipLazyStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be skipped after failed parsing attempt.</param>
		public TryParseThenSkipLazyStrategy(int skipRuleId)
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

			// First try parse
			var parseResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			if (parseResult.success)
				return parseResult;

			// Then alternate Skip -> TryParse -> Skip -> TryParse ... until success or nothing consumes
			while (true)
			{
				var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
				if (parsedSkip.success)
				{
					ruleContext.position = context.position = parsedSkip.endIndex;

					parseResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
					if (parseResult.success)
						return parseResult;

					continue;
				}

				// If skip failed and we haven't parsed anything, return failure
				return ParsedRule.Fail;
			}
		}
	}
}