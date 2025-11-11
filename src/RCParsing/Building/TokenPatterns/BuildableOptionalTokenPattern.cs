using System;
using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
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

		/// <summary>
		/// The fallback intermadiate value that will be returned when child fails.
		/// </summary>
		public object? FallbackValue { get; set; }

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new OptionalTokenPattern(tokenChildren[0], FallbackValue);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableOptionalTokenPattern other &&
				   Child == other.Child &&
				   FallbackValue == other.FallbackValue;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + FallbackValue?.GetHashCode() ?? 0;
			return hashCode;
		}
	}
}