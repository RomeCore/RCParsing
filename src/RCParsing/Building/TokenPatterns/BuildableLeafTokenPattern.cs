using System;
using System.Collections.Generic;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that is a leaf node. This means it does not have any children.
	/// </summary>
	public sealed class BuildableLeafTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern to build.
		/// </summary>
		public TokenPattern TokenPattern { get; set; }

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => null;

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return TokenPattern ?? throw new ParserBuildingException("Token pattern cannot be null.");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableLeafTokenPattern other &&
				   Equals(TokenPattern, other.TokenPattern);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode ^= 23 * TokenPattern.GetHashCode();
			return hashCode;
		}
	}
}