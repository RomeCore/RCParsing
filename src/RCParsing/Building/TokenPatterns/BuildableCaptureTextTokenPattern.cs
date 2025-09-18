using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches a single element
	/// and captures its matched substring as intermediate value,
	/// with optional trimming of characters from the start and end.
	/// </summary>
	public class BuildableCaptureTextTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern of the child that should be parsed.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the number of characters to trim from the start of the matched substring.
		/// </summary>
		public int TrimStart { get; set; }

		/// <summary>
		/// Gets or sets the number of characters to trim from the end of the matched substring.
		/// </summary>
		public int TrimEnd { get; set; }

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			Child.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new CaptureTextTokenPattern(tokenChildren[0], TrimStart, TrimEnd);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableCaptureTextTokenPattern other &&
				   Child == other.Child &&
				   TrimStart == other.TrimStart &&
				   TrimEnd == other.TrimEnd;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + TrimStart.GetHashCode();
			hashCode = hashCode * 397 + TrimEnd.GetHashCode();
			return hashCode;
		}
	}
}