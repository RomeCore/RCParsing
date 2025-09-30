using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	public partial class Parser
	{
		static ParsedRule TryRecover(ParserRule rule, ref ParserContext context,
			ref ParserSettings settings, ref ParserSettings childSettings)
		{
			var recovery = rule.ErrorRecovery;
			context.position++;
			switch (recovery.strategy)
			{
				default:
				case ErrorRecoveryStrategy.None:
					return ParsedRule.Fail;

				case ErrorRecoveryStrategy.FindNext:
					if (context.errors.Count > 0)
						context.errorRecoveryIndices.Add(context.errors.Count);
					return RecoverFindNext(ref recovery, rule, ref context, ref settings, ref childSettings);

				case ErrorRecoveryStrategy.SkipUntilAnchor:
					if (context.errors.Count > 0)
						context.errorRecoveryIndices.Add(context.errors.Count);
					return RecoverSkipUntilAnchor(ref recovery, rule, ref context, ref settings, ref childSettings);

				case ErrorRecoveryStrategy.SkipAfterAnchor:
					if (context.errors.Count > 0)
						context.errorRecoveryIndices.Add(context.errors.Count);
					return RecoverSkipAfterAnchor(ref recovery, rule, ref context, ref settings, ref childSettings);
			}
		}



		private static ParsedRule RecoverFindNext(ref ErrorRecovery recovery, ParserRule rule,
			ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
		{
			settings.errorHandling = ParserErrorHandlingMode.NoRecord;
			childSettings.errorHandling = ParserErrorHandlingMode.NoRecord;

			int barrierPosition = context.barrierTokens.GetNextBarrierPosition(context.position, context.passedBarriers);
			if (barrierPosition == -1)
				barrierPosition = context.maxPosition;

			if (recovery.stopRule != -1)
			{
				var stopRule = rule.Parser.Rules[recovery.stopRule];
				var stopCtx = context;
				var stopSettings = settings;
				stopRule.AdvanceContext(ref stopCtx, ref stopSettings, out var stopChildSettings);

				while (context.position <= barrierPosition)
				{
					if (TryParse(rule, ref context, ref settings, ref childSettings, out var parsedRule))
						return parsedRule;

					var parsedStopRule = stopRule.Parse(context, stopSettings, stopChildSettings);
					if (parsedStopRule.success)
						break;

					context.position++;
				}

				return ParsedRule.Fail;
			}

			while (context.position <= barrierPosition)
			{
				if (TryParse(rule, ref context, ref settings, ref childSettings, out var parsedRule))
					return parsedRule;
				context.position++;
			}

			return ParsedRule.Fail;
		}

		private static ParsedRule RecoverSkipUntilAnchor(ref ErrorRecovery recovery, ParserRule rule,
			ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
		{
			settings.errorHandling = ParserErrorHandlingMode.NoRecord;
			childSettings.errorHandling = ParserErrorHandlingMode.NoRecord;

			var parser = rule.Parser;
			var anchorRule = parser.Rules[recovery.anchorRule];
			var anchorCtx = context;
			var anchorSettings = settings;
			anchorRule.AdvanceContext(ref anchorCtx, ref anchorSettings, out var anchorChildSettings);

			int barrierPosition = context.barrierTokens.GetNextBarrierPosition(context.position, context.passedBarriers);
			if (barrierPosition == -1)
				barrierPosition = context.maxPosition;

			if (recovery.stopRule != -1)
			{
				var stopRule = rule.Parser.Rules[recovery.stopRule];
				var stopCtx = context;
				var stopSettings = settings;
				stopRule.AdvanceContext(ref stopCtx, ref stopSettings, out var stopChildSettings);

				while (context.position <= barrierPosition)
				{
					var parsedAnchorRule = anchorRule.Parse(context, anchorSettings, anchorChildSettings);
					if (parsedAnchorRule.success)
					{
						context.position = parsedAnchorRule.startIndex;
						var parsedRule = parser.TryParseRule(rule, ref context,
							ref settings, ref childSettings, false);

						if (parsedRule.success)
							return parsedRule;
						if (!recovery.repeatSkip)
							return ParsedRule.Fail;
					}

					var parsedStopRule = stopRule.Parse(context, stopSettings, stopChildSettings);
					if (parsedStopRule.success)
						break;

					context.position++;
				}

				return ParsedRule.Fail;
			}

			while (context.position <= barrierPosition)
			{
				var parsedAnchorRule = anchorRule.Parse(context, anchorSettings, anchorChildSettings);
				if (parsedAnchorRule.success)
				{
					context.position = parsedAnchorRule.startIndex;
					var parsedRule = parser.TryParseRule(rule, ref context,
						ref settings, ref childSettings, false);

					if (parsedRule.success)
						return parsedRule;
					if (!recovery.repeatSkip)
						return ParsedRule.Fail;
				}

				context.position++;
			}

			return ParsedRule.Fail;
		}

		private static ParsedRule RecoverSkipAfterAnchor(ref ErrorRecovery recovery, ParserRule rule,
			ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
		{
			settings.errorHandling = ParserErrorHandlingMode.NoRecord;
			childSettings.errorHandling = ParserErrorHandlingMode.NoRecord;

			var parser = rule.Parser;
			var anchorRule = parser.Rules[recovery.anchorRule];
			var anchorCtx = context;
			var anchorSettings = settings;
			anchorRule.AdvanceContext(ref anchorCtx, ref anchorSettings, out var anchorChildSettings);

			int barrierPosition = context.barrierTokens.GetNextBarrierPosition(context.position, context.passedBarriers);
			if (barrierPosition == -1)
				barrierPosition = context.maxPosition;

			if (recovery.stopRule != -1)
			{
				var stopRule = rule.Parser.Rules[recovery.stopRule];
				var stopCtx = context;
				var stopSettings = settings;
				stopRule.AdvanceContext(ref stopCtx, ref stopSettings, out var stopChildSettings);

				while (context.position <= barrierPosition)
				{
					var parsedAnchorRule = anchorRule.Parse(context, anchorSettings, anchorChildSettings);
					if (parsedAnchorRule.success)
					{
						context.position = parsedAnchorRule.startIndex + parsedAnchorRule.length;
						var parsedRule = parser.TryParseRule(rule, ref context,
							ref settings, ref childSettings, false);

						if (parsedRule.success)
							return parsedRule;
						if (!recovery.repeatSkip)
							return ParsedRule.Fail;
					}

					var parsedStopRule = stopRule.Parse(context, stopSettings, stopChildSettings);
					if (parsedStopRule.success)
						break;

					context.position++;
				}

				return ParsedRule.Fail;
			}

			while (context.position <= barrierPosition)
			{
				var parsedAnchorRule = anchorRule.Parse(context, anchorSettings, anchorChildSettings);
				if (parsedAnchorRule.success)
				{
					context.position = parsedAnchorRule.startIndex + parsedAnchorRule.length;
					var parsedRule = parser.TryParseRule(rule, ref context,
						ref settings, ref childSettings, false);

					if (parsedRule.success)
						return parsedRule;
					if (!recovery.repeatSkip)
						return ParsedRule.Fail;
				}

				context.position++;
			}

			return ParsedRule.Fail;
		}
	}
}