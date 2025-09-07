using System;
using System.Collections.Generic;
using System.Text;
using RCParsing.Building.ParserRules;

namespace RCParsing.Building
{
	public partial class RuleBuilder
	{
		/// <summary>
		/// Wraps the current rule into separated repeat rule that repeats at least one time.
		/// </summary>
		/// <param name="separatorBuilderAction">Action for creating separator.</param>
		/// <param name="allowTrailing">Whether to allow trailing separator.</param>
		/// <param name="includeSeparators">Whether to include separators in the AST.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder SeparatedBy(Action<RuleBuilder> separatorBuilderAction,
			bool allowTrailing = false, bool includeSeparators = false,
			Func<ParsedRuleResultBase, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new RuleBuilder(ParserBuilder);
			separatorBuilderAction(builder);

			if (!CanBeBuilt)
				throw new ParserBuildingException("Repeated child rule cannot be empty.");

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Separator child rule cannot be empty.");

			var prevRule = BuildingRule;
			BuildingRule = null;

			return Rule(new BuildableSeparatedRepeatParserRule
			{
				MinCount = 1,
				MaxCount = -1,
				AllowTrailingSeparator = allowTrailing,
				IncludeSeparatorsInResult = includeSeparators,
				Child = prevRule.Value,
				Separator = builder.BuildingRule.Value
			}, factory, config);
		}

		/// <summary>
		/// Wraps the current rule into separated repeat rule that repeats at least zero times.
		/// </summary>
		/// <param name="separatorBuilderAction">Action for creating separator.</param>
		/// <param name="allowTrailing">Whether to allow trailing separator.</param>
		/// <param name="includeSeparators">Whether to include separators in the AST.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder OptionallySeparatedBy(Action<RuleBuilder> separatorBuilderAction,
			bool allowTrailing = false, bool includeSeparators = false,
			Func<ParsedRuleResultBase, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new RuleBuilder(ParserBuilder);
			separatorBuilderAction(builder);

			if (!CanBeBuilt)
				throw new ParserBuildingException("Repeated child rule cannot be empty.");

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Separator child rule cannot be empty.");

			var prevRule = BuildingRule;
			BuildingRule = null;

			return Rule(new BuildableSeparatedRepeatParserRule
			{
				MinCount = 0,
				MaxCount = -1,
				AllowTrailingSeparator = allowTrailing,
				IncludeSeparatorsInResult = includeSeparators,
				Child = prevRule.Value,
				Separator = builder.BuildingRule.Value
			}, factory, config);
		}

		/// <summary>
		/// Adds a list of items separated by a specific literal to the current sequence.
		/// </summary>
		/// <remarks>
		/// Equivalent to: <c>ZeroOrMoreSeparated(itemBuilderAction, () => Literal(separator), ...)</c>
		/// </remarks>
		/// <param name="itemBuilderAction">The action to build the item rule.</param>
		/// <param name="separator">The literal separator.</param>
		/// <param name="allowTrailing">Whether to allow trailing separator.</param>
		/// <param name="includeSeparators">Whether to include separators in the AST.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder List(Action<RuleBuilder> itemBuilderAction, string separator = ",",
			bool allowTrailing = false, bool includeSeparators = false,
			Func<ParsedRuleResultBase, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return ZeroOrMoreSeparated(itemBuilderAction, b => b.Literal(separator), allowTrailing, includeSeparators, factory, config);
		}

		/// <summary>
		/// Adds a sequence of literals to the current sequence.
		/// </summary>
		/// <remarks>
		/// Equivalent to: <c>Literal(lit1).Literal(lit2).Literal(lit3)...</c>
		/// </remarks>
		/// <param name="literals">The literals to add. Each literal will be added as a separate token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Literals(params string[] literals)
		{
			foreach (var literal in literals)
				Literal(literal);
			return this;
		}
	}
}