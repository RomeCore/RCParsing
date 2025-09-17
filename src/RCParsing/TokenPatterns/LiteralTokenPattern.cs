using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches a literal string in the input text.
	/// </summary>
	/// Passes a matched original literal <see cref="string"/> (not captured) as an intermediate value.
	/// For example, if pattern was "HELLO" with case-insensitive comparison,
	/// then the intermediate value would be "HELLO", not "hello".
	public class LiteralTokenPattern : TokenPattern
	{
		/// <summary>
		/// The literal string to match.
		/// </summary>
		public string Literal { get; }

		/// <summary>
		/// Gets the string comparison type used for literal matching.
		/// </summary>
		public StringComparison Comparison { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LiteralTokenPattern"/> class.
		/// </summary>
		/// <param name="literal">The literal string to match.</param>
		/// <param name="comparison">The string comparison type to use for literal matching.</param>
		public LiteralTokenPattern(string literal, StringComparison comparison = StringComparison.Ordinal)
		{
			Literal = string.IsNullOrEmpty(literal)
				? throw new ArgumentException("Literal cannot be null or empty.", nameof(literal))
				: literal;
			Comparison = comparison;
		}

		protected override HashSet<char>? FirstCharsCore => Comparison != StringComparison.Ordinal ? null :
			new(new [] { Literal[0] });



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			if (position + Literal.Length > barrierPosition)
				return ParsedElement.Fail;

			if (input.AsSpan(position, Literal.Length).Equals(Literal.AsSpan(), Comparison))
				return new ParsedElement(position, Literal.Length, Literal);

			return ParsedElement.Fail;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return $"literal '{Literal}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is LiteralTokenPattern pattern &&
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