using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// The match function used for matching the <see cref="CustomTokenPattern"/>.
	/// </summary>
	/// <param name="self">The current custom token pattern.</param>
	/// <param name="input">The input text to match.</param>
	/// <param name="start">The position in the input text to start matching from.</param>
	/// <param name="end">The position in the input text to stop matching at.</param>
	/// <param name="parameter">The optional context parameter to that have been passed from parser.</param>
	/// <param name="calculateIntermediateValue">Whether to calculate intermediate value.</param>
	/// <param name="furthestError">The furthest error encountered during parsing.</param>
	/// <param name="children">The child token patterns that was specified during token construction.</param>
	/// <returns>The <see cref="ParsedElement"/> result of matching token.</returns>
	public delegate ParsedElement CustomTokenMatchFunction(CustomTokenPattern self, string input,
		int start, int end, object? parameter, bool calculateIntermediateValue,
		ref ParsingError furthestError, TokenPattern[] children);

	/// <summary>
	/// Repesents a custom token pattern that can be used to match tokens in a text.
	/// </summary>
	public class CustomTokenPattern : TokenPattern
	{
		/// <summary>
		/// The function to use for matching the custom token pattern.
		/// </summary>
		public CustomTokenMatchFunction MatchFunction { get; }

		/// <summary>
		/// The child token patterns.
		/// </summary>
		public IReadOnlyList<int> Children { get; }

		/// <summary>
		/// Gets the string representation of this custom token pattern.
		/// </summary>
		public string StringRepresentation { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomTokenPattern"/> class.
		/// </summary>
		/// <param name="matchFunction">The function to use for matching the token pattern.</param>
		/// <param name="childrenIds">IDs of child token patterns.</param>
		/// <param name="stringRepresentation">The string representation of this custom token pattern.</param>
		public CustomTokenPattern(CustomTokenMatchFunction matchFunction,
			IEnumerable<int> childrenIds, string stringRepresentation = "custom")
		{
			MatchFunction = matchFunction ?? throw new ArgumentNullException(nameof(matchFunction));
			Children = childrenIds?.ToArray().AsReadOnlyList() ?? throw new ArgumentNullException(nameof(childrenIds));
			StringRepresentation = stringRepresentation ?? throw new ArgumentNullException(nameof(stringRepresentation));
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		private TokenPattern[] _children;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);

			_children = Children.Select(i => GetTokenPattern(i)).ToArray();
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			return MatchFunction(this, input, position, barrierPosition, parserParameter,
				calculateIntermediateValue, ref furthestError, _children);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (Children.Count == 0)
				return StringRepresentation;
			else
				return $"{StringRepresentation}\n{string.Join(Environment.NewLine, Children.Select(i => GetTokenPattern(i).ToString(remainingDepth - 1))).Indent("  ")}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) && 
				   obj is CustomTokenPattern other && 
				   MatchFunction == other.MatchFunction &&
				   Children.SequenceEqual(other.Children) && 
				   StringRepresentation == other.StringRepresentation;
		}

		public override int GetHashCode()
		{
			var hc = base.GetHashCode();
			hc = (hc * 397) ^ MatchFunction.GetHashCode();
			hc = (hc * 397) ^ Children.GetSequenceHashCode();
			hc = (hc * 397) ^ StringRepresentation.GetHashCode();
			return hc;
		}
	}
}