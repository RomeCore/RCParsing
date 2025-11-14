using System.Collections.Generic;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// Represents a token pattern that performs lookahead (positive or negative),
	/// without consuming any characters.
	/// </summary>
	public class LookaheadTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID to look ahead.
		/// </summary>
		public int Child { get; }

		/// <summary>
		/// Gets a value indicating whether this is a positive lookahead.
		/// </summary>
		public bool IsPositive { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="LookaheadTokenPattern"/> class.
		/// </summary>
		/// <param name="child">The child token pattern ID.</param>
		/// <param name="isPositive">Indicates whether this is a positive lookahead.</param>
		public LookaheadTokenPattern(int child, bool isPositive)
		{
			Child = child;
			IsPositive = isPositive;
		}

		protected override HashSet<char> FirstCharsCore => GetTokenPattern(Child).FirstChars;
		protected override bool IsFirstCharDeterministicCore => GetTokenPattern(Child).IsFirstCharDeterministic;
		protected override bool IsOptionalCore => true;



		private TokenPattern _pattern;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			_pattern = GetTokenPattern(Child);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			var result = _pattern.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue && IsPositive, ref furthestError);
			bool success = IsPositive ? result.success : !result.success;

			if (success)
				return new ParsedElement(position, 0, result.intermediateValue);
			else
				return ParsedElement.Fail;
		}



		public override string ToStringOverride(int remainingDepth = 2)
		{
			string prefix = IsPositive ? "&" : "!";
			if (remainingDepth <= 0)
				return $"{prefix}lookahead...";
			return $"{prefix}lookahead: {GetTokenPattern(Child).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is LookaheadTokenPattern pattern &&
				   Child == pattern.Child &&
				   IsPositive == pattern.IsPositive;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + IsPositive.GetHashCode();
			return hashCode;
		}
	}
}