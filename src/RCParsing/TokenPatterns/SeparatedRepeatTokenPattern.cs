using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// A token pattern that repeats a specified element token with a separator token between elements.
	/// </summary>
	public class SeparatedRepeatTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the element token ID.
		/// </summary>
		public int Token { get; }

		/// <summary>
		/// Gets the separator token ID.
		/// </summary>
		public int Separator { get; }

		/// <summary>
		/// Gets the minimum number of elements.
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// Gets the maximum number of elements, or -1 for no limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// Gets whether a trailing separator without a following element is allowed.
		/// </summary>
		public bool AllowTrailingSeparator { get; }

		/// <summary>
		/// Gets whether separators should be included in the result children tokens for the passage function.
		/// </summary>
		public bool IncludeSeparatorsInResult { get; }

		/// <summary>
		/// The function to pass the intermediate values from each pattern to the result intermediate value.
		/// </summary>
		public Func<IReadOnlyList<object?>, object?>? PassageFunction { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="SeparatedRepeatTokenPattern"/> class.
		/// </summary>
		/// <param name="token">The ID of the token to repeat.</param>
		/// <param name="separatorRule">The ID of the token to use as a separator.</param>
		/// <param name="minCount">The minimum number of elements.</param>
		/// <param name="maxCount">The maximum number of elements, or -1 for no limit.</param>
		/// <param name="allowTrailingSeparator">Whether a trailing separator without a following element is allowed.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children tokens.</param>
		/// <param name="passageFunction">The passage function used to pass children intermediate values into this element's intermediate value.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="minCount"/> is less than 0 or less than <paramref name="maxCount"/> if specified.</exception>
		public SeparatedRepeatTokenPattern(
			int token,
			int separatorRule,
			int minCount,
			int maxCount,
			bool allowTrailingSeparator = false,
			bool includeSeparatorsInResult = false,
			Func<IReadOnlyList<object?>, object?>? passageFunction = null)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be >= 0");

			if (maxCount < minCount && maxCount >= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be >= minCount or -1");

			Token = token;
			Separator = separatorRule;
			MinCount = minCount;
			MaxCount = Math.Max(maxCount, -1);
			AllowTrailingSeparator = allowTrailingSeparator;
			IncludeSeparatorsInResult = includeSeparatorsInResult;
			PassageFunction = passageFunction;
		}

		protected override HashSet<char>? FirstCharsCore => MinCount == 0 ? null :
			GetTokenPattern(Token).FirstChars;

		private TokenPattern _token, _separator;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);

			_token = GetTokenPattern(Token);
			_separator = GetTokenPattern(Separator);
		}



		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter)
		{
			List<object>? elements = null;
			var initialPosition = position;

			// Try to parse the first element (if required - error if not found; if optional - may return empty result)
			var firstElement = _token.Match(input, position, barrierPosition, parserParameter);
			if (!firstElement.success)
			{
				// No first element found
				// If minCount == 0 — OK: empty sequence (but first check if there's a separator at the start)
				if (MinCount == 0)
				{
					// If there is a separator at the start — this is an error (unexpected leading separator)
					var sepAtStart = _separator.Match(input, position, barrierPosition, parserParameter);
					if (sepAtStart.success)
						return ParsedElement.Fail;

					// No elements and no separator — return successful empty result
					return new ParsedElement(initialPosition, position - initialPosition,
						PassageFunction?.Invoke(elements as IReadOnlyList<object> ?? Array.Empty<object>()));
				}
				else
				{
					return ParsedElement.Fail;
				}
			}

			// We have the first element
			if (firstElement.length == 0)
			{
				return ParsedElement.Fail;
			}

			elements ??= new List<object>();
			elements.Add(firstElement.intermediateValue);
			position = firstElement.startIndex + firstElement.length;

			// Parse "separator + element" until limit reached
			while (MaxCount == -1 || elements.Count < MaxCount)
			{
				// Try to parse the separator
				var parsedSep = _separator.Match(input, position, barrierPosition, parserParameter);
				if (!parsedSep.success)
					break; // no separator — end of sequence

				if (parsedSep.length == 0)
				{
					return ParsedElement.Fail;
				}

				// Include separator in result if requested
				if (IncludeSeparatorsInResult)
					elements.Add(parsedSep);

				// Separator successfully parsed — position already updated inside TryParseRule, but update again for safety:
				position = parsedSep.startIndex + parsedSep.length;

				// Try to parse the next element
				var nextElement = _token.Match(input, position, barrierPosition, parserParameter); ;
				if (!nextElement.success)
				{
					// Separator was found, but next element is missing
					if (AllowTrailingSeparator)
					{
						// Trailing separator allowed — consider separator consumed and stop.
						// Keep childContext.position after separator as is and exit loop.
						break;
					}
					else
					{
						return ParsedElement.Fail;
					}
				}

				if (nextElement.length == 0)
				{
					return ParsedElement.Fail;
				}

				elements.Add(nextElement);
				position = nextElement.startIndex + nextElement.length;

				// loop continues — try to find next separator + element
			}

			// Check minimum count
			if (elements.Count < MinCount)
			{
				return ParsedElement.Fail;
			}

			return new ParsedElement(initialPosition, position - initialPosition,
				PassageFunction?.Invoke(elements as IReadOnlyList<object> ?? Array.Empty<object>()));
		}

		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}' " : string.Empty;

			string trailing = AllowTrailingSeparator ? " (allow trailing)" : "";
			if (remainingDepth <= 0)
				return $"separatedRepeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]{trailing}...";

			return $"separatedRepeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]{trailing}: " +
				   $"{GetTokenPattern(Token).ToString(remainingDepth - 1)} sep {GetTokenPattern(Separator).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SeparatedRepeatTokenPattern other &&
				   Token == other.Token &&
				   Separator == other.Separator &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount &&
				   AllowTrailingSeparator == other.AllowTrailingSeparator &&
				   IncludeSeparatorsInResult == other.IncludeSeparatorsInResult &&
				   PassageFunction == other.PassageFunction;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Token.GetHashCode();
			hashCode = hashCode * 397 + Separator.GetHashCode();
			hashCode = hashCode * 397 + MinCount.GetHashCode();
			hashCode = hashCode * 397 + MaxCount.GetHashCode();
			hashCode = hashCode * 397 + AllowTrailingSeparator.GetHashCode();
			hashCode = hashCode * 397 + IncludeSeparatorsInResult.GetHashCode();
			hashCode = hashCode * 397 + PassageFunction.GetHashCode();
			return hashCode;
		}
	}
}