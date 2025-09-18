using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches a child pattern
	/// while skipping any whitespace characters before it.
	/// The intermediate value of the child pattern is passed up.
	/// </summary>
	public class BuildableSkipWhitespacesTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern that should be parsed after skipping whitespaces.
		/// </summary>
		public Or<string, BuildableTokenPattern> Pattern { get; set; } = string.Empty;

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			Pattern.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new SkipWhitespacesTokenPattern(tokenChildren[0]);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSkipWhitespacesTokenPattern other &&
				   Pattern == other.Pattern;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Pattern.GetHashCode();
			return hashCode;
		}
	}
}