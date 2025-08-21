using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a repeat pattern.
	/// </summary>
	public class BuildableRepeatTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the child of this token pattern.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the minimum number of times the child pattern can be repeated.
		/// </summary>
		public int MinCount { get; set; } = 0;

		/// <summary>
		/// Gets or sets the maximum number of times the child pattern can be repeated. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; set; } = -1;

		/// <summary>
		/// Gets the children of this token pattern.
		/// </summary>
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Child.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new RepeatTokenPattern(tokenChildren[0], MinCount, MaxCount);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableRepeatTokenPattern other &&
				   Child == other.Child &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode ^= Child.GetHashCode() * 23;
			hashCode ^= MinCount.GetHashCode() * 29;
			hashCode ^= MaxCount.GetHashCode() * 31;
			return hashCode;
		}
	}
}