using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches as much text as possible that does not contain a specified set of forbidden sequences, 
	/// with support for escaping using a set of escape mappings. Escapes are processed and replaced 
	/// in the parsed value. Uses Tries for efficient lookup of escape and forbidden sequences.
	/// </summary>
	/// <remarks>
	/// Passes a captured <see cref="string"/> with replacements applied as an intermediate value.
	/// </remarks>
	public class EscapedTextTokenPattern : TokenPattern
	{
		private readonly bool _comparerWasSet;
		private readonly bool _escapeNonEmpty;
		private readonly Trie _escape;
		private readonly Trie _forbidden;

		/// <summary>
		/// The set of escape mappings to use for escaping sequences in the input text.
		/// </summary>
		public ImmutableDictionary<string, string> EscapeMappings { get; }

		/// <summary>
		/// The set of forbidden sequences that cannot appear in the input text if they are not escaped.
		/// </summary>
		public ImmutableHashSet<string> ForbiddenSequences { get; }

		/// <summary>
		/// Gets a value indicating whether empty strings are allowed as valid matches.
		/// </summary>
		public bool AllowsEmpty { get; }

		/// <summary>
		/// The string comparer used for comparing and searching within the Trie nodes.
		/// </summary>
		public StringComparer Comparer { get; }

		/// <summary>
		/// The character comparer used for comparing and searching within the Trie nodes.
		/// </summary>
		public CharComparer CharComparer { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EscapedTextTokenPattern"/> class.
		/// </summary>
		/// <param name="escapeMappings">The mappings for escape sequences to their replacements.</param>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match if encountered unescaped.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		public EscapedTextTokenPattern(IEnumerable<KeyValuePair<string, string>> escapeMappings,
			IEnumerable<string> forbidden, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			if (escapeMappings == null)
				throw new ArgumentNullException(nameof(escapeMappings));
			if (forbidden == null)
				throw new ArgumentNullException(nameof(forbidden));

			Comparer = comparer ?? StringComparer.Ordinal;
			CharComparer = new CharComparer(Comparer);
			EscapeMappings = ImmutableDictionary.CreateRange(Comparer, escapeMappings);
			ForbiddenSequences = ImmutableHashSet.CreateRange(Comparer, forbidden);
			AllowsEmpty = allowsEmpty;

			_comparerWasSet = comparer != null;
			_escape = new Trie(escapeMappings.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)),
				comparer != null ? CharComparer : null);
			_forbidden = new Trie(forbidden,comparer != null ? CharComparer : null);
			_escapeNonEmpty = _escape.Count > 0;
		}

		protected override HashSet<char>? FirstCharsCore => null;



		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with double character escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="charSource"/> is "{}", then will be created a pattern
		/// with "{{" -> "{", "}}" -> "}" as escape sequences with "{" and "}" as forbidden sequences.
		/// </remarks>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateDoubleCharacters(IEnumerable<char> charSource, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return new EscapedTextTokenPattern(
				charSource.Select(c => new KeyValuePair<string, string>(new string(c, 2), new string(c, 1))),
				charSource.Select(c => c.ToString()),
				allowsEmpty,
				comparer
			);
		}

		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with double sequence escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="sequences"/> is ["ab", "bc"], then will be created a pattern
		/// with "abab" -> "ab", "bcbc" -> "bc" as escape sequences with "ab" and "bc" as forbidden sequences.
		/// </remarks>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateDoubleSequences(IEnumerable<string> sequences, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			var sourceList = sequences.AsReadOnlyList();
			return new EscapedTextTokenPattern(
				sourceList.Select(c => new KeyValuePair<string, string>(c + c, c)),
				sourceList,
				allowsEmpty,
				comparer
			);
		}

		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with prefix escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="charSource"/> is "abc" and <paramref name="prefix"/> is "\" (backslash),
		/// then will be created a pattern with "\a" -> "a", "\b" -> "b", "\c" -> "c" as escape sequences with "a", "b" and "c" as forbidden sequences.
		/// </remarks>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreatePrefix(IEnumerable<char> charSource, char prefix = '\\', bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return new EscapedTextTokenPattern(
				charSource.Select(c => new KeyValuePair<string, string>($"{prefix}{c}", c.ToString())),
				charSource.Select(c => c.ToString()),
				allowsEmpty,
				comparer
			);
		}

		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with prefix escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="sequences"/> is ["ab", "bc"] and <paramref name="prefix"/> is "\" (backslash),
		/// then will be created a pattern with "\ab" -> "ab", "\bc" -> "bc" as escape sequences with "ab" and "bc" as forbidden sequences.
		/// </remarks>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreatePrefix(IEnumerable<string> sequences, string prefix = "\\", bool allowsEmpty = true, StringComparer? comparer = null)
		{
			var sourceList = sequences.AsReadOnlyList();
			return new EscapedTextTokenPattern(
				sourceList.Select(c => new KeyValuePair<string, string>(prefix + c, c)),
				sourceList,
				allowsEmpty,
				comparer
			);
		}

		/// <summary>
		/// Creates an <see cref="EscapedTextTokenPattern"/> that matches text until any of the specified forbidden characters is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbiddenChars">The set of forbidden characters that terminate the match if encountered.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateUntil(IEnumerable<char> forbiddenChars, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return new EscapedTextTokenPattern(
				Array.Empty<KeyValuePair<string, string>>(),
				forbiddenChars.Select(c => c.ToString()),
				allowsEmpty,
				comparer
			);
		}

		/// <summary>
		/// Creates an <see cref="EscapedTextTokenPattern"/> that matches text until any of the specified forbidden sequences is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match if encountered.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateUntil(IEnumerable<string> forbidden, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return new EscapedTextTokenPattern(
				Array.Empty<KeyValuePair<string, string>>(),
				forbidden,
				allowsEmpty,
				comparer
			);
		}



		// Two identical methods for maximum speed
		// It's been already tested and will be barely changed

		private ParsedElement MatchWithoutCalculation(string input, int position, int barrierPosition)
		{
			int start = position;
			int pos = start;

			while (pos < barrierPosition)
			{
				// 1) Try to match the longest escape starting at pos.
				//    If found — apply replacement and continue.
				if (_escapeNonEmpty && _escape.TryGetLongestMatch(input, pos, barrierPosition, out var replacement, out int escapeConsumed))
				{
					pos += escapeConsumed;
					continue;
				}

				// 2) No escape terminal at this position.
				//    If a forbidden terminal starts here, stop (do not consume forbidden).
				if (_forbidden.ContainsMatch(input, pos, barrierPosition, out int forbiddenConsumed))
				{
					break; // unescaped forbidden sequence -> end of matched text
				}

				// TODO: Maybe remove this? Needs tesing

				/*// 3) No terminal found for escape or forbidden.
				//    We must detect the *real* incomplete-escape case:
				//    If the remainder of the input starting at pos is a strict prefix of some escape
				//    AND we are at the end of the input (no more chars to try) => incomplete escape -> error.
				//    Otherwise treat the current char as normal text.
				if (_escape.IsStrictPrefixOfAny(input, pos, barrierPosition))
				{
					// If the remaining input is a strict prefix of some escape and we are at EOF
					// (i.e. there are no more characters to complete that escape), then it's invalid.
					// We only error when pos..end matches prefix-of-some-escape and there are no more characters
					// that can arrive (we operate on the full string), so this is true incomplete escape.
					if (pos + (barrierPosition - pos) >= barrierPosition) // redundant but explicit: we are at end-of-input suffix
					{
						return ParsedElement.Fail;
					}
					// If we are not at EOF, we still append current char as normal — future iterations
					// may complete into a terminal escape (rare here because we scan the whole input).
				}*/

				// 4) Advance.
				pos++;
			}

			// Produce token
			int length = pos - start;

			if (length == 0 && !AllowsEmpty) // empty match and not allowed -> error
				return ParsedElement.Fail;

			return new ParsedElement(start, length);
		}

		private ParsedElement MatchWithCalculation(string input, int position, int barrierPosition)
		{
			int start = position;
			int pos = start;
			int lastFoundEscape = start;
			StringBuilder? sb = null;

			while (pos < barrierPosition)
			{
				// 1) Try to match the longest escape starting at pos.
				//    If found — apply replacement and continue.
				if (_escapeNonEmpty && _escape.TryGetLongestMatch(input, pos, barrierPosition, out var replacement, out int escapeConsumed))
				{
					sb ??= new StringBuilder();

					if (pos > lastFoundEscape)
						sb.Append(input, lastFoundEscape, pos - lastFoundEscape);
					sb.Append(replacement as string ?? string.Empty);

					pos += escapeConsumed;
					lastFoundEscape = pos;
					continue;
				}

				// 2) No escape terminal at this position.
				//    If a forbidden terminal starts here, stop (do not consume forbidden).
				if (_forbidden.ContainsMatch(input, pos, barrierPosition, out int forbiddenConsumed))
				{
					break; // unescaped forbidden sequence -> end of matched text
				}

				// TODO: Maybe remove this? Needs tesing

				/*// 3) No terminal found for escape or forbidden.
				//    We must detect the *real* incomplete-escape case:
				//    If the remainder of the input starting at pos is a strict prefix of some escape
				//    AND we are at the end of the input (no more chars to try) => incomplete escape -> error.
				//    Otherwise treat the current char as normal text.
				if (_escape.IsStrictPrefixOfAny(input, pos, barrierPosition))
				{
					// If the remaining input is a strict prefix of some escape and we are at EOF
					// (i.e. there are no more characters to complete that escape), then it's invalid.
					// We only error when pos..end matches prefix-of-some-escape and there are no more characters
					// that can arrive (we operate on the full string), so this is true incomplete escape.
					if (pos + (barrierPosition - pos) >= barrierPosition) // redundant but explicit: we are at end-of-input suffix
					{
						return ParsedElement.Fail;
					}
					// If we are not at EOF, we still append current char as normal — future iterations
					// may complete into a terminal escape (rare here because we scan the whole input).
				}*/

				// 4) Advance.
				pos++;
			}

			// Produce token
			int length = pos - start;

			if (length == 0) // empty match and not allowed -> error
			{
				if (!AllowsEmpty)
					return ParsedElement.Fail;
				return new ParsedElement(start, 0, string.Empty);
			}

			if (lastFoundEscape == start)
				return new ParsedElement(start, length, input.Substring(start, length));
			if (pos > lastFoundEscape)
				sb.Append(input, lastFoundEscape, pos - lastFoundEscape);

			return new ParsedElement(start, length, sb?.ToString());
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			if (calculateIntermediateValue)
				return MatchWithCalculation(input, position, barrierPosition);
			else
				return MatchWithoutCalculation(input, position, barrierPosition);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string escapes = string.Join(" ", EscapeMappings.Keys.Select(e => $"'{e}'"));
			string forbidden = string.Join(" ", ForbiddenSequences.Select(e => $"'{e}'"));
			string allowsEmpty = AllowsEmpty ? " allows empty" : " disallows empty";
			return $"escaped {{{escapes}}} forbidden: {{{forbidden}}}{allowsEmpty}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is EscapedTextTokenPattern other &&
				   EscapeMappings.SequenceEqual(other.EscapeMappings) &&
				   ForbiddenSequences.SetEquals(other.ForbiddenSequences) &&
				   Equals(Comparer, other.Comparer) &&
				   AllowsEmpty == other.AllowsEmpty &&
				   _comparerWasSet == other._comparerWasSet;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + EscapeMappings.GetSetHashCode(Comparer);
			hashCode = hashCode * -1521134295 + ForbiddenSequences.GetSetHashCode();
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			hashCode = hashCode * -1521134295 + AllowsEmpty.GetHashCode();
			hashCode = hashCode * -1521134295 + _comparerWasSet.GetHashCode();
			return hashCode;
		}
	}
}