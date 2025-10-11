using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a buildable token pattern for lookahead (positive or negative).
	/// </summary>
	public class BuildableLookaheadTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// The child of this token pattern.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether this is a positive lookahead.
		/// </summary>
		public bool IsPositive { get; set; } = true;

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Child.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new LookaheadTokenPattern(tokenChildren[0], IsPositive);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableLookaheadTokenPattern other &&
				   Child == other.Child &&
				   IsPositive == other.IsPositive;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode() * 23;
			hashCode = hashCode * 397 + IsPositive.GetHashCode();
			return hashCode;
		}
	}

}