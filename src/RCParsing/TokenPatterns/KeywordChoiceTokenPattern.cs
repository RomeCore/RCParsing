using System;
using System.Collections;
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
		/// Gets the set of keywords to match mapped with intermediate values.
		/// </summary>
		public IReadOnlyList<KeyValuePair<string, object?>> KeywordsMap { get; }

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
		public Func<char, bool> ProhibitedCharacterPredicate { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="KeywordChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="keywords">The collection of keywords to match.</param>
		/// <param name="identifierCharacterPredicate">Predicate to identify characters that should not follow the keyword.</param>
		/// <param name="comparer">The comparer to use for keyword matching.</param>
		public KeywordChoiceTokenPattern(IEnumerable<string> keywords,
			Func<char, bool> identifierCharacterPredicate, StringComparer? comparer = null)
			: this(keywords?.Select(k => new KeyValuePair<string, object?>(k, k)),
				identifierCharacterPredicate, comparer)
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="KeywordChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="keywords">The collection of keywords to match mapped with intermediate values.</param>
		/// <param name="prohibitedCharacterPredicate">Predicate to identify characters that should not follow the keyword.</param>
		/// <param name="comparer">The comparer to use for keyword matching.</param>
		public KeywordChoiceTokenPattern(IEnumerable<KeyValuePair<string, object?>> keywords,
			Func<char, bool> prohibitedCharacterPredicate, StringComparer? comparer = null)
		{
			if (keywords == null)
				throw new ArgumentNullException(nameof(keywords));

			KeywordsMap = keywords.Distinct().ToList().AsReadOnlyList();
			if (KeywordsMap.Count == 0)
				throw new ArgumentException("Keywords collection is empty.", nameof(keywords));
			if (KeywordsMap.Any(k => string.IsNullOrEmpty(k.Key)))
				throw new ArgumentException("One of keywords is null or empty.", nameof(keywords));
			Keywords = KeywordsMap.Select(k => k.Key).ToList().AsReadOnlyList();

			ProhibitedCharacterPredicate = prohibitedCharacterPredicate ?? throw new ArgumentNullException(nameof(prohibitedCharacterPredicate));
			Comparer = comparer ?? StringComparer.Ordinal;
			CharComparer = new CharComparer(Comparer);

			_root = new Trie(keywords,
				!comparer.IsDefaultIgnoreCase() ? null : CharComparer);
		}

		protected override HashSet<char> FirstCharsCore => !Comparer.IsDefaultIgnoreCase() ?
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
		/// Creates a new instance of the <see cref="KeywordChoiceTokenPattern"/> class with ASCII identifier character checking.
		/// </summary>
		/// <param name="keywords">The collection of keywords to match.</param>
		/// <param name="comparer">The comparer to use for keyword matching.</param>
		public static KeywordChoiceTokenPattern AsciiKeywordChoice(IEnumerable<KeyValuePair<string, object?>> keywords, StringComparer? comparer = null)
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
		
		/// <summary>
		/// Creates a new instance of the <see cref="KeywordChoiceTokenPattern"/> class with Unicode identifier character checking.
		/// </summary>
		/// <param name="keywords">The collection of keywords to match.</param>
		/// <param name="comparer">The comparer to use for keyword matching.</param>
		public static KeywordChoiceTokenPattern UnicodeKeywordChoice(IEnumerable<KeyValuePair<string, object?>> keywords, StringComparer? comparer = null)
		{
			return new KeywordChoiceTokenPattern(keywords,
				c => char.IsLetterOrDigit(c) || c == '_',
				comparer);
		}



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (_root.TryGetLongestMatch(input, position, barrierPosition, out var intermediateValue, out int matchedLength))
			{
				// Check if the next character (if exists) is not an prohibited character
				int nextPos = position + matchedLength;
				if (nextPos < barrierPosition && ProhibitedCharacterPredicate(input[nextPos]))
				{
					if (position >= furthestError.position)
						furthestError = new ParsingError(position, 0, $"Keyword is followed by prohibited character.", Id, true);
					return ParsedElement.Fail;
				}

				return new ParsedElement(position, matchedLength, intermediateValue);
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
				   KeywordsMap.SetEqual(other.KeywordsMap) &&
				   Equals(Comparer, other.Comparer) &&
				   ProhibitedCharacterPredicate == other.ProhibitedCharacterPredicate;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + KeywordsMap.GetSetHashCode();
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			hashCode = hashCode * -1521134295 + ProhibitedCharacterPredicate.GetHashCode();
			return hashCode;
		}
	}
}