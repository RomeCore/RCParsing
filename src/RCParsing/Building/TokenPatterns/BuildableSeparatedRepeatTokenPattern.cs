using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	public class BuildableSeparatedRepeatTokenPattern : BuildableTokenPattern, IPassageFunctionHolder
	{
		/// <summary>
		/// Gets or sets the child pattern to repeat.
		/// </summary>
		public Or<string, BuildableTokenPattern> Child { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the separator pattern.
		/// </summary>
		public Or<string, BuildableTokenPattern> Separator { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the minimum number of repetitions.
		/// </summary>
		public int MinCount { get; set; } = 0;

		/// <summary>
		/// Gets or sets the maximum number of repetitions. -1 indicates no upper limit.
		/// </summary>
		public int MaxCount { get; set; } = -1;

		/// <summary>
		/// Gets or sets a value indicating whether trailing separator is allowed.
		/// </summary>
		public bool AllowTrailingSeparator { get; set; } = false;

		/// <summary>
		/// Gets or sets a value indicating whether separators should be included in the
		/// result intermediate values to provide them to passage function.
		/// </summary>
		public bool IncludeSeparatorsInResult { get; set; } = false;

		/// <summary>
		/// The function to pass the intermediate values from each pattern to the result intermediate value.
		/// </summary>
		public Func<IReadOnlyList<object?>, object?>? PassageFunction { get; set; } = null;

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren
			=> Child.WrapIntoEnumerable().Concat(Separator.WrapIntoEnumerable());

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new SeparatedRepeatTokenPattern(
				tokenChildren[0],
				tokenChildren[1],
				MinCount,
				MaxCount,
				AllowTrailingSeparator,
				IncludeSeparatorsInResult,
				PassageFunction);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableSeparatedRepeatTokenPattern other &&
				   Child == other.Child &&
				   Separator == other.Separator &&
				   MinCount == other.MinCount &&
				   MaxCount == other.MaxCount &&
				   AllowTrailingSeparator == other.AllowTrailingSeparator &&
				   IncludeSeparatorsInResult == other.IncludeSeparatorsInResult;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode() * 23;
			hashCode = hashCode * 397 + Separator.GetHashCode() * 29;
			hashCode = hashCode * 397 + MinCount.GetHashCode() * 31;
			hashCode = hashCode * 397 + MaxCount.GetHashCode() * 37;
			hashCode = hashCode * 397 + AllowTrailingSeparator.GetHashCode() * 41;
			hashCode = hashCode * 397 + IncludeSeparatorsInResult.GetHashCode() * 50;
			return hashCode;
		}
	}
}