using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a buildable token pattern that used for easy construction of custom token implementations.
	/// </summary>
	public class BuildableFactoryTokenPattern : BuildableTokenPattern
	{
		/// <summary>
		/// Gets or sets the factory function to create token pattern.
		/// </summary>
		public Func<List<int>, TokenPattern> Factory { get; set; }

		/// <summary>
		/// The child rules of the factory token.
		/// </summary>
		public List<Or<string, BuildableTokenPattern>> Children { get; } = new();

		public override IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren => Children;

		protected override TokenPattern BuildToken(List<int>? tokenChildren)
		{
			if (Factory == null)
				throw new NullReferenceException($"{nameof(Factory)} property is not set.");
			return Factory.Invoke(tokenChildren)
				?? throw new NullReferenceException($"{nameof(Factory)} returned a null token.");
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableFactoryTokenPattern other &&
				   Children.SequenceEqual(other.Children) &&
				   Factory == other.Factory;
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode = hashCode * 397 ^ Children.GetSequenceHashCode();
			hashCode = hashCode * 397 ^ (Factory?.GetHashCode() ?? 0);
			return hashCode;
		}
	}
}