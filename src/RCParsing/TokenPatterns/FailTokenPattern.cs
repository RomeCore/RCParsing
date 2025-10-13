using System.Collections.Generic;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents an token pattern that always fails.
	/// </summary>
	public class FailTokenPattern : TokenPattern
	{
		/// <summary>
		/// Initializes a new instance of <see cref="FailTokenPattern"/> class.
		/// </summary>
		public FailTokenPattern()
		{
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Fail token triggered.", Id, true);
			return ParsedElement.Fail;
		}



		public override bool Equals(object obj)
		{
			return base.Equals(obj) &&
				   obj is FailTokenPattern;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "fail";
		}
	}
}