using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RCParsing
{
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
		/// Gets the global settings used by this parser for configuring rules parsing processes.
		/// </summary>
		public ParserSettings GlobalSettings { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Parser"/> class.
		/// </summary>
		/// <param name="tokenPatterns">The token patterns to use. </param>
		/// <param name="rules">The rules to use.</param>
		/// <param name="tokenizers">The barrier tokenizers to use.</param>
		/// <param name="mainSettings">The main settings to use for the parser itself.</param>
		/// <param name="globalSettings">The global settings to use.</param>
		/// <param name="mainRuleAlias">The optional alias of the main rule to use.</param>
		/// <param name="initFlags">The initialization flags to use. Default is <see cref="ParserInitFlags.None"/>.</param>
		public Parser(ImmutableArray<TokenPattern> tokenPatterns, ImmutableArray<ParserRule> rules,
			ImmutableArray<BarrierTokenizer> tokenizers, ParserMainSettings mainSettings, ParserSettings globalSettings,
			string? mainRuleAlias = null, ParserInitFlags initFlags = ParserInitFlags.None)

			: this(tokenPatterns, rules, tokenizers, mainSettings, globalSettings, mainRuleAlias, e => initFlags)

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
		/// <param name="mainRuleAlias">The optional alias of the main rule to use.</param>
		/// <param name="initFlagsFactory">The initialization flags factory to use.</param>
		public Parser(ImmutableArray<TokenPattern> tokenPatterns, ImmutableArray<ParserRule> rules,
			ImmutableArray<BarrierTokenizer> tokenizers, ParserMainSettings mainSettings, ParserSettings globalSettings,
			string? mainRuleAlias = null, Func<ParserElement, ParserInitFlags>? initFlagsFactory = null)
		{
			Rules = rules;
			TokenPatterns = tokenPatterns;
			Tokenizers = tokenizers;
			MainSettings = mainSettings;
			GlobalSettings = globalSettings;

			foreach (var rule in rules)
			{
				if (rule.Parser != null)
					throw new InvalidOperationException("Parser already set for a rule.");
				rule.Parser = this;

				foreach (var alias in rule.Aliases)
				{
					if (_rulesAliases.ContainsKey(alias))
						throw new InvalidOperationException("Alias already used by another rule.");
					_rulesAliases.Add(alias, rule.Id);
				}
			}

			if (mainRuleAlias != null)
			{
				if (!_rulesAliases.TryGetValue(mainRuleAlias, out _mainRuleId))
					throw new InvalidOperationException("Main rule alias not found.");
			}
			else
			{
				_mainRuleId = -1;
			}

			foreach (var pattern in tokenPatterns)
			{
				if (pattern.Parser != null)
					throw new InvalidOperationException("Parser already set for a token pattern.");
				pattern.Parser = this;

				foreach (var alias in pattern.Aliases)
				{
					if (_tokenPatternsAliases.ContainsKey(alias))
						throw new InvalidOperationException("Alias already used by another token pattern.");
					_tokenPatternsAliases.Add(alias, pattern.Id);
				}
			}

			var initFlagsDict = tokenPatterns.Cast<ParserElement>().Concat(rules).ToDictionary(e => e,
				initFlagsFactory ?? (e => ParserInitFlags.None));

			foreach (var pattern in tokenPatterns)
				pattern.PreInitializeInternal(initFlagsDict[pattern]);
			foreach (var rule in rules)
				rule.PreInitializeInternal(initFlagsDict[rule]);

			foreach (var pattern in tokenPatterns)
				pattern.InitializeInternal(initFlagsDict[pattern]);
			foreach (var rule in rules)
				rule.InitializeInternal(initFlagsDict[rule]);

			foreach (var pattern in tokenPatterns)
				pattern.PostInitializeInternal(initFlagsDict[pattern]);
			foreach (var rule in rules)
				rule.PostInitializeInternal(initFlagsDict[rule]);
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
		/// Creates a <see cref="ParsingException"/> from the current parser context.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private ParsingException ExceptionFromContext(ParserContext context)
		{
			var errors = context.errors.ToArray();

			if (errors.Length == 0)
				return new ParsingException(context, "Unknown error.");

			return new ParsingException(context, errors);
		}

		/// <summary>
		/// Matches the given input using the specified token identifier and parser context.
		/// </summary>
		/// <returns>A parsed token object containing the result of the match.</returns>
		internal ParsedElement MatchToken(int tokenPatternId, string input, int position, int barrierPosition, object? parameter)
		{
			var tokenPattern = TokenPatterns[tokenPatternId];
			return tokenPattern.Match(input, position, barrierPosition, parameter);
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

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			ParsedRule Parse()
			{
				var parsedRule = rule.Parse(context, settings, childSettings);
				if (parsedRule.success && parsedRule.startIndex < context.str.Length)
					context.successPositions[parsedRule.startIndex] = true;
				return parsedRule;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			bool TryParse(out ParsedRule result)
			{
				var parsedRule = rule.Parse(context, settings, childSettings);
				if (parsedRule.success && parsedRule.startIndex < context.str.Length)
					context.successPositions[parsedRule.startIndex] = true;
				result = parsedRule;
				return parsedRule.success;
			}

			if (settings.skipRule == -1 || settings.skippingStrategy == ParserSkippingStrategy.Default)
				return Parse();

			// Skip rule preparation

			var skipRule = Rules[settings.skipRule];
			var skipContext = context;
			var skipSettings = settings;
			skipRule.AdvanceContext(ref skipContext, ref skipSettings, out var childSkipSettings);
			skipSettings.skipRule = -1;
			childSkipSettings.skipRule = -1;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			bool TrySkip()
			{
				if (context.position >= context.str.Length)
					return false;
				if (skipContext.shared.positionsToAvoidSkipping[skipContext.position])
					return false;

				var parsedSkipRule = skipRule.Parse(skipContext, skipSettings, childSkipSettings);
				int newPosition = parsedSkipRule.startIndex + parsedSkipRule.length;

				if (parsedSkipRule.success && newPosition != context.position)
				{
					context.position = skipContext.position = newPosition;
					context.skippedRules.Add(parsedSkipRule);
					return true;
				}
				return false;
			}

			switch (settings.skippingStrategy)
			{
				case ParserSkippingStrategy.SkipBeforeParsing:

					if (TrySkip())
					{
						if (context.position < context.str.Length)
							context.shared.positionsToAvoidSkipping[context.position] = true;
					}
					return Parse();

				case ParserSkippingStrategy.SkipBeforeParsingLazy:

					// Alternate: Skip -> TryParse -> Skip -> TryParse ... until TryParse succeeds
					while (true)
					{
						if (TrySkip())
						{
							if (TryParse(out var res))
							{
								return res;
							}
							continue;
						}
						Parse();
					}

				case ParserSkippingStrategy.SkipBeforeParsingGreedy:

					int c = 0;
					while (TrySkip()) { c++; }
					if (c > 0 && context.position < context.str.Length)
						context.shared.positionsToAvoidSkipping[context.position] = true;
					return Parse();

				case ParserSkippingStrategy.TryParseThenSkip:

					if (TryParse(out var result))
						return result;

					if (TrySkip())
					{
						if (context.position < context.str.Length)
							context.shared.positionsToAvoidSkipping[context.position] = true;
						return Parse();
					}

					return ParsedRule.Fail;

				case ParserSkippingStrategy.TryParseThenSkipLazy:

					// First try parse (handled above in TryParseThenSkip pattern),
					// then alternate Skip -> TryParse -> Skip -> TryParse ... until success or nothing consumes
					if (TryParse(out var firstResult))
					{
						return firstResult;
					}

					while (true)
					{
						if (TrySkip())
						{
							if (TryParse(out var res))
							{
								return res;
							}
							continue;
						}
						if (context.position < context.str.Length)
							context.shared.positionsToAvoidSkipping[context.position] = true;
						return ParsedRule.Fail;
					}

				case ParserSkippingStrategy.TryParseThenSkipGreedy:

					// Try parse; if failed, greedily skip then parse once
					if (TryParse(out var firstRes))
						return firstRes;

					while (TrySkip()) { }
					if (context.position < context.str.Length)
						context.shared.positionsToAvoidSkipping[context.position] = true;

					return Parse();

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
			context.barrierTokens.FillWith(tokens, context.str, this);
		}



		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, string input, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = new ParserContext(this, input, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.str, context.position, context.str.Length, parameter);
			return new ParsedTokenResult(null, context, parsedToken);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed token containing the result of the parse.</param>
		/// <returns><see langword="true"/> if a token was matched, <see langword="false"/> otherwise.</returns>
		public bool TryMatchToken(string tokenPatternAlias, string input, out ParsedTokenResult result)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = new ParserContext(this, input, null);
			var parsedToken = MatchToken(tokenPatternId, context.str, context.position, context.str.Length, null);
			result = new ParsedTokenResult(null, context, parsedToken);
			return parsedToken.success;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <param name="result">The parsed token containing the result of the parse.</param>
		/// <returns><see langword="true"/> if a token was matched, <see langword="false"/> otherwise.</returns>
		public bool TryMatchToken(string tokenPatternAlias, string input, object? parameter, out ParsedTokenResult result)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = new ParserContext(this, input, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.str, context.position, context.str.Length, parameter);
			result = new ParsedTokenResult(null, context, parsedToken);
			return parsedToken.success;
		}



		/// <summary>
		/// Parses the given input using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns>A parsed rule containing the result of the parse.</returns>
		public ParsedRuleResult ParseRule(string ruleAlias, string input, object? parameter = null)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = ParseRule(ruleId, context, GlobalSettings);
			return new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
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
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = ParseRule(ruleId, context, GlobalSettings);
			return new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule).GetValue<T>();
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParseRule(string ruleAlias, string input, out ParsedRuleResult result)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, null);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(ruleId, context, GlobalSettings);
			result = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
			return parsedRule.success;
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
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, null);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(ruleId, context, GlobalSettings);
			result = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule).GetValue<T>();
			return parsedRule.success;
		}

		/// <summary>
		/// Tries to parse a rule using the specified rule alias and input text.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, false otherwise.</returns>
		public bool TryParseRule(string ruleAlias, string input, object? parameter, out ParsedRuleResult result)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(ruleId, context, GlobalSettings);
			result = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
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
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(ruleId, context, GlobalSettings);
			result = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule).GetValue<T>();
			return parsedRule.success;
		}



		/// <summary>
		/// Parses the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns>A parsed rule containing the result of the parse.</returns>
		public ParsedRuleResult Parse(string input, object? parameter = null)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = ParseRule(_mainRuleId, context, GlobalSettings);
			return new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
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
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = ParseRule(_mainRuleId, context, GlobalSettings);
			return new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule).GetValue<T>();
		}

		/// <summary>
		/// Tries to parse the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse(string input, out ParsedRuleResult result)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, null);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(_mainRuleId, context, GlobalSettings);
			result = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
			return parsedRule.success;
		}

		/// <summary>
		/// Tries to parse the given input using the main rule, input text and optional parameter.
		/// </summary>
		/// <param name="input">The input text to parse.</param>
		/// <param name="result">The parsed rule containing the result of the parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse(string input, object? parameter, out ParsedRuleResult result)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(_mainRuleId, context, GlobalSettings);
			result = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
			return parsedRule.success;
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
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, null);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(_mainRuleId, context, GlobalSettings);
			var parsedResult = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
			if (parsedRule.success)
			{
				result = parsedResult.GetValue<T>();
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
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the transformation functions.</param>
		/// <returns><see langword="true"/> if a rule was parsed successfully, <see langword="false"/> otherwise.</returns>
		public bool TryParse<T>(string input, object? parameter, out T result)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			EmitBarriers(ref context);
			var parsedRule = TryParseRule(_mainRuleId, context, GlobalSettings);
			var parsedResult = new ParsedRuleResult(ParseTreeOptimization.None, null, context, parsedRule);
			if (parsedRule.success)
			{
				result = parsedResult.GetValue<T>();
				return true;
			}
			result = default!;
			return false;
		}
	}
}