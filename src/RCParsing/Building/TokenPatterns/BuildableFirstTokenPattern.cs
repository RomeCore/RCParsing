using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches a sequence of two elements,
	/// passing the first element's intermediate value up.
	/// </summary>
	public class BuildableFirstTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern that should be parsed first,
		/// intermediate value of this element will be passed up.
		/// </summary>
		public Or<string, BuildableTokenPattern> First { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the token pattern that should be parsed second.
		/// </summary>
		public Or<string, BuildableTokenPattern> Second { get; set; } = string.Empty;

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			new[] { First, Second };

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new FirstTokenPattern(tokenChildren[0], tokenChildren[1]);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableFirstTokenPattern other &&
				   First == other.First &&
				   Second == other.Second;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + First.GetHashCode();
			hashCode = hashCode * 397 + Second.GetHashCode();
			return hashCode;
		}
	}
}