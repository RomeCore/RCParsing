using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.Building.ParserRules;
using RCParsing.Building.TokenPatterns;
using RCParsing.ParserRules;
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
				TokenPattern = token
			});
		}

		/// <summary>
		/// Adds a child token to the current sequence.
		/// </summary>
		/// <param name="token">The child token to add.</param>
		/// <returns>Current instance for method chaining.</returns>
		public override RuleBuilder Token(BuildableTokenPattern token)
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
		public RuleBuilder Recovery(Action<ErrorRecoveryStrategyBuilder> recoveryConfigAction)
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
		public RuleBuilder RecoveryLast(Action<ErrorRecoveryStrategyBuilder> recoveryConfigAction)
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
		/// Adds a custom rule to the current sequence.
		/// </summary>
		/// <param name="parseFunction">The function to use for parsing the rule.</param>
		/// <param name="childRules">The children rule builders actions.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Custom(CustomRuleParseFunction parseFunction, params Action<RuleBuilder>[] childRules)
		{
			var rule = new BuildableCustomParserRule
			{
				ParseFunction = parseFunction
			};

			rule.Children.AddRange(childRules.Select(action =>
			{
				var builder = new RuleBuilder(ParserBuilder);
				action.Invoke(builder);
				if (!builder.CanBeBuilt)
					throw new ParserBuildingException("Builder action did not add any rules.");
				return builder.BuildingRule.Value;
			}));

			return Rule(rule);
		}
		
		/// <summary>
		/// Adds a custom rule to the current sequence.
		/// </summary>
		/// <param name="parseFunction">The function to use for parsing the rule.</param>
		/// <param name="stringRepresentation">The string representation of custom rule.</param>
		/// <param name="childRules">The children rule builders actions.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Custom(CustomRuleParseFunction parseFunction, string stringRepresentation,
			params Action<RuleBuilder>[] childRules)
		{
			var rule = new BuildableCustomParserRule
			{
				ParseFunction = parseFunction,
				StringRepresentation = stringRepresentation
			};

			rule.Children.AddRange(childRules.Select(action =>
			{
				var builder = new RuleBuilder(ParserBuilder);
				action.Invoke(builder);
				if (!builder.CanBeBuilt)
					throw new ParserBuildingException("Builder action did not add any rules.");
				return builder.BuildingRule.Value;
			}));

			return Rule(rule);
		}

		/// <summary>
		/// Adds a rule to the current sequence.
		/// </summary>
		/// <param name="factory">The function to create the rule from children IDs.</param>
		/// <param name="childRules">The children rule builders actions.</param>
		/// <returns>Current instance for method chaining.</returns>
		public RuleBuilder Rule(Func<List<int>, ParserRule> factory, params Action<RuleBuilder>[] childRules)
		{
			var rule = new BuildableFactoryParserRule
			{
				Factory = factory
			};

			rule.Children.AddRange(childRules.Select(action =>
			{
				var builder = new RuleBuilder(ParserBuilder);
				action.Invoke(builder);
				if (!builder.CanBeBuilt)
					throw new ParserBuildingException("Builder action did not add any rules.");
				return builder.BuildingRule.Value;
			}));

			return Rule(rule);
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
				Child = builder.BuildingRule.Value
			});
		}

		/// <summary>
		/// Adds a lookahead rule to the current sequence.
		/// </summary>
		/// <param name="isPositive">Whether to perform a positive lookahead (true) or negative lookahead (false).</param>
		/// <param name="builderAction">The rule builder action to build the child rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder Lookahead(bool isPositive, Action<RuleBuilder> builderAction)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Positive lookahead child rule cannot be empty.");

			return Rule(new BuildableLookaheadParserRule
			{
				IsPositive = isPositive,
				Child = builder.BuildingRule.Value
			});
		}

		/// <summary>
		/// Adds a positive lookahead rule to the current sequence.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the child rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder PositiveLookahead(Action<RuleBuilder> builderAction)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Positive lookahead child rule cannot be empty.");

			return Rule(new BuildableLookaheadParserRule
			{
				IsPositive = true,
				Child = builder.BuildingRule.Value
			});
		}

		/// <summary>
		/// Adds a negative lookahead rule to the current sequence.
		/// </summary>
		/// <param name="builderAction">The rule builder action to build the child rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the builder action did not add any elements.</exception>
		public RuleBuilder NegativeLookahead(Action<RuleBuilder> builderAction)
		{
			var builder = new RuleBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Negative lookahead child rule cannot be empty.");

			return Rule(new BuildableLookaheadParserRule
			{
				IsPositive = false,
				Child = builder.BuildingRule.Value
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
				Child = builder.BuildingRule.Value
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
		/// <param name="mode">The behaviour to use when selecting resulting choice.</param>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(ChoiceMode mode, IEnumerable<Or<Action<RuleBuilder>, string>> choices)
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
			choice.Mode = mode;
			choice.Choices.AddRange(builtValues);
			return Rule(choice);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="mode">The behaviour to use when selecting resulting choice.</param>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(ChoiceMode mode, IEnumerable<Action<RuleBuilder>> choices)
		{
			return Choice(mode, choices.Select(c => new Or<Action<RuleBuilder>, string>(c)));
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <param name="mode">The behaviour to use when selecting resulting choice.</param>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(ChoiceMode mode, params Action<RuleBuilder>[] choices)
		{
			return Choice(mode, (IEnumerable<Action<RuleBuilder>>)choices);
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <remarks>
		/// Stops at first succeeded element and returns it.
		/// </remarks>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(IEnumerable<Action<RuleBuilder>> choices)
		{
			return Choice(ChoiceMode.First, choices.Select(c => new Or<Action<RuleBuilder>, string>(c)).ToArray());
		}

		/// <summary>
		/// Adds a choice rule to the current sequence.
		/// </summary>
		/// <remarks>
		/// Stops at first succeeded element and returns it.
		/// </remarks>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder Choice(params Action<RuleBuilder>[] choices)
		{
			return Choice((IEnumerable<Action<RuleBuilder>>)choices);
		}

		/// <summary>
		/// Adds a shortest choice rule to the current sequence.
		/// </summary>
		/// <remarks>
		/// Tries to match all elements and returns the first shortest one.
		/// </remarks>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder ShortestChoice(IEnumerable<Action<RuleBuilder>> choices)
		{
			return Choice(ChoiceMode.Longest, choices.Select(c => new Or<Action<RuleBuilder>, string>(c)).ToArray());
		}

		/// <summary>
		/// Adds a shortest choice rule to the current sequence.
		/// </summary>
		/// <remarks>
		/// Tries to match all elements and returns the first shortest one.
		/// </remarks>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder ShortestChoice(params Action<RuleBuilder>[] choices)
		{
			return ShortestChoice((IEnumerable<Action<RuleBuilder>>)choices);
		}

		/// <summary>
		/// Adds a longest choice rule to the current sequence.
		/// </summary>
		/// <remarks>
		/// Tries to match all elements and returns the first longest one.
		/// </remarks>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder LongestChoice(IEnumerable<Action<RuleBuilder>> choices)
		{
			return Choice(ChoiceMode.Longest, choices.Select(c => new Or<Action<RuleBuilder>, string>(c)).ToArray());
		}

		/// <summary>
		/// Adds a longest choice rule to the current sequence.
		/// </summary>
		/// <remarks>
		/// Tries to match all elements and returns the first longest one.
		/// </remarks>
		/// <param name="choices">The choices for this rule.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public RuleBuilder LongestChoice(params Action<RuleBuilder>[] choices)
		{
			return LongestChoice((IEnumerable<Action<RuleBuilder>>)choices);
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
				IncludeSeparatorsInResult = includeSeparatorsInResult
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

		/// <summary>
		/// Adds a conditional rule that chooses between two branches based on parser parameter.
		/// </summary>
		/// <param name="condition">The parser parameter condition function that determines which branch to take.</param>
		/// <param name="trueBuilder">The rule builder action for the true branch.</param>
		/// <param name="falseBuilder">The rule builder action for the false branch.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder actions have not added any elements.</exception>
		public RuleBuilder If(Func<object?, bool> condition, Action<RuleBuilder> trueBuilder, Action<RuleBuilder>? falseBuilder = null)
		{
			var trueBranchBuilder = new RuleBuilder(ParserBuilder);
			trueBuilder(trueBranchBuilder);
			if (!trueBranchBuilder.CanBeBuilt)
				throw new ParserBuildingException("True branch rule cannot be empty.");

			var falseBranchBuilder = new RuleBuilder(ParserBuilder);
			if (falseBuilder != null)
			{
				falseBuilder(falseBranchBuilder);
				if (!falseBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("False branch rule cannot be empty.");
			}

			return Rule(new BuildableIfParserRule
			{
				Condition = condition,
				TrueBranch = trueBranchBuilder.BuildingRule.Value,
				FalseBranch = falseBranchBuilder.BuildingRule ?? default
			});
		}

		/// <summary>
		/// Adds a conditional rule that chooses between two branches based on parser parameter.
		/// </summary>
		/// <param name="condition">The parser parameter condition function that determines which branch to take.</param>
		/// <param name="trueBuilder">The rule builder action for the true branch.</param>
		/// <param name="falseBuilder">The rule builder action for the false branch.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder actions have not added any elements.</exception>
		public RuleBuilder If<T>(Func<T, bool> condition, Action<RuleBuilder> trueBuilder, Action<RuleBuilder>? falseBuilder = null)
		{
			var trueBranchBuilder = new RuleBuilder(ParserBuilder);
			trueBuilder(trueBranchBuilder);
			if (!trueBranchBuilder.CanBeBuilt)
				throw new ParserBuildingException("True branch rule cannot be empty.");

			var falseBranchBuilder = new RuleBuilder(ParserBuilder);
			if (falseBuilder != null)
			{
				falseBuilder(falseBranchBuilder);
				if (!falseBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("False branch rule cannot be empty.");
			}

			return Rule(new BuildableIfParserRule
			{
				Condition = p => p is T t && condition(t),
				TrueBranch = trueBranchBuilder.BuildingRule.Value,
				FalseBranch = falseBranchBuilder.BuildingRule ?? default
			});
		}

		/// <summary>
		/// Adds a switch rule that chooses between multiple branches based on parser parameter.
		/// </summary>
		/// <param name="selector">The selector function that determines which branch to take, returns index.</param>
		/// <param name="defaultBranch">The builder for the default branch.</param>
		/// <param name="branches">The builder actions for the branches.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public RuleBuilder Switch(Func<object?, int> selector, Action<RuleBuilder>? defaultBranch, params Action<RuleBuilder>[] branches)
		{
			var branchRules = new List<Or<string, BuildableParserRule>>();

			foreach (var branchBuilderAction in branches)
			{
				var branchBuilder = new RuleBuilder(ParserBuilder);
				branchBuilderAction(branchBuilder);
				if (!branchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Branch rule cannot be empty.");

				branchRules.Add(branchBuilder.BuildingRule.Value);
			}

			Or<string, BuildableParserRule> defaultBranchRule = default;

			if (defaultBranch != null)
			{
				var defaultBranchBuilder = new RuleBuilder(ParserBuilder);
				defaultBranch(defaultBranchBuilder);
				if (!defaultBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Default branch rule cannot be empty.");
				defaultBranchRule = defaultBranchBuilder.BuildingRule.Value;
			}

			return Rule(new BuildableSwitchParserRule
			{
				Selector = selector,
				Branches = branchRules,
				DefaultBranch = defaultBranchRule
			});
		}

		/// <summary>
		/// Adds a switch rule that chooses between multiple branches based on parser parameter.
		/// </summary>
		/// <param name="selector">The selector function that determines which branch to take, returns index.</param>
		/// <param name="defaultBranch">The builder for the default branch.</param>
		/// <param name="branches">The builder actions for the branches.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public RuleBuilder Switch<T>(Func<T, int> selector, Action<RuleBuilder>? defaultBranch, params Action<RuleBuilder>[] branches)
		{
			var branchRules = new List<Or<string, BuildableParserRule>>();

			foreach (var branchBuilderAction in branches)
			{
				var branchBuilder = new RuleBuilder(ParserBuilder);
				branchBuilderAction(branchBuilder);
				if (!branchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Branch rule cannot be empty.");

				branchRules.Add(branchBuilder.BuildingRule.Value);
			}

			Or<string, BuildableParserRule> defaultBranchRule = default;

			if (defaultBranch != null)
			{
				var defaultBranchBuilder = new RuleBuilder(ParserBuilder);
				defaultBranch(defaultBranchBuilder);
				if (!defaultBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Default branch rule cannot be empty.");
				defaultBranchRule = defaultBranchBuilder.BuildingRule.Value;
			}

			return Rule(new BuildableSwitchParserRule
			{
				Selector = p => p is T t ? selector(t) : -1,
				Branches = branchRules,
				DefaultBranch = defaultBranchRule
			});
		}

		/// <summary>
		/// Adds a switch rule that chooses between multiple branches based on parser parameter.
		/// </summary>
		/// <param name="defaultBranch">The builder for the default branch.</param>
		/// <param name="branches">The builder actions for the branches paired with conditions.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public RuleBuilder Switch(Action<RuleBuilder>? defaultBranch, params (Func<object?, bool>, Action<RuleBuilder>)[] branches)
		{
			var branchRules = new List<Or<string, BuildableParserRule>>();
			var conditions = new List<Func<object?, bool>>();

			foreach (var (condition, branchBuilderAction) in branches)
			{
				var branchBuilder = new RuleBuilder(ParserBuilder);
				branchBuilderAction(branchBuilder);
				if (!branchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Branch rule cannot be empty.");

				branchRules.Add(branchBuilder.BuildingRule.Value);
				conditions.Add(condition);
			}

			Or<string, BuildableParserRule> defaultBranchRule = default;

			if (defaultBranch != null)
			{
				var defaultBranchBuilder = new RuleBuilder(ParserBuilder);
				defaultBranch(defaultBranchBuilder);
				if (!defaultBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Default branch rule cannot be empty.");
				defaultBranchRule = defaultBranchBuilder.BuildingRule.Value;
			}

			Func<object?, int> selector = param =>
			{
				for (int i = 0; i < conditions.Count; i++)
				{
					if (conditions[i](param))
						return i;
				}
				return -1;
			};

			return Rule(new BuildableSwitchParserRule
			{
				Selector = selector,
				Branches = branchRules,
				DefaultBranch = defaultBranchRule
			});
		}

		/// <summary>
		/// Adds a switch rule that chooses between multiple branches based on parser parameter.
		/// </summary>
		/// <param name="defaultBranch">The builder for the default branch.</param>
		/// <param name="branches">The builder actions for the branches paired with conditions.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public RuleBuilder Switch<T>(Action<RuleBuilder>? defaultBranch, params (Func<T, bool>, Action<RuleBuilder>)[] branches)
		{
			var branchRules = new List<Or<string, BuildableParserRule>>();
			var conditions = new List<Func<T, bool>>();

			foreach (var (condition, branchBuilderAction) in branches)
			{
				var branchBuilder = new RuleBuilder(ParserBuilder);
				branchBuilderAction(branchBuilder);
				if (!branchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Branch rule cannot be empty.");

				branchRules.Add(branchBuilder.BuildingRule.Value);
				conditions.Add(condition);
			}

			Or<string, BuildableParserRule> defaultBranchRule = default;

			if (defaultBranch != null)
			{
				var defaultBranchBuilder = new RuleBuilder(ParserBuilder);
				defaultBranch(defaultBranchBuilder);
				if (!defaultBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Default branch rule cannot be empty.");
				defaultBranchRule = defaultBranchBuilder.BuildingRule.Value;
			}

			Func<object?, int> selector = param =>
			{
				if (param is T typedParam)
				{
					for (int i = 0; i < conditions.Count; i++)
					{
						if (conditions[i](typedParam))
							return i;
					}
				}
				return -1;
			};

			return Rule(new BuildableSwitchParserRule
			{
				Selector = selector,
				Branches = branchRules,
				DefaultBranch = defaultBranchRule
			});
		}

		/// <summary>
		/// Adds a switch rule that chooses between multiple branches based on parser parameter.
		/// </summary>
		/// <param name="branches">The builder actions for the branches paired with conditions.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public RuleBuilder Switch(params (Func<object?, bool>, Action<RuleBuilder>)[] branches)
		{
			return Switch(null, branches);
		}

		/// <summary>
		/// Adds a switch rule that chooses between multiple branches based on parser parameter.
		/// </summary>
		/// <param name="branches">The builder actions for the branches paired with conditions.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public RuleBuilder Switch<T>(params (Func<T, bool>, Action<RuleBuilder>)[] branches)
		{
			return Switch<T>(null, branches);
		}
	}
}