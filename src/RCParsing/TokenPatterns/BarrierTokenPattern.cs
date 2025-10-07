using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents a virtual or barrier token pattern that used for matching a barrier token in a text.
	/// </summary>
	public sealed class BarrierTokenPattern : TokenPattern
	{
		/// <summary>
		/// The main alias for the virtual/barrier token.
		/// </summary>
		public string MainAlias { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BarrierTokenPattern"/> class.
		/// </summary>
		/// <param name="mainAlias">The main alias for the virtual/barrier token.</param>
		public BarrierTokenPattern(string mainAlias)
		{
			MainAlias = mainAlias;
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			throw new InvalidOperationException("This pattern is not meant to be used directly. Should be used from a parent rule (not token pattern).");
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return $"barrier '{MainAlias}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) && obj is BarrierTokenPattern other && MainAlias == other.MainAlias;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + MainAlias.GetHashCode();
			return hashCode;
		}
	}
}