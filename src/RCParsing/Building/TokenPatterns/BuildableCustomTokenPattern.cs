using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a buildable custom token pattern.
	/// </summary>
	public class BuildableCustomTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the match function for this custom token pattern.
		/// </summary>
		public CustomTokenMatchFunction MatchFunction { get; set; }

		/// <summary>
		/// Gets or sets the string representation of custom token pattern.
		/// </summary>
		public string StringRepresentation { get; set; } = "custom";

		/// <summary>
		/// The children of the custom token pattern.
		/// </summary>
		public List<Or<string, BuildableTokenPattern>> Children { get; } = new List<Or<string, BuildableTokenPattern>>();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Children;

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new CustomTokenPattern(MatchFunction, tokenChildren, StringRepresentation);
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableCustomTokenPattern other &&
				   Children.SequenceEqual(other.Children) &&
				   MatchFunction == other.MatchFunction &&
				   StringRepresentation == other.StringRepresentation;
		}

		public override int GetHashCode()
		{
			int hashCode = 1930700721;
			hashCode = hashCode * -1521134295 + Children.GetSequenceHashCode();
			hashCode = hashCode * -1521134295 + (MatchFunction?.GetHashCode() ?? 0);
			hashCode = hashCode * -1521134295 + (StringRepresentation?.GetHashCode() ?? 0);
			return hashCode;
		}
	}
}