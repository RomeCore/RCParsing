using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a recovery action that can be taken when an error occurs during parsing a specific rule.
	/// </summary>
	public struct ErrorRecovery : IEquatable<ErrorRecovery>
	{
		/// <summary>
		/// The recovery strategy that defines the behaviour.
		/// </summary>
		public ErrorRecoveryStrategy strategy;

		/// <summary>
		/// The rule ID that the parser should jump to after an error has been encountered.
		/// </summary>
		public int anchorRule;

		/// <summary>
		/// The rule ID that the parser should stop at after an error has been encountered.
		/// </summary>
		public int stopRule;

		public override bool Equals(object? obj)
		{
			return obj is ErrorRecovery other &&
				   Equals(other);
		}

		public bool Equals(ErrorRecovery other)
		{
			return strategy == other.strategy &&
				   anchorRule == other.anchorRule &&
				   stopRule == other.stopRule;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 397 + strategy.GetHashCode();
			hash = hash * 397 + anchorRule.GetHashCode();
			hash = hash * 397 + stopRule.GetHashCode();
			return hash;
		}

		public static bool operator ==(ErrorRecovery left, ErrorRecovery right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ErrorRecovery left, ErrorRecovery right)
		{
			return !left.Equals(right);
		}
	}
}