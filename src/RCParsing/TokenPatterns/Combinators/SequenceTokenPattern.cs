using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// Matches a sequence of token patterns in order.
	/// </summary>
	public class SequenceTokenPattern : TokenPattern
	{
		private readonly int[] _tokenPatternsIds;

		/// <summary>
		/// The IDs of the token patterns to match in sequence.
		/// </summary>
		public IReadOnlyList<int> TokenPatterns { get; }

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
			_tokenPatternsIds = tokenPatternIds?.ToArray() ?? throw new ArgumentNullException(nameof(tokenPatternIds));
			TokenPatterns = _tokenPatternsIds.AsReadOnlyList();
			if (_tokenPatternsIds.Length == 0)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
			PassageFunction = passageFunction;
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				HashSet<char> firstChars = new();
				for (int i = 0; i < TokenPatterns.Count; i++)
				{
					var tokenPattern = GetTokenPattern(TokenPatterns[i]);
					foreach (var ch in tokenPattern.FirstChars)
						firstChars.Add(ch);
					if (!tokenPattern.IsOptional)
						break;
				}
				return firstChars;
			}
		}
		protected override bool IsFirstCharDeterministicCore
		{
			get
			{
				bool isDeterministic = true;
				for (int i = 0; i < TokenPatterns.Count; i++)
				{
					var tokenPattern = GetTokenPattern(TokenPatterns[i]);
					isDeterministic = isDeterministic && tokenPattern.IsFirstCharDeterministic;
					if (!tokenPattern.IsOptional)
						break;
				}
				return isDeterministic;
			}
		}
		protected override bool IsOptionalCore => TokenPatterns.All(i => GetTokenPattern(i).IsOptional);



		private TokenPattern[] _patterns;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			_patterns = TokenPatterns.Select(p => GetTokenPattern(p)).ToArray();
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (calculateIntermediateValue && PassageFunction != null)
			{
				var initialPosition = position;
				object?[]? intermediateValues = null;

				for (int i = 0; i < _patterns.Length; i++)
				{
					var pattern = _patterns[i];
					var token = pattern.Match(input, position, barrierPosition, parserParameter, true, ref furthestError);
					if (!token.success)
						return ParsedElement.Fail;

					if (PassageFunction != null)
					{
						intermediateValues ??= new object?[_patterns.Length];
						intermediateValues[i] = token.intermediateValue;
					}
					position = token.startIndex + token.length;
				}

				var intermediateValue = PassageFunction(intermediateValues);

				return new ParsedElement(initialPosition, position - initialPosition, intermediateValue);
			}
			else
			{
				var initialPosition = position;

				for (int i = 0; i < _patterns.Length; i++)
				{
					var pattern = _patterns[i];
					var token = pattern.Match(input, position, barrierPosition, parserParameter, false, ref furthestError);
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