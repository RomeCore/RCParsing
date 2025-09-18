using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.Building.TokenPatterns;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a builder for constructing tokens for parsing.
	/// </summary>
	public class TokenBuilder : ParserElementBuilder<TokenBuilder>
	{
		private static object? DefaultFactory_Token(ParsedRuleResultBase r) => r.IntermediateValue;

		/// <summary>
		/// Initializes a new instance of <see cref="TokenBuilder"/> class.
		/// </summary>
		public TokenBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="TokenBuilder"/> class.
		/// </summary>
		/// <param name="parserBuilder">The master parser builder associated with this token builder.</param>
		public TokenBuilder(ParserBuilder? parserBuilder) : base(parserBuilder)
		{
		}

		/// <summary>
		/// Gets the token being built.
		/// </summary>
		public Or<string, BuildableTokenPattern>? BuildingPattern { get; set; }

		public override bool CanBeBuilt => BuildingPattern.HasValue;

		protected override TokenBuilder GetThis() => this;

		public TokenBuilder Token(Or<string, BuildableTokenPattern> childToken)
		{
			if (!BuildingPattern.HasValue)
			{
				BuildingPattern = childToken;
			}
			else if (BuildingPattern.Value.VariantIndex == 1 &&
					BuildingPattern.Value.AsT2() is BuildableSequenceTokenPattern sequencePattern)
			{
				sequencePattern.Elements.Add(childToken);
			}
			else
			{
				var newSequence = new BuildableSequenceTokenPattern();
				newSequence.ParsedValueFactory = DefaultFactory_Token;
				newSequence.Elements.Add(BuildingPattern.Value);
				newSequence.Elements.Add(childToken);
				BuildingPattern = newSequence;
			}
			return this;
		}

		public override TokenBuilder Token(TokenPattern token,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var leafPattern = new BuildableLeafTokenPattern
			{
				TokenPattern = token
			};
			return Token(leafPattern, factory, config);
		}

		public override TokenBuilder Token(string tokenName)
		{
			return Token(tokenName);
		}

		/// <summary>
		/// Add a child pattern to the current sequence.
		/// </summary>
		/// <param name="tokenPattern">The child pattern to add.</param>
		/// <param name="factory">The default factory function to create a parsed value.</param>
		/// <param name="config">The default action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public TokenBuilder Token(BuildableTokenPattern tokenPattern,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			tokenPattern.ParsedValueFactory = factory ?? DefaultFactory_Token;
			config?.Invoke(tokenPattern.Settings);

			return Token(new Or<string, BuildableTokenPattern>(tokenPattern));
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="builderAction">The token pattern builder.</param>
		/// <param name="factory">The default factory function to create a parsed value.</param>
		/// <param name="config">The default action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Token(Action<TokenBuilder> builderAction, Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Builder action did not add any tokens.");

			if (builder.BuildingPattern.Value.VariantIndex == 0)
			{
				if (factory != null)
					throw new ParserBuildingException("Cannot apply parsed value factory to a direct token reference.");
				if (config != null)
					throw new ParserBuildingException("Cannot apply configuration to a direct token reference.");

				return Token(builder.BuildingPattern.Value.Value1);
			}
			else
				return Token(builder.BuildingPattern.Value.Value2, factory, config);
		}

		/// <summary>
		/// Converts the pattern into a sequence if it is not already one.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public TokenBuilder ToSequence()
		{
			if (!BuildingPattern.HasValue)
			{
				throw new ParserBuildingException("Cannot convert empty pattern to sequence.");
			}
			else if (BuildingPattern.Value.VariantIndex != 1 ||
					BuildingPattern.Value.AsT2() is not BuildableSequenceTokenPattern)
			{
				var newSequence = new BuildableSequenceTokenPattern();
				newSequence.ParsedValueFactory = DefaultFactory_Token;
				newSequence.Elements.Add(BuildingPattern.Value);
				BuildingPattern = newSequence;
			}
			return this;
		}

		/// <summary>
		/// Sets the default transformation function (parsed value factory) to the current pattern.
		/// </summary>
		/// <param name="factory">The transformation function (parsed value factory) to set.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the token pattern is not set or it is a direct reference to a named pattern.</exception>
		public TokenBuilder Transform(Func<ParsedRuleResultBase, object?>? factory)
		{
			if (BuildingPattern?.AsT2() is BuildableTokenPattern pattern)
				pattern.ParsedValueFactory = factory;
			else
				throw new ParserBuildingException("Token pattern is not set or it is a direct reference to a named pattern.");
			return this;
		}

		/// <summary>
		/// Sets the default configuration action for the current token pattern.
		/// </summary>
		/// <param name="configAction">The configuration action.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the token pattern is not set or it is a direct reference to a named pattern.</exception>
		public TokenBuilder Configure(Action<ParserLocalSettingsBuilder> configAction)
		{
			if (BuildingPattern?.AsT2() is BuildableTokenPattern pattern)
				configAction(pattern.Settings);
			else
				throw new ParserBuildingException("Token pattern is not set or it is a direct reference to a named pattern.");
			return this;
		}

		/// <summary>
		/// Sets the intermediate value passage function for the current sequence pattern.
		/// </summary>
		/// <remarks>
		/// This method should be called after adding at least two child elements to the sequence or be applied to repeat pattern.
		/// </remarks>
		/// <param name="passageFunction">The function to pass the intermediate values from each pattern to the result intermediate value.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the current pattern is not a sequence or repeat or has fewer than two child elements.</exception>
		public TokenBuilder Pass(Func<IReadOnlyList<object?>, object?>? passageFunction)
		{
			if (BuildingPattern?.AsT2() is IPassageFunctionHolder passageHolder)
				passageHolder.PassageFunction = passageFunction;
			else
				throw new ParserBuildingException("Passage function can only be set on a several token " +
					"patterns that matches variable amount of multiple children tokens, " +
					"such as Sequence, Repeat, SeparatedRepeat");
			return this;
		}

		/// <summary>
		/// Sets the intermediate value passage function for the current sequence pattern.
		/// </summary>
		/// <remarks>
		/// This method should be called after adding at least two child elements to the sequence or be applied to repeat pattern.
		/// </remarks>
		/// <param name="index">The index of the child element to pass the intermediate value from. The first child has an index of 0.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the current pattern is not a sequence or repeat or has fewer than two child elements.</exception>
		public TokenBuilder Pass(int index)
		{
			return Pass(v => v[index]);
		}

		/// <summary>
		/// Adds an optional token pattern to the current sequence.
		/// </summary>
		/// <param name="builderAction">The action to build the optional token pattern.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Optional(Action<TokenBuilder> builderAction,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Optional child token pattern cannot be empty.");

			return Token(new BuildableOptionalTokenPattern
			{
				Child = builder.BuildingPattern.Value
			}, factory, config);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <param name="max">The maximum number of times the token can be repeated. -1 indicates no upper limit.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Repeat(Action<TokenBuilder> builderAction, int min, int max,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child token pattern cannot be empty.");

			return Token(new BuildableRepeatTokenPattern
			{
				MinCount = min,
				MaxCount = max,
				Child = builder.BuildingPattern.Value
			}, factory, config);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Repeat(Action<TokenBuilder> builderAction, int min,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, min, -1, factory, config);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches zero or more occurrences of the child pattern.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder ZeroOrMore(Action<TokenBuilder> builderAction,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, 0, -1, factory, config);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches one or more occurrences of the child pattern.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder OneOrMore(Action<TokenBuilder> builderAction,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Repeat(builderAction, 1, -1, factory, config);
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(IEnumerable<Or<Action<TokenBuilder>, string>> choices,
			Func<ParsedRuleResultBase, object?>? factory,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builtValues = choices.Select(c =>
			{
				var builder = new TokenBuilder(ParserBuilder);

				if (c.VariantIndex == 0)
				{
					c.AsT1().Invoke(builder);
					if (!builder.CanBeBuilt)
						throw new ParserBuildingException("Choice child token pattern cannot be empty.");
					return builder.BuildingPattern.Value;
				}
				else
				{
					var name = c.AsT2();
					return new Or<string, BuildableTokenPattern>(name);
				}

			}).ToList();

			var choice = new BuildableChoiceTokenPattern();
			choice.Choices.AddRange(builtValues);
			return Token(choice, factory, config);
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(IEnumerable<Action<TokenBuilder>> choices,
			Func<ParsedRuleResultBase, object?>? factory,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return Choice(choices.Select(c => new Or<Action<TokenBuilder>, string>(c)).ToArray(),
				factory, config);
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(params Action<TokenBuilder>[] choices)
		{
			return Choice(choices, null, null);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum and maximum occurrences, separated by a provided separator.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="separatorBuilderAction">The token builder action to build the separator token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <param name="max">The maximum number of times the token can be repeated. -1 indicates no upper limit.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result intermediate values to put in the passage function.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder RepeatSeparated(Action<TokenBuilder> builderAction,
			Action<TokenBuilder> separatorBuilderAction, int min, int max,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Repeated child token pattern cannot be empty.");

			var separatorBuilder = new TokenBuilder(ParserBuilder);
			separatorBuilderAction(separatorBuilder);
			if (!separatorBuilder.CanBeBuilt)
				throw new ParserBuildingException("Separator child token pattern cannot be empty.");

			return Token(new BuildableSeparatedRepeatTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Separator = separatorBuilder.BuildingPattern.Value,
				AllowTrailingSeparator = allowTrailingSeparator,
				IncludeSeparatorsInResult = includeSeparatorsInResult,
				MinCount = min,
				MaxCount = max
			}, factory, config);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum occurrences, separated by a provided separator.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="separatorBuilderAction">The token builder action to build the separator token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result intermediate values to put in the passage function.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder RepeatSeparated(Action<TokenBuilder> builderAction,
			Action<TokenBuilder> separatorBuilderAction, int min,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, min, -1,
				allowTrailingSeparator, includeSeparatorsInResult, factory, config);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches zero or more occurrences of the child pattern, separated by a provided separator.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="separatorBuilderAction">The token builder action to build the separator token.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result intermediate values to put in the passage function.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder ZeroOrMoreSeparated(Action<TokenBuilder> builderAction,
			Action<TokenBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, 0, -1,
				allowTrailingSeparator, includeSeparatorsInResult, factory, config);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches one or more occurrences of the child pattern, separated by a provided separator.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="separatorBuilderAction">The token builder action to build the separator token.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result intermediate values to put in the passage function.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder OneOrMoreSeparated(Action<TokenBuilder> builderAction,
			Action<TokenBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, 1, -1,
				allowTrailingSeparator, includeSeparatorsInResult, factory, config);
		}

		/// <summary>
		/// Adds a token pattern that matches a sequence of three elements, passing middle element's intermediate value up.
		/// </summary>
		/// <param name="firstBuilderAction">The token builder action to build the first token.</param>
		/// <param name="middleBuilderAction">The token builder action to build the middle token.</param>
		/// <param name="lastBuilderAction">The token builder action to build the last token.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Between(Action<TokenBuilder> firstBuilderAction,
			Action<TokenBuilder> middleBuilderAction, Action<TokenBuilder> lastBuilderAction,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var firstBuilder = new TokenBuilder(ParserBuilder);
			firstBuilderAction(firstBuilder);
			if (!firstBuilder.CanBeBuilt)
				throw new ParserBuildingException("First token pattern cannot be empty.");

			var middleBuilder = new TokenBuilder(ParserBuilder);
			middleBuilderAction(middleBuilder);
			if (!middleBuilder.CanBeBuilt)
				throw new ParserBuildingException("Middle token pattern cannot be empty.");

			var lastBuilder = new TokenBuilder(ParserBuilder);
			lastBuilderAction(lastBuilder);
			if (!lastBuilder.CanBeBuilt)
				throw new ParserBuildingException("Last token pattern cannot be empty.");

			return Token(new BuildableBetweenTokenPattern
			{
				First = firstBuilder.BuildingPattern.Value,
				Middle = middleBuilder.BuildingPattern.Value,
				Last = lastBuilder.BuildingPattern.Value
			}, factory, config);
		}

		/// <summary>
		/// Adds a token pattern that matches a sequence of two elements, passing the first element's intermediate value up.
		/// </summary>
		/// <param name="firstBuilderAction">The token builder action to build the first token.</param>
		/// <param name="secondBuilderAction">The token builder action to build the second token.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder First(Action<TokenBuilder> firstBuilderAction,
			Action<TokenBuilder> secondBuilderAction,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var firstBuilder = new TokenBuilder(ParserBuilder);
			firstBuilderAction(firstBuilder);
			if (!firstBuilder.CanBeBuilt)
				throw new ParserBuildingException("First token pattern cannot be empty.");

			var secondBuilder = new TokenBuilder(ParserBuilder);
			secondBuilderAction(secondBuilder);
			if (!secondBuilder.CanBeBuilt)
				throw new ParserBuildingException("Second token pattern cannot be empty.");

			return Token(new BuildableFirstTokenPattern
			{
				First = firstBuilder.BuildingPattern.Value,
				Second = secondBuilder.BuildingPattern.Value
			}, factory, config);
		}

		/// <summary>
		/// Adds a token pattern that matches a sequence of two elements, passing the second element's intermediate value up.
		/// </summary>
		/// <param name="firstBuilderAction">The token builder action to build the first token.</param>
		/// <param name="secondBuilderAction">The token builder action to build the second token.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Second(Action<TokenBuilder> firstBuilderAction,
			Action<TokenBuilder> secondBuilderAction,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var firstBuilder = new TokenBuilder(ParserBuilder);
			firstBuilderAction(firstBuilder);
			if (!firstBuilder.CanBeBuilt)
				throw new ParserBuildingException("First token pattern cannot be empty.");

			var secondBuilder = new TokenBuilder(ParserBuilder);
			secondBuilderAction(secondBuilder);
			if (!secondBuilder.CanBeBuilt)
				throw new ParserBuildingException("Second token pattern cannot be empty.");

			return Token(new BuildableSecondTokenPattern
			{
				First = firstBuilder.BuildingPattern.Value,
				Second = secondBuilder.BuildingPattern.Value
			}, factory, config);
		}

		/// <summary>
		/// Adds a token pattern that matches a child pattern while skipping any whitespace characters before it.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder SkipWhitespaces(Action<TokenBuilder> builderAction,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableSkipWhitespacesTokenPattern
			{
				Pattern = builder.BuildingPattern.Value
			}, factory, config);
		}

		/// <summary>
		/// Adds a token pattern that matches a single element, always returning a fixed intermediate value.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="value">The fixed intermediate value to return upon successful match.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Return(Action<TokenBuilder> builderAction, object? value,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableReturnTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Value = value
			}, factory, config);
		}

		/// <summary>
		/// Adds a token pattern that matches a single element, transforming its intermediate value with a provided function.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="mapper">The transformation function applied to the child's intermediate value.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Map(Action<TokenBuilder> builderAction, Func<object?, object?> mapper,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableMapTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Mapper = mapper
			}, factory, config);
		}

		/// <summary>
		/// Adds a token pattern that matches a single element, transforming its intermediate value with a provided function.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="mapper">The transformation function applied to the child's intermediate value.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Map<T>(Action<TokenBuilder> builderAction, Func<T, object?> mapper,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableMapTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Mapper = v => mapper((T)v)
			}, factory, config);
		}

		/// <summary>
		/// Adds a token pattern that matches a single element and captures its matched substring as intermediate value.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="trimStart">The number of characters to trim from the start of the captured text.</param>
		/// <param name="trimEnd">The number of characters to trim from the end of the captured text.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder CaptureText(Action<TokenBuilder> builderAction,
			int trimStart = 0, int trimEnd = 0,
			Func<ParsedRuleResultBase, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			if (trimStart < 0) throw new ArgumentOutOfRangeException(nameof(trimStart));
			if (trimEnd < 0) throw new ArgumentOutOfRangeException(nameof(trimEnd));

			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableCaptureTextTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				TrimStart = trimStart,
				TrimEnd = trimEnd
			}, factory, config);
		}
	}
}