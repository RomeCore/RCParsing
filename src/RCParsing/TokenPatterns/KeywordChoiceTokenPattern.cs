using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches one of a set of keywords in the input text, ensuring the matched keyword is not followed by prohibited (identifier) characters.
	/// Uses a Trie for efficient lookup of multiple keywords.
	/// </summary>
	/// <remarks>
	/// A keyword is a literal string that must be followed by a non-identifier or non-prohibited character (or end of input)
	/// to avoid matching partial identifiers. For example, keyword "SELECT" would match "SELECT" but not
	/// "SELECTION" since "I" is an identifier character.
	/// Passes the matched original keyword <see cref="string"/> as an intermediate value.
	/// </remarks>
	public class KeywordChoiceTokenPattern : TokenPattern
	{
		private readonly Trie _root;

		/// <summary>
		/// Gets the set of keywords to match.
		/// </summary>
		public IReadOnlyList<string> Keywords { get; }

		/// <summary>
		/// Gets the comparer used for keyword matching.
		/// </summary>
		public StringComparer Comparer { get; }

		/// <summary>
		/// Gets the character comparer used for keyword matching.
		/// </summary>
		public CharComparer CharComparer { get; }

		/// <summary>
		/// Predicate to determine if a character is an identifier character (should not follow keyword).
		/// </summary>
		public Func<char, bool> IdentifierCharacterPredicate { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="KeywordChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="keywords">The collection of keywords to match.</param>
		/// <param name="identifierCharacterPredicate">Predicate to identify characters that should not follow the keyword.</param>
		/// <param name="comparer">The comparer to use for keyword matching.</param>
		public KeywordChoiceTokenPattern(IEnumerable<string> keywords, Func<char, bool> identifierCharacterPredicate,
			StringComparer? comparer = null)
		{
			if (keywords == null)
				throw new ArgumentNullException(nameof(keywords));

			Keywords = keywords.ToList().AsReadOnlyList();
			if (Keywords.Count == 0)
				throw new ArgumentException("Keywords collection is empty.", nameof(keywords));
			if (Keywords.Any(k => string.IsNullOrEmpty(k)))
				throw new ArgumentException("One of keywords is null or empty.", nameof(keywords));

			IdentifierCharacterPredicate = identifierCharacterPredicate ?? throw new ArgumentNullException(nameof(identifierCharacterPredicate));
			Comparer = comparer ?? StringComparer.Ordinal;
			CharComparer = new CharComparer(Comparer);

			_root = new Trie(Keywords.Select(k => new KeyValuePair<string, object?>(k, k)),
				comparer.IsDefaultIgnoreCase() ? CharComparer : null);
		}

		protected override HashSet<char> FirstCharsCore => Comparer.IsDefaultCaseSensitive() ?
			new(Keywords.Select(k => k[0])) :
			new(Keywords.SelectMany(k => new char[] { char.ToLower(k[0]), char.ToUpper(k[0]) }));
		protected override bool IsFirstCharDeterministicCore => Comparer.IsNullOrDefault();
		protected override bool IsOptionalCore => false;



		/// <summary>
		/// Creates a new instance of the <see cref="KeywordChoiceTokenPattern"/> class with ASCII identifier character checking.
		/// </summary>
		/// <param name="keywords">The collection of keywords to match.</param>
		/// <param name="comparer">The comparer to use for keyword matching.</param>
		public static KeywordChoiceTokenPattern AsciiKeywordChoice(IEnumerable<string> keywords, StringComparer? comparer = null)
		{
			return new KeywordChoiceTokenPattern(keywords,
				c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_',
				comparer);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="KeywordChoiceTokenPattern"/> class with Unicode identifier character checking.
		/// </summary>
		/// <param name="keywords">The collection of keywords to match.</param>
		/// <param name="comparer">The comparer to use for keyword matching.</param>
		public static KeywordChoiceTokenPattern UnicodeKeywordChoice(IEnumerable<string> keywords, StringComparer? comparer = null)
		{
			return new KeywordChoiceTokenPattern(keywords,
				c => char.IsLetterOrDigit(c) || c == '_',
				comparer);
		}



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (_root.TryGetLongestMatch(input, position, barrierPosition, out var matchedKeyword, out int matchedLength))
			{
				// Check if the next character (if exists) is not an prohibited character
				int nextPos = position + matchedLength;
				if (nextPos < barrierPosition && IdentifierCharacterPredicate(input[nextPos]))
				{
					if (position >= furthestError.position)
						furthestError = new ParsingError(position, 0, $"Keyword is followed by prohibited character.", Id, true);
					return ParsedElement.Fail;
				}

				return new ParsedElement(position, matchedLength, matchedKeyword);
			}

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Cannot match any keyword.", Id, true);
			return ParsedElement.Fail;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return $"keyword choice '{string.Join("|", Keywords)}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is KeywordChoiceTokenPattern other &&
				   Keywords.SetEqual(other.Keywords) &&
				   Equals(Comparer, other.Comparer) &&
				   IdentifierCharacterPredicate == other.IdentifierCharacterPredicate;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Keywords.GetSetHashCode();
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			hashCode = hashCode * -1521134295 + IdentifierCharacterPredicate.GetHashCode();
			return hashCode;
		}
	}
}