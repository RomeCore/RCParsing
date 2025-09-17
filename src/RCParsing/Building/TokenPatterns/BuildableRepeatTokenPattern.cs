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
	public class BuildableRepeatTokenPattern : BuildableTokenPattern, IPassageFunctionHolder
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
		/// The function to pass the intermediate values from each pattern to the result intermediate value.
		/// </summary>
		public Func<IReadOnlyList<object?>, object?>? PassageFunction { get; set; } = null;

		/// <summary>
		/// Gets the children of this token pattern.
		/// </summary>
		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Child.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new RepeatTokenPattern(tokenChildren[0], MinCount, MaxCount, PassageFunction);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableRepeatTokenPattern other &&
				   Child == other.Child &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount &&
				   Equals(PassageFunction, other.PassageFunction);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode() * 23;
			hashCode = hashCode * 397 + MinCount.GetHashCode() * 29;
			hashCode = hashCode * 397 + MaxCount.GetHashCode() * 31;
			hashCode = hashCode * 397 + (PassageFunction?.GetHashCode() ?? 0) * 37;
			return hashCode;
		}
	}
}