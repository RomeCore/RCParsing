using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCParsing
{
	public partial class Parser
	{
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

		static bool TryParse(ParserRule rule, ref ParserContext context,
				ref ParserSettings settings, ref ParserSettings childSettings, out ParsedRule parsedRule)
		{
			parsedRule = rule.Parse(context, settings, childSettings);
			return parsedRule.success;
		}

		static ParsedRule Parse(ParserRule rule, ref ParserContext context,
				ref ParserSettings settings, ref ParserSettings childSettings, bool canRecover)
		{
			var parsedRule = rule.Parse(context, settings, childSettings);
			if (!parsedRule.success && canRecover)
				if (rule.CanRecover)
					return TryRecover(rule, ref context, ref settings, ref childSettings);
			return parsedRule;
		}



		private static ParsedRule ParseWithSkip(ParserSkippingStrategy strategy,
			ParserRule rule, ParserRule skipRule, ref ParserContext context,
			ref ParserSettings settings, ref ParserSettings skipSettings,
			ref ParserSettings childSettings, ref ParserSettings childSkipSettings,
			bool record, bool canRecover)
		{
			switch (strategy)
			{
				case ParserSkippingStrategy.SkipBeforeParsing:

					TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record);
					context.shared.positionsToAvoidSkipping[context.position] = true;
					return Parse(rule, ref context, ref settings, ref childSettings, canRecover);

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
						return Parse(rule, ref context, ref settings, ref childSettings, canRecover);
					}

				case ParserSkippingStrategy.SkipBeforeParsingGreedy:

					while (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
					{
					}
					context.shared.positionsToAvoidSkipping[context.position] = true;
					return Parse(rule, ref context, ref settings, ref childSettings, canRecover);

				case ParserSkippingStrategy.TryParseThenSkip:

					if (TryParse(rule, ref context, ref settings, ref childSettings, out var result))
						return result;

					if (TrySkip(ref context, skipRule, ref skipSettings, ref childSkipSettings, record))
					{
						context.shared.positionsToAvoidSkipping[context.position] = true;
						return Parse(rule, ref context, ref settings, ref childSettings, canRecover);
					}

					if (canRecover)
						return TryRecover(rule, ref context, ref settings, ref childSettings);
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
						if (canRecover)
							return TryRecover(rule, ref context, ref settings, ref childSettings);
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

					return Parse(rule, ref context, ref settings, ref childSettings, canRecover);

				default:

					return ParsedRule.Fail;
			}
		}

		private ParsedRule TryParseRule(ParserRule rule, 
			ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings,
			bool canRecover)
		{
			if (MainSettings.useOptimizedWhitespaceSkip)
			{
				while (context.position < context.maxPosition && char.IsWhiteSpace(context.input[context.position]))
					context.position++;
				return Parse(rule, ref context, ref settings, ref childSettings, canRecover);
			}

			if (settings.skipRule == -1 ||
				settings.skippingStrategy == ParserSkippingStrategy.Default ||
				context.positionsToAvoidSkipping[context.position])
				return Parse(rule, ref context, ref settings, ref childSettings, canRecover);

			var skipRule = _rules[settings.skipRule];
			var skipSettings = settings;
			var skipContext = context;
			skipRule.AdvanceContext(ref skipContext, ref skipSettings, out var childSkipSettings);
			skipSettings.skipRule = -1;
			childSkipSettings.skipRule = -1;
			bool record = MainSettings.recordSkippedRules;
			return ParseWithSkip(settings.skippingStrategy, rule, skipRule, ref context,
				 ref settings, ref skipSettings, ref childSettings, ref childSkipSettings, record, canRecover);
		}

		// Oh yea i know that i duplicated the code down here, but otherwise i will get -10% for speed lol

		/// <summary>
		/// Tries to parse the given input using the specified rule identifier and parser context.
		/// </summary>
		internal ParsedRule TryParseRule(int ruleId, ParserContext context,
			ParserSettings settings)
		{
			var rule = _rules[ruleId];
			rule.AdvanceContext(ref context, ref settings, out var childSettings);

			if (MainSettings.useOptimizedWhitespaceSkip)
			{
				while (context.position < context.maxPosition && char.IsWhiteSpace(context.input[context.position]))
					context.position++;
				return Parse(rule, ref context, ref settings, ref childSettings, true);
			}

			if (settings.skipRule == -1 ||
				settings.skippingStrategy == ParserSkippingStrategy.Default ||
				context.positionsToAvoidSkipping[context.position])
				return Parse(rule, ref context, ref settings, ref childSettings, true);

			var skipRule = _rules[settings.skipRule];
			var skipSettings = settings;
			var skipContext = context;
			skipRule.AdvanceContext(ref skipContext, ref skipSettings, out var childSkipSettings);
			skipSettings.skipRule = -1;
			childSkipSettings.skipRule = -1;
			bool record = MainSettings.recordSkippedRules;
			return ParseWithSkip(settings.skippingStrategy, rule, skipRule, ref context,
				 ref settings, ref skipSettings, ref childSettings, ref childSkipSettings, record, true);
		}



		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EmitBarriers(ref ParserContext context)
		{
			if (_tokenizers.Length == 0)
				return;

			var ctx = context;
			var tokens = _tokenizers.SelectMany(t => t.Tokenize(ctx));
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
			var result = CreateResult(ref context, ref parsedRule);
			return result;
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
	}
}