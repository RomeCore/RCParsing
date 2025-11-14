using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches one of multiple patterns based on parser parameter.
	/// </summary>
	public class BuildableSwitchTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the selector function that determines which branch to take.
		/// </summary>
		public Func<object?, int> Selector { get; set; } = null!;

		/// <summary>
		/// Gets or sets the token patterns for the branches.
		/// </summary>
		public List<Or<string, BuildableTokenPattern>> Branches { get; set; } = new();

		/// <summary>
		/// Gets or sets the token pattern for default branch.
		/// </summary>
		public Or<string, BuildableTokenPattern> DefaultBranch { get; set; }

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			Branches.Append(DefaultBranch);

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			var branches = tokenChildren.Take(tokenChildren.Count - 1);
			var defaultBranch = tokenChildren[tokenChildren.Count - 1];
			return new SwitchTokenPattern(Selector, branches, defaultBranch);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSwitchTokenPattern other &&
				   Equals(Selector, other.Selector) &&
				   Branches.SequenceEqual(other.Branches) &&
				   Equals(DefaultBranch, other.DefaultBranch);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Selector.GetHashCode();
			hashCode = hashCode * 397 + Branches.GetSequenceHashCode();
			hashCode = hashCode * 397 + DefaultBranch.GetHashCode();
			return hashCode;
		}
	}
}