using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// The settings builder for the parser itself.
	/// </summary>
	public class ParserSettingsBuilder
	{
		private ParserSettings _settings = new ParserSettings();
		private Or<string, BuildableParserRule>? _skipRule = null;

		/// <summary>
		/// Gets the children of the settings builder.
		/// </summary>
		public IEnumerable<Or<string, BuildableParserRule>?> RuleChildren =>
			new Or<string, BuildableParserRule>?[] { _skipRule };

		/// <summary>
		/// Builds the settings for parser.
		/// </summary>
		/// <param name="ruleChildren">The list of child elements.</param>
		/// <returns>The built settings for parser.</returns>
		public ParserSettings Build(List<int> ruleChildren)
		{
			var result = _settings;
			result.skipRule = ruleChildren[0];
			return result;
		}

		public override bool Equals(object? obj)
		{
			return obj is ParserSettingsBuilder other &&
				   _settings == other._settings &&
				   _skipRule == other._skipRule;
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= _settings.GetHashCode() * 23;
			hashCode ^= (_skipRule?.GetHashCode() ?? 0) * 47;
			return hashCode;
		}



		/// <summary>
		/// Sets the skip rule that will be skipped before parsing rules.
		/// </summary>
		/// <param name="builderAction">The action to build the skip rule.</param>
		/// <param name="skippingStrategy">The skipping strategy to use.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder Skip(Action<RuleBuilder> builderAction,
			ParserSkippingStrategy skippingStrategy = ParserSkippingStrategy.SkipBeforeParsing)
		{
			var builder = new RuleBuilder();
			builderAction(builder);
			_skipRule = builder.BuildingRule;
			_settings.skippingStrategy = skippingStrategy;
			return this;
		}

		/// <summary>
		/// Sets the skipping strategy.
		/// </summary>
		/// <param name="skippingStrategy">The skipping strategy to use.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder SkippingStrategy(ParserSkippingStrategy skippingStrategy)
		{
			_settings.skippingStrategy = skippingStrategy;
			return this;
		}

		/// <summary>
		/// Removes the skip rule.
		/// </summary>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder NoSkipping()
		{
			_settings.skippingStrategy = ParserSkippingStrategy.Default;
			return this;
		}

		/// <summary>
		/// Sets the error handling mode.
		/// </summary>
		/// <param name="mode">The error handling mode.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder ErrorHandling(ParserErrorHandlingMode mode)
		{
			_settings.errorHandling = mode;
			return this;
		}

		/// <summary>
		/// Sets the default error handling mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to record errors.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder RecordErrors()
		{
			return ErrorHandling(ParserErrorHandlingMode.Default);
		}

		/// <summary>
		/// Sets the no error record handling mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to ignore any errors when trying to record them.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder IgnoreErrors()
		{
			return ErrorHandling(ParserErrorHandlingMode.NoRecord);
		}

		/// <summary>
		/// Sets the error throw handling mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to throw errors when trying to record them.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder ThrowErrors()
		{
			return ErrorHandling(ParserErrorHandlingMode.Throw);
		}

		/// <summary>
		/// Sets the detailed error messages mode when throwing exceptions.
		/// </summary>
		/// <remarks>
		/// Useful for debugging purposes.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder DetailedErrors()
		{
			return ErrorHandling(_settings.errorHandling | ParserErrorHandlingMode.DisplayRules |
				ParserErrorHandlingMode.DisplayMessages | ParserErrorHandlingMode.DisplayExtended);
		}

		/// <summary>
		/// Sets the caching mode.
		/// </summary>
		/// <param name="mode">The caching mode.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder Caching(ParserCachingMode mode)
		{
			_settings.caching = mode;
			return this;
		}

		/// <summary>
		/// Sets the default (disabled) caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to ignore any caching.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder NoCaching()
		{
			return Caching(ParserCachingMode.Default);
		}

		/// <summary>
		/// Sets the token and rule caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to use and write both rules and token patterns via caching.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder CacheAll()
		{
			return Caching(ParserCachingMode.CacheAll);
		}

		/// <summary>
		/// Sets the only rules caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to use and write rules via caching.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder CacheOnlyRules()
		{
			return Caching(ParserCachingMode.Rules);
		}

		/// <summary>
		/// Sets the only tokens caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to use and write token patterns via caching.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder CacheOnlyTokens()
		{
			return Caching(ParserCachingMode.TokenPatterns);
		}

		/// <summary>
		/// Sets the maximum recursion depth.
		/// </summary>
		/// <param name="depth">The maximum recursion depth. Can be 0 for no limit.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder MaxRecursionDepth(int depth)
		{
			_settings.maxRecursionDepth = depth;
			return this;
		}

		/// <summary>
		/// Sets the infinite recursion depth.
		/// </summary>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder DisableRecursionLimit()
		{
			return MaxRecursionDepth(0);
		}
	}
}