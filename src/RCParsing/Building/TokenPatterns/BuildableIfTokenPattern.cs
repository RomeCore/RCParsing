using System;
using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that conditionally matches one of two patterns based on parser parameter.
	/// </summary>
	public class BuildableIfTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the condition function that determines which branch to take.
		/// </summary>
		public Func<object?, bool> Condition { get; set; } = null!;

		/// <summary>
		/// Gets or sets the token pattern for the true branch.
		/// </summary>
		public Or<string, BuildableTokenPattern> TrueBranch { get; set; }

		/// <summary>
		/// Gets or sets the token pattern for the false branch.
		/// </summary>
		public Or<string, BuildableTokenPattern> FalseBranch { get; set; }

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			new[] { TrueBranch, FalseBranch };

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new IfTokenPattern(Condition, tokenChildren[0], tokenChildren[1]);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableIfTokenPattern other &&
				   Equals(Condition, other.Condition) &&
				   TrueBranch == other.TrueBranch &&
				   FalseBranch == other.FalseBranch;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Condition.GetHashCode();
			hashCode = hashCode * 397 + TrueBranch.GetHashCode();
			hashCode = hashCode * 397 + FalseBranch.GetHashCode();
			return hashCode;
		}
	}
}