using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches all text until the barrier position.
	/// Empty matches are allowed.
	/// </summary>
	public class AllTextTokenPattern : TokenPattern
	{
		/// <summary>
		/// Creates a new instance of the <see cref="AllTextTokenPattern"/> class.
		/// </summary>
		public AllTextTokenPattern()
		{
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			// Empty match is always allowed when position <= barrierPosition
			if (position <= barrierPosition)
			{
				int length = barrierPosition - position;
				return new ParsedElement(position, length);
			}

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Cannot match text until barrier, position exceeds the barrier position.", Id, true);
			return ParsedElement.Fail;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return "all text";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is AllTextTokenPattern;
		}

		public override int GetHashCode()
		{
			var hashCode = base.GetHashCode();
			return hashCode;
		}
	}
}