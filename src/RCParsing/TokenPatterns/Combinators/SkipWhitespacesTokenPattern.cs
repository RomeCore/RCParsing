using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches a child pattern while skipping any whitespace characters
	/// before it. The intermediate value of the child pattern is passed up.
	/// </summary>
	public class SkipWhitespacesTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID that should be parsed after skipping whitespaces.
		/// </summary>
		public int Child { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="SkipWhitespacesTokenPattern"/> class.
		/// </summary>
		/// <param name="child">The token pattern ID that should be parsed after skipping whitespaces.</param>
		public SkipWhitespacesTokenPattern(int child)
		{
			Child = child;
		}

		protected override HashSet<char> FirstCharsCore => new(GetTokenPattern(Child).FirstChars
			.Concat(new char[] { ' ', '\t', '\r', '\n' }));
		protected override bool IsFirstCharDeterministicCore => GetTokenPattern(Child).IsFirstCharDeterministic;
		protected override bool IsOptionalCore => GetTokenPattern(Child).IsOptional;



		private TokenPattern _pattern = null!;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);
			_pattern = GetTokenPattern(Child);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			var initialPosition = position;

			// Skip any whitespace characters
			while (position < barrierPosition && char.IsWhiteSpace(input[position]))
				position++;

			// Match the child pattern at the new position
			var result = _pattern.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue, ref furthestError);

			if (!result.success)
				return ParsedElement.Fail;

			// Calculate the total length including skipped whitespace
			var totalLength = (result.startIndex + result.length) - initialPosition;

			return new ParsedElement(initialPosition, totalLength, result.intermediateValue);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "skipWhitespaces...";

			return $"skipWhitespaces:\n" +
				   GetTokenPattern(Child).ToString(remainingDepth - 1).Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SkipWhitespacesTokenPattern pattern &&
				   Child == pattern.Child;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			return hashCode;
		}
	}
}