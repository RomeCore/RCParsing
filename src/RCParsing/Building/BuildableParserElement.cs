using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a parser element that can be built. This is an abstract base class for both token patterns and rules.
	/// </summary>
	/// <remarks>
	/// Its recommended to implement the Equals and GetHashCode methods to remove redudancy when compiling parser.
	/// </remarks>
	public abstract class BuildableParserElement
	{
		/// <summary>
		/// Gets the rule children of this parser element. Each child can be name reference or a buildable parser rule.
		/// </summary>
		public abstract IEnumerable<Or<string, BuildableParserRule>>? RuleChildren { get; }

		/// <summary>
		/// Gets the token children of this parser element. Each child can be a name reference or a buildable token pattern.
		/// </summary>
		public abstract IEnumerable<Or<string, BuildableTokenPattern>>? TokenChildren { get; }

		/// <summary>
		/// Builds the parser element with the given children.
		/// </summary>
		/// <param name="ruleChildren">The rule children IDs to build the parser element with.</param>
		/// <param name="tokenChildren">The token children IDs to build the parser element with.</param>
		/// <returns>The built parser element.</returns>
		public abstract ParserElement Build(List<int>? ruleChildren, List<int>? tokenChildren);

		public override bool Equals(object? obj)
		{
			return obj is BuildableParserElement other;
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			return hashCode;
		}
	}
}