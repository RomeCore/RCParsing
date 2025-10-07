using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches one or more characters based on a predicate function.
	/// </summary>
	public class RepeatCharactersTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the predicate that determines whether a character is part of this pattern.
		/// </summary>
		public Func<char, bool> CharacterPredicate { get; }

		/// <summary>
		/// The minimum number of characters to match (inclusive).
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// The maximum number of characters to match (inclusive). -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="RepeatCharactersTokenPattern"/> class.
		/// </summary>
		/// <param name="characterPredicate">The predicate that determines whether a character is part of this pattern.</param>
		/// <param name="minCount">The minimum number of characters to match (inclusive).</param>
		/// <param name="maxCount">The maximum number of characters to match (inclusive). -1 indicates no upper limit.</param>
		public RepeatCharactersTokenPattern(Func<char, bool> characterPredicate, int minCount, int maxCount)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be greater than or equal to 0");

			if (maxCount < minCount && maxCount != -1)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be greater than or equal to minCount or be -1 if no maximum is specified.");

			CharacterPredicate = characterPredicate ?? throw new ArgumentNullException(nameof(characterPredicate));
			MinCount = minCount;
			MaxCount = maxCount;
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => true;
		protected override bool IsOptionalCore => MinCount == 0;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			int initialPosition = position;
			while (position < barrierPosition &&
				(MaxCount == -1 || position - initialPosition < MaxCount) &&
				CharacterPredicate(input[position]))
				position++;

			int count = position - initialPosition;
			if (count < MinCount)
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, "Minimum count of repeated characters not met.", Id, true);
				return ParsedElement.Fail;
			}

			return new ParsedElement(initialPosition, count);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return $"repeat predicate[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is RepeatCharactersTokenPattern other &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount &&
				   CharacterPredicate == other.CharacterPredicate;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash *= MinCount.GetHashCode() * 17 + 397;
			hash *= MaxCount.GetHashCode() * 17 + 397;
			hash *= CharacterPredicate.GetHashCode() * 17 + 397;
			return hash;
		}
	}
}