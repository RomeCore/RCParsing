using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches a sequence of three elements,
	/// passing middle element's intermediate value up.
	/// </summary>
	public class BuildableBetweenTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern that should be parsed first.
		/// </summary>
		public Or<string, BuildableTokenPattern> First { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the middle token pattern that will be parsed second,
		/// intermediate value of this element will be passed up.
		/// </summary>
		public Or<string, BuildableTokenPattern> Middle { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the token pattern that should be parsed last.
		/// </summary>
		public Or<string, BuildableTokenPattern> Last { get; set; } = string.Empty;

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			new[] { First, Middle, Last };

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new BetweenTokenPattern(tokenChildren[0], tokenChildren[1], tokenChildren[2]);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableBetweenTokenPattern other &&
				   First == other.First &&
				   Middle == other.Middle &&
				   Last == other.Last;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + First.GetHashCode();
			hashCode = hashCode * 397 + Middle.GetHashCode();
			hashCode = hashCode * 397 + Last.GetHashCode();
			return hashCode;
		}
	}
}