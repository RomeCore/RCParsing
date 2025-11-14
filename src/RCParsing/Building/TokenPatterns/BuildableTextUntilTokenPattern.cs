using System.Collections.Generic;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that can be built into a pattern that matches text until the child pattern is found.
	/// </summary>
	public class BuildableTextUntilTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the token pattern of the stop condition.
		/// </summary>
		public Or<string, BuildableTokenPattern> StopPattern { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets whether empty matches are allowed.
		/// </summary>
		public bool AllowEmpty { get; set; } = true;

		/// <summary>
		/// Gets or sets whether to consume the stop pattern.
		/// </summary>
		public bool ConsumeStop { get; set; } = false;

		/// <summary>
		/// Gets or sets whether to fail when end of input is reached.
		/// </summary>
		public bool FailOnEof { get; set; } = false;

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren =>
			StopPattern.WrapIntoEnumerable();

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			return new TextUntilTokenPattern(tokenChildren[0], AllowEmpty, ConsumeStop, FailOnEof);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableTextUntilTokenPattern other &&
				   StopPattern == other.StopPattern &&
				   AllowEmpty == other.AllowEmpty &&
				   ConsumeStop == other.ConsumeStop &&
				   FailOnEof == other.FailOnEof;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + StopPattern.GetHashCode();
			hashCode = hashCode * 397 + AllowEmpty.GetHashCode();
			hashCode = hashCode * 397 + ConsumeStop.GetHashCode();
			hashCode = hashCode * 397 + FailOnEof.GetHashCode();
			return hashCode;
		}
	}
}