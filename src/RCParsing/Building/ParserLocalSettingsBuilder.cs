using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// The local settings builder for the parser elements.
	/// </summary>
	public class ParserLocalSettingsBuilder
	{
		private ParserLocalSettings _settings = default;
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
				result.maxRecursionDepthUseMode == ParserSettingMode.InheritForSelfAndChildren &&
				result.errorHandlingUseMode == ParserSettingMode.InheritForSelfAndChildren &&
				result.cachingUseMode == ParserSettingMode.InheritForSelfAndChildren;

			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ParserLocalSettingsBuilder other &&
				   _settings.Equals(other._settings) && 
				   _skipRule.Equals(other._skipRule);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= _settings.GetHashCode() * 23;
			hashCode ^= (_skipRule?.GetHashCode() ?? 0) * 27;
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
		/// Sets the caching mode.
		/// </summary>
		/// <param name="mode">The caching mode.</param>
		/// <param name="overrideMode">The override mode for the caching setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder Caching(ParserCachingMode mode, ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_changed = true;
			_settings.caching = mode;
			_settings.cachingUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Sets the default (disabled) caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser element to ignore any caching.
		/// </remarks>
		/// <param name="overrideMode">The override mode for the caching setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder NoCaching(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return Caching(ParserCachingMode.Default, overrideMode);
		}

		/// <summary>
		/// Sets the token and rule caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser element to use and write both rules and token patterns via caching.
		/// </remarks>
		/// <param name="overrideMode">The override mode for the caching setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder CacheAll(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return Caching(ParserCachingMode.CacheAll, overrideMode);
		}

		/// <summary>
		/// Sets the only rules caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser element to use and write rules via caching.
		/// </remarks>
		/// <param name="overrideMode">The override mode for the caching setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder CacheOnlyRules(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return Caching(ParserCachingMode.Rules, overrideMode);
		}

		/// <summary>
		/// Sets the only tokens caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser element to use and write token patterns via caching.
		/// </remarks>
		/// <param name="overrideMode">The override mode for the caching setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder CacheOnlyTokens(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return Caching(ParserCachingMode.TokenPatterns, overrideMode);
		}

		/// <summary>
		/// Sets the maximum recursion depth.
		/// </summary>
		/// <param name="depth">The maximum recursion depth. Can be 0 for no limit.</param>
		/// <param name="overrideMode">The override mode for the maximum recursion depth setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder MaxRecursionDepth(int depth, ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			_changed = true;
			_settings.maxRecursionDepth = depth;
			_settings.maxRecursionDepthUseMode = overrideMode;
			return this;
		}

		/// <summary>
		/// Sets the infinite recursion depth.
		/// </summary>
		/// <param name="overrideMode">The override mode for the maximum recursion depth setting.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserLocalSettingsBuilder DisableRecursionLimit(ParserSettingMode overrideMode = ParserSettingMode.LocalForSelfAndChildren)
		{
			return MaxRecursionDepth(0, overrideMode);
		}
	}
}