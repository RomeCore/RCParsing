using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches a single element,
	/// always returning a fixed intermediate value.
	/// </summary>
	public class BuildableReturnTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern of the child that should be parsed.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the fixed intermediate value that will always be passed up upon successful match.
		/// </summary>
		public object? Value { get; set; }

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			Child.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new ReturnTokenPattern(tokenChildren[0], Value);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableReturnTokenPattern other &&
				   Child == other.Child &&
				   Equals(Value, other.Value);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + (Value?.GetHashCode() ?? 0);
			return hashCode;
		}
	}
}