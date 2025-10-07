using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents an empty token pattern that always matches a zero count of characters,
	/// fails if position is equal or less compared to input length.
	/// </summary>
	public class EmptyTokenPattern : TokenPattern
	{
		/// <summary>
		/// Initializes a new instance of <see cref="EmptyTokenPattern"/> class.
		/// </summary>
		public EmptyTokenPattern()
		{
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (position <= barrierPosition)
				return new ParsedElement(position, 0);

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Cannot match empty token, position exceeds the barrier or end of input.", Id, true);
			return ParsedElement.Fail;
		}



		public override bool Equals(object obj)
		{
			return base.Equals(obj) &&
				   obj is EmptyTokenPattern;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "empty";
		}
	}
}