using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches a sequence of token patterns in order.
	/// </summary>
	public class SequenceTokenPattern : TokenPattern
	{
		/// <summary>
		/// The IDs of the token patterns to match in sequence.
		/// </summary>
		public ImmutableArray<int> TokenPatterns { get; }

		/// <summary>
		/// The function to pass the intermediate values from each pattern to the result intermediate value.
		/// </summary>
		public Func<IReadOnlyList<object?>, object?>? PassageFunction { get; }



		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to match in sequence.</param>
		/// <param name="passageFunction">The function to pass the intermediate values from each pattern to the result intermediate value.</param>
		public SequenceTokenPattern(IEnumerable<int> tokenPatternIds, Func<IReadOnlyList<object?>, object?>? passageFunction = null)
		{
			TokenPatterns = tokenPatternIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (TokenPatterns.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
			PassageFunction = passageFunction;
		}

		protected override HashSet<char>? FirstCharsCore => GetTokenPattern(TokenPatterns[0]).FirstChars;



		private TokenPattern[] _patterns;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			_patterns = TokenPatterns.Select(p => GetTokenPattern(p)).ToArray();
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			if (calculateIntermediateValue && PassageFunction != null)
			{
				var initialPosition = position;
				ParsedElement[]? tokens = null;

				for (int i = 0; i < _patterns.Length; i++)
				{
					var pattern = _patterns[i];
					var token = pattern.Match(input, position, barrierPosition, parserParameter, true);
					if (!token.success)
						return ParsedElement.Fail;

					if (PassageFunction != null)
					{
						tokens ??= new ParsedElement[TokenPatterns.Length];
						tokens[i] = token;
					}
					position = token.startIndex + token.length;
				}

				var intermediateValues = new ListSelectWrapper<ParsedElement, object?>(tokens, t => t.intermediateValue);
				var intermediateValue = PassageFunction(intermediateValues);

				return new ParsedElement(initialPosition, position - initialPosition, intermediateValue);
			}
			else
			{
				var initialPosition = position;

				for (int i = 0; i < _patterns.Length; i++)
				{
					var pattern = _patterns[i];
					var token = pattern.Match(input, position, barrierPosition, parserParameter, false);
					if (!token.success)
						return ParsedElement.Fail;

					position = token.startIndex + token.length;
				}

				return new ParsedElement(initialPosition, position - initialPosition);
			}
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "sequence...";
			return $"sequence:\n" +
				string.Join("\n", TokenPatterns.Select(c => GetTokenPattern(c).ToString(remainingDepth - 1)))
				.Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SequenceTokenPattern pattern &&
				   TokenPatterns.SequenceEqual(pattern.TokenPatterns) &&
				   Equals(PassageFunction, pattern.PassageFunction);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + TokenPatterns.GetSequenceHashCode();
			hashCode = hashCode * -1521134295 + PassageFunction?.GetHashCode() ?? 0;
			return hashCode;
		}
	}
}