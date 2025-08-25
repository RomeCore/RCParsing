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
		/// Gets the parsed value factory that will be used as the default parsed value factory for the parent rule.
		/// </summary>
		public Func<ParsedRuleResult, object?>? DefaultParsedValueFactory { get; set; } = null;

		/// <summary>
		/// Gets the local settings builder action that will be used as the default configuration action for the parent rule.
		/// </summary>
		public Action<ParserLocalSettingsBuilder>? DefaultConfigurationAction { get; set; } = null;

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
				   obj is BuildableTokenPattern other &&
				   Equals(DefaultParsedValueFactory, other.DefaultParsedValueFactory) &&
				   Equals(DefaultConfigurationAction, other.DefaultConfigurationAction);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + DefaultParsedValueFactory?.GetHashCode() * 23 ?? 0;
			hashCode = hashCode * 397 + DefaultConfigurationAction?.GetHashCode() * 23 ?? 0;
			return hashCode;
		}
	}
}