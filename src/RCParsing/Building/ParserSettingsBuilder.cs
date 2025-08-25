using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// The settings builder for the parser itself.
	/// </summary>
	public class ParserSettingsBuilder
	{
		private ParserSettings _settings = new ParserSettings();
		private Func<ParserElement, ParserInitFlags> _initFlagsFactory = e => ParserInitFlags.None;
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
		public (ParserSettings, Func<ParserElement, ParserInitFlags>) Build(List<int> ruleChildren)
		{
			var result = _settings;
			result.skipRule = ruleChildren[0];
			return (result, _initFlagsFactory);
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
		/// Uses the specified initialization flags for all elements.
		/// </summary>
		/// <param name="flags">The initialization flags to use.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder UseInitFlags(ParserInitFlags flags)
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var _flags = prevFactory(e);
				return _flags | flags;
			};
			return this;
		}

		/// <summary>
		/// Sets the token and rule caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to use and write both rules and token patterns via caching.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder UseCaching()
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var flags = prevFactory(e);
				flags |= ParserInitFlags.EnableMemoization;
				return flags;
			};
			return this;
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
			return UseCachingOn(e => e is not TokenPattern && e is not TokenParserRule);
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
			return UseCachingOn(e => e is TokenPattern || e is TokenParserRule);
		}

		/// <summary>
		/// Sets the caching mode for token patterns and rules based on the specified predicate.
		/// </summary>
		/// <param name="predicate">The predicate to determine whether a parser element should be cached.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder UseCachingOn(Predicate<ParserElement> predicate)
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var flags = prevFactory(e);
				if (predicate(e))
					flags |= ParserInitFlags.EnableMemoization;
				return flags;
			};
			return this;
		}

		/// <summary>
		/// Sets the caching mode for token patterns and rules based on the specified predicate.
		/// </summary>
		/// <remarks>
		/// Overrides the previous caching mode with the new one for all elements.
		/// </remarks>
		/// <param name="predicate">The predicate to determine whether a parser element should be cached.</param>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder UseCachingOnOnly(Predicate<ParserElement> predicate)
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var flags = prevFactory(e);
				if (predicate(e))
					flags |= ParserInitFlags.EnableMemoization;
				else
					flags &= ~ParserInitFlags.EnableMemoization;
				return flags;
			};
			return this;
		}

		/// <summary>
		/// Uses the first character match mode for all elements.
		/// </summary>
		/// <remarks>
		/// May improve performance by avoiding unnecessary backtracking in some cases. However, it can also reduce helpful errors.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder UseFirstCharacterMatch()
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var flags = prevFactory(e);
				flags |= ParserInitFlags.FirstCharacterMatch;
				return flags;
			};
			return this;
		}

		/// <summary>
		/// Uses the inlining mode for all rules.
		/// </summary>
		/// <remarks>
		/// May lead to unexpected behavior if not used properly. Use with caution.
		/// </remarks>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder UseInlining()
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var flags = prevFactory(e);
				flags |= ParserInitFlags.InlineRules;
				return flags;
			};
			return this;
		}

		/// <summary>
		/// Sets the stack trace writing mode to enabled.
		/// </summary>
		/// <returns>This instance for method chaining.</returns>
		public ParserSettingsBuilder WriteStackTrace()
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var flags = prevFactory(e);
				flags |= ParserInitFlags.StackTraceWriting;
				return flags;
			};
			return this;
		}
	}
}