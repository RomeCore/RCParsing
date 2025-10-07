using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// The token pattern that matches one or more whitespace characters (' ', '\t', '\r' or '\n').
	/// </summary>
	public class WhitespacesTokenPattern : TokenPattern
	{
		/// <summary>
		/// Creates a new instance of the <see cref="WhitespacesTokenPattern"/> class.
		/// </summary>
		public WhitespacesTokenPattern()
		{
		}

		protected override HashSet<char>? FirstCharsCore => new(new[] { ' ', '\t', '\n', '\r' });
		protected override bool IsFirstCharDeterministicCore => true;
		protected override bool IsOptionalCore => false;


		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			int initialPosition = position;
			while (position < barrierPosition && char.IsWhiteSpace(input[position]))
				position++;

			if (initialPosition < position)
				return new ParsedElement(initialPosition, position - initialPosition);

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Cannot match whitespaces.", Id, true);
			return ParsedElement.Fail;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return "whitespaces";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is WhitespacesTokenPattern;
		}

		public override int GetHashCode()
		{
			var hashCode = base.GetHashCode();
			return hashCode;
		}
	}
}