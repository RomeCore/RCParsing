using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCParsing
{
	public partial class Parser
	{
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
			var rule = _rules[ruleId];
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

					var parsed = Parse(rule, ref context, ref settings, ref childSettings, true);

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
				settings.skippingStrategy == ParserSkippingStrategy.Default
				// || context.positionsToAvoidSkipping[context.position]
				)
			{
				while (context.position < context.maxPosition)
				{
					var parsed = Parse(rule, ref context, ref settings, ref childSettings, true);

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

			var skipRule = _rules[settings.skipRule];
			var skipSettings = settings;
			var skipContext = context;
			skipRule.AdvanceContext(ref skipContext, ref skipSettings, out var childSkipSettings);
			skipSettings.skipRule = -1;
			childSkipSettings.skipRule = -1;
			bool record = MainSettings.recordSkippedRules;

			while (context.position < context.maxPosition)
			{
				var parsed = ParseWithSkip(settings.skippingStrategy, rule, skipRule, ref context,
					ref settings, ref skipSettings, ref childSettings, ref childSkipSettings, record, true);

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