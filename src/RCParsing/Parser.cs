using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using RCParsing.ParserRules;
using RCParsing.TokenPatterns;

namespace RCParsing
{
	// Ooofff... So much code... 1200 lines of code...

	/// <summary>
	/// Represents a parser for parsing text data into AST.
	/// </summary>
	public class Parser
	{
		private readonly Dictionary<string, int> _tokenPatternsAliases = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _rulesAliases = new Dictionary<string, int>();
		private readonly int _mainRuleId = -1;

		/// <summary>
		/// Gets the token patterns registered in this parser.
		/// </summary>
		public ImmutableArray<TokenPattern> TokenPatterns { get; }

		/// <summary>
		/// Gets the rules registered in this parser.
		/// </summary>
		public ImmutableArray<ParserRule> Rules { get; }

		/// <summary>
		/// Gets the barrier tokenizers used by when tokenizing input strings.
		/// </summary>
		public ImmutableArray<BarrierTokenizer> Tokenizers { get; }

		/// <summary>
		/// Gets the main settings used by this parser.
		/// </summary>
		public ParserMainSettings MainSettings { get; }

		/// <summary>
		/// Gets the global settings used by this parser for configuring rules.
		/// </summary>
		public ParserSettings GlobalSettings { get; }

		/// <summary>
		/// Gets the main rule ID that used by default, or -1 if not specified,
		/// </summary>
		public int MainRuleId => _mainRuleId;
		
		/// <summary>
		/// Gets the main rule that used by default, if any.
		/// </summary>
		public ParserRule? MainRule => _mainRuleId == -1 ? null : Rules[_mainRuleId];

		/// <summary>
		/// Initializes a new instance of the <see cref="Parser"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to use. </param>
		/// <param name="rules">The rules to use.</param>
		/// <param name="tokenizers">The barrier tokenizers to use.</param>
		/// <param name="mainSettings">The main settings to use for the parser itself.</param>
		/// <param name="globalSettings">The global settings to use.</param>
		/// <param name="mainRuleId">The ID of the main rule to use.</param>
		/// <param name="initFlags">The initialization flags to use. Default is <see cref="ParserInitFlags.None"/>.</param>
		public Parser(ImmutableArray<TokenPattern> tokenPatterns, ImmutableArray<ParserRule> rules,
			ImmutableArray<BarrierTokenizer> tokenizers, ParserMainSettings mainSettings, ParserSettings globalSettings,
			int mainRuleId = -1, ParserInitFlags initFlags = ParserInitFlags.None)

			: this(tokenPatterns, rules, tokenizers, mainSettings, globalSettings, mainRuleId, e => initFlags)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Parser"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to use. </param>
		/// <param name="rules">The rules to use.</param>
		/// <param name="tokenizers">The barrier tokenizers to use.</param>
		/// <param name="mainSettings">The main settings to use for the parser itself.</param>
		/// <param name="globalSettings">The global settings to use.</param>
		/// <param name="mainRuleId">The ID of the main rule to use.</param>
		/// <param name="initFlagsFactory">The initialization flags factory to use.</param>
		public Parser(ImmutableArray<TokenPattern> tokenPatterns, ImmutableArray<ParserRule> rules,
			ImmutableArray<BarrierTokenizer> tokenizers, ParserMainSettings mainSettings, ParserSettings globalSettings,
			int mainRuleId = -1, Func<ParserElement, ParserInitFlags>? initFlagsFactory = null)
		{
			if (mainSettings.tabSize <= 0)
				mainSettings.tabSize = 4;

			Rules = rules;
			TokenPatterns = tokenPatterns;
			Tokenizers = tokenizers;
			MainSettings = mainSettings;
			GlobalSettings = globalSettings;

			for (int i = 0; i < rules.Length; i++)
			{
				var rule = rules[i];
				if (rule.Parser != null)
					throw new InvalidOperationException("Parser already set for a rule.");
				rule.Id = i;
				rule.Parser = this;

				foreach (var alias in rule.Aliases)
				{
					if (_rulesAliases.ContainsKey(alias))
						throw new InvalidOperationException("Alias already used by another rule.");
					_rulesAliases.Add(alias, rule.Id);
				}
			}
			
			for (int i = 0; i < tokenPatterns.Length; i++)
			{
				var token = tokenPatterns[i];
				if (token.Parser != null)
					throw new InvalidOperationException("Parser already set for a token.");
				token.Id = i;
				token.Parser = this;

				foreach (var alias in token.Aliases)
				{
					if (_tokenPatternsAliases.ContainsKey(alias))
						throw new InvalidOperationException("Alias already used by another token pattern.");
					_tokenPatternsAliases.Add(alias, token.Id);
				}
			}

			_mainRuleId = mainRuleId;

			ParserInitFlags TokenFactory(TokenPattern token)
			{
				return initFlagsFactory?.Invoke(token) ?? ParserInitFlags.None;
			}
			ParserInitFlags RuleFactory(ParserRule rule)
			{
				return initFlagsFactory?.Invoke(rule) ?? ParserInitFlags.None;
			}

			var initFlagsTokenMap = tokenPatterns.ToDictionary(t => t.Id, TokenFactory);
			var initFlagsRuleMap = rules.ToDictionary(t => t.Id, RuleFactory);

			foreach (var pattern in tokenPatterns)
				pattern.PreInitializeInternal(initFlagsTokenMap[pattern.Id]);
			foreach (var rule in rules)
				rule.PreInitializeInternal(initFlagsRuleMap[rule.Id]);

			foreach (var pattern in tokenPatterns)
				pattern.InitializeInternal(initFlagsTokenMap[pattern.Id]);
			foreach (var rule in rules)
				rule.InitializeInternal(initFlagsRuleMap[rule.Id]);

			foreach (var pattern in tokenPatterns)
				pattern.PostInitializeInternal(initFlagsTokenMap[pattern.Id]);
			foreach (var rule in rules)
				rule.PostInitializeInternal(initFlagsRuleMap[rule.Id]);
		}

