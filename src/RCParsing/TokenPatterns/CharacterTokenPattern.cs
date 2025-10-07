using System;
using System.Collections.Generic;
using System.Linq;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches one character based on a predicate function.
	/// </summary>
	public class CharacterTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the predicate that determines whether a character is part of this pattern.
		/// </summary>
		public Func<char, bool> CharacterPredicate { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="CharacterTokenPattern"/> class.
		/// </summary>
		/// <param name="characterPredicate">The predicate that determines whether a character is part of this pattern.</param>
		public CharacterTokenPattern(Func<char, bool> characterPredicate)
		{
			CharacterPredicate = characterPredicate ?? throw new ArgumentNullException(nameof(characterPredicate));
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (position < barrierPosition && CharacterPredicate(input[position]))
			{
				return new ParsedElement(position, 1);
			}

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Failed to match predicate", Id, true);
			return ParsedElement.Fail;
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "predicate";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is CharacterTokenPattern other &&
				   CharacterPredicate == other.CharacterPredicate;
		}

		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
			hash *= CharacterPredicate.GetHashCode() * 17;
			return hash;
		}
	}
}