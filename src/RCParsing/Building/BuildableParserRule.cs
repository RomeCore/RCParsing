using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a buildable parser rule. This is an abstract base class that represents a parser rule that can be built.
	/// </summary>
	/// <remarks>
	/// Its recommended to implement the Equals and GetHashCode methods to remove redudancy when compiling parser.
	/// </remarks>
	public abstract class BuildableParserRule : BuildableParserElement
	{
		/// <summary>
		/// Gets the parsed value factory associated with this rule.
		/// </summary>
		public Func<ParsedRuleResultBase, object?>? ParsedValueFactory { get; set; } = null;

		/// <summary>
		/// Gets the local settings builder for this parser rule.
		/// </summary>
		public ParserLocalSettingsBuilder Settings { get; } = new ParserLocalSettingsBuilder();

		/// <summary>
		/// Builds the parser rule with the given children.
		/// </summary>
		/// <param name="ruleChildren">The rule children IDs to build the parser rule with.</param>
		/// <param name="tokenChildren">The token children IDs to build the parser rule with.</param>
		/// <returns>The built parser rule.</returns>
		protected abstract ParserRule BuildRule(List<int>? ruleChildren, List<int>? tokenChildren);

		public sealed override ParserElement Build(List<int>? ruleChildren, List<int>? tokenChildren)
		{
			var rule = BuildRule(ruleChildren, tokenChildren);
			rule.ParsedValueFactory = ParsedValueFactory;
			return rule;
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BuildableParserRule other &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + (ParsedValueFactory?.GetHashCode() ?? 0) * 23;
			return hashCode;
		}
	}
}