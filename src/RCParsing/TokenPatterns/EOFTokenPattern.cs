using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that matches the end of file.
	/// </summary>
	public class EOFTokenPattern : TokenPattern
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EOFTokenPattern"/> class.
		/// </summary>
		public EOFTokenPattern()
		{
		}

		protected override HashSet<char>? FirstCharsCore => null;



		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter)
		{
			if (position >= input.Length && position <= barrierPosition)
				return new ParsedElement(Id, input.Length, 0);

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
			return "[EOF]";
		}
	}
}