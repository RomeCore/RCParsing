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
		public Func<List<object?>, object?>? PassageFunction { get; }



		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to match in sequence.</param>
		/// <param name="passageFunction">The function to pass the intermediate values from each pattern to the result intermediate value.</param>
		public SequenceTokenPattern(IEnumerable<int> tokenPatternIds, Func<List<object?>, object?>? passageFunction = null)
		{
			TokenPatterns = tokenPatternIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (TokenPatterns.IsEmpty)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
			PassageFunction = passageFunction;
		}

		protected override HashSet<char>? FirstCharsCore => GetTokenPattern(TokenPatterns[0]).FirstChars;



		public override ParsedElement Match(string input, int position, object? parserParameter)
		{
			var initialPosition = position;
			var tokens = new List<ParsedElement>();

			foreach (var tokenId in TokenPatterns)
			{
				var token = TryMatchToken(tokenId, input, position, parserParameter);
				if (!token.success)
					return ParsedElement.Fail;

				tokens.Add(token);
				position = token.startIndex + token.length;
			}

			object? intermediateValue = null;
			if (PassageFunction != null)
			{
				var intermediateValues = tokens.Select(t => t.intermediateValue).ToList();
				intermediateValue = PassageFunction(intermediateValues);
			}

			return new ParsedElement(Id, initialPosition, position - initialPosition, intermediateValue);
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