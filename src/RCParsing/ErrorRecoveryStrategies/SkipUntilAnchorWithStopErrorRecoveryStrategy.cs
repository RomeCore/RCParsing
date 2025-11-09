namespace RCParsing.ErrorRecoveryStrategies
{
	/// <summary>
	/// Represents an error recovery strategy that skips input until an anchor rule is found, with an optional stop rule.
	/// </summary>
	public class SkipUntilAnchorWithStopErrorRecoveryStrategy : ErrorRecoveryStrategy, IInitializeAfterBuild
	{
		/// <summary>
		/// Gets the ID of the anchor rule that the recovery strategy should look for.
		/// </summary>
		public int AnchorRuleId { get; }

		/// <summary>
		/// Gets the ID of the stop rule that should terminate the recovery process.
		/// </summary>
		public int StopRuleId { get; }

		/// <summary>
		/// Gets a value indicating whether the skip should be repeated if parsing fails after finding the anchor.
		/// </summary>
		public bool RepeatSkip { get; }

		/// <summary>
		/// Gets the anchor rule that the recovery strategy should look for.
		/// </summary>
		public ParserRule AnchorRule { get; private set; }

		/// <summary>
		/// Gets the stop rule that should terminate the recovery process.
		/// </summary>
		public ParserRule StopRule { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SkipUntilAnchorWithStopErrorRecoveryStrategy"/> class.
		/// </summary>
		/// <param name="anchorRuleId">The ID of the anchor rule that the recovery strategy should look for.</param>
		/// <param name="stopRuleId">The ID of the stop rule that should terminate the recovery process.</param>
		/// <param name="repeatSkip">A value indicating whether the skip should be repeated if parsing fails after finding the anchor.</param>
		public SkipUntilAnchorWithStopErrorRecoveryStrategy(int anchorRuleId, int stopRuleId, bool repeatSkip = false)
		{
			AnchorRuleId = anchorRuleId;
			StopRuleId = stopRuleId;
			RepeatSkip = repeatSkip;
		}

		public void Initialize(Parser parser)
		{
			AnchorRule = parser.GetRule(AnchorRuleId);
			StopRule = parser.GetRule(StopRuleId);
		}

		public override ParsedRule TryRecover(ParserContext context, ParserSettings settings,
			ParserRule rule, ParserContext ruleContext, ParserSettings ruleSettings, ParserSettings ruleChildSettings)
		{
			var recoveryContext = context;
			var recoverySettings = settings;
			recoveryContext.RecordErrorRecoveryIndex();
			AnchorRule.AdvanceContext(ref recoveryContext, ref recoverySettings, out var anchorChildSettings);
			StopRule.AdvanceContext(ref recoveryContext, ref recoverySettings, out var stopChildSettings);

			// Disable error recording during recovery
			recoverySettings.errorHandling = ParserErrorHandlingMode.NoRecord;
			ruleSettings.errorHandling = ParserErrorHandlingMode.NoRecord;

			var barrierPosition = recoveryContext.barrierTokens.GetNextBarrierPosition(recoveryContext.position, recoveryContext.passedBarriers);
			if (barrierPosition == -1)
				barrierPosition = recoveryContext.maxPosition;

			ruleContext.position++;
			recoveryContext.position = ruleContext.position;
			while (ruleContext.position <= barrierPosition)
			{
				var parsedStop = StopRule.Parse(recoveryContext, recoverySettings, stopChildSettings);
				if (parsedStop.success)
					break;

				var parsedAnchor = AnchorRule.Parse(recoveryContext, recoverySettings, anchorChildSettings);
				if (parsedAnchor.success)
				{
					// Skip past the anchor and try to parse the rule
					context.position = ruleContext.position = parsedAnchor.startIndex;
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