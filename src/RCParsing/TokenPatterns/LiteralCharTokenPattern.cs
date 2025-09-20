using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches a literal character in the input text.
	/// </summary>
	/// Passes a matched original literal <see cref="char"/> (not captured) as an intermediate value.
	/// For example, if pattern was 'H' with case-insensitive comparison,
	/// then the intermediate value would be 'H', not 'h'.
	public class LiteralCharTokenPattern : TokenPattern
	{
		readonly char[] charPool = new char[1];
		readonly object? boxedChar; // The character that has been boxed, so it can be passed as an object without boxing it again.

		/// <summary>
		/// The literal character to match.
		/// </summary>
		public char Literal { get; }

		/// <summary>
		/// Gets the string comparison type used for literal matching.
		/// </summary>
		public StringComparison Comparison { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralCharTokenPattern"/> class.
		/// </summary>
		/// <param name="literal">The literal character to match.</param>
		/// <param name="comparison">The string comparison type to use for literal matching.</param>
		public LiteralCharTokenPattern(char literal, StringComparison comparison = StringComparison.Ordinal)
		{
			Literal = literal;
			Comparison = comparison;
			charPool[0] = literal;
			boxedChar = literal;
		}

		protected override HashSet<char>? FirstCharsCore => new(new [] { Literal });



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			if (position + 1 > barrierPosition)
			{
				return ParsedElement.Fail;
			}

			if (Comparison == StringComparison.Ordinal)
			{
				if (Literal == input[position])
				{
					return new ParsedElement(position, 1, boxedChar);
				}
			}
			else
			{
				if (input.AsSpan(position, 1).Equals(charPool.AsSpan(), Comparison))
				{
					return new ParsedElement(position, 1, boxedChar);
				}
			}

			return ParsedElement.Fail;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return $"literal '{Literal}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is LiteralCharTokenPattern pattern &&
				   Literal == pattern.Literal &&
				   Comparison == pattern.Comparison;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Literal.GetHashCode();
			hashCode = hashCode * -1521134295 + Comparison.GetHashCode();
			return hashCode;
		}
	}
}