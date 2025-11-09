using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a parser element (rule or token) that can be built. This is an abstract base class for both token patterns and rules.
	/// </summary>
	/// <remarks>
	/// Its recommended to implement the Equals and GetHashCode methods to remove redudancy when compiling parser.
	/// </remarks>
	public abstract class BuildableParserElement : BuildableParserElementBase<ParserElement>
	{
		/// <summary>
		/// Gets the parsed value factory associated with this parser element. <br/>
		/// For rules it will be applied directly; for tokens it will be applied to parent rule.
		/// </summary>
		public Func<ParsedRuleResultBase, object?>? ParsedValueFactory { get; set; } = null;

		/// <summary>
		/// Gets the local settings builder for this parser element. <br/>
		/// For rules it will be applied directly; for tokens it will be applied to parent rule.
		/// </summary>
		public ParserLocalSettingsBuilder Settings { get; } = new ParserLocalSettingsBuilder();

		/// <summary>
		/// Gets the error recovery builder for this parser element. <br/>
		/// For rules it will be applied directly; for tokens it will be applied to parent rule.
		/// </summary>
		public ErrorRecoveryStrategyBuilder ErrorRecovery { get; } = new ErrorRecoveryStrategyBuilder();

		public override IEnumerable<BuildableParserElementBase?>? ElementChildren =>
			new BuildableParserElementBase?[] { Settings, ErrorRecovery };

		/// <summary>
		/// Builds the parser element with the given children.
		/// </summary>
		/// <param name="ruleChildren">The rule children IDs to build the parser element with.</param>
		/// <param name="tokenChildren">The token children IDs to build the parser element with.</param>
		/// <returns>The built parser element.</returns>
		public abstract ParserElement Build(List<int>? ruleChildren, List<int>? tokenChildren);

		public override ParserElement BuildTyped(List<int>? ruleChildren,
			List<int>? tokenChildren, List<object?>? elementChildren)
		{
			var element = Build(ruleChildren, tokenChildren);

			var builtSettings = (ParserLocalSettings)elementChildren[0];
			var builtErrorRecovery = (ErrorRecoveryStrategy)elementChildren[1];

			if (element is ParserRule rule)
			{
				rule.ParsedValueFactory = ParsedValueFactory;
				rule.Settings = builtSettings;
				rule.ErrorRecovery = builtErrorRecovery;
			}
			else if (element is TokenPattern pattern)
			{
				pattern.DefaultParsedValueFactory = ParsedValueFactory;
				pattern.DefaultSettings = builtSettings;
				pattern.DefaultErrorRecovery = builtErrorRecovery;
			}

			return element;
		}

		public override bool Equals(object? obj)
		{
			return obj is BuildableParserElement other &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory) &&
				   Equals(Settings, other.Settings) &&
				   Equals(ErrorRecovery, other.ErrorRecovery);
		}

		public override int GetHashCode()
		{
			int hashCode = 397;
			hashCode = hashCode * 397 + ParsedValueFactory?.GetHashCode() ?? 0;
			hashCode = hashCode * 397 + Settings.GetHashCode();
			hashCode = hashCode * 397 + ErrorRecovery.GetHashCode();
			return hashCode;
		}
	}
}