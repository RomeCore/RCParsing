using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// The buildable token pattern. This is an abstract class that represents a token pattern that can be built into a token.
	/// </summary>
	/// <remarks>
	/// Its recommended to implement the Equals and GetHashCode methods to remove redudancy when compiling parser.
	/// </remarks>
	public abstract class BuildableTokenPattern : BuildableParserElement
	{
		public sealed override IEnumerable<Or<string, BuildableParserRule>>? RuleChildren => null;

		/// <summary>
		/// Builds the token pattern with the given children.
		/// </summary>
		/// <param name="tokenChildren">The token children IDs to build the parser element with.</param>
		/// <returns>The built token pattern.</returns>
		protected abstract TokenPattern BuildToken(List<int>? tokenChildren);

		public override ParserElement Build(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			var token = BuildToken(tokenChildren);
			return token;
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableTokenPattern;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			return hashCode;
		}
	}
}