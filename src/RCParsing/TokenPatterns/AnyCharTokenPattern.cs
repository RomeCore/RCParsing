using System.Collections.Generic;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents an any character token pattern that matches any single character and returns it as intermediate value.
	/// </summary>
	public class AnyCharTokenPattern : TokenPattern
	{
		/// <summary>
		/// Initializes a new instance of <see cref="AnyCharTokenPattern"/> class.
		/// </summary>
		public AnyCharTokenPattern()
		{
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => true;
		protected override bool IsOptionalCore => false;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (position >= barrierPosition)
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, "Cannot match any char token, position exceeds the barrier or end of input.", Id, true);
				return ParsedElement.Fail;
			}

			return new ParsedElement(position, 1, calculateIntermediateValue ? input[position] : null);
		}



		public override bool Equals(object obj)
		{
			return base.Equals(obj) &&
				   obj is AnyCharTokenPattern;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "anychar";
		}
	}
}