using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches a single element,
	/// but fails if the condition function returns true for the intermediate value.
	/// </summary>
	public class BuildableFailIfTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern of the child that should be parsed.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; }

		/// <summary>
		/// Gets or sets the condition function that determines if the match should fail.
		/// </summary>
		public Func<object?, bool> Condition { get; set; } = null!;

		/// <summary>
		/// Gets or sets the error message to use when the condition fails.
		/// </summary>
		public string ErrorMessage { get; set; } = "Condition failed.";

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			Child.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new FailIfTokenPattern(tokenChildren[0], Condition, ErrorMessage);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableFailIfTokenPattern other &&
				   Child == other.Child &&
				   Equals(Condition, other.Condition) &&
				   ErrorMessage == other.ErrorMessage;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + Condition.GetHashCode();
			hashCode = hashCode * 397 + ErrorMessage.GetHashCode();
			return hashCode;
		}
	}
}