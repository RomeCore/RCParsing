using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// The local settings builder for the parser elements.
	/// </summary>
	public class ParserLocalSettingsBuilder
	{
		private ParserLocalSettings _settings = new ();
		private bool _changed = false;
		private Or<string, BuildableParserRule>? _skipRule = null;

		/// <summary>
		/// Gets the children of the settings builder.
		/// </summary>
		public IEnumerable<Or<string, BuildableParserRule>?> RuleChildren =>
			new Or<string, BuildableParserRule>?[] { _skipRule };

		/// <summary>
		/// Gets a value indicating whether the settings have been changed (at least one setting has been changed).
		/// </summary>
		public bool HaveBeenChanged => _changed;

		/// <summary>
		/// Builds the settings for parser.
		/// </summary>
		/// <param name="ruleChildren">The list of rule children IDs.</param>
		/// <returns>The built settings for parser.</returns>
		public ParserLocalSettings Build(List<int> ruleChildren)
		{
			var result = _settings;
			result.skipRule = ruleChildren[0];

			result.isDefault =
				result.skippingStrategyUseMode == ParserSettingMode.InheritForSelfAndChildren &&
				result.skipRuleUseMode == ParserSettingMode.InheritForSelfAndChildren &&
				result.errorHandlingUseMode == ParserSettingMode.InheritForSelfAndChildren &&
				result.ignoreBarriersUseMode == ParserSettingMode.InheritForSelfAndChildren;

			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ParserLocalSettingsBuilder other &&
				   _changed == other._changed && 
				   _settings.Equals(other._settings) && 
				   _skipRule.Equals(other._skipRule);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode = hashCode * 397 + _changed.GetHashCode();
			hashCode = hashCode * 397 + _settings.GetHashCode();
			hashCode = hashCode * 397 + _skipRule?.GetHashCode() ?? 0;
			return hashCode;
		}


		/// <summary>
		/// Sets the skip rule that will be skipped before parsing the current rule.
		/// </summary>
		/// <param name="builderAction">The action to build the skip rule.</param>
		/// <param name="skippingStrategy">The skipping strategy to use.</param>
		/// <param name="overrideMode">The override mode for the skip rule and skipping strategy setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder Skip(Action<RuleBuilder> builderAction,
			ParserSkippingStrategy skippingStrategy = ParserSkippingStrategy.SkipBeforeParsing,
			ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_changed = true;
			var builder = new RuleBuilder();
			builderAction(builder);
			_skipRule = builder.BuildingRule;
			_settings.skipRuleUseMode = overrideMode;
			_settings.skippingStrategy = skippingStrategy;
			_settings.skippingStrategyUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Sets the skipping strategy for the current rule.
		/// </summary>
		/// <param name="skippingStrategy">The skipping strategy to use.</param>
		/// <param name="overrideMode">The override mode for the skipping strategy setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder SkippingStrategy(ParserSkippingStrategy skippingStrategy,
			ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_changed = true;
			_settings.skippingStrategy = skippingStrategy;
			_settings.skippingStrategyUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Removes the skip rule.
		/// </summary>
		/// <param name="overrideMode">The override mode for the skip rule setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder NoSkipping(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_changed = true;
			_settings.skippingStrategy = ParserSkippingStrategy.Default;
			_settings.skippingStrategyUseMode = overrideMode;
			_skipRule = null;
			_settings.skipRuleUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Sets the error handling mode.
		/// </summary>
		/// <param name="mode">The error handling mode.</param>
		/// <param name="overrideMode">The override mode for the error handling setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder ErrorHandling(ParserErrorHandlingMode mode, ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_changed = true;
			_settings.errorHandling = mode;
			_settings.errorHandlingUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Sets the default error handling mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser element to record errors.
		/// </remarks>
		/// <param name="overrideMode">The override mode for the error handling setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder RecordErrors(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return ErrorHandling(ParserErrorHandlingMode.Default, overrideMode);
		}

		/// <summary>
		/// Sets the no error record handling mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser element to ignore any errors when trying to record them.
		/// </remarks>
		/// <param name="overrideMode">The override mode for the error handling setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder IgnoreErrors(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return ErrorHandling(ParserErrorHandlingMode.NoRecord, overrideMode);
		}

		/// <summary>
		/// Sets the error throw handling mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser element to throw errors when trying to record them.
		/// </remarks>
		/// <param name="overrideMode">The override mode for the error handling setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder ThrowErrors(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return ErrorHandling(ParserErrorHandlingMode.Throw, overrideMode);
		}

		/// <summary>
		/// Sets the barrier tokens to be ignored while parsing.
		/// </summary>
		/// <param name="ignore">Whether to ignore barrier tokens or not.</param>
		/// <param name="overrideMode">The override mode for the barriers ignore setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder IgnoreBarriers(bool ignore, ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_settings.ignoreBarriers = ignore;
			_settings.ignoreBarriersUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Ignores barrier tokens while parsing.
		/// </summary>
		/// <param name="overrideMode">The override mode for the barriers ignore setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder IgnoreBarriers(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_settings.ignoreBarriers = true;
			_settings.ignoreBarriersUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Enables barrier tokens while parsing.
		/// </summary>
		/// <param name="overrideMode">The override mode for the barriers ignore setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder RestoreBarriers(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_settings.ignoreBarriers = false;
			_settings.ignoreBarriersUseMode = overrideMode;
			return this;
		}
	}
}