using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches a keyword in the input text, ensuring it is not followed by identifier characters.
	/// </summary>
	/// <remarks>
	/// A keyword is a literal string that must be followed by a non-identifier character (or end of input)
	/// to avoid matching partial identifiers. For example, keyword "SELECT" would match "SELECT" but not
	/// "SELECTION" since "I" is an identifier character.
	/// Passes the matched original keyword <see cref="string"/> as an intermediate value.
	/// </remarks>
	public class KeywordTokenPattern : TokenPattern
	{
		/// <summary>
		/// The keyword string to match.
		/// </summary>
		public string Keyword { get; }

		/// <summary>
		/// Gets the string comparison type used for keyword matching.
		/// </summary>
		public StringComparison Comparison { get; }

		/// <summary>
		/// Predicate to determine if a character is a prohibited character (should not follow keyword).
		/// </summary>
		public Func<char, bool> ProhibitedCharacterPredicate { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="KeywordTokenPattern"/> class.
		/// </summary>
		/// <param name="keyword">The keyword string to match.</param>
		/// <param name="prohibitedCharacterPredicate">
		/// Predicate to identify characters that should not follow the keyword.
		/// Should return <see langword="true"/> for characters that should not follow the keyword.
		/// </param>
		/// <param name="comparison">The string comparison type to use for keyword matching.</param>
		public KeywordTokenPattern(string keyword, Func<char, bool> prohibitedCharacterPredicate,
			StringComparison comparison = StringComparison.Ordinal)
		{
			Keyword = string.IsNullOrEmpty(keyword)
				? throw new ArgumentException("Keyword cannot be null or empty.", nameof(keyword))
				: keyword;
			ProhibitedCharacterPredicate = prohibitedCharacterPredicate ?? throw new ArgumentNullException(nameof(prohibitedCharacterPredicate));
			Comparison = comparison;
		}

		protected override HashSet<char> FirstCharsCore => Comparison.IsIgnoreCase() ? 
			new(new char[] { char.ToLower(Keyword[0]), char.ToUpper(Keyword[0]) }) : new(new char[] { Keyword[0] });
		protected override bool IsFirstCharDeterministicCore => true;
		protected override bool IsOptionalCore => false;



		/// <summary>
		/// Creates a new instance of the <see cref="KeywordTokenPattern"/> class with ASCII identifier character checking.
		/// </summary>
		/// <param name="keyword">The keyword string to match.</param>
		/// <param name="comparison">The string comparison type to use for keyword matching.</param>
		public static KeywordTokenPattern AsciiKeyword(string keyword, StringComparison comparison = StringComparison.Ordinal)
		{
			return new KeywordTokenPattern(keyword,
				c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_',
				comparison);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="KeywordTokenPattern"/> class with Unicode identifier character checking.
		/// </summary>
		/// <param name="keyword">The keyword string to match.</param>
		/// <param name="comparison">The string comparison type to use for keyword matching.</param>
		public static KeywordTokenPattern UnicodeKeyword(string keyword, StringComparison comparison = StringComparison.Ordinal)
		{
			return new KeywordTokenPattern(keyword,
				c => char.IsLetterOrDigit(c) || c == '_',
				comparison);
		}



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (position + Keyword.Length > barrierPosition)
				return ParsedElement.Fail;

			// Check if the keyword matches
			if (!input.AsSpan(position, Keyword.Length).Equals(Keyword.AsSpan(), Comparison))
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, $"Cannot match a keyword.", Id, true);
				return ParsedElement.Fail;
			}

			// Check if the next character (if exists) is not a prohibited character
			int nextPos = position + Keyword.Length;
			if (nextPos < barrierPosition && ProhibitedCharacterPredicate(input[nextPos]))
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, "Keyword is followed by prohibited character.", Id, true);
				return ParsedElement.Fail;
			}

			return new ParsedElement(position, Keyword.Length, Keyword);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return $"keyword '{Keyword}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is KeywordTokenPattern pattern &&
				   Keyword == pattern.Keyword &&
				   Comparison == pattern.Comparison &&
				   ProhibitedCharacterPredicate == pattern.ProhibitedCharacterPredicate;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Keyword.GetHashCode();
			hashCode = hashCode * -1521134295 + Comparison.GetHashCode();
			hashCode = hashCode * -1521134295 + ProhibitedCharacterPredicate.GetHashCode();
			return hashCode;
		}
	}
}