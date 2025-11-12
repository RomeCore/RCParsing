using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents an escaping strategy for <see cref="EscapedTextTokenPattern"/>.
	/// </summary>
	public abstract class EscapingStrategy
	{
		/// <summary>
		/// Tries to match the escape sequence in the input text and returns replacement text.
		/// </summary>
		/// <param name="input">The input text to match.</param>
		/// <param name="position">The start position to begin match from.</param>
		/// <param name="maxPosition">The max position to stop matching at.</param>
		/// <param name="consumedLength">A length of matched escape sequence (or length of consumed input).</param>
		/// <param name="replacement">The replacement text (unescaped sequence) to paste into resulting unescaped text.</param>
		/// <returns><see langword="true"/> if this method did matched any escape sequence; otherwise, <see langword="false"/>.</returns>
		public abstract bool TryEscape(string input, int position, int maxPosition, out int consumedLength, out string replacement);

		/// <summary>
		/// Checks if input text contains the stop sequence, where parser should stop matching at.
		/// </summary>
		/// <param name="input">The input text to match.</param>
		/// <param name="position">The start position to begin match from.</param>
		/// <param name="maxPosition">The max position to stop matching at.</param>
		/// <param name="consumedLength">A length of matched stop sequence (or length of consumed input).</param>
		/// <returns><see langword="true"/> if this method did matched any stop sequence and parser should stop; otherwise, <see langword="false"/>.</returns>
		public abstract bool TryStop(string input, int position, int maxPosition, out int consumedLength);
	}

	/// <summary>
	/// Represents a combined escaping strategy that tries multiple strategies in order,
	/// using the first one that matches for escape sequences and stop conditions.
	/// </summary>
	public class CombinedEscapingStrategy : EscapingStrategy
	{
		private readonly EscapingStrategy[] _strategies;

		/// <summary>
		/// Gets the sequence of escaping strategies that will be tried in order.
		/// </summary>
		public IReadOnlyList<EscapingStrategy> Strategies => _strategies;

		/// <summary>
		/// Initializes a new instance of the <see cref="CombinedEscapingStrategy"/> class.
		/// </summary>
		/// <param name="strategies">The escaping strategies to try in order.</param>
		/// <exception cref="ArgumentNullException">Thrown when strategies is null.</exception>
		/// <exception cref="ArgumentException">Thrown when strategies is empty or contains null elements.</exception>
		public CombinedEscapingStrategy(params EscapingStrategy[] strategies)
		{
			if (strategies == null)
				throw new ArgumentNullException(nameof(strategies));
			if (strategies.Length == 0)
				throw new ArgumentException("At least one strategy must be provided.", nameof(strategies));
			if (strategies.Any(s => s == null))
				throw new ArgumentException("Strategies array cannot contain null elements.", nameof(strategies));

			_strategies = strategies.ToArray();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CombinedEscapingStrategy"/> class.
		/// </summary>
		/// <param name="strategies">The escaping strategies to try in order.</param>
		/// <exception cref="ArgumentNullException">Thrown when strategies is null.</exception>
		/// <exception cref="ArgumentException">Thrown when strategies is empty or contains null elements.</exception>
		public CombinedEscapingStrategy(IEnumerable<EscapingStrategy> strategies)
		{
			if (strategies == null)
				throw new ArgumentNullException(nameof(strategies));

			var strategiesArray = strategies.ToArray();
			if (strategiesArray.Length == 0)
				throw new ArgumentException("At least one strategy must be provided.", nameof(strategies));
			if (strategiesArray.Any(s => s == null))
				throw new ArgumentException("Strategies collection cannot contain null elements.", nameof(strategies));

			_strategies = strategiesArray;
		}

		public override bool TryEscape(string input, int position, int maxPosition, out int consumedLength, out string replacement)
		{
			// Try each strategy in order until one matches
			foreach (var strategy in _strategies)
			{
				if (strategy.TryEscape(input, position, maxPosition, out consumedLength, out replacement))
					return true;
			}

			// No strategy matched
			consumedLength = 0;
			replacement = string.Empty;
			return false;
		}

		public override bool TryStop(string input, int position, int maxPosition, out int consumedLength)
		{
			// Try each strategy in order until one matches
			foreach (var strategy in _strategies)
			{
				if (strategy.TryStop(input, position, maxPosition, out consumedLength))
					return true;
			}

			// No strategy matched
			consumedLength = 0;
			return false;
		}

		public override string ToString()
		{
			var strategyStrings = _strategies.Select(s => s.ToString());
			return $"combined: [{string.Join(" | ", strategyStrings)}]";
		}

		public override bool Equals(object? obj)
		{
			return obj is CombinedEscapingStrategy other &&
				   _strategies.SequenceEqual(other._strategies);
		}

		public override int GetHashCode()
		{
			var hashCode = 17;
			foreach (var strategy in _strategies)
			{
				hashCode = hashCode * 397 + strategy.GetHashCode();
			}
			return hashCode;
		}
	}

	/// <summary>
	/// Represents a default, trie-based escaping strategy with escape mappings 
	/// </summary>
	public class TrieEscapingStrategy : EscapingStrategy
	{
		private readonly Trie _escape;
		private readonly Trie _forbidden;

		/// <summary>
		/// The set of escape mappings to use for escaping sequences in the input text.
		/// </summary>
		public IDictionary<string, string> EscapeMappings { get; }

		/// <summary>
		/// The set of forbidden sequences that cannot appear in the input text if they are not escaped.
		/// </summary>
		public IReadOnlyList<string> ForbiddenSequences { get; }

		/// <summary>
		/// The string comparer used for comparing and searching within the Trie nodes.
		/// </summary>
		public StringComparer Comparer { get; }

		/// <summary>
		/// The character comparer used for comparing and searching within the Trie nodes.
		/// </summary>
		public CharComparer CharComparer { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TrieEscapingStrategy"/> class.
		/// </summary>
		/// <param name="escapeMappings">The mappings for escape sequences to their replacements.</param>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match if encountered unescaped.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		public TrieEscapingStrategy(IEnumerable<KeyValuePair<string, string>> escapeMappings,
			IEnumerable<string> forbidden, StringComparer? comparer = null)
		{
			if (escapeMappings == null)
				throw new ArgumentNullException(nameof(escapeMappings));
			if (forbidden == null)
				throw new ArgumentNullException(nameof(forbidden));

			Comparer = comparer ?? StringComparer.Ordinal;
			CharComparer = new CharComparer(Comparer);
			EscapeMappings = new ReadOnlyDictionary<string, string>(escapeMappings.ToDictionary(k => k.Key, v => v.Value, Comparer));
			ForbiddenSequences = forbidden.ToArray().AsReadOnlyList();

			_escape = new Trie(escapeMappings.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)),
				comparer != null ? CharComparer : null);
			_forbidden = new Trie(forbidden, comparer != null ? CharComparer : null);
		}

		public override bool TryEscape(string input, int position, int maxPosition, out int consumedLength, out string replacement)
		{
			var res = _escape.TryGetLongestMatch(input, position, maxPosition, out var _replacement, out consumedLength);
			replacement = _replacement as string;
			return res;
		}

		public override bool TryStop(string input, int position, int maxPosition, out int consumedLength)
		{
			return _forbidden.TryGetLongestMatch(input, position, maxPosition, out _, out consumedLength);
		}

		public override string ToString()
		{
			string escapes = string.Join(" ", EscapeMappings.Keys.Select(e => $"'{e}'"));
			string forbidden = string.Join(" ", ForbiddenSequences.Select(e => $"'{e}'"));
			return $"escapes: {{{escapes}}} forbidden: {{{forbidden}}}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is TrieEscapingStrategy other &&
				   EscapeMappings.SetEqual(other.EscapeMappings) &&
				   ForbiddenSequences.SetEqual(other.ForbiddenSequences) &&
				   Equals(Comparer, other.Comparer);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + EscapeMappings.GetSetHashCode(Comparer);
			hashCode = hashCode * -1521134295 + ForbiddenSequences.GetSetHashCode(Comparer);
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			return hashCode;
		}
	}



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
		/// <summary>
		/// The escaping strategy to use.
		/// </summary>
		public EscapingStrategy EscapingStrategy { get; }

		/// <summary>
		/// Gets a value indicating whether empty strings are allowed as valid matches.
		/// </summary>
		public bool AllowsEmpty { get; }

		/// <summary>
		/// Gets a value indicating whether need to capture/consume the stop sequence for match.
		/// </summary>
		/// <remarks>
		/// Affects both on intermediate value (unescaped text) and capture length.
		/// </remarks>
		public bool ConsumeStopSequence { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="EscapedTextTokenPattern"/> class.
		/// </summary>
		/// <param name="escapingStrategy">The escaping strategy to use.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="consumeStopSequence">Indicates whether need to capture/consume the stop sequence for match.</param>
		public EscapedTextTokenPattern(EscapingStrategy escapingStrategy,
			bool allowsEmpty = true, bool consumeStopSequence = false)
		{
			EscapingStrategy = escapingStrategy ?? throw new ArgumentNullException(nameof(escapingStrategy));
			AllowsEmpty = allowsEmpty;
			ConsumeStopSequence = consumeStopSequence;
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="EscapedTextTokenPattern"/> class.
		/// </summary>
		/// <param name="escapeMappings">The mappings for escape sequences to their replacements.</param>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match if encountered unescaped.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="consumeStopSequence">Indicates whether need to capture/consume the stop sequence for match.</param>
		public EscapedTextTokenPattern(IEnumerable<KeyValuePair<string, string>> escapeMappings,
			IEnumerable<string> forbidden, StringComparer? comparer = null,
			bool allowsEmpty = true, bool consumeStopSequence = false)
		{
			EscapingStrategy = new TrieEscapingStrategy(escapeMappings, forbidden, comparer);
			AllowsEmpty = allowsEmpty;
			ConsumeStopSequence = consumeStopSequence;
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => AllowsEmpty;



		/// <summary>
		/// Creates a <see cref="EscapedTextTokenPattern"/> that matches the escaped text with double character escaping strategy.
		/// </summary>
		/// <remarks>
		/// For example, if the <paramref name="charSource"/> is "{}", then will be created a pattern
		/// with "{{" -> "{", "}}" -> "}" as escape sequences with "{" and "}" as forbidden sequences.
		/// </remarks>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="consumeStopSequence">Indicates whether need to capture/consume the stop sequence for match.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateDoubleCharacters(IEnumerable<char> charSource, StringComparer? comparer = null,
			bool allowsEmpty = true, bool consumeStopSequence = false)
		{
			return new EscapedTextTokenPattern(
				charSource.Select(c => new KeyValuePair<string, string>(new string(c, 2), new string(c, 1))),
				charSource.Select(c => c.ToString()),
				comparer,
				allowsEmpty,
				consumeStopSequence
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
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="consumeStopSequence">Indicates whether need to capture/consume the stop sequence for match.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateDoubleSequences(IEnumerable<string> sequences, StringComparer? comparer = null,
			bool allowsEmpty = true, bool consumeStopSequence = false)
		{
			var sourceList = sequences.AsReadOnlyList();
			return new EscapedTextTokenPattern(
				sourceList.Select(c => new KeyValuePair<string, string>(c + c, c)),
				sourceList,
				comparer,
				allowsEmpty,
				consumeStopSequence
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
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="consumeStopSequence">Indicates whether need to capture/consume the stop sequence for match.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreatePrefix(IEnumerable<char> charSource, char prefix = '\\', StringComparer? comparer = null,
			bool allowsEmpty = true, bool consumeStopSequence = false)
		{
			return new EscapedTextTokenPattern(
				charSource.Select(c => new KeyValuePair<string, string>($"{prefix}{c}", c.ToString())),
				charSource.Select(c => c.ToString()),
				comparer,
				allowsEmpty,
				consumeStopSequence
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
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="consumeStopSequence">Indicates whether need to capture/consume the stop sequence for match.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreatePrefix(IEnumerable<string> sequences, string prefix = "\\", StringComparer? comparer = null,
			bool allowsEmpty = true, bool consumeStopSequence = false)
		{
			var sourceList = sequences.AsReadOnlyList();
			return new EscapedTextTokenPattern(
				sourceList.Select(c => new KeyValuePair<string, string>(prefix + c, c)),
				sourceList,
				comparer,
				allowsEmpty,
				consumeStopSequence
			);
		}

		/// <summary>
		/// Creates an <see cref="EscapedTextTokenPattern"/> that matches text until any of the specified forbidden characters is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbiddenChars">The set of forbidden characters that terminate the match if encountered.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="consumeStopSequence">Indicates whether need to capture/consume the stop sequence for match.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateUntil(IEnumerable<char> forbiddenChars, StringComparer? comparer = null,
			bool allowsEmpty = true, bool consumeStopSequence = false)
		{
			return new EscapedTextTokenPattern(
				Array.Empty<KeyValuePair<string, string>>(),
				forbiddenChars.Select(c => c.ToString()),
				comparer,
				allowsEmpty,
				consumeStopSequence
			);
		}

		/// <summary>
		/// Creates an <see cref="EscapedTextTokenPattern"/> that matches text until any of the specified forbidden sequences is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match if encountered.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		/// <param name="allowsEmpty">Indicates whether empty strings are allowed as valid matches.</param>
		/// <param name="consumeStopSequence">Indicates whether need to capture/consume the stop sequence for match.</param>
		/// <returns>A created <see cref="EscapedTextTokenPattern"/> instance.</returns>
		public static EscapedTextTokenPattern CreateUntil(IEnumerable<string> forbidden, StringComparer? comparer = null,
			bool allowsEmpty = true, bool consumeStopSequence = false)
		{
			return new EscapedTextTokenPattern(
				Array.Empty<KeyValuePair<string, string>>(),
				forbidden,
				comparer,
				allowsEmpty,
				consumeStopSequence
			);
		}



		// Two identical methods for maximum speed
		// It's been already tested and will be barely changed

		private ParsedElement MatchWithoutCalculation(string input, int position, int barrierPosition, ref ParsingError furthestError)
		{
			int start = position;
			var strategy = EscapingStrategy;

			while (position < barrierPosition)
			{
				// 1) Try to match the longest escape starting at pos.
				//    If found — apply replacement and continue.
				if (strategy.TryEscape(input, position, barrierPosition, out var consumedLength, out var replacement))
				{
					position += consumedLength;
					continue;
				}

				// 2) No escape terminal at this position.
				//    If a forbidden terminal starts here, stop (do not consume forbidden).
				if (strategy.TryStop(input, position, barrierPosition, out consumedLength))
				{
					if (ConsumeStopSequence)
						position += consumedLength;
					break; // unescaped forbidden sequence -> end of matched text
				}

				// 3) Advance.
				position++;
			}

			// Produce token
			int length = position - start;

			if (length == 0 && !AllowsEmpty) // empty match and not allowed -> error
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, "Empty match is not allowed.", Id, true);
				return ParsedElement.Fail;
			}

			return new ParsedElement(start, length);
		}

		private ParsedElement MatchWithCalculation(string input, int position, int barrierPosition, ref ParsingError furthestError)
		{
			int start = position;
			var strategy = EscapingStrategy;
			int lastFoundEscape = start;
			StringBuilder? sb = null;

			while (position < barrierPosition)
			{
				// 1) Try to match the longest escape starting at pos.
				//    If found — apply replacement and continue.
				if (strategy.TryEscape(input, position, barrierPosition, out var consumedLength, out var replacement))
				{
					sb ??= new StringBuilder();

					if (position > lastFoundEscape)
						sb.Append(input, lastFoundEscape, position - lastFoundEscape);
					sb.Append(replacement as string ?? string.Empty);

					position += consumedLength;
					lastFoundEscape = position;
					continue;
				}

				// 2) No escape terminal at this position.
				//    If a forbidden terminal starts here, stop (do not consume forbidden).
				if (strategy.TryStop(input, position, barrierPosition, out consumedLength))
				{
					if (ConsumeStopSequence)
						position += consumedLength;
					break; // unescaped forbidden sequence -> end of matched text
				}

				// 4) Advance.
				position++;
			}

			// Produce token
			int length = position - start;

			if (length == 0) // empty match and not allowed -> error
			{
				if (!AllowsEmpty)
				{
					if (position >= furthestError.position)
						furthestError = new ParsingError(position, 0, "Empty match is not allowed.", Id, true);
					return ParsedElement.Fail;
				}
				return new ParsedElement(start, 0, string.Empty);
			}

			if (lastFoundEscape == start)
				return new ParsedElement(start, length, input.Substring(start, length));
			if (position > lastFoundEscape)
				sb.Append(input, lastFoundEscape, position - lastFoundEscape);

			return new ParsedElement(start, length, sb?.ToString());
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (calculateIntermediateValue)
				return MatchWithCalculation(input, position, barrierPosition, ref furthestError);
			else
				return MatchWithoutCalculation(input, position, barrierPosition, ref furthestError);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string allowsEmpty = AllowsEmpty ? " allows empty" : " disallows empty";
			string consumesStop = ConsumeStopSequence ? " consumes stop" : " does not consumes stop";
			return $"escaped text {EscapingStrategy}{allowsEmpty}{consumesStop}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is EscapedTextTokenPattern other &&
				   Equals(EscapingStrategy, other.EscapingStrategy) &&
				   AllowsEmpty == other.AllowsEmpty &&
				   ConsumeStopSequence == other.ConsumeStopSequence;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + EscapingStrategy.GetHashCode();
			hashCode = hashCode * 397 + AllowsEmpty.GetHashCode();
			hashCode = hashCode * 397 + ConsumeStopSequence.GetHashCode();
			return hashCode;
		}
	}
}