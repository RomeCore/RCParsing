using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that matches the end of file or the some barrier.
	/// </summary>
	public class EOFTokenPattern : TokenPattern
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EOFTokenPattern"/> class.
		/// </summary>
		public EOFTokenPattern()
		{
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (position >= barrierPosition)
				return new ParsedElement(barrierPosition, 0);

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Cannot match EOF.", Id, true);
			return ParsedElement.Fail;
		}



		public override bool Equals(object obj)
		{
			return base.Equals(obj) &&
				   obj is EOFTokenPattern;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "end of file";
		}
	}
}