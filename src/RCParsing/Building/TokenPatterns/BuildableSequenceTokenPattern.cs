using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a buildable sequence parser rule.
	/// </summary>
	public class BuildableSequenceTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// The elements of the sequence parser rule.
		/// </summary>
		public List<Or<string, BuildableTokenPattern>> Elements { get; } = new List<Or<string, BuildableTokenPattern>>();
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Elements;

		/// <summary>
		/// The function to pass the intermediate values from each pattern to the result intermediate value.
		/// </summary>
		public Func<IReadOnlyList<object?>, object?>? PassageFunction { get; set; } = null;

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new SequenceTokenPattern(tokenChildren, PassageFunction);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSequenceTokenPattern other &&
				   Elements.SequenceEqual(other.Elements) &&
				   Equals(PassageFunction, other.PassageFunction);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Elements.GetSequenceHashCode() * 23;
			hashCode = hashCode * 397 + PassageFunction?.GetHashCode() ?? 0 * 39;
			return hashCode;
		}
	}
}