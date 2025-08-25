using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Repesents a custom token pattern that can be used to match tokens in a text.
	/// </summary>
	public class CustomTokenPattern : TokenPattern
	{
		/// <summary>
		/// The function to use for matching the custom token pattern.
		/// </summary>
		public Func<CustomTokenPattern, string, int, int, object?, ParsedElement> MatchFunction { get; }

		/// <summary>
		/// Gets the string representation of this custom token pattern.
		/// </summary>
		public string StringRepresentation { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomTokenPattern"/> class.
		/// </summary>
		/// <param name="matchFunction">
		/// The function to use for matching the token pattern. <br/>
		/// Parameters: <br/>
		/// - <see cref="CustomTokenPattern"/>: The current custom token pattern. <br/>
		/// - <see cref="string"/>: The input text to match. <br/>
		/// - <see cref="int"/>: The position in the input text to start matching from. <br/>
		/// - <see cref="int"/>: The position in the input text to stop matching at. <br/>
		/// - <see cref="object"/>: The optional context parameter to that have been passed from parser. <br/>
		/// Returns: <br/>
		/// - <see cref="ParsedElement"/>: The parsed element containing the result of the match.
		/// </param>
		/// <param name="stringRepresentation">The string representation of this custom token pattern.</param>
		public CustomTokenPattern(Func<CustomTokenPattern, string, int, int, object?, ParsedElement> matchFunction,
			string stringRepresentation = "custom")
		{
			MatchFunction = matchFunction ?? throw new ArgumentNullException(nameof(matchFunction));
			StringRepresentation = stringRepresentation ?? throw new ArgumentNullException(nameof(stringRepresentation));
		}

		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter)
		{
			return MatchFunction(this, input, position, barrierPosition, parserParameter);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return StringRepresentation;
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) && 
				   obj is CustomTokenPattern other && 
				   MatchFunction == other.MatchFunction && 
				   StringRepresentation == other.StringRepresentation;
		}

		public override int GetHashCode()
		{
			var hc = base.GetHashCode();
			hc = (hc * 397) ^ MatchFunction.GetHashCode();
			hc = (hc * 397) ^ StringRepresentation.GetHashCode();
			return hc;
		}
	}
}