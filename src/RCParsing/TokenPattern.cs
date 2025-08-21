using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a pattern that can be used to match tokens in a text.
	/// </summary>
	public abstract class TokenPattern : ParserElement
	{
		/// <summary>
		/// Tries to match the given context with this pattern.
		/// </summary>
		/// <param name="input">The input text to match.</param>
		/// <param name="position">The position in the input text to start matching from.</param>
		/// <returns>The parsed element containing the result of the match.</returns>
		public abstract ParsedElement Match(string input, int position);

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is TokenPattern other;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			return hashCode;
		}
	}
}