		/// <summary>
		/// Gets a token pattern by its alias.
		/// </summary>
		/// <param name="alias">The alias of the token pattern.</param>
		/// <returns>The token pattern with the specified alias.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no token pattern is found with the specified alias.</exception>
		public TokenPattern GetTokenPattern(string alias)
		{
			if (_tokenPatternsAliases.TryGetValue(alias, out var id))
				return TokenPatterns[id];
			throw new InvalidOperationException($"Token pattern not found with alias '{alias}'.");
		}

		/// <summary>
		/// Gets a token pattern by its ID.
		/// </summary>
		/// <param name="id">The ID of the token pattern.</param>
		/// <returns>The token pattern with the specified alias.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the provided ID is out of range.</exception>
		public TokenPattern GetTokenPattern(int id)
		{
			if (id >= 0 && id < TokenPatterns.Length)
				return TokenPatterns[id];
			throw new ArgumentOutOfRangeException(nameof(id), "Invalid token pattern ID.");
		}

		/// <summary>
		/// Gets a rule by its alias.
		/// </summary>
		/// <param name="alias">The alias of the rule.</param>
		/// <returns>The rule with the specified alias.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no rule is found with the specified alias.</exception>
		public ParserRule GetRule(string alias)
		{
			if (_rulesAliases.TryGetValue(alias, out var id))
				return Rules[id];
			throw new InvalidOperationException($"Rule not found with alias '{alias}'.");
		}

		/// <summary>
		/// Gets a rule by its ID.
		/// </summary>
		/// <param name="id">The ID of the rule.</param>
		/// <returns>The rule with the specified ID.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the provided ID is out of range.</exception>
		public ParserRule GetRule(int id)
		{
			if (id >= 0 && id < Rules.Length)
				return Rules[id];
			throw new ArgumentOutOfRangeException(nameof(id), "Invalid rule ID.");
		}

		/// <summary>
		/// Creates a <see cref="ParsingException"/> from the current parser context.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ParsingException ExceptionFromContext(ParserContext context)
		{
			return new ParsingException(context);
		}

		/// <summary>
		/// Matches the given input using the specified token identifier and parser context.
		/// </summary>
		/// <returns>A parsed token object containing the result of the match.</returns>
		internal ParsedElement MatchToken(int tokenPatternId, string input, int position,
			int barrierPosition, object? parameter, bool calculateIntermediateValue)
		{
			var tokenPattern = TokenPatterns[tokenPatternId];
			return tokenPattern.Match(input, position, barrierPosition, parameter, calculateIntermediateValue);
		}

		/// <summary>
		/// Parses the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <remarks>
		/// Throws a <see cref="ParsingException"/> if parsing fails.
		/// </remarks>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="settings">The settings to use for parsing.</param>
		/// <returns>A parsed rule object containing the result of the parse.</returns>
		internal ParsedRule ParseRule(int ruleId, ParserContext context, ParserSettings settings)
		{
			var parsedRule = TryParseRule(ruleId, context, settings);
			if (parsedRule.success)
				return parsedRule;

			throw ExceptionFromContext(context);
		}

		static bool TrySkip(ref ParserContext context, ParserRule skipRule,
				ref ParserSettings skipSettings, ref ParserSettings childSkipSettings, bool record)
		{
			if (context.shared.positionsToAvoidSkipping[context.position])
				return false;
			if (context.position >= context.maxPosition)
				return false;

			var parsedSkipRule = skipRule.Parse(context, skipSettings, childSkipSettings);
			int newPosition = parsedSkipRule.startIndex + parsedSkipRule.length;

			if (parsedSkipRule.success && newPosition != context.position)
			{
				context.position = newPosition;
				if (record)
					context.skippedRules.Add(parsedSkipRule);
				return true;
			}
			return false;
		}

		static ParsedRule Parse(ParserRule rule, ref ParserContext context,
				ref ParserSettings settings, ref ParserSettings childSettings)
		{
			var parsedRule = rule.Parse(context, settings, childSettings);
			if (parsedRule.success)
				context.successPositions[parsedRule.startIndex] = true;
			return parsedRule;
		}

		static bool TryParse(ParserRule rule, ref ParserContext context,
				ref ParserSettings settings, ref ParserSettings childSettings, out ParsedRule parsedRule)
		{
			parsedRule = rule.Parse(context, settings, childSettings);
			if (parsedRule.success)
				context.successPositions[parsedRule.startIndex] = true;
			return parsedRule.success;
		}

		/// <summary>
		/// Tries to parse the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="settings">The settings to use for parsing.</param>
		/// <returns>A parsed rule object containing the result of the parse.</returns>
		internal ParsedRule TryParseRule(int ruleId, ParserContext context, ParserSettings settings)
		{
			var rule = Rules[ruleId];
			rule.AdvanceContext(ref context, ref settings, out var childSettings);

			if (MainSettings.useOptimizedWhitespaceSkip)
			{
				while (context.position < context.maxPosition && char.IsWhiteSpace(context.input[context.position]))
					context.position++;
				return Parse(rule, ref context, ref settings, ref childSettings);
			}

			if (settings.skipRule == -1 ||
				settings.skippingStrategy == ParserSkippingStrategy.Default ||
				context.positionsToAvoidSkipping[context.position])
				return Parse(rule, ref context, ref settings, ref childSettings);

			// Skip rule preparation

			var skipRule = Rules[settings.skipRule];
			var skipSettings = settings;
			var skipContext = context;
			skipRule.AdvanceContext(ref skipContext, ref skipSettings, out var childSkipSettings);
			skipSettings.skipRule = -1;
			childSkipSettings.skipRule = -1;
			bool record = MainSettings.recordSkippedRules;

			switch (settings.skippingStrategy)
			{
				case ParserSkippingStrategy.SkipBeforeParsing:

					TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record);
					context.shared.positionsToAvoidSkipping[context.position] = true;
					return Parse(rule, ref context, ref settings, ref childSettings);

				case ParserSkippingStrategy.SkipBeforeParsingLazy:

					// Alternate: Skip -> TryParse -> Skip -> TryParse ... until TryParse succeeds
					while (true)
					{
						if (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
						{
							if (TryParse(rule, ref context, ref settings, ref childSettings, out var res))
							{
								return res;
							}
							continue;
						}
						return Parse(rule, ref context, ref settings, ref childSettings);
					}

				case ParserSkippingStrategy.SkipBeforeParsingGreedy:

					while (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
					{
					}
					context.shared.positionsToAvoidSkipping[context.position] = true;
					return Parse(rule, ref context, ref settings, ref childSettings);

				case ParserSkippingStrategy.TryParseThenSkip:

					if (TryParse(rule, ref context, ref settings, ref childSettings, out var result))
						return result;

					if (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
					{
						context.shared.positionsToAvoidSkipping[context.position] = true;
						return Parse(rule, ref context, ref settings, ref childSettings);
					}

					return ParsedRule.Fail;

				case ParserSkippingStrategy.TryParseThenSkipLazy:

					// First try parse (handled above in TryParseThenSkip pattern),
					// then alternate Skip -> TryParse -> Skip -> TryParse ... until success or nothing consumes
					if (TryParse(rule, ref context, ref settings, ref childSettings, out var firstResult))
					{
						return firstResult;
					}

					while (true)
					{
						if (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
						{
							if (TryParse(rule, ref context, ref settings, ref childSettings, out var res))
							{
								return res;
							}
							continue;
						}
						context.shared.positionsToAvoidSkipping[context.position] = true;
						return ParsedRule.Fail;
					}

				case ParserSkippingStrategy.TryParseThenSkipGreedy:

					// Try parse; if failed, greedily skip then parse once
					if (TryParse(rule, ref context, ref settings, ref childSettings, out var firstRes))
						return firstRes;

					while (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
					{
					}
					context.shared.positionsToAvoidSkipping[context.position] = true;

					return Parse(rule, ref context, ref settings, ref childSettings);

				default:
					throw new ParsingException(context, "Invalid skipping strategy.");
			}
		}

		/// <summary>
		/// Tries to find all matches in the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The unique identifier for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="settings">The settings to use for parsing.</param>
		/// <param name="overlap">Whether to allow overlapping matches.</param>
		/// <returns>The all matches found in the input.</returns>
		internal IEnumerable<ParsedRule> FindAllMatches(int ruleId, ParserContext context, ParserSettings settings, bool overlap = false)
		{
			var rule = Rules[ruleId];
			rule.AdvanceContext(ref context, ref settings, out var childSettings);

			if (MainSettings.useOptimizedWhitespaceSkip)
			{
				while (context.position < context.maxPosition)
				{
					if (char.IsWhiteSpace(context.input[context.position]))
					{
						context.position++;
						continue;
					}

					var parsed = Parse(rule, ref context, ref settings, ref childSettings);

					if (parsed.success)
					{
						yield return parsed;
						if (overlap)
							context.position++;
						else
							context.position = parsed.startIndex + parsed.length;
					}
					else
					{
						context.position++;
					}
				}
				yield break;
			}

			if (settings.skipRule == -1 ||
				settings.skippingStrategy == ParserSkippingStrategy.Default ||
				context.positionsToAvoidSkipping[context.position])
			{
				while (context.position < context.maxPosition)
				{
					var parsed = Parse(rule, ref context, ref settings, ref childSettings);

					if (parsed.success)
					{
						yield return parsed;
						if (overlap)
							context.position++;
						else
							context.position = parsed.startIndex + parsed.length;
					}
					else
					{
						context.position++;
					}
				}
				yield break;
			}

			var skipRule = Rules[settings.skipRule];
			var skipSettings = settings;
			var skipContext = context;
			skipRule.AdvanceContext(ref skipContext, ref skipSettings, out var childSkipSettings);
			skipSettings.skipRule = -1;
			childSkipSettings.skipRule = -1;
			bool record = MainSettings.recordSkippedRules;

			switch (settings.skippingStrategy)
			{
				case ParserSkippingStrategy.SkipBeforeParsing:

					while (context.position < context.maxPosition)
					{
						// Skip -> Parse
						TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record);
						context.shared.positionsToAvoidSkipping[context.position] = true;
						var parsed = Parse(rule, ref context, ref settings, ref childSettings);

						if (parsed.success)
						{
							yield return parsed;
							if (overlap)
								context.position++;
							else
								context.position = parsed.startIndex + parsed.length;
						}
						else
						{
							context.position++;
						}
					}
					yield break;

				case ParserSkippingStrategy.SkipBeforeParsingLazy:

					while (context.position < context.maxPosition)
					{
						// Alternate: Skip -> TryParse -> Skip -> TryParse ... until TryParse succeeds
						if (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
						{
							if (TryParse(rule, ref context, ref settings, ref childSettings, out var parsed))
							{
								yield return parsed;
								if (overlap)
									context.position++;
								else
									context.position = parsed.startIndex + parsed.length;
								continue;
							}
						}

						if (TryParse(rule, ref context, ref settings, ref childSettings, out var parsed1))
						{
							context.shared.positionsToAvoidSkipping[context.position] = true;

							yield return parsed1;
							if (overlap)
								context.position++;
							else
								context.position = parsed1.startIndex + parsed1.length;
							continue;
						}

						context.shared.positionsToAvoidSkipping[context.position] = true;
						context.position++;
					}
					yield break;

				case ParserSkippingStrategy.SkipBeforeParsingGreedy:

					while (context.position < context.maxPosition)
					{
						// Skip ->  Skip -> Skip ... until Skip fails
						while (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
						{
						}
						context.shared.positionsToAvoidSkipping[context.position] = true;

						if (TryParse(rule, ref context, ref settings, ref childSettings, out var parsed))
						{
							yield return parsed;
							if (overlap)
								context.position++;
							else
								context.position = parsed.startIndex + parsed.length;
							continue;
						}
						context.position++;
					}
					yield break;

				case ParserSkippingStrategy.TryParseThenSkip:

					// Parse -> Skip -> Parse
					while (context.position < context.maxPosition)
					{
						if (TryParse(rule, ref context, ref settings, ref childSettings, out var parsed))
						{
							yield return parsed;
							if (overlap)
								context.position++;
							else
								context.position = parsed.startIndex + parsed.length;
							continue;
						}

						if (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
						{
							context.shared.positionsToAvoidSkipping[context.position] = true;

							if (TryParse(rule, ref context, ref settings, ref childSettings, out parsed))
							{
								yield return parsed;
								if (overlap)
									context.position++;
								else
									context.position = parsed.startIndex + parsed.length;
								continue;
							}
						}

						context.shared.positionsToAvoidSkipping[context.position] = true;
						context.position++;
					}
					yield break;

				case ParserSkippingStrategy.TryParseThenSkipLazy:

					// First try parse (handled above in TryParseThenSkip pattern),
					// then alternate Skip -> TryParse -> Skip -> TryParse ... until success or nothing consumes
					while (context.position < context.maxPosition)
					{
						if (TryParse(rule, ref context, ref settings, ref childSettings, out var parsed))
						{
							yield return parsed;
							if (overlap)
								context.position++;
							else
								context.position = parsed.startIndex + parsed.length;
							continue;
						}

						while (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
						{
							if (TryParse(rule, ref context, ref settings, ref childSettings, out parsed))
							{
								yield return parsed;
								if (overlap)
									context.position++;
								else
									context.position = parsed.startIndex + parsed.length;
								continue;
							}
						}

						context.shared.positionsToAvoidSkipping[context.position] = true;
						context.position++;
					}
					yield break;

				case ParserSkippingStrategy.TryParseThenSkipGreedy:

					// Try parse; if failed, greedily skip then parse once
					while (context.position < context.maxPosition)
					{
						if (TryParse(rule, ref context, ref settings, ref childSettings, out var parsed))
						{
							yield return parsed;
							if (overlap)
								context.position++;
							else
								context.position = parsed.startIndex + parsed.length;
							continue;
						}

						while (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
						{
						}
						context.shared.positionsToAvoidSkipping[context.position] = true;

						if (TryParse(rule, ref context, ref settings, ref childSettings, out parsed))
						{
							yield return parsed;
							if (overlap)
								context.position++;
							else
								context.position = parsed.startIndex + parsed.length;
							continue;
						}
						context.position++;
					}
					yield break;

				default:
					throw new ParsingException(context, "Invalid skipping strategy.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EmitBarriers(ref ParserContext context)
		{
			if (Tokenizers.Length == 0)
				return;

			var ctx = context;
			var tokens = Tokenizers.SelectMany(t => t.Tokenize(ctx));
			context.barrierTokens.FillWith(tokens, context.input, this);
		}

		private ParsedRuleResultBase CreateResult(ref ParserContext context, ref ParsedRule parsedRule)
		{
			switch (MainSettings.astType)
			{
				case ParserASTType.Lazy:
					return new ParsedRuleResultLazy(ParseTreeOptimization.None, null, context, parsedRule);
					default:
				case ParserASTType.Lightweight:
					return new ParsedRuleResult(null, context, parsedRule);
			}
		}



		/// <summary>
		/// Creates a parser context for the given input string.
		/// </summary>
		/// <param name="input">The input string to parse.</param>
		/// <param name="parameter">The optional parameter to pass to the token patterns. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A new parser context initialized with the provided input string.</returns>
		public ParserContext CreateContext(string input, object? parameter = null)
		{
			return new ParserContext(this, input, parameter);
		}

		/// <summary>
		/// Creates a parser context for the given input string.
		/// </summary>
		/// <param name="input">The input string to parse.</param>
		/// <param name="startIndex">The starting index in the input string to parse.</param>
		/// <param name="parameter">The optional parameter to pass to the token patterns. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A new parser context initialized with the provided input string.</returns>
		public ParserContext CreateContext(string input, int startIndex, object? parameter = null)
		{
			return new ParserContext(this, input, parameter)
			{
				position = startIndex
			};
		}

		/// <summary>
		/// Creates a parser context for the given input string.
		/// </summary>
		/// <param name="input">The input string to parse.</param>
		/// <param name="startIndex">The starting index in the input string to parse.</param>
		/// <param name="length">The number of characters to parse from the input string.</param>
		/// <param name="parameter">The optional parameter to pass to the token patterns. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A new parser context initialized with the provided input string.</returns>
		public ParserContext CreateContext(string input, int startIndex, int length, object? parameter = null)
		{
			return new ParserContext(this, input, parameter)
			{
				position = startIndex,
				maxPosition = startIndex + length
			};
		}



		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Does not throw an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="context">The parser context to use for matching.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, ParserContext context, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true);
			return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="context">The parser context to use for matching.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T MatchToken<T>(string tokenPatternAlias, ParserContext context, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, context.parserParameter, true);
			return (T)parsedToken.intermediateValue;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Does not throw an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, string input, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true);
			return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T MatchToken<T>(string tokenPatternAlias, string input, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, 0,
				input.Length, parameter, true);
			return (T)parsedToken.intermediateValue;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Does not throw an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, string input, int startIndex, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, startIndex, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true);
			return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T MatchToken<T>(string tokenPatternAlias, string input, int startIndex, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				input.Length, parameter, true);
			return (T)parsedToken.intermediateValue;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Does not throw an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="length">Number of characters to parse from the input text.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, string input, int startIndex, int length, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, startIndex, length, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true);
			return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="length">Number of characters to parse from the input text.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T MatchToken<T>(string tokenPatternAlias, string input, int startIndex, int length, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				startIndex + length, parameter, true);
			return (T)parsedToken.intermediateValue;
		}



		/// <summary>
		/// Parses the given input using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>A parsed rule containing the result of the parse.</returns>
		public ParsedRuleResultBase ParseRule(string ruleAlias, ParserContext context)
		{
			if (context.parser != this)
				throw new InvalidOperationException("Parser context is not associated with this parser.");
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			EmitBarriers(ref context);
			var parsedRule = ParseRule(ruleId, context, GlobalSettings);
			return CreateResult(ref context, ref parsedRule);
		}

		/// <summary>
		/// Parses the given input using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns>A parsed rule containing the result of the parse.</returns>
		public ParsedRuleResultBase ParseRule(string ruleAlias, string input, object? parameter = null)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = ParseRule(ruleId, context, GlobalSettings);
			return CreateResult(ref context, ref parsedRule);
		}

		/// <summary>
		/// Parses the given input using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>The result of the parse converted to the specified type.</returns>
		public T ParseRule<T>(string ruleAlias, ParserContext context)
		{
			return ParseRule(ruleAlias, context).GetValue<T>();
		}

		/// <summary>
		/// Parses the given input using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns>The result of the parse converted to the specified type.</returns>
		public T ParseRule<T>(string ruleAlias, string input, object? parameter = null)
		{
			return ParseRule(ruleAlias, input, parameter).GetValue<T>();
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParseRule(string ruleAlias, ParserContext context, out ParsedRuleResultBase result)
		{
			if (context.parser != this)
				throw new InvalidOperationException("Parser context is not associated with this parser.");
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			EmitBarriers(ref context);
			var parsedRule = TryParseRule(ruleId, context, GlobalSettings);
			result = CreateResult(ref context, ref parsedRule);
			return parsedRule.success;
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParseRule(string ruleAlias, string input, out ParsedRuleResultBase result)
		{
			return TryParseRule(ruleAlias, input, null, out result);
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="result">The result of the parse converted to the specified type.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParseRule<T>(string ruleAlias, ParserContext context, out T result)
		{
			if (TryParseRule(ruleAlias, context, out var ast))
			{
				result = ast.GetValue<T>();
				return true;
			}
			result = default!;
			return false;
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The result of the parse converted to the specified type.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParseRule<T>(string ruleAlias, string input, out T result)
		{
			return TryParseRule(ruleAlias, input, null, out result);
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, false otherwise.</returns>
		public bool TryParseRule(string ruleAlias, string input, object? parameter, out ParsedRuleResultBase result)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(ruleId, context, GlobalSettings);
			result = CreateResult(ref context, ref parsedRule);
			return parsedRule.success;
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The result of the parse converted to the specified type.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParseRule<T>(string ruleAlias, string input, object? parameter, out T result)
		{
			if (TryParseRule(ruleAlias, input, parameter, out var ast))
			{
				result = ast.GetValue<T>();
				return true;
			}
			result = default!;
			return false;
		}



		/// <summary>
		/// Parses the given input using the main rule and context.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>A parsed rule containing the result of the parse.</returns>
		public ParsedRuleResultBase Parse(ParserContext context)
		{
			if (context.parser != this)
				throw new InvalidOperationException("Parser context is not associated with this parser.");
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			EmitBarriers(ref context);
			var parsedRule = ParseRule(_mainRuleId, context, GlobalSettings);
			return CreateResult(ref context, ref parsedRule);
		}

		/// <summary>
		/// Parses the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns>A parsed rule containing the result of the parse.</returns>
		public ParsedRuleResultBase Parse(string input, object? parameter = null)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = ParseRule(_mainRuleId, context, GlobalSettings);
			return CreateResult(ref context, ref parsedRule);
		}

		/// <summary>
		/// Parses the given input using the main rule and context.
		/// </summary>
		/// <typeparam name="T">The type of the parsed result..</typeparam>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>The result of the parse converted to the specified type.</returns>
		public T Parse<T>(ParserContext context)
		{
			return Parse(context).GetValue<T>();
		}

		/// <summary>
		/// Parses the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <typeparam name="T">The type of the parsed result..</typeparam>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns>The result of the parse converted to the specified type.</returns>
		public T Parse<T>(string input, object? parameter = null)
		{
			return Parse(input, parameter).GetValue<T>();
		}

		/// <summary>
		/// Tries to parse the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse(ParserContext context, out ParsedRuleResultBase result)
		{
			if (context.parser != this)
				throw new InvalidOperationException("Parser context is not associated with this parser.");
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			EmitBarriers(ref context);
			var parsedRule = TryParseRule(_mainRuleId, context, GlobalSettings);
			result = CreateResult(ref context, ref parsedRule);
			return parsedRule.success;
		}

		/// <summary>
		/// Tries to parse the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse(string input, out ParsedRuleResultBase result)
		{
			return TryParse(input, null, out result);
		}

		/// <summary>
		/// Tries to parse the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse(string input, object? parameter, out ParsedRuleResultBase result)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(_mainRuleId, context, GlobalSettings);
			result = CreateResult(ref context, ref parsedRule);
			return parsedRule.success;
		}

		/// <summary>
		/// Tries to parse the given input using the main rule and context.
		/// </summary>
		/// <typeparam name="T">The type of the parsed result..</typeparam>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="result">The result of the parse converted to the specified type.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse<T>(ParserContext context, out T result)
		{
			if (TryParse(context, out var res))
			{
				result = res.GetValue<T>();
				return true;
			}
			result = default!;
			return false;
		}

		/// <summary>
		/// Tries to parse the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <typeparam name="T">The type of the parsed result..</typeparam>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The result of the parse converted to the specified type.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse<T>(string input, out T result)
		{
			return TryParse(input, null, out result);
		}

		/// <summary>
		/// Tries to parse the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <typeparam name="T">The type of the parsed result..</typeparam>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The result of the parse converted to the specified type.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse<T>(string input, object? parameter, out T result)
		{
			if (TryParse(input, parameter, out var res))
			{
				result = res.GetValue<T>();
				return true;
			}
			result = default!;
			return false;
		}



		/// <summary>
		/// Attempts to find all occurrences of the main rule in the input text, working similarly to regex matching.
		/// Parsing starts at each position until the end of input is reached.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="overlap">Whether to allow overlapping matches. When <see langword="true"/>, parser will advance by 1 anyway, instead of to end position of match.</param>
		/// <returns>An enumerable collection of all successfully parsed rule results.</returns>
		public IEnumerable<ParsedRuleResultBase> FindAllMatches(ParserContext context, bool overlap = false)
		{
			if (context.parser != this)
				throw new InvalidOperationException("Parser context is not associated with this parser.");
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			EmitBarriers(ref context);
			var parsedRules = FindAllMatches(_mainRuleId, context, GlobalSettings, overlap);
			return parsedRules.Select(r => CreateResult(ref context, ref r));
		}

		/// <summary>
		/// Attempts to find all occurrences of the main rule in the input text, working similarly to regex matching.
		/// Parsing starts at each position until the end of input is reached.
		/// </summary>
		/// <param name="input">The input text to scan.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <param name="overlap">Whether to allow overlapping matches. When <see langword="true"/>, parser will advance by 1 anyway, instead of to end position of match.</param>
		/// <returns>An enumerable collection of all successfully parsed rule results.</returns>
		public IEnumerable<ParsedRuleResultBase> FindAllMatches(string input, object? parameter = null, bool overlap = false)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRules = FindAllMatches(_mainRuleId, context, GlobalSettings, overlap);
			return parsedRules.Select(r => CreateResult(ref context, ref r));
		}

		/// <summary>
		/// Attempts to find all occurrences of the specified rule in the input text, working similarly to regex matching.
		/// Parsing starts at each position until the end of input is reached.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="overlap">Whether to allow overlapping matches. When <see langword="true"/>, parser will advance by 1 anyway, instead of to end position of match.</param>
		/// <returns>An enumerable collection of all successfully parsed rule results.</returns>
		public IEnumerable<ParsedRuleResultBase> FindAllMatches(string ruleAlias, ParserContext context, bool overlap = false)
		{
			if (context.parser != this)
				throw new InvalidOperationException("Parser context is not associated with this parser.");
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			EmitBarriers(ref context);
			var parsedRules = FindAllMatches(ruleId, context, GlobalSettings, overlap);
			return parsedRules.Select(r => CreateResult(ref context, ref r));
		}

		/// <summary>
		/// Attempts to find all occurrences of the specified rule in the input text, working similarly to regex matching.
		/// Parsing starts at each position until the end of input is reached.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to scan.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <param name="overlap">Whether to allow overlapping matches. When <see langword="true"/>, parser will advance by 1 anyway, instead of to end position of match.</param>
		/// <returns>An enumerable collection of all successfully parsed rule results.</returns>
		public IEnumerable<ParsedRuleResultBase> FindAllMatches(string ruleAlias, string input, object? parameter = null, bool overlap = false)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRules = FindAllMatches(ruleId, context, GlobalSettings, overlap);
			return parsedRules.Select(r => CreateResult(ref context, ref r));
		}

		/// <summary>
		/// Attempts to find all occurrences of the main rule in the input text, working similarly to regex matching.
		/// Returns the parsed results converted to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="overlap">Whether to allow overlapping matches. When <see langword="true"/>, parser will advance by 1 anyway, instead of to end position of match.</param>
		/// <returns>An enumerable collection of all successfully parsed and converted results.</returns>
		public IEnumerable<T> FindAllMatches<T>(ParserContext context, bool overlap = false)
		{
			foreach (var result in FindAllMatches(context, overlap))
			{
				yield return result.GetValue<T>();
			}
		}

		/// <summary>
		/// Attempts to find all occurrences of the main rule in the input text, working similarly to regex matching.
		/// Returns the parsed results converted to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="input">The input text to scan.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <param name="overlap">Whether to allow overlapping matches. When <see langword="true"/>, parser will advance by 1 anyway, instead of to end position of match.</param>
		/// <returns>An enumerable collection of all successfully parsed and converted results.</returns>
		public IEnumerable<T> FindAllMatches<T>(string input, object? parameter = null, bool overlap = false)
		{
			foreach (var result in FindAllMatches(input, parameter, overlap))
			{
				yield return result.GetValue<T>();
			}
		}

		/// <summary>
		/// Attempts to find all occurrences of the specified rule in the input text, working similarly to regex matching.
		/// Returns the parsed results converted to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="overlap">Whether to allow overlapping matches. When <see langword="true"/>, parser will advance by 1 anyway, instead of to end position of match.</param>
		/// <returns>An enumerable collection of all successfully parsed and converted results.</returns>
		public IEnumerable<T> FindAllMatches<T>(string ruleAlias, ParserContext context, bool overlap = false)
		{
			foreach (var result in FindAllMatches(ruleAlias, context, overlap))
			{
				yield return result.GetValue<T>();
			}
		}

		/// <summary>
		/// Attempts to find all occurrences of the specified rule in the input text, working similarly to regex matching.
		/// Returns the parsed results converted to the specified type.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to scan.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <param name="overlap">Whether to allow overlapping matches. When <see langword="true"/>, parser will advance by 1 anyway, instead of to end position of match.</param>
		/// <returns>An enumerable collection of all successfully parsed and converted results.</returns>
		public IEnumerable<T> FindAllMatches<T>(string ruleAlias, string input, object? parameter = null, bool overlap = false)
		{
			foreach (var result in FindAllMatches(ruleAlias, input, parameter, overlap))
			{
				yield return result.GetValue<T>();
			}
		}
	}
}