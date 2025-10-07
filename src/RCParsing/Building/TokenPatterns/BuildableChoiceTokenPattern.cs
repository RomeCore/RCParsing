using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a choice of multiple patterns.
	/// </summary>
	public class BuildableChoiceTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the child element selection behaviour of this choice token pattern.
		/// </summary>
		public ChoiceMode Mode { get; set; }

		/// <summary>
		/// The choices of this token pattern.
		/// </summary>
		public List<Or<string, BuildableTokenPattern>> Choices { get; } = new List<Or<string, BuildableTokenPattern>>();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Choices;

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new ChoiceTokenPattern(Mode, tokenChildren);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableChoiceTokenPattern other &&
				   Choices.SequenceEqual(other.Choices);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Mode.GetHashCode() * 23;
			hashCode = hashCode * 397 + Choices.GetSequenceHashCode() * 23;
			return hashCode;
		}
	}
}