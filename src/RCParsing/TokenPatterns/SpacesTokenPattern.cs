using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches just spaces, the ' ' or '\t'
	/// </summary>
	public class SpacesTokenPattern : TokenPattern
	{
		/// <summary>
		/// Creates a new instance of the <see cref="SpacesTokenPattern"/> class.
		/// </summary>
		public SpacesTokenPattern()
		{
		}



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			int initialPosition = position;
			while (position < barrierPosition && (input[position] == ' ' || input[position] == '\t'))
			{
				position++;
			}

			if (initialPosition < position)
				return new ParsedElement(initialPosition, position - initialPosition);

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Cannot match spaces.", Id, true);
			return ParsedElement.Fail;
		}



		protected override HashSet<char>? FirstCharsCore => new(new[] { ' ', '\t' });

		public override string ToStringOverride(int remainingDepth)
		{
			return "spaces";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SpacesTokenPattern;
		}

		public override int GetHashCode()
		{
			var hashCode = base.GetHashCode();
			return hashCode;
		}
	}
}