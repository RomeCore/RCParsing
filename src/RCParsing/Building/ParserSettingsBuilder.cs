using System;
using System.Collections;
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
		private ParserMainSettings _mainSettings = default;
		private ParserSettings _settings = default;
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
		public (ParserMainSettings, ParserSettings, Func<ParserElement, ParserInitFlags>) Build(List<int> ruleChildren)
		{
			var result = _settings;
			result.skipRule = ruleChildren[0];
			return (_mainSettings, result, _initFlagsFactory);
		}



		/// <summary>
		/// Sets the skip rule that will be skipped before parsing rules.
		/// </summary>
		/// <param name="builderAction">The action to build the skip rule.</param>
		/// <param name="skippingStrategy">The skipping strategy to use.</param>
		/// <returns>Current instance for method chaining.</returns>
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
		/// Sets the 'Whitespaces' skip rule that will be skipped before parsing rules.
		/// </summary>
		/// <param name="skippingStrategy">The skipping strategy to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder SkipWhitespaces(ParserSkippingStrategy skippingStrategy = ParserSkippingStrategy.SkipBeforeParsing)
		{
			var builder = new RuleBuilder();
			builder.Whitespaces().ConfigureForSkip();
			_skipRule = builder.BuildingRule;
			_settings.skippingStrategy = skippingStrategy;
			return this;
		}

		/// <summary>
		/// Sets the skipping strategy.
		/// </summary>
		/// <param name="skippingStrategy">The skipping strategy to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder SkippingStrategy(ParserSkippingStrategy skippingStrategy)
		{
			_settings.skippingStrategy = skippingStrategy;
			return this;
		}

		/// <summary>
		/// Removes the skip rule.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder NoSkipping()
		{
			_settings.skippingStrategy = ParserSkippingStrategy.Default;
			return this;
		}

		/// <summary>
		/// Sets the error handling mode.
		/// </summary>
		/// <param name="mode">The error handling mode.</param>
		/// <returns>Current instance for method chaining.</returns>
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
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder RecordErrors()
		{
			return ErrorHandling(ParserErrorHandlingMode.Default);
		}

		/// <summary>
		/// Enables token error recording. If not set, only rule parsing errors will be recorded.
		/// </summary>
		/// <remarks>
		/// Very useful when working with complex token combinators.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder RecordTokenErrors()
		{
			return UseInitFlags(ParserInitFlags.RecordTokenErrors);
		}

		/// <summary>
		/// Sets the no error record handling mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to ignore any errors when trying to record them.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
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
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder ThrowErrors()
		{
			return ErrorHandling(ParserErrorHandlingMode.Throw);
		}

		/// <summary>
		/// Sets the error formatting flags that control how parsing errors are formatted when throwing exceptions.
		/// </summary>
		/// <remarks>
		/// Useful for debugging purposes.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder ErrorFormatting(ErrorFormattingFlags flags)
		{
			_mainSettings.errorFormattingFlags = flags;
			return this;
		}

		/// <summary>
		/// Sets the detailed error messages mode when throwing exceptions.
		/// </summary>
		/// <remarks>
		/// Useful for debugging purposes.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder DetailedErrors()
		{
			return ErrorFormatting(ErrorFormattingFlags.DisplayRules |
				ErrorFormattingFlags.DisplayMessages | ErrorFormattingFlags.MoreGroups);
		}

		/// <summary>
		/// Sets the creating AST type to the <see cref="ParsedRuleResult"/>.
		/// </summary>
		/// <remarks>
		/// This type of AST stores minimum amount of data, should not be used when AST nodes is reused.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseLightAST()
		{
			_mainSettings.astFactory = (ctx, res) => new ParsedRuleResult(null, ctx, res);
			return this;
		}
		
		/// <summary>
		/// Sets the creating AST type to the <see cref="ParsedRuleResultOptimized"/>.
		/// </summary>
		/// <remarks>
		/// This type of AST stores minimum amount of data, should not be used when AST nodes is reused.
		/// </remarks>
		/// <param name="optimization">The optimization flags to apply to creating parsed rules.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseLightAST(ParseTreeOptimization optimization)
		{
			_mainSettings.astFactory = (ctx, res) => new ParsedRuleResultOptimized(optimization,
				null, ctx, res);
			return this;
		}

		/// <summary>
		/// Sets the creating AST type to the <see cref="ParsedRuleResultLazy"/>.
		/// </summary>
		/// <remarks>
		/// Prevents from AST recalculations.
		/// This type of AST can do more allocations, impacting on memory usage and speed,
		/// but crucial for reusable AST nodes.
		/// </remarks>
		/// <param name="optimization">The optimization flags to apply to creating parsed rules.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseLazyAST(ParseTreeOptimization optimization = ParseTreeOptimization.None)
		{
			_mainSettings.astFactory = (ctx, res) => new ParsedRuleResultLazy(optimization,
				null, ctx, res);
			return this;
		}

		/// <summary>
		/// Sets the creating AST type to the specified type.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseASTFactory(Func<ParserContext, ParsedRule, ParsedRuleResultBase>? factory)
		{
			_mainSettings.astFactory = factory;
			return this;
		}

		/// <summary>
		/// Allows the parser to record skipped rules to the context.
		/// </summary>
		/// <remarks>
		/// Useful for debugging purposes and syntax highlighting.
		/// </remarks>
		/// <param name="record">Whether to record skipped rules. Default is true.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder RecordSkippedRules(bool record = true)
		{
			_mainSettings.recordSkippedRules = record;
			return this;
		}

		/// <summary>
		/// Prevents the parser from recording skipped rules to the context.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder DoNotRecordSkippedRules()
		{
			_mainSettings.recordSkippedRules = false;
			return this;
		}

		/// <summary>
		/// Sets the tab size, mostly used for debugging and better error display.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder SetTabSize(int tabSize = 4)
		{
			_mainSettings.tabSize = tabSize;
			return this;
		}

		/// <summary>
		/// Sets the optimized skip whitespace mode, where parser directly skips whitespaces before parsing rules. <br/>
		/// </summary>
		/// <remarks>
		/// This mode prevents any other skip rules, strategies, recording and barriers calculation. Use with caution.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder SkipWhitespacesOptimized()
		{
			_mainSettings.useOptimizedWhitespaceSkip = true;
			return this;
		}

		/// <summary>
		/// Sets the barrier tokens to be ignored while parsing.
		/// </summary>
		/// <param name="ignore">Whether to ignore barrier tokens or not.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder IgnoreBarriers(bool ignore)
		{
			_settings.ignoreBarriers = ignore;
			return this;
		}

		/// <summary>
		/// Ignores barrier tokens while parsing.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder IgnoreBarriers()
		{
			_settings.ignoreBarriers = true;
			return this;
		}

		/// <summary>
		/// Enables barrier tokens while parsing.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder RestoreBarriers()
		{
			_settings.ignoreBarriers = false;
			return this;
		}

		/// <summary>
		/// Uses the specified initialization flags for all elements.
		/// </summary>
		/// <param name="flags">The initialization flags to use.</param>
		/// <returns>Current instance for method chaining.</returns>
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
		/// Uses the specified initialization flags for specific elements.
		/// </summary>
		/// <param name="flags">The initialization flags to use.</param>
		/// <param name="predicate">The predicate to determine whether a parser element should be cached.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseInitFlagsOn(ParserInitFlags flags, Predicate<ParserElement> predicate)
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var _flags = prevFactory(e);
				if (predicate(e))
					_flags |= flags;
				return _flags;
			};
			return this;
		}

		/// <summary>
		/// Uses the specified initialization flags for specific elements and negates for others.
		/// </summary>
		/// <param name="flags">The initialization flags to use.</param>
		/// <param name="predicate">The predicate to determine whether a parser element should be cached.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseInitFlagsOnOnly(ParserInitFlags flags, Predicate<ParserElement> predicate)
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var _flags = prevFactory(e);
				if (predicate(e))
					_flags |= flags;
				else
					_flags &= ~flags;
				return _flags;
			};
			return this;
		}

		/// <summary>
		/// Sets the token and rule caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to use and write both rules and token patterns via caching.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseCaching()
		{
			return UseInitFlags(ParserInitFlags.EnableMemoization);
		}

		/// <summary>
		/// Sets the only rules caching mode.
		/// </summary>
		/// <remarks>
		/// This will cause the parser to use and write rules via caching.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
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
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder CacheOnlyTokens()
		{
			return UseCachingOn(e => e is TokenPattern || e is TokenParserRule);
		}

		/// <summary>
		/// Sets the caching mode for token patterns and rules based on the specified predicate.
		/// </summary>
		/// <param name="predicate">The predicate to determine whether a parser element should be cached.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseCachingOn(Predicate<ParserElement> predicate)
		{
			return UseInitFlagsOn(ParserInitFlags.EnableMemoization, predicate);
		}

		/// <summary>
		/// Sets the caching mode for token patterns and rules based on the specified predicate.
		/// </summary>
		/// <remarks>
		/// Overrides the previous caching mode with the new one for all elements.
		/// </remarks>
		/// <param name="predicate">The predicate to determine whether a parser element should be cached.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseCachingOnOnly(Predicate<ParserElement> predicate)
		{
			return UseInitFlagsOnOnly(ParserInitFlags.EnableMemoization, predicate);
		}

		/// <summary>
		/// Uses the first character match mode for all elements.
		/// </summary>
		/// <remarks>
		/// May improve performance by avoiding unnecessary backtracking in some cases. However, it can decreace amount of helpful errors.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
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
		/// <returns>Current instance for method chaining.</returns>
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
		/// <returns>Current instance for method chaining.</returns>
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

		/// <summary>
		/// Sets the maximum number of walk steps to display in error messages. Default is 30.
		/// </summary>
		/// <param name="maxWalkStepsDisplay">The maximum number of walk steps to display. Default is 30.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder SetMaxStepsToDisplay(int maxWalkStepsDisplay = 30)
		{
			_mainSettings.maxWalkStepsDisplay = maxWalkStepsDisplay;
			return this;
		}

		/// <summary>
		/// Records the walk trace during parsing.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder RecordWalkTrace()
		{
			var prevFactory = _initFlagsFactory;
			_initFlagsFactory = e =>
			{
				var flags = prevFactory(e);
				flags |= ParserInitFlags.WalkTraceRecording;
				return flags;
			};
			return this;
		}

		/// <summary>
		/// Sets the stack trace writing mode, walk trace recording mode and detailed errors mode to enabled.
		/// </summary>
		/// <remarks>
		/// Useful for debuggings grammars.
		/// </remarks>
		/// <param name="tabSize">The tab size used for formatting the error messages and visual column calculation.</param>
		/// <param name="maxWalkStepsDisplay">The maximum number of steps to display in the walk trace when formatting errors.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserSettingsBuilder UseDebug(int tabSize = 4, int maxWalkStepsDisplay = 40)
		{
			return this.SetTabSize(tabSize).WriteStackTrace().RecordWalkTrace()
				.SetMaxStepsToDisplay(maxWalkStepsDisplay).DetailedErrors();
		}
	}
}