using System;
using System.Collections.Generic;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into an optional pattern.
	/// </summary>
	public class BuildableOptionalTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// The child of this token pattern.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Child.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new OptionalTokenPattern(tokenChildren[0]);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableOptionalTokenPattern other &&
				   Child == other.Child;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode() * 23;
			return hashCode;
		}
	}
}