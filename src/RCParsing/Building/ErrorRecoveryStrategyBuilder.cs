using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.Building.ErrorRecoveryStrategies;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a buildable error recovery mechanism for parsing rules.
	/// </summary>
	public class ErrorRecoveryStrategyBuilder : BuildableParserElementBase<ErrorRecoveryStrategy>
	{
		/// <summary>
		/// Gets or sets the error recovery strategy being built.
		/// </summary>
		public BuildableErrorRecoveryStrategy? BuildingErrorRecovery { get; set; }

		public override IEnumerable<BuildableParserElementBase?>? ElementChildren =>
			new[] { BuildingErrorRecovery };

		public override ErrorRecoveryStrategy BuildTyped(List<int>? ruleChildren,
			List<int>? tokenChildren, List<object?>? elementChildren)
		{
			return elementChildren[0] as ErrorRecoveryStrategy;
		}

		public override bool Equals(object obj)
		{
			return obj is ErrorRecoveryStrategyBuilder other &&
				Equals(BuildingErrorRecovery, other.BuildingErrorRecovery);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode = hashCode * 397 + BuildingErrorRecovery?.GetHashCode() ?? 0;
			return hashCode;
		}

		/// <summary>
		/// Sets the recovery strategy to 'None' and clears any previously set anchor or stop rules.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ErrorRecoveryStrategyBuilder NoRecovery()
		{
			BuildingErrorRecovery = new BuildableLeafErrorRecoveryStrategy
			{
				Strategy = ErrorRecoveryStrategy.NoRecovery
			};
			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'FindNext' which attempts to find the next valid occurence
		/// and clears any previously set anchor or stop rules.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ErrorRecoveryStrategyBuilder FindNext()
		{
			BuildingErrorRecovery = new BuildableLeafErrorRecoveryStrategy
			{
				Strategy = ErrorRecoveryStrategy.FindNext
			};
			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'FindNext' with a stop rule that defines when to stop searching.
		/// The parser will search for the next valid token until the stop rule is matched.
		/// </summary>
		/// <param name="stopBuilderAction">Action that configures the stop rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the stop rule cannot be built.</exception>
		public ErrorRecoveryStrategyBuilder FindNextUntil(Action<RuleBuilder> stopBuilderAction)
		{
			var stopBuilder = new RuleBuilder();
			stopBuilderAction(stopBuilder);

			BuildingErrorRecovery = new BuildableFindNextErrorRecoveryStrategy
			{
				StopRule = stopBuilder.BuildingRule
			};
			return this;
		}

		/// <summary>
		/// Sets the recovery strategy to 'SkipUntilAnchor' which skips text until the anchor rule is matched.
		/// </summary>
		/// <param name="anchorBuilderAction">Action that configures the anchor rule.</param>
		/// <param name="repeat">Whether to repeat the search if another error occurs when skipping.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the anchor rule cannot be built.</exception>
		public ErrorRecoveryStrategyBuilder SkipUntil(Action<RuleBuilder> anchorBuilderAction, bool repeat = false)
		{
			var anchorBuilder = new RuleBuilder();
			anchorBuilderAction(anchorBuilder);
			if (!anchorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Anchor rule must be set for SkipUntilAnchor recovery strategy.");

			BuildingErrorRecovery = new BuildableSkipUntilAnchorErrorRecoveryStrategy
			{
				AnchorRule = anchorBuilder.BuildingRule.Value,
				RepeatSkip = repeat
			};
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
		public ErrorRecoveryStrategyBuilder SkipUntil(Action<RuleBuilder> anchorBuilderAction,
			Action<RuleBuilder> stopBuilderAction, bool repeat = false)
		{
			var anchorBuilder = new RuleBuilder();
			anchorBuilderAction(anchorBuilder);
			if (!anchorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Anchor rule must be set for SkipUntilAnchor recovery strategy.");

			var stopBuilder = new RuleBuilder();
			stopBuilderAction(stopBuilder);

			BuildingErrorRecovery = new BuildableSkipUntilAnchorErrorRecoveryStrategy
			{
				AnchorRule = anchorBuilder.BuildingRule.Value,
				StopRule = stopBuilder.BuildingRule,
				RepeatSkip = repeat
			};
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
		public ErrorRecoveryStrategyBuilder SkipAfter(Action<RuleBuilder> anchorBuilderAction, bool repeat = false)
		{
			var anchorBuilder = new RuleBuilder();
			anchorBuilderAction(anchorBuilder);
			if (!anchorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Anchor rule must be set for SkipAfterAnchor recovery strategy.");

			BuildingErrorRecovery = new BuildableSkipAfterAnchorErrorRecoveryStrategy
			{
				AnchorRule = anchorBuilder.BuildingRule.Value,
				RepeatSkip = repeat
			};
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
		public ErrorRecoveryStrategyBuilder SkipAfter(Action<RuleBuilder> anchorBuilderAction,
			Action<RuleBuilder> stopBuilderAction, bool repeat = false)
		{
			var anchorBuilder = new RuleBuilder();
			anchorBuilderAction(anchorBuilder);
			if (!anchorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Anchor rule must be set for SkipAfterAnchor recovery strategy.");

			var stopBuilder = new RuleBuilder();
			stopBuilderAction(stopBuilder);

			BuildingErrorRecovery = new BuildableSkipAfterAnchorErrorRecoveryStrategy
			{
				AnchorRule = anchorBuilder.BuildingRule.Value,
				StopRule = stopBuilder.BuildingRule,
				RepeatSkip = repeat
			};
			return this;
		}
	}
}