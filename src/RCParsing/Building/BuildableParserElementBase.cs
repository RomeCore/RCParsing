using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a base class for all buildable parser elements, including tokens, rules, skip strategies and error recovery strategies.
	/// </summary>
	public abstract class BuildableParserElementBase
	{
		/// <summary>
		/// Gets the rule children of this parser element. Each child can be name reference or a buildable parser rule.
		/// </summary>
		public virtual IEnumerable<Or<string, BuildableParserRule>?>? RuleChildren => null;

		/// <summary>
		/// Gets the token children of this parser element. Each child can be a name reference or a buildable token pattern.
		/// </summary>
		public virtual IEnumerable<Or<string, BuildableTokenPattern>?>? TokenChildren => null;

		/// <summary>
		/// Gets the element children of this parser element.
		/// </summary>
		public virtual IEnumerable<BuildableParserElementBase?>? ElementChildren => null;

		/// <summary>
		/// Builds the parser element with the given children.
		/// </summary>
		/// <param name="ruleChildren">The rule children IDs paired with built rule to build the parser element with.</param>
		/// <param name="tokenChildren">The token children IDs paired with built token to build the parser element with.</param>
		/// <param name="elementChildren">The elements  to build the parser element with.</param>
		/// <returns>The built parser element.</returns>
		public abstract object? Build(List<(int, ParserRule?)>? ruleChildren,
			List<(int, TokenPattern?)>? tokenChildren, List<object?>? elementChildren);
	}
}