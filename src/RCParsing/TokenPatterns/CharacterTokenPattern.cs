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

		protected override HashSet<char>? FirstCharsCore => null;



		public override ParsedElement Match(string input, int position, object? parserParameter)
		{
			if (position < input.Length && CharacterPredicate(input[position]))
			{
				return new ParsedElement(Id, position, 1);
			}

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