using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches a newline ('\r\n', '\r' or '\n').
	/// </summary>
	public class NewlineTokenPattern : TokenPattern
	{
		/// <summary>
		/// Creates a new instance of the <see cref="NewlineTokenPattern"/> class.
		/// </summary>
		public NewlineTokenPattern()
		{
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter)
		{
			if (position < barrierPosition && input[position] == '\r')
			{
				int nextPos = position + 1;
				if (nextPos < barrierPosition && input[nextPos] == '\n')
					return new ParsedElement(position, 2);
				return new ParsedElement(position, 1);
			}

			return ParsedElement.Fail;
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "newline";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is NewlineTokenPattern;
		}

		public override int GetHashCode()
		{
			var hashCode = base.GetHashCode();
			return hashCode;
		}
	}
}