using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches one of a set of literal strings in the input text, using a Trie for efficient lookup.
	/// </summary>
	/// <remarks>
	/// Passes a matched original literal <see cref="string"/> (not captured) as an intermediate value.
	/// For example, if pattern was choice of "HELLO" or "WORLD" with case-insensitive comparison,
	/// then the intermediate value would be "HELLO" or "WORLD", not "hello" or "world".
	/// </remarks>
	public class LiteralChoiceTokenPattern : TokenPattern
	{
		private readonly bool _comparerWasSet;
		private readonly Trie _root;

		/// <summary>
		/// Gets the set of literal strings to match.
		/// </summary>
		public IReadOnlyList<string> Literals { get; }
		
		/// <summary>
		/// Gets the set of literal strings to match mapped with intermediate values.
		/// </summary>
		public IReadOnlyList<KeyValuePair<string, object?>> LiteralsMap { get; }

		/// <summary>
		/// Gets the comparer used for matching.
		/// </summary>
		public StringComparer Comparer { get; }

		/// <summary>
		/// Gets the character comparer used for matching.
		/// </summary>
		public CharComparer CharComparer { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="literals">The collection of literal strings to match.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		public LiteralChoiceTokenPattern(IEnumerable<string> literals,
			StringComparer? comparer = null)
			: this(literals?.Select(l => new KeyValuePair<string, object?>(l, l)), comparer)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="literals">The collection of literal strings to match mapped with intermediate values.</param>
		/// <param name="comparer">The comparer to use for matching.</param>
		public LiteralChoiceTokenPattern(IEnumerable<KeyValuePair<string, object?>> literals,
			StringComparer? comparer = null)
		{
			if (literals == null)
				throw new ArgumentNullException(nameof(literals));

			Comparer = comparer ?? StringComparer.Ordinal;
			CharComparer = new CharComparer(Comparer);
			LiteralsMap = literals.Distinct().ToList().AsReadOnly();

			if (LiteralsMap.Count == 0)
				throw new ArgumentException("Literals collection is empty.", nameof(literals));
			if (LiteralsMap.Any(l => string.IsNullOrEmpty(l.Key)))
				throw new ArgumentException("One of literals is null or empty.", nameof(literals));

			Literals = LiteralsMap.Select(l => l.Key).ToList().AsReadOnly();

			_comparerWasSet = comparer != null;
			_root = new Trie(LiteralsMap, comparer.IsDefaultCaseSensitive() || comparer == null ? null : CharComparer);
		}

		protected override HashSet<char> FirstCharsCore => Comparer.IsDefaultCaseSensitive() ?
			new(Literals.Select(l => l[0])) :
			new(Literals.SelectMany(l => new char[] { char.ToLower(l[0]), char.ToUpper(l[0]) }));
		protected override bool IsFirstCharDeterministicCore => Comparer.IsNullOrDefault();
		protected override bool IsOptionalCore => false;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (_root.TryGetLongestMatch(input, position, barrierPosition, out var intermediateValue, out int matchedLength))
			{
				return new ParsedElement(position, matchedLength, intermediateValue);
			}

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Cannot match literal choice.", Id, true);
			return ParsedElement.Fail;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return $"literal choice '{string.Join("|", Literals)}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is LiteralChoiceTokenPattern other &&
				   Literals.SetEqual(other.Literals) &&
				   Equals(Comparer, other.Comparer);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Literals.GetSetHashCode();
			hashCode = hashCode * -1521134295 + Comparer.GetHashCode();
			return hashCode;
		}
	}
}