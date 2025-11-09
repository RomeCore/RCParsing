using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a pattern that can be used to match tokens in a text.
	/// </summary>
	public abstract class TokenPattern : ParserElement
	{
		/// <summary>
		/// Gets the parsed value factory that will be applied to parent rule.
		/// </summary>
		public Func<ParsedRuleResultBase, object?>? DefaultParsedValueFactory { get; internal set; } = null;

		/// <summary>
		/// Gets the local settings that will be applied to parent rule with each setting configurable by override modes.
		/// </summary>
		public ParserLocalSettings DefaultSettings { get; internal set; } = default;

		/// <summary>
		/// Gets the error recovery strategy that will be applied to parent rule.
		/// </summary>
		public ErrorRecoveryStrategy DefaultErrorRecovery { get; internal set; } = default;

		/// <summary>
		/// Tries to match the given context with this pattern.
		/// </summary>
		/// <param name="input">The input text to match.</param>
		/// <param name="position">The position in the input text to start matching from.</param>
		/// <param name="barrierPosition">
		/// The position in the input text to stop matching at.
		/// Can be length of input text or an index to next barrier token.
		/// </param>
		/// <param name="parserParameter">The optional context parameter to pass to the pattern.</param>
		/// <param name="calculateIntermediateValue">
		/// Whether to calculate intermediate value.
		/// Will be <see langword="false"/> when it will be ignored and should not be calculated.
		/// </param>
		/// <param name="furthestError">The last and furthest error that occurred during the parsing.</param>
		/// <returns>The parsed element containing the result of the match.</returns>
		public abstract ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError);

		/// <inheritdoc cref="Match(string, int, int, object?, bool, ref ParsingError)"/>
		public ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			var furthestError = ParsingError.Empty;
			return Match(input, position, barrierPosition, parserParameter, calculateIntermediateValue, ref furthestError);
		}

		/// <inheritdoc cref="Match(string, int, int, object?, bool, ref ParsingError)"/>
		public ParsedElement Match(string input, int position, int barrierPosition,
			bool calculateIntermediateValue = true)
		{
			var furthestError = ParsingError.Empty;
			return Match(input, position, barrierPosition, null, calculateIntermediateValue, ref furthestError);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is TokenPattern other;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			return hashCode;
		}
	}
}