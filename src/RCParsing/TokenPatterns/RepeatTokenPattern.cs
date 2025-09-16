using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that repeats a specific token multiple times.
	/// </summary>
	public class RepeatTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID to repeat.
		/// </summary>
		public int TokenPattern { get; }

		/// <summary>
		/// Gets the minimum number of times the token pattern must repeat.
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// Gets the maximum number of times the token pattern can repeat. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// The function to pass the intermediate values from each pattern to the result intermediate value.
		/// </summary>
		public Func<IReadOnlyList<object?>, object?>? PassageFunction { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepeatTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternId">The token pattern ID to repeat.</param>
		/// <param name="minCount">The minimum number of times the token pattern must repeat.</param>
		/// <param name="maxCount">The maximum number of times the token pattern can repeat. -1 indicates no upper limit.</param>
		/// <param name="passageFunction">The function to pass the intermediate values from each pattern to the result intermediate value.</param>
		public RepeatTokenPattern(int tokenPatternId, int minCount, int maxCount, Func<IReadOnlyList<object?>, object?>? passageFunction = null)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be greater than or equal to 0");

			if (maxCount < minCount && maxCount != -1)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be greater than or equal to minCount or be -1 if no maximum is specified.");

			TokenPattern = tokenPatternId;
			MinCount = minCount;
			MaxCount = maxCount;
			PassageFunction = passageFunction;
		}

		protected override HashSet<char>? FirstCharsCore => MinCount == 0 ? null :
			GetTokenPattern(TokenPattern).FirstChars;



		private TokenPattern _pattern;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			_pattern = GetTokenPattern(TokenPattern);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter)
		{
			if (PassageFunction == null)
			{
				var initialPosition = position;
				int count = 0;

				for (int i = 0; i < MaxCount || MaxCount == -1; i++)
				{
					ParsedElement matchedToken = _pattern.Match(input, position, barrierPosition, parserParameter);
					if (!matchedToken.success || matchedToken.startIndex + matchedToken.length == position)
					{
						break;
					}

					position = matchedToken.startIndex + matchedToken.length;
					count++;
				}

				if (count < this.MinCount)
					return ParsedElement.Fail;

				return new ParsedElement(Id, initialPosition, position - initialPosition);
			}
			else
			{
				var tokens = new List<ParsedElement>();
				var initialPosition = position;

				for (int i = 0; i < MaxCount || MaxCount == -1; i++)
				{
					ParsedElement matchedToken = _pattern.Match(input, position, barrierPosition, parserParameter);
					if (!matchedToken.success || matchedToken.startIndex + matchedToken.length == position)
					{
						break;
					}

					position = matchedToken.startIndex + matchedToken.length;
					tokens.Add(matchedToken);
				}

				if (tokens.Count < this.MinCount)
					return ParsedElement.Fail;

				var intermediateValues = new ListSelectWrapper<ParsedElement, object?>(tokens, t => t.intermediateValue);
				var intermediateValue = PassageFunction(intermediateValues);

				return new ParsedElement(Id, initialPosition, position - initialPosition, intermediateValue);
			}
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return $"repeat[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]...";
			return $"repeat[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]: " +
				$"{GetTokenPattern(TokenPattern).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is RepeatTokenPattern pattern &&
				   TokenPattern == pattern.TokenPattern &&
				   MinCount == pattern.MinCount &&
				   MaxCount == pattern.MaxCount &&
				   Equals(PassageFunction, pattern.PassageFunction);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + TokenPattern.GetHashCode();
			hashCode = hashCode * -1521134295 + MinCount.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxCount.GetHashCode();
			hashCode = hashCode * -1521134295 + (PassageFunction?.GetHashCode() ?? 0);
			return hashCode;
		}
	}
}