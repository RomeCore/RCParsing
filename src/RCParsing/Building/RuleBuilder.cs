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
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Token(Or<string, BuildableTokenPattern> childToken)
		{
			return Rule(new BuildableTokenParserRule
			{
				Child = childToken
			});
		}

		public override RuleBuilder Token(TokenPattern token)
		{
			return Token(new BuildableLeafTokenPattern
			{
				TokenPattern = token,
				ParsedValueFactory = DefaultFactory_Token
			});
		}

		/// <summary>
		/// Adds a child token to the current sequence.
		/// </summary>
		/// <param name="token">The child token to add.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Token(BuildableTokenPattern token)
		{
			return Token(new Or<string, BuildableTokenPattern>(token));
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
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Rule(BuildableParserRule childRule)
		{
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
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Rule(Action<RuleBuilder> builderAction)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Builder action did not add any rules.");

			return Rule(builder.BuildingRule.Value);
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="tokenName">The name of the token to add.</param>
		/// <returns>Current instance for method chaining.</returns>
		public override RuleBuilder Token(string tokenName)
		{
			return Token(tokenName);
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="builderAction">The token pattern builder.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public RuleBuilder Token(Action<TokenBuilder> builderAction)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Builder action did not add any tokens.");

			return Token(builder.BuildingPattern.Value);
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
		/// Configures the local settings for the current rule.
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
		/// Configures the error recovery settings for the current rule.
		/// </summary>
		/// <param name="recoveryConfigAction">The error recovery configuration action.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the parser rule is not set or it is a direct reference to a named rule.</exception>
		public RuleBuilder Recovery(Action<ErrorRecoveryBuilder> recoveryConfigAction)
		{
			if (BuildingRule?.AsT2() is BuildableParserRule rule)
				recoveryConfigAction(rule.ErrorRecovery);
			else
				throw new ParserBuildingException("Parser rule is not set or it is a direct reference to named rule.");
			return this;
		}

		/// <summary>
		/// Configures the error recovery settings for the last rule in the sequence.
		/// </summary>
		/// <param name="recoveryConfigAction">The error recovery configuration action.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the parser rule is not set or it is a direct reference to a named rule.</exception>
		public RuleBuilder RecoveryLast(Action<ErrorRecoveryBuilder> recoveryConfigAction)
		{
			if (BuildingRule?.AsT2() is BuildableSequenceParserRule sequenceRule)
			{
				if (sequenceRule.Elements.LastOrDefault().AsT2() is BuildableParserRule rule)
					recoveryConfigAction(rule.ErrorRecovery);
				else
					throw new ParserBuildingException("Last rule in the sequence is not set or it is a direct reference to named rule.");
				return this;
			}
			else if (BuildingRule?.AsT2() is BuildableParserRule rule)
				recoveryConfigAction(rule.ErrorRecovery);
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
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Optional(Action<RuleBuilder> builderAction)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Optional child rule cannot be empty.");

			return Rule(new BuildableOptionalParserRule
			{
				Child = builder.BuildingRule.Value,
				ParsedValueFactory = DefaultFactory_Optional
			});
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="max">The maximum number of times the rule can be repeated. -1 indicates no upper limit.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Repeat(Action<RuleBuilder> builderAction, int min, int max)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child rule cannot be empty.");

			return Rule(new BuildableRepeatParserRule
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingRule.Value,
				ParsedValueFactory = DefaultFactory_Repeat
			});
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Repeat(Action<RuleBuilder> builderAction, int min)
		{
			return Repeat(builderAction, min, -1);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence that matches zero or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder ZeroOrMore(Action<RuleBuilder> builderAction)
		{
			return Repeat(builderAction, 0, -1);
		}

		/// <summary>
		/// Adds a repeatable rule to the current sequence that matches one or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder OneOrMore(Action<RuleBuilder> builderAction)
		{
			return Repeat(builderAction, 1, -1);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(IEnumerable<Or<Action<RuleBuilder>, string>> choices)
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
			choice.ParsedValueFactory = DefaultFactory_Choice;
			return Rule(choice);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(IEnumerable<Action<RuleBuilder>> choices)
		{
			return Choice(choices.Select(c => new Or<Action<RuleBuilder>, string>(c)).ToArray());
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(params Action<RuleBuilder>[] choices)
		{
			return Choice((IEnumerable<Action<RuleBuilder>>)choices);
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
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder RepeatSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			int min, int max, bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false)
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
				IncludeSeparatorsInResult = includeSeparatorsInResult,
				ParsedValueFactory = DefaultFactory_RepeatSeparated
			});
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="min">The minimum number of times the rule can be repeated.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children rules.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder RepeatSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			int min, bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, min, -1, allowTrailingSeparator,
				 includeSeparatorsInResult);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence that matches zero or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children rules.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder ZeroOrMoreSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, 0, -1, allowTrailingSeparator,
				includeSeparatorsInResult);
		}

		/// <summary>
		/// Adds a separated repeatable rule to the current sequence that matches one or more occurrences of the child rule.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the repeatable rule.</param>
		/// <param name="separatorBuilderAction">The rule builder action to build the separator rule.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children rules.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of the builder actions did not add any elements.</exception>
		public RuleBuilder OneOrMoreSeparated(Action<RuleBuilder> builderAction, Action<RuleBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, 1, -1, allowTrailingSeparator,
				includeSeparatorsInResult);
		}
	}
}