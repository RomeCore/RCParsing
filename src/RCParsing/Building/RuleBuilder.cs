using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.Building.ParserRules;
using RCParsing.Building.TokenPatterns;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a builder for constructing rules that are used in parsing processes.
	/// </summary>
	/// <remarks>
	/// Also can be easily used for creating sugar methods.
	/// </remarks>
	public partial class RuleBuilder : ParserElementBuilder<RuleBuilder>
	{
		private object? DefaultFactory_Sequence(ParsedRuleResultBase r) => r[0].Value;
		private object? DefaultFactory_Optional(ParsedRuleResultBase r) => r.Count > 0 ? r[0].Value : null;
		private object? DefaultFactory_Repeat(ParsedRuleResultBase r) => r.SelectArray();
		private object? DefaultFactory_Choice(ParsedRuleResultBase r) => r[0].Value;
		private object? DefaultFactory_RepeatSeparated(ParsedRuleResultBase r) => r.SelectArray();
		private static object? DefaultFactory_Token(ParsedRuleResultBase r) => r.IntermediateValue;

		/// <summary>
		/// Initializes a new instance of <see cref="RuleBuilder"/> class.
		/// </summary>
		public RuleBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="RuleBuilder"/> class.
		/// </summary>
		/// <param name="parserBuilder">The master parser builder associated with this rule builder.</param>
		public RuleBuilder(ParserBuilder? parserBuilder) : base(parserBuilder)
		{
		}

		/// <summary>
		/// Gets or sets the rule being built. Or the named reference to rule.
		/// </summary>
		public Or<string, BuildableParserRule>? BuildingRule { get; set; }

		public override bool CanBeBuilt => BuildingRule.HasValue;

		protected override RuleBuilder GetThis() => this;

		/// <summary>
		/// Adds a token (name or child pattern) to the current sequence.
		/// </summary>
		/// <param name="childToken">The token to add. Can be a name or a child pattern.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Token(Or<string, BuildableTokenPattern> childToken,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			if (childToken.VariantIndex == 1)
				factory ??= childToken.Value2.DefaultParsedValueFactory ?? DefaultFactory_Token;
			return Rule(new BuildableTokenParserRule
			{
				Child = childToken
			}, factory, config);
		}

		public override RuleBuilder Token(TokenPattern token, Func<ParsedRuleResultBase, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Token(new BuildableLeafTokenPattern
			{
				TokenPattern = token
			}, factory, config);
		}

		/// <summary>
		/// Adds a child token to the current sequence.
		/// </summary>
		/// <param name="token">The child token to add.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Token(BuildableTokenPattern token, Func<ParsedRuleResultBase, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Token(new Or<string, BuildableTokenPattern>(token), factory, config);
		}

		/// <summary>
		/// Adds a rule to the current sequence.
		/// </summary>
		/// <param name="childRule">The rule to add. Can be a name or a child pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Rule(Or<string, BuildableParserRule> childRule)
		{
			if (!BuildingRule.HasValue)
			{
				BuildingRule = childRule;
			}
			else if (BuildingRule.Value.VariantIndex == 1 &&
					BuildingRule.Value.AsT2() is BuildableSequenceParserRule sequenceRule)
			{
				sequenceRule.Elements.Add(childRule);
			}
			else
			{
				var newSequence = new BuildableSequenceParserRule();
				newSequence.ParsedValueFactory = DefaultFactory_Sequence;
				newSequence.Elements.Add(BuildingRule.Value);
				newSequence.Elements.Add(childRule);
				BuildingRule = newSequence;
			}
			return this;
		}

		/// <summary>
		/// Adds a rule to the current sequence.
		/// </summary>
		/// <param name="childRule">The rule to add. Can be a name or a child pattern.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Rule(BuildableParserRule childRule,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			childRule.ParsedValueFactory = factory;
			config?.Invoke(childRule.Settings);
			return Rule(new Or<string, BuildableParserRule>(childRule));
		}

		/// <summary>
		/// Adds a rule to the current sequence.
		/// </summary>
		/// <param name="ruleName">The name of the rule to add.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Rule(string ruleName)
		{
			return Rule(new Or<string, BuildableParserRule>(ruleName));
		}

		/// <summary>
		/// Adds a rule to the current sequence.
		/// </summary>
		/// <param name="builderAction">The action to build the rule.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Rule(Action<RuleBuilder> builderAction, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Builder action did not add any rules.");

			if (builder.BuildingRule.Value.VariantIndex == 0)
			{
				if (factory != null)
					throw new ParserBuildingException("Cannot apply parsed value factory to a direct rule reference.");
				if (config != null)
					throw new ParserBuildingException("Cannot apply configuration to a direct rule reference.");

				return Rule(builder.BuildingRule.Value.Value1);
			}
			else
				return Rule(builder.BuildingRule.Value.Value2, factory, config);
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="tokenName">The name of the token to add.</param>
		/// <returns>Current instance for method chaining.</returns>
		public override RuleBuilder Token(string tokenName)
		{
			return Token(tokenName, null, null);
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="tokenName">The name of the token to add.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Token(string tokenName, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Token(new Or<string, BuildableTokenPattern>(tokenName), factory, config);
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="builderAction">The token pattern builder.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public RuleBuilder Token(Action<TokenBuilder> builderAction, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Builder action did not add any tokens.");

			return Token(builder.BuildingPattern.Value, factory, config);
		}

		/// <summary>
		/// Converts the pattern into a sequence if it is not already one.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder ToSequence()
		{
			if (!BuildingRule.HasValue)
			{
				throw new ParserBuildingException("Cannot convert empty rule to sequence.");
			}
			else if (BuildingRule.Value.VariantIndex != 1 ||
					BuildingRule.Value.AsT2() is not BuildableSequenceParserRule)
			{
				var newSequence = new BuildableSequenceParserRule();
				newSequence.ParsedValueFactory = DefaultFactory_Sequence;
				newSequence.Elements.Add(BuildingRule.Value);
				BuildingRule = newSequence;
			}
			return this;
		}

		/// <summary>
		/// Sets the transformation function to the current sequence rule.
		/// </summary>
		/// <param name="factory">The transformation function (parsed value factory) to set.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the parser rule is not set or it is a direct reference to a named rule.</exception>
		public RuleBuilder Transform(Func<ParsedRuleResultBase, object?>? factory)
		{
			if (BuildingRule?.AsT2() is BuildableParserRule rule)
				rule.ParsedValueFactory = factory;
			else
				throw new ParserBuildingException("Parser rule is not set or it is a direct reference to named rule.");
			return this;
		}

		/// <summary>
		/// Sets the transformation function to the last rule in sequence or for the current rule.
		/// </summary>
		/// <param name="factory">The transformation function (parsed value factory) to set.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the parser rule is not set or sequence rule is empty or it is a direct reference to a named rule.</exception>
		public RuleBuilder TransformLast(Func<ParsedRuleResultBase, object?>? factory)
		{
			if (BuildingRule?.AsT2() is BuildableSequenceParserRule sequenceRule)
			{
				if (sequenceRule.Elements.LastOrDefault().AsT2() is BuildableParserRule rule)
					rule.ParsedValueFactory = factory;
				else
					throw new ParserBuildingException("Last rule in the sequence is not set or it is a direct reference to named rule.");
				return this;
			}
			else if (BuildingRule?.AsT2() is BuildableParserRule rule)
				rule.ParsedValueFactory = factory;
			else
				throw new ParserBuildingException("Parser rule is not set or it is a direct reference to named rule.");
			return this;
		}

		/// <summary>
		/// Configures the local settings for the current sequence rule.
		/// </summary>
		/// <param name="configAction">The configuration action.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the parser rule is not set or it is a direct reference to a named rule.</exception>
		public RuleBuilder Configure(Action<ParserLocalSettingsBuilder> configAction)
		{
			if (BuildingRule?.AsT2() is BuildableParserRule rule)
				configAction(rule.Settings);
			else
				throw new ParserBuildingException("Parser rule is not set or it is a direct reference to named rule.");
			return this;
		}

		/// <summary>
		/// Configures the local settings for the last rule in the sequence.
		/// </summary>
		/// <param name="configAction">The configuration action.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the parser rule is not set or sequence rule is empty or it is a direct reference to a named rule.</exception>
		public RuleBuilder ConfigureLast(Action<ParserLocalSettingsBuilder> configAction)
		{
			if (BuildingRule?.AsT2() is BuildableSequenceParserRule sequenceRule)
			{
				if (sequenceRule.Elements.LastOrDefault().AsT2() is BuildableParserRule rule)
					configAction(rule.Settings);
				else
					throw new ParserBuildingException("Last rule in the sequence is not set or it is a direct reference to named rule.");
				return this;
			}
			else if (BuildingRule?.AsT2() is BuildableParserRule rule)
				configAction(rule.Settings);
			else
				throw new ParserBuildingException("Parser rule is not set or it is a direct reference to named rule.");
			return this;
		}

		/// <summary>
		/// Configures the local settings for the current sequence rule to skip parsing and ignore errors.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the parser rule is not set or it is a direct reference to a named rule.</exception>
		public RuleBuilder ConfigureForSkip()
		{
			return Configure(c => c.NoSkipping().IgnoreErrors());
		}

		/// <summary>
		/// Adds an optional rule to the current sequence.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the child rule.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Optional(Action<RuleBuilder> builderAction, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Optional child rule cannot be empty.");

			return Rule(new BuildableOptionalParserRule
			{
				Child = builder.BuildingRule.Value
			}, factory ?? DefaultFactory_Optional, config);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="max">The maximum number of times the rule can be repeated. -1 indicates no upper limit.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Repeat(Action<RuleBuilder> builderAction, int min, int max, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child rule cannot be empty.");

			return Rule(new BuildableRepeatParserRule
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingRule.Value
			}, factory ?? DefaultFactory_Repeat, config);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Repeat(Action<RuleBuilder> builderAction, int min, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, min, -1, factory, config);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence that matches zero or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder ZeroOrMore(Action<RuleBuilder> builderAction, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, 0, -1, factory, config);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence that matches one or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder OneOrMore(Action<RuleBuilder> builderAction, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, 1, -1, factory, config);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(IEnumerable<Or<Action<RuleBuilder>, string>> choices, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builtValues = choices.Select(c =>
			{
				if (c.VariantIndex == 0)
				{
					var builder = new RuleBuilder(ParserBuilder);
					c.AsT1().Invoke(builder);
					if (!builder.CanBeBuilt)
						throw new ParserBuildingException("Choice child rule cannot be empty.");
					return builder.BuildingRule.Value;
				}
				else
				{
					var name = c.AsT2();
					return new Or<string, BuildableParserRule>(name);
				}
			}).ToList();

			var choice = new BuildableChoiceParserRule();
			choice.Choices.AddRange(builtValues);
			return Rule(choice, factory ?? DefaultFactory_Choice, config);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(IEnumerable<Action<RuleBuilder>> choices, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Choice(choices.Select(c => new Or<Action<RuleBuilder>, string>(c)).ToArray(),
				factory, config);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(params Action<RuleBuilder>[] choices)
		{
			return Choice(choices, null, null);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="max">The maximum number of times the rule can be repeated. -1 indicates no upper limit.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children rules.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder RepeatSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			int min, int max, bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false,
			Func<ParsedRuleResultBase, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			RuleBuilder builder = new(ParserBuilder), separatorBuilder = new(ParserBuilder);
			builderAction(builder);
			separatorBuilderAction(separatorBuilder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child rule cannot be empty.");

			if (!separatorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Separator child rule cannot be empty.");

			return Rule(new BuildableSeparatedRepeatParserRule
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingRule.Value,
				Separator = separatorBuilder.BuildingRule.Value,
				AllowTrailingSeparator = allowTrailingSeparator,
				IncludeSeparatorsInResult = includeSeparatorsInResult
			}, factory ?? DefaultFactory_RepeatSeparated, config);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children rules.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder RepeatSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			int min, bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false,
			Func<ParsedRuleResultBase, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, min, -1, allowTrailingSeparator,
				 includeSeparatorsInResult, factory, config);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence that matches zero or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children rules.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder ZeroOrMoreSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, 0, -1, allowTrailingSeparator,
				includeSeparatorsInResult, factory, config);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence that matches one or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children rules.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder OneOrMoreSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false,
			Func<ParsedRuleResultBase, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, 1, -1, allowTrailingSeparator,
				includeSeparatorsInResult, factory, config);
		}
	}
}