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

		/// <summary>
		/// Tries to parse the given input using the specified rule identifier and parser context.
		/// </summary>
		internal ParsedRule TryParseRule(int ruleId, ParserContext context, ParserSettings settings)
		{
			var rule = _rules[ruleId];
			var ruleSettings = settings;
			var ruleContext = context;
			rule.AdvanceContext(ref ruleContext, ref ruleSettings, out var ruleChildSettings);

			var skipStrategy = settings.skippingStrategy ?? SkipStrategy.NoSkipping;
			var result = skipStrategy.ParseWithSkip(context, settings,
				rule, ruleContext, ruleSettings, ruleChildSettings);

			if (result.success)
				return result;

			var recovery = rule.ErrorRecovery ?? ErrorRecoveryStrategy.NoRecovery;
			return recovery.TryRecover(context, settings, rule, ruleContext, ruleSettings, ruleChildSettings);
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
			var factory = MainSettings.astFactory;
			if (factory != null)
				return factory.Invoke(context, parsedRule);
			return new ParsedRuleResult(null, context, parsedRule);
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