using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that is optional. It can match either the wrapped token pattern or no tokens at all.
	/// </summary>
	public class OptionalTokenPattern : TokenPattern
	{
		/// <summary>
		/// The token pattern ID that this optional pattern wraps.
		/// </summary>
		public int TokenPattern { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionalTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternId">The token pattern ID that this optional pattern wraps.</param>
		public OptionalTokenPattern(int tokenPatternId)
		{
			TokenPattern = tokenPatternId;
		}

		protected override HashSet<char>? FirstCharsCore => null;



		private TokenPattern _pattern;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			_pattern = GetTokenPattern(TokenPattern);
		}



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			var token = _pattern.Match(input, position, barrierPosition, parserParameter, calculateIntermediateValue);
			if (token.success)
				return new ParsedElement(token.startIndex, token.length, token.intermediateValue);
			else
				return new ParsedElement(position, 0);
		}



		public override string ToStringOverride(int remainingDepth = 2)
		{
			if (remainingDepth <= 0)
				return "optional...";
			return $"optional: {GetTokenPattern(TokenPattern).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is OptionalTokenPattern pattern &&
				   TokenPattern == pattern.TokenPattern;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + TokenPattern.GetHashCode();
			return hashCode;
		}
	}
}