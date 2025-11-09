using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using RCParsing.Building.SkipStrategies;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// The local settings builder for the parser elements.
	/// </summary>
	public class ParserLocalSettingsBuilder : BuildableParserElementBase
	{
		private ParserLocalSettings _settings = new();
		private BuildableSkipStrategy? _skipStrategy = null;

		public override IEnumerable<BuildableParserElementBase?>? ElementChildren => 
			new[] { _skipStrategy };

		public override object? Build(List<int>? ruleChildren,
			List<int>? tokenChildren, List<object?>? elementChildren)
		{
			var result = _settings;
			result.skippingStrategy = elementChildren[0] as SkipStrategy;

			result.isDefault =
				result.skippingStrategyUseMode == ParserSettingMode.InheritForSelfAndChildren &&
				result.errorHandlingUseMode == ParserSettingMode.InheritForSelfAndChildren &&
				result.ignoreBarriersUseMode == ParserSettingMode.InheritForSelfAndChildren;

			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ParserLocalSettingsBuilder other &&
				   _settings.Equals(other._settings) && 
				   Equals(_skipStrategy, other._skipStrategy);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode = hashCode * 397 + _settings.GetHashCode();
			hashCode = hashCode * 397 + _skipStrategy?.GetHashCode() ?? 0;
			return hashCode;
		}


		/// <summary>
		/// Sets the skip rule and builtin skip strategy.
		/// </summary>
		/// <param name="builderAction">The action to build the skip strategy.</param>
		/// <param name="overrideMode">The override mode for the skip rule and skipping strategy setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder Skip(Action<SkipStrategyBuilder> builderAction,
			ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			var builder = new SkipStrategyBuilder();
			builderAction(builder);

			_skipStrategy = builder.BuildingSkipStrategy;
			_settings.skippingStrategyUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Sets the skip rule and builtin skip strategy.
		/// </summary>
		/// <param name="builderAction">The action to build the skip rule.</param>
		/// <param name="skippingStrategy">The builtin skipping strategy to use.</param>
		/// <param name="overrideMode">The override mode for the skip rule and skipping strategy setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder Skip(Action<RuleBuilder> builderAction,
			ParserSkippingStrategy skippingStrategy = ParserSkippingStrategy.SkipBeforeParsing,
			ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			var builder = new RuleBuilder();
			builderAction(builder);

			_skipStrategy = new BuildableSimpleSkipStrategy
			{
				Strategy = skippingStrategy,
				SkipRule = builder.BuildingRule,
			};
			_settings.skippingStrategyUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Sets the builtin skip strategy without skip-rule.
		/// </summary>
		/// <param name="skippingStrategy">The builtin skipping strategy to use.</param>
		/// <param name="overrideMode">The override mode for the skip rule and skipping strategy setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder Skip(ParserSkippingStrategy skippingStrategy,
			ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_skipStrategy = new BuildableSimpleSkipStrategy
			{
				Strategy = skippingStrategy,
				SkipRule = null,
			};
			_settings.skippingStrategyUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Sets the whitespaces skip token and <see cref="ParserSkippingStrategy.SkipBeforeParsing"/> strategy.
		/// </summary>
		/// <param name="overrideMode">The override mode for the skip rule and skipping strategy setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder SkipWhitespaces(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return Skip(r => r.Whitespaces(), overrideMode: overrideMode);
		}

		/// <summary>
		/// Sets the whitespaces skip strategy that directly skips whitespace characters before parsing.
		/// </summary>
		/// <param name="overrideMode">The override mode for the skip rule and skipping strategy setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder SkipWhitespacesOptimized(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return Skip(ParserSkippingStrategy.Whitespaces, overrideMode: overrideMode);
		}

		/// <summary>
		/// Removes the skip rule.
		/// </summary>
		/// <param name="overrideMode">The override mode for the skip rule setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder NoSkipping(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_skipStrategy = new BuildableSimpleSkipStrategy
			{
				Strategy = ParserSkippingStrategy.Default,
				SkipRule = null,
			};
			_settings.skippingStrategyUseMode = overrideMode;
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