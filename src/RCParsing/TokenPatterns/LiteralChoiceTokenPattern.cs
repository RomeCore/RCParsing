using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		public ImmutableHashSet<string> Literals { get; }

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
		public LiteralChoiceTokenPattern(IEnumerable<string> literals, StringComparer? comparer = null)
		{
			if (literals == null)
				throw new ArgumentNullException(nameof(literals));

			Comparer = comparer ?? StringComparer.Ordinal;
			CharComparer = new CharComparer(Comparer);
			Literals = ImmutableHashSet.CreateRange(Comparer, literals);

			_comparerWasSet = comparer != null;
			_root = new Trie(Literals.Select(l => new KeyValuePair<string, object?>(l, l)), _comparerWasSet ? CharComparer : null);
		}

		protected override HashSet<char>? FirstCharsCore => _comparerWasSet ? null :
			new (Literals.Select(l => l[0]).Distinct());



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (_root.TryGetLongestMatch(input, position, barrierPosition, out var matchedLiteral, out int matchedLength))
			{
				return new ParsedElement(position, matchedLength, matchedLiteral);
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
				   Literals.SetEquals(other.Literals) &&
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