namespace RCParsing.ErrorRecoveryStrategies
{
	/// <summary>
	/// Represents an error recovery strategy that skips input after an anchor rule is found.
	/// </summary>
	public class SkipAfterAnchorErrorRecoveryStrategy : ErrorRecoveryStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the ID of the anchor rule that the recovery strategy should look for.
		/// </summary>
		public int AnchorRuleId { get; }

		/// <summary>
		/// Gets a value indicating whether the skip should be repeated if parsing fails after finding the anchor.
		/// </summary>
		public bool RepeatSkip { get; }

		/// <summary>
		/// Gets the anchor rule that the recovery strategy should look for.
		/// </summary>
		public ParserRule AnchorRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SkipAfterAnchorErrorRecoveryStrategy"/> class.
		/// </summary>
		/// <param name="anchorRuleId">The ID of the anchor rule that the recovery strategy should look for.</param>
		/// <param name="repeatSkip">A value indicating whether the skip should be repeated if parsing fails after finding the anchor.</param>
		public SkipAfterAnchorErrorRecoveryStrategy(int anchorRuleId, bool repeatSkip = false)
		{
			AnchorRuleId = anchorRuleId;
			RepeatSkip = repeatSkip;
		}

		public void Initialize(Parser parser)
		{
			AnchorRule = parser.GetRule(AnchorRuleId);
		}

		public override ParsedRule TryRecover(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			var recoveryContext = context;
			var recoverySettings = settings;
			recoveryContext.RecordErrorRecoveryIndex();

			// Disable error recording during recovery
			recoverySettings.errorHandling = ParserErrorHandlingMode.NoRecord;
			ruleSettings.errorHandling = ParserErrorHandlingMode.NoRecord;
			ruleChildSettings.errorHandling = ParserErrorHandlingMode.NoRecord;

			AnchorRule.AdvanceContext(ref recoveryContext, ref recoverySettings, out var anchorChildSettings);

			var barrierPosition = recoveryContext.barrierTokens.GetNextBarrierPosition(recoveryContext.position, recoveryContext.passedBarriers);
			if (barrierPosition == -1)
				barrierPosition = recoveryContext.maxPosition;

			ruleContext.position++;
			recoveryContext.position = ruleContext.position;
			while (ruleContext.position <= barrierPosition)
			{
				var parsedAnchor = AnchorRule.Parse(recoveryContext, recoverySettings, anchorChildSettings);
				if (parsedAnchor.success)
				{
					// Skip past the anchor and try to parse the rule
					context.position = ruleContext.position = parsedAnchor.startIndex + parsedAnchor.length;
					var skipStrategy = ruleSettings.skippingStrategy ?? SkipStrategy.NoSkipping;
					var parseResult = skipStrategy.ParseWithSkip(context, settings,
						rule, ruleContext, ruleSettings, ruleChildSettings);

					if (parseResult.success)
						return parseResult;

					if (!RepeatSkip)
						return ParsedRule.Fail;

					// If repeat is enabled, continue searching
				}

				ruleContext.position++;
				recoveryContext.position = ruleContext.position;
			}

			return ParsedRule.Fail;
		}
	}
}