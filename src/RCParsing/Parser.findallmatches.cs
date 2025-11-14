using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
			var ruleSettings = settings;
			var ruleContext = context;
			rule.AdvanceContext(ref ruleContext, ref ruleSettings, out var ruleChildSettings);

			var skipStrategy = settings.skippingStrategy ?? SkipStrategy.NoSkipping;
			return skipStrategy.FindAllMatches(context, settings, overlap,
				rule, ruleContext, ruleSettings, ruleChildSettings);
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



		/// <summary>
		/// Walks through all matches of the given rule and performs non-overlapping replacement.
		/// </summary>
		/// <param name="ruleId">The rule ID to find.</param>
		/// <param name="context">Parser context for the input.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		internal string ReplaceAllMatches(int ruleId, ParserContext context, Func<ParsedRuleResultBase, string> replacementSelector)
		{
			if (context.parser != this)
				throw new InvalidOperationException("Parser context is not associated with this parser.");

			EmitBarriers(ref context);

			var input = context.input;
			if (string.IsNullOrEmpty(input))
				return input;

			// Non-overlapping replacement: always move past the end of the last match.
			var matches = FindAllMatches(ruleId, context, GlobalSettings, overlap: false)
				.Select(r => CreateResult(ref context, ref r))
				.ToList();

			if (matches.Count == 0)
				return input;

			var sb = new StringBuilder(context.maxPosition - context.position);
			var currentIndex = context.position;

			foreach (var match in matches)
			{
				var start = match.StartIndex;
				var length = match.Length;

				if (start < currentIndex)
					continue; // Skip overlapping or invalid match.

				if (start > currentIndex)
					sb.Append(input, currentIndex, start - currentIndex);

				var replacement = replacementSelector(match) ?? string.Empty;
				sb.Append(replacement);

				currentIndex = start + length;
			}

			if (currentIndex < input.Length)
				sb.Append(input, currentIndex, context.maxPosition - currentIndex);

			return sb.ToString();
		}

		/// <summary>
		/// Replaces all occurrences of the main rule in the input text with the specified replacement.
		/// The replacement text is taken from <c>result.Value?.ToString()</c> if not null,
		/// otherwise from <paramref name="fallbackReplacement"/>.
		/// </summary>
		/// <param name="input">The input text to process.</param>
		/// <param name="fallbackReplacement">Fallback replacement text used when <c>result.Value</c> is null.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(string input, string? fallbackReplacement = null, object? parameter = null)
		{
			fallbackReplacement ??= string.Empty;
			return ReplaceAllMatches(input, parameter, result => result.Value?.ToString() ?? fallbackReplacement);
		}

		/// <summary>
		/// Replaces all occurrences of the main rule in the input using the specified context,
		/// taking replacement text from <c>result.Value?.ToString()</c> if not null,
		/// otherwise from <paramref name="fallbackReplacement"/>.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="fallbackReplacement">Fallback replacement text used when <c>result.Value</c> is null.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(ParserContext context, string? fallbackReplacement = null)
		{
			fallbackReplacement ??= string.Empty;
			return ReplaceAllMatches(context, result => result.Value?.ToString() ?? fallbackReplacement);
		}

		/// <summary>
		/// Replaces all occurrences of the specified rule in the input text with the specified replacement.
		/// The replacement text is taken from <c>result.Value?.ToString()</c> if not null,
		/// otherwise from <paramref name="fallbackReplacement"/>.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to process.</param>
		/// <param name="fallbackReplacement">Fallback replacement text used when <c>result.Value</c> is null.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(string ruleAlias, string input, string? fallbackReplacement = null, object? parameter = null)
		{
			fallbackReplacement ??= string.Empty;
			return ReplaceAllMatches(ruleAlias, input, parameter, result => result.Value?.ToString() ?? fallbackReplacement);
		}

		/// <summary>
		/// Replaces all occurrences of the specified rule in the input using the specified context,
		/// taking replacement text from <c>result.Value?.ToString()</c> if not null,
		/// otherwise from <paramref name="fallbackReplacement"/>.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="fallbackReplacement">Fallback replacement text used when <c>result.Value</c> is null.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(string ruleAlias, ParserContext context, string? fallbackReplacement = null)
		{
			fallbackReplacement ??= string.Empty;
			return ReplaceAllMatches(ruleAlias, context, result => result.Value?.ToString() ?? fallbackReplacement);
		}

		/// <summary>
		/// Replaces all occurrences of the main rule in the input text with a value provided by <paramref name="replacementSelector"/>.
		/// </summary>
		/// <param name="input">The input text to process.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(string input, object? parameter, Func<ParsedRuleResultBase, string> replacementSelector)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			return ReplaceAllMatches(_mainRuleId, context, replacementSelector);
		}

		/// <summary>
		/// Replaces all occurrences of the main rule in the input text with a value provided by <paramref name="replacementSelector"/>.
		/// </summary>
		/// <param name="input">The input text to process.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(string input, Func<ParsedRuleResultBase, string> replacementSelector)
		{
			return ReplaceAllMatches(input, null, replacementSelector);
		}

		/// <summary>
		/// Replaces all occurrences of the main rule in the input using the specified context
		/// with a value provided by <paramref name="replacementSelector"/>.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(ParserContext context, Func<ParsedRuleResultBase, string> replacementSelector)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			return ReplaceAllMatches(_mainRuleId, context, replacementSelector);
		}

		/// <summary>
		/// Replaces all occurrences of the specified rule in the input text with a value provided by <paramref name="replacementSelector"/>.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to process.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(string ruleAlias, string input, object? parameter, Func<ParsedRuleResultBase, string> replacementSelector)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			return ReplaceAllMatches(ruleId, context, replacementSelector);
		}

		/// <summary>
		/// Replaces all occurrences of the specified rule in the input text with a value provided by <paramref name="replacementSelector"/>.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to process.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(string ruleAlias, string input, Func<ParsedRuleResultBase, string> replacementSelector)
		{
			return ReplaceAllMatches(ruleAlias, input, fallbackReplacement: null, replacementSelector);
		}

		/// <summary>
		/// Replaces all occurrences of the specified rule in the input using the specified context
		/// with a value provided by <paramref name="replacementSelector"/>.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches(string ruleAlias, ParserContext context, Func<ParsedRuleResultBase, string> replacementSelector)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			return ReplaceAllMatches(ruleId, context, replacementSelector);
		}

		/// <summary>
		/// Replaces all occurrences of the main rule in the input text with a value provided by <paramref name="replacementSelector"/>.
		/// The parsed result value is converted to <typeparamref name="T"/> or <see langword="default"/> before being passed to the selector.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="input">The input text to process.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches<T>(string input, object? parameter, Func<T?, string> replacementSelector)
		{
			return ReplaceAllMatches(input, parameter, r => replacementSelector(r.TryGetValue<T>()));
		}

		/// <summary>
		/// Replaces all occurrences of the main rule in the input text with a value provided by <paramref name="replacementSelector"/>.
		/// The parsed result value is converted to <typeparamref name="T"/> or <see langword="default"/> before being passed to the selector.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="input">The input text to process.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches<T>(string input, Func<T?, string> replacementSelector)
		{
			return ReplaceAllMatches(input, null, replacementSelector);
		}

		/// <summary>
		/// Replaces all occurrences of the main rule in the input using the specified context
		/// with a value provided by <paramref name="replacementSelector"/>.
		/// The parsed result value is converted to <typeparamref name="T"/> or <see langword="default"/> before being passed to the selector.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches<T>(ParserContext context, Func<T?, string> replacementSelector)
		{
			return ReplaceAllMatches(context, r => replacementSelector(r.TryGetValue<T>()));
		}

		/// <summary>
		/// Replaces all occurrences of the specified rule in the input text with a value provided by <paramref name="replacementSelector"/>.
		/// The parsed result value is converted to <typeparamref name="T"/> or <see langword="default"/> before being passed to the selector.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to process.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches<T>(string ruleAlias, string input, object? parameter, Func<T?, string> replacementSelector)
		{
			return ReplaceAllMatches(ruleAlias, input, parameter, r => replacementSelector(r.TryGetValue<T>()));
		}

		/// <summary>
		/// Replaces all occurrences of the specified rule in the input text with a value provided by <paramref name="replacementSelector"/>.
		/// The parsed result value is converted to <typeparamref name="T"/> or <see langword="default"/> before being passed to the selector.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="input">The input text to process.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches<T>(string ruleAlias, string input, Func<T?, string> replacementSelector)
		{
			return ReplaceAllMatches(ruleAlias, input, parameter: null, replacementSelector);
		}

		/// <summary>
		/// Replaces all occurrences of the specified rule in the input using the specified context
		/// with a value provided by <paramref name="replacementSelector"/>.
		/// The parsed result value is converted to <typeparamref name="T"/> or <see langword="default"/> before being passed to the selector.
		/// </summary>
		/// <typeparam name="T">The type to convert the parsed results to.</typeparam>
		/// <param name="ruleAlias">The alias for the parser rule to use.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="replacementSelector">Function that returns the replacement text for each match.</param>
		/// <returns>The input string with all matches replaced.</returns>
		public string ReplaceAllMatches<T>(string ruleAlias, ParserContext context, Func<T?, string> replacementSelector)
		{
			return ReplaceAllMatches(ruleAlias, context, r => replacementSelector(r.TryGetValue<T>()));
		}



		/// <summary>
		/// Splits the input string into substrings by all non-overlapping matches of the specified rule.
		/// Works similarly to <see cref="System.Text.RegularExpressions.Regex.Split(string)"/>.
		/// </summary>
		/// <param name="ruleId">The rule ID to use as a delimiter.</param>
		/// <param name="context">Parser context for the input.</param>
		/// <returns>An enumerable sequence of substrings between matches.</returns>
		internal IEnumerable<string> Split(int ruleId, ParserContext context)
		{
			if (context.parser != this)
				throw new InvalidOperationException("Parser context is not associated with this parser.");

			EmitBarriers(ref context);

			var input = context.input;

			// Non-overlapping: the same as ReplaceAllMatches (overlap = false).
			var matches = FindAllMatches(ruleId, context, GlobalSettings, overlap: false);

			var currentIndex = context.position;

			foreach (var match in matches)
			{
				var start = match.startIndex;
				var length = match.length;

				if (start < currentIndex)
					continue;

				// Segment before match.
				yield return input.Substring(currentIndex, start - currentIndex);

				currentIndex = start + length;
			}

			// Tail after last match.
			yield return input.Substring(currentIndex, context.maxPosition - currentIndex);
		}

		/// <summary>
		/// Splits the input string into substrings by all non-overlapping matches of the main rule.
		/// Works similarly to <see cref="System.Text.RegularExpressions.Regex.Split(string)"/>.
		/// </summary>
		/// <param name="input">The input text to split.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <returns>An enumerable sequence of substrings between matches.</returns>
		public IEnumerable<string> Split(string input, object? parameter = null)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			var context = new ParserContext(this, input, parameter);
			return Split(_mainRuleId, context);
		}

		/// <summary>
		/// Splits the input represented by the specified context into substrings by
		/// all non-overlapping matches of the main rule.
		/// </summary>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>An enumerable sequence of substrings between matches.</returns>
		public IEnumerable<string> Split(ParserContext context)
		{
			if (_mainRuleId == -1)
				throw new InvalidOperationException("Main rule is not set.");

			return Split(_mainRuleId, context);
		}

		/// <summary>
		/// Splits the input string into substrings by all non-overlapping matches of the specified rule.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use as a delimiter.</param>
		/// <param name="input">The input text to split.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <returns>An enumerable sequence of substrings between matches.</returns>
		public IEnumerable<string> Split(string ruleAlias, string input, object? parameter = null)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			var context = new ParserContext(this, input, parameter);
			return Split(ruleId, context);
		}

		/// <summary>
		/// Splits the input represented by the specified context into substrings by
		/// all non-overlapping matches of the specified rule.
		/// </summary>
		/// <param name="ruleAlias">The alias for the parser rule to use as a delimiter.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <returns>An enumerable sequence of substrings between matches.</returns>
		public IEnumerable<string> Split(string ruleAlias, ParserContext context)
		{
			if (!_rulesAliases.TryGetValue(ruleAlias, out var ruleId))
				throw new ArgumentException("Invalid rule alias", nameof(ruleAlias));

			return Split(ruleId, context);
		}
	}
}