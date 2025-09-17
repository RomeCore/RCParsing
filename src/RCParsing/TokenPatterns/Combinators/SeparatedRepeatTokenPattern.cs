using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace RCParsing.TokenPatterns.Combinators
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



		private ParsedElement MatchWithoutCalculation(string input, int position, int barrierPosition, object? parserParameter)
		{
			var initialPosition = position;
			var firstElement = _token.Match(input, position, barrierPosition, parserParameter, false);
			if (!firstElement.success)
			{
				if (MinCount == 0)
				{
					var sepAtStart = _separator.Match(input, position, barrierPosition,
						parserParameter, true);
					if (sepAtStart.success)
						return ParsedElement.Fail;

					return new ParsedElement(initialPosition, position - initialPosition);
				}
				else
				{
					return ParsedElement.Fail;
				}
			}

			if (firstElement.length == 0)
				return ParsedElement.Fail;

			int count = 1;
			position = firstElement.startIndex + firstElement.length;

			while (MaxCount == -1 || count < MaxCount)
			{
				var parsedSep = _separator.Match(input, position, barrierPosition, parserParameter, false);
				if (!parsedSep.success)
					break;

				if (parsedSep.length == 0)
					return ParsedElement.Fail;

				if (IncludeSeparatorsInResult)
					count++;

				position = parsedSep.startIndex + parsedSep.length;

				var nextElement = _token.Match(input, position, barrierPosition, parserParameter, false);
				if (!nextElement.success)
				{
					if (AllowTrailingSeparator)
					{
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

				count++;
				position = nextElement.startIndex + nextElement.length;
			}

			if (count < MinCount)
			{
				return ParsedElement.Fail;
			}

			return new ParsedElement(initialPosition, position - initialPosition);
		}
		
		private ParsedElement MatchWithCalculation(string input, int position, int barrierPosition, object? parserParameter)
		{
			List<object>? elements = null;
			var initialPosition = position;

			var firstElement = _token.Match(input, position, barrierPosition, parserParameter, true);
			if (!firstElement.success)
			{
				if (MinCount == 0)
				{
					var sepAtStart = _separator.Match(input, position, barrierPosition,
						parserParameter, true);
					if (sepAtStart.success)
						return ParsedElement.Fail;

					return new ParsedElement(initialPosition, position - initialPosition,
						PassageFunction(elements as IReadOnlyList<object> ?? Array.Empty<object>()));
				}
				else
				{
					return ParsedElement.Fail;
				}
			}

			if (firstElement.length == 0)
				return ParsedElement.Fail;

			elements ??= new List<object>();
			elements.Add(firstElement.intermediateValue);
			position = firstElement.startIndex + firstElement.length;

			while (MaxCount == -1 || elements.Count < MaxCount)
			{
				var parsedSep = _separator.Match(input, position, barrierPosition, parserParameter, true);
				if (!parsedSep.success)
					break;

				if (parsedSep.length == 0)
				{
					return ParsedElement.Fail;
				}

				if (IncludeSeparatorsInResult)
					elements.Add(parsedSep);

				position = parsedSep.startIndex + parsedSep.length;

				var nextElement = _token.Match(input, position, barrierPosition, parserParameter, true);
				if (!nextElement.success)
				{
					if (AllowTrailingSeparator)
					{
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
			}

			if (elements.Count < MinCount)
			{
				return ParsedElement.Fail;
			}

			return new ParsedElement(initialPosition, position - initialPosition, PassageFunction(elements));
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			if (calculateIntermediateValue && PassageFunction != null)
				return MatchWithCalculation(input, position, barrierPosition, parserParameter);
			else
				return MatchWithoutCalculation(input, position, barrierPosition, parserParameter);
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