namespace RCParsing.SkipStrategies
{
	/// <summary>
	/// Represents a skip strategy that tries to parse first, then skips if parsing fails.
	/// </summary>
	public class TryParseThenSkipStrategy : SkipStrategy, IInitializeAfterBuild
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
		/// Initializes a new instance of the <see cref="TryParseThenSkipStrategy"/> class.
		/// </summary>
		/// <param name="skipRuleId">The ID of the rule to be skipped after failed parsing attempt.</param>
		public TryParseThenSkipStrategy(int skipRuleId)
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

			// First try to parse
			var parseResult = rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			if (parseResult.success)
				return parseResult;

			// If parsing failed, try to skip then parse again
			var parsedSkip = SkipRule.Parse(context, settings, childSkipSettings);
			if (parsedSkip.success)
			{
				ruleContext.position = context.position = parsedSkip.endIndex;
				return rule.Parse(ruleContext, ruleSettings, ruleChildSettings);
			}

			// If skip also failed, return failure
			return ParsedRule.Fail;
		}
	}
}