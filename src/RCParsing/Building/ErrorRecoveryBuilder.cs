using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a buildable error recovery mechanism for parsing rules.
	/// </summary>
	public class ErrorRecoveryBuilder
	{
		private ErrorRecovery _recovery = new();
		private Or<string, BuildableParserRule>? _anchorRule = null;
		private Or<string, BuildableParserRule>? _stopRule = null;

		/// <summary>
		/// Gets the children of the settings builder.
		/// </summary>
		public IEnumerable<Or<string, BuildableParserRule>?> RuleChildren =>
			new Or<string, BuildableParserRule>?[] { _anchorRule, _stopRule };

		/// <summary>
		/// Builds the error recovery for parser.
		/// </summary>
		/// <param name="ruleChildren">The list of rule children IDs.</param>
		/// <returns>The built error recovery for parser.</returns>
		public ErrorRecovery Build(List<int> ruleChildren)
		{
			var result = _recovery;
			result.anchorRule = ruleChildren[0];
			result.stopRule = ruleChildren[1];

			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ErrorRecoveryBuilder other &&
				   _recovery.Equals(other._recovery) &&
				   _anchorRule.Equals(other._anchorRule) &&
				   _stopRule.Equals(other._stopRule);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode = hashCode * 397 + _recovery.GetHashCode();
			hashCode = hashCode * 397 + _anchorRule?.GetHashCode() ?? 0;
			hashCode = hashCode * 397 + _stopRule?.GetHashCode() ?? 0;
			return hashCode;
		}

		/// <summary>
		/// Sets the recovery strategy to 'None' and clears any previously set anchor or stop rules.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ErrorRecoveryBuilder NoRecovery()
		{
			_recovery.strategy = ErrorRecoveryStrategy.None;
			_anchorRule = null;
			_stopRule = null;
			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'FindNext' which attempts to find the next valid occurence
		/// and clears any previously set anchor or stop rules.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ErrorRecoveryBuilder FindNext()
		{
			_recovery.strategy = ErrorRecoveryStrategy.FindNext;
			_anchorRule = null;
			_stopRule = null;
			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'FindNext' with a stop rule that defines when to stop searching.
		/// The parser will search for the next valid token until the stop rule is matched.
		/// </summary>
		/// <param name="stopBuilderAction">Action that configures the stop rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the stop rule cannot be built.</exception>
		public ErrorRecoveryBuilder FindNextUntil(Action<RuleBuilder> stopBuilderAction)
		{
			_recovery.strategy = ErrorRecoveryStrategy.FindNext;
			_anchorRule = null;

			var stopBuilder = new RuleBuilder();
			stopBuilderAction(stopBuilder);
			_stopRule = stopBuilder.BuildingRule;

			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'SkipUntilAnchor' which skips text until the anchor rule is matched.
		/// </summary>
		/// <param name="anchorBuilderAction">Action that configures the anchor rule.</param>
		/// <param name="repeat">Whether to repeat the search if another error occurs when skipping.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the anchor rule cannot be built.</exception>
		public ErrorRecoveryBuilder SkipUntil(Action<RuleBuilder> anchorBuilderAction, bool repeat = false)
		{
			_recovery.strategy = ErrorRecoveryStrategy.SkipUntilAnchor;
			_recovery.repeatSkip = repeat;

			var anchorBuilder = new RuleBuilder();
			anchorBuilderAction(anchorBuilder);
			if (!anchorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Anchor rule must be set for SkipUntilAnchor recovery strategy.");
			_anchorRule = anchorBuilder.BuildingRule;

			_stopRule = null;

			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'SkipUntilAnchor' with both anchor and stop rules.
		/// Skips text until either the anchor rule is matched or the stop rule is encountered.
		/// </summary>
		/// <param name="anchorBuilderAction">Action that configures the anchor rule.</param>
		/// <param name="stopBuilderAction">Action that configures the stop rule.</param>
		/// <param name="repeat">Whether to repeat the search if another error occurs when skipping.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the anchor rule cannot be built.</exception>
		public ErrorRecoveryBuilder SkipUntil(Action<RuleBuilder> anchorBuilderAction,
			Action<RuleBuilder> stopBuilderAction, bool repeat = false)
		{
			var anchorBuilder = new RuleBuilder();
			anchorBuilderAction(anchorBuilder);
			if (!anchorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Anchor rule must be set for SkipUntilAnchor recovery strategy.");

			_recovery.strategy = ErrorRecoveryStrategy.SkipUntilAnchor;
			_recovery.repeatSkip = repeat;
			_anchorRule = anchorBuilder.BuildingRule;

			var stopBuilder = new RuleBuilder();
			stopBuilderAction(stopBuilder);
			_stopRule = stopBuilder.BuildingRule;

			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'SkipAfterAnchor' which skips text until after the anchor rule is matched.
		/// The parser will resume parsing after the matched anchor.
		/// </summary>
		/// <param name="anchorBuilderAction">Action that configures the anchor rule.</param>
		/// <param name="repeat">Whether to repeat the search if another error occurs when skipping.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the anchor rule cannot be built.</exception>
		public ErrorRecoveryBuilder SkipAfter(Action<RuleBuilder> anchorBuilderAction, bool repeat = false)
		{
			var anchorBuilder = new RuleBuilder();
			anchorBuilderAction(anchorBuilder);
			if (!anchorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Anchor rule must be set for SkipAfterAnchor recovery strategy.");

			_recovery.strategy = ErrorRecoveryStrategy.SkipAfterAnchor;
			_recovery.repeatSkip = repeat;
			_anchorRule = anchorBuilder.BuildingRule;
			_stopRule = null;

			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'SkipAfterAnchor' with both anchor and stop rules.
		/// Skips text until after the anchor rule is matched or until the stop rule is encountered.
		/// </summary>
		/// <param name="anchorBuilderAction">Action that configures the anchor rule.</param>
		/// <param name="stopBuilderAction">Action that configures the stop rule.</param>
		/// <param name="repeat">Whether to repeat the search if another error occurs when skipping.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the anchor rule cannot be built.</exception>
		public ErrorRecoveryBuilder SkipAfter(Action<RuleBuilder> anchorBuilderAction,
			Action<RuleBuilder> stopBuilderAction, bool repeat = false)
		{
			var anchorBuilder = new RuleBuilder();
			anchorBuilderAction(anchorBuilder);
			if (!anchorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Anchor rule must be set for SkipAfterAnchor recovery strategy.");

			_recovery.strategy = ErrorRecoveryStrategy.SkipAfterAnchor;
			_recovery.repeatSkip = repeat;
			_anchorRule = anchorBuilder.BuildingRule;

			var stopBuilder = new RuleBuilder();
			stopBuilderAction(stopBuilder);
			_stopRule = stopBuilder.BuildingRule;

			return this;
		}
	}
}