namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that tries to parse first, then greedily skips if parsing fails.
	/// </summary>
	public class TryParseThenSkipGreedyStrategy : SkipStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the ID of the rule to be greedily skipped after failed parsing attempt.
		/// </summary>
		public int SkipRuleId { get; }

		/// <summary>
		/// Gets the rule to be greedily skipped after failed parsing attempt.
		/// </summary>
		public ParserRule SkipRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TryParseThenSkipGreedyStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be greedily skipped after failed parsing attempt.</param>
		public TryParseThenSkipGreedyStrategy(int skipRuleId)
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

			// Try parse first
			var parseResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			if (parseResult.success)
				return parseResult;

			// If parsing failed, greedily skip then parse once
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