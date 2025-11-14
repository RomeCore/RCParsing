using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.Building.TokenPatterns;
using RCParsing.TokenPatterns;
using RCParsing.TokenPatterns.Combinators;
using RCParsing.Utils;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a builder for constructing tokens for parsing.
	/// </summary>
	public class TokenBuilder : ParserElementBuilder<TokenBuilder>
	{
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
				newSequence.Elements.Add(BuildingPattern.Value);
				newSequence.Elements.Add(childToken);
				BuildingPattern = newSequence;
			}
			return this;
		}

		public override TokenBuilder Token(TokenPattern token)
		{
			var leafPattern = new BuildableLeafTokenPattern
			{
				TokenPattern = token
			};
			return Token(leafPattern);
		}

		public override TokenBuilder Token(string tokenName)
		{
			return Token(tokenName);
		}

		/// <summary>
		/// Add a child pattern to the current sequence.
		/// </summary>
		/// <param name="tokenPattern">The child pattern to add.</param>
		/// <returns>Current instance for method chaining.</returns>
		public override TokenBuilder Token(BuildableTokenPattern tokenPattern)
		{
			return Token(new Or<string, BuildableTokenPattern>(tokenPattern));
		}

		/// <summary>
		/// Adds a token to the current sequence.
		/// </summary>
		/// <param name="builderAction">The token pattern builder.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Token(Action<TokenBuilder> builderAction)
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
				newSequence.Elements.Add(BuildingPattern.Value);
				BuildingPattern = newSequence;
			}
			return this;
		}

		/// <summary>
		/// Configures the local settings for the current pattern to skip parsing and ignore errors.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the token pattern is not set or it is a direct reference to a named pattern.</exception>
		public TokenBuilder ConfigureForSkip()
		{
			return Configure(c => c.NoSkipping().IgnoreErrors());
		}

		/// <summary>
		/// Sets the default transformation function (parsed value factory) to the current pattern that will be applied to parent rule.
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
		/// Configures the default local settings for the current token pattern that will be applied to parent rule.
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
		/// Configures the default error recovery settings for the current token pattern that will be applied to parent rule.
		/// </summary>
		/// <param name="recoveryConfigAction">The error recovery configuration action.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if the parser rule is not set or it is a direct reference to a named rule.</exception>
		public TokenBuilder Recovery(Action<ErrorRecoveryStrategyBuilder> recoveryConfigAction)
		{
			if (BuildingPattern?.AsT2() is BuildableTokenPattern pattern)
				recoveryConfigAction(pattern.ErrorRecovery);
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
		/// <param name="builderAction">The action to build the optional token pattern child.</param>
		/// <param name="fallbackValue">The fallback intermadiate value that will be returned when child fails.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Optional(Action<TokenBuilder> builderAction, object? fallbackValue = null)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Optional child token pattern cannot be empty.");

			return Token(new BuildableOptionalTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				FallbackValue = fallbackValue
			});
		}

		/// <summary>
		/// Adds a lookahead token pattern to the current sequence.
		/// </summary>
		/// <param name="isPositive">Determines whether the lookahead is positive (true) or negative (false).</param>
		/// <param name="builderAction">The action to build the inner token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action did not add any elements.</exception>
		public TokenBuilder Lookahead(bool isPositive, Action<TokenBuilder> builderAction)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException($"{(isPositive ? "Positive" : "Negative")} lookahead child token pattern cannot be empty.");

			return Token(new BuildableLookaheadTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				IsPositive = isPositive
			});
		}

		/// <summary>
		/// Adds a positive lookahead token pattern to the current sequence.
		/// </summary>
		/// <param name="builderAction">The action to build the inner token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action did not add any elements.</exception>
		public TokenBuilder PositiveLookahead(Action<TokenBuilder> builderAction)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Positive lookahead child token pattern cannot be empty.");

			return Token(new BuildableLookaheadTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				IsPositive = true
			});
		}

		/// <summary>
		/// Adds a negative lookahead token pattern to the current sequence.
		/// </summary>
		/// <param name="builderAction">The action to build the inner token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action did not add any elements.</exception>
		public TokenBuilder NegativeLookahead(Action<TokenBuilder> builderAction)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);

			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Negative lookahead child token pattern cannot be empty.");

			return Token(new BuildableLookaheadTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				IsPositive = false
			});
		}


		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <param name="max">The maximum number of times the token can be repeated. -1 indicates no upper limit.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Repeat(Action<TokenBuilder> builderAction, int min, int max)
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
			});
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Repeat(Action<TokenBuilder> builderAction, int min)
		{
			return Repeat(builderAction, min, -1);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches zero or more occurrences of the child pattern.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder ZeroOrMore(Action<TokenBuilder> builderAction)
		{
			return Repeat(builderAction, 0, -1);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches one or more occurrences of the child pattern.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder OneOrMore(Action<TokenBuilder> builderAction)
		{
			return Repeat(builderAction, 1, -1);
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="mode">The behaviour to use when selecting resulting choice.</param>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(ChoiceMode mode, IEnumerable<Or<Action<TokenBuilder>, string>> choices)
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
			choice.Mode = mode;
			choice.Choices.AddRange(builtValues);
			return Token(choice);
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="mode">The behaviour to use when selecting resulting choice.</param>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(ChoiceMode mode, IEnumerable<Action<TokenBuilder>> choices)
		{
			return Choice(mode, choices.Select(c => new Or<Action<TokenBuilder>, string>(c)));
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <param name="mode">The behaviour to use when selecting resulting choice.</param>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(ChoiceMode mode, params Action<TokenBuilder>[] choices)
		{
			return Choice(mode, (IEnumerable<Action<TokenBuilder>>)choices);
		}
		
		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <remarks>
		/// Stops at first succeeded element and returns it.
		/// </remarks>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(IEnumerable<Action<TokenBuilder>> choices)
		{
			return Choice(ChoiceMode.First, choices.Select(c => new Or<Action<TokenBuilder>, string>(c)).ToArray());
		}

		/// <summary>
		/// Adds a choice token pattern to the current sequence.
		/// </summary>
		/// <remarks>
		/// Stops at first succeeded element and returns it.
		/// </remarks>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder Choice(params Action<TokenBuilder>[] choices)
		{
			return Choice((IEnumerable<Action<TokenBuilder>>)choices);
		}

		/// <summary>
		/// Adds a shortest choice token pattern to the current sequence.
		/// </summary>
		/// <remarks>
		/// Tries to match all elements and returns the first shortest one.
		/// </remarks>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder ShortestChoice(IEnumerable<Action<TokenBuilder>> choices)
		{
			return Choice(ChoiceMode.Shortest, choices.Select(c => new Or<Action<TokenBuilder>, string>(c)).ToArray());
		}

		/// <summary>
		/// Adds a shortest choice token pattern to the current sequence.
		/// </summary>
		/// <remarks>
		/// Tries to match all elements and returns the first shortest one.
		/// </remarks>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder ShortestChoice(params Action<TokenBuilder>[] choices)
		{
			return ShortestChoice((IEnumerable<Action<TokenBuilder>>)choices);
		}

		/// <summary>
		/// Adds a longest choice token pattern to the current sequence.
		/// </summary>
		/// <remarks>
		/// Tries to match all elements and returns the first longest one.
		/// </remarks>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder LongestChoice(IEnumerable<Action<TokenBuilder>> choices)
		{
			return Choice(ChoiceMode.Longest, choices.Select(c => new Or<Action<TokenBuilder>, string>(c)).ToArray());
		}

		/// <summary>
		/// Adds a longest choice token pattern to the current sequence.
		/// </summary>
		/// <remarks>
		/// Tries to match all elements and returns the first longest one.
		/// </remarks>
		/// <param name="choices">The choices for this token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any of builder actions have not added any elements.</exception>
		public TokenBuilder LongestChoice(params Action<TokenBuilder>[] choices)
		{
			return LongestChoice((IEnumerable<Action<TokenBuilder>>)choices);
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
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder RepeatSeparated(Action<TokenBuilder> builderAction,
			Action<TokenBuilder> separatorBuilderAction, int min, int max,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false)
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
			});
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence with specified minimum occurrences, separated by a provided separator.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="separatorBuilderAction">The token builder action to build the separator token.</param>
		/// <param name="min">The minimum number of times the token can be repeated.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result intermediate values to put in the passage function.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder RepeatSeparated(Action<TokenBuilder> builderAction,
			Action<TokenBuilder> separatorBuilderAction, int min,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, min, -1,
				allowTrailingSeparator, includeSeparatorsInResult);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches zero or more occurrences of the child pattern, separated by a provided separator.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="separatorBuilderAction">The token builder action to build the separator token.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result intermediate values to put in the passage function.</param>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder ZeroOrMoreSeparated(Action<TokenBuilder> builderAction,
			Action<TokenBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, 0, -1,
				allowTrailingSeparator, includeSeparatorsInResult);
		}

		/// <summary>
		/// Adds a repeatable token pattern to the current sequence that matches one or more occurrences of the child pattern, separated by a provided separator.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the repeatable token.</param>
		/// <param name="separatorBuilderAction">The token builder action to build the separator token.</param>
		/// <param name="allowTrailingSeparator">Whether to allow a trailing separator.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result intermediate values to put in the passage function.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder OneOrMoreSeparated(Action<TokenBuilder> builderAction,
			Action<TokenBuilder> separatorBuilderAction,
			bool allowTrailingSeparator = false, bool includeSeparatorsInResult = false)
		{
			return RepeatSeparated(builderAction, separatorBuilderAction, 1, -1,
				allowTrailingSeparator, includeSeparatorsInResult);
		}

		/// <summary>
		/// Adds a token pattern that matches a sequence of three elements, passing middle element's intermediate value up.
		/// </summary>
		/// <param name="firstBuilderAction">The token builder action to build the first token.</param>
		/// <param name="middleBuilderAction">The token builder action to build the middle token.</param>
		/// <param name="lastBuilderAction">The token builder action to build the last token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Between(Action<TokenBuilder> firstBuilderAction,
			Action<TokenBuilder> middleBuilderAction, Action<TokenBuilder> lastBuilderAction)
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
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a sequence of two elements, passing the first element's intermediate value up.
		/// </summary>
		/// <param name="firstBuilderAction">The token builder action to build the first token.</param>
		/// <param name="secondBuilderAction">The token builder action to build the second token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder First(Action<TokenBuilder> firstBuilderAction,
			Action<TokenBuilder> secondBuilderAction)
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
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a sequence of two elements, passing the second element's intermediate value up.
		/// </summary>
		/// <param name="firstBuilderAction">The token builder action to build the first token.</param>
		/// <param name="secondBuilderAction">The token builder action to build the second token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Second(Action<TokenBuilder> firstBuilderAction,
			Action<TokenBuilder> secondBuilderAction)
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
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a child pattern while skipping any whitespace characters before it.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder SkipWhitespaces(Action<TokenBuilder> builderAction)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableSkipWhitespacesTokenPattern
			{
				Pattern = builder.BuildingPattern.Value
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a single element, always returning a fixed intermediate value.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="value">The fixed intermediate value to return upon successful match.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Return(Action<TokenBuilder> builderAction, object? value)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableReturnTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Value = value
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a single element and captures its matched substring as intermediate value.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="trimStart">The number of characters to trim from the start of the captured text.</param>
		/// <param name="trimEnd">The number of characters to trim from the end of the captured text.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder CaptureText(Action<TokenBuilder> builderAction,
			int trimStart = 0, int trimEnd = 0)
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
			});
		}

		/// <summary>
		/// Adds a token pattern that matches text until the child pattern is found, capturing the text as intermediate value.
		/// </summary>
		/// <param name="stopBuilder">The builder action for the stop condition pattern.</param>
		/// <param name="allowEmpty">Whether empty matches are allowed.</param>
		/// <param name="consumeStop">Whether to consume the stop pattern.</param>
		/// <param name="failOnEof">Whether to fail when end of input is reached.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder TextUntil(Action<TokenBuilder> stopBuilder, bool allowEmpty = true, bool consumeStop = false, bool failOnEof = false)
		{
			var stopPatternBuilder = new TokenBuilder(ParserBuilder);
			stopBuilder(stopPatternBuilder);
			if (!stopPatternBuilder.CanBeBuilt)
				throw new ParserBuildingException("Stop pattern cannot be empty.");

			return Token(new BuildableTextUntilTokenPattern
			{
				StopPattern = stopPatternBuilder.BuildingPattern.Value,
				AllowEmpty = allowEmpty,
				ConsumeStop = consumeStop,
				FailOnEof = failOnEof
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a single element, transforming its matched text span with a provided function.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="mapper">The transformation function applied to the matched text span.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder MapSpan(Action<TokenBuilder> builderAction, SpanMapper mapper)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableMapSpanTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Mapper = mapper
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a single element, transforming its intermediate value with a provided function.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="mapper">The transformation function applied to the child's intermediate value.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Map(Action<TokenBuilder> builderAction, Func<object?, object?> mapper)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableMapTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Mapper = mapper
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a single element, transforming its intermediate value with a provided function.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="mapper">The transformation function applied to the child's intermediate value.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder Map<T>(Action<TokenBuilder> builderAction, Func<T, object?> mapper)
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableMapTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Mapper = v => mapper((T)v)
			});
		}

		/// <summary>
		/// Adds a token pattern that matches a single element, but fails if the condition function returns true for the intermediate value.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="condition">The condition function that determines if the match should fail.</param>
		/// <param name="errorMessage">The error message to use when the condition fails.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder FailIf(Action<TokenBuilder> builderAction, Func<object?, bool> condition, string errorMessage = "Condition failed.")
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableFailIfTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Condition = condition,
				ErrorMessage = errorMessage
			});
		}
		
		/// <summary>
		/// Adds a token pattern that matches a single element, but fails if the condition function returns true for the intermediate value.
		/// </summary>
		/// <param name="builderAction">The token builder action to build the child token.</param>
		/// <param name="condition">The condition function that determines if the match should fail.</param>
		/// <param name="errorMessage">The error message to use when the condition fails.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder action have not added any elements.</exception>
		public TokenBuilder FailIf<T>(Action<TokenBuilder> builderAction, Func<T, bool> condition, string errorMessage = "Condition failed.")
		{
			var builder = new TokenBuilder(ParserBuilder);
			builderAction(builder);
			if (!builder.CanBeBuilt)
				throw new ParserBuildingException("Child token pattern cannot be empty.");

			return Token(new BuildableFailIfTokenPattern
			{
				Child = builder.BuildingPattern.Value,
				Condition = v => v is T t && condition(t),
				ErrorMessage = errorMessage
			});
		}

		/// <summary>
		/// Adds a token pattern that conditionally matches one of two patterns based on parser parameter.
		/// </summary>
		/// <param name="condition">The condition function that determines which branch to take.</param>
		/// <param name="trueBuilder">The builder action for the true branch.</param>
		/// <param name="falseBuilder">The builder action for the false branch.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder actions have not added any elements.</exception>
		public TokenBuilder If(Func<object?, bool> condition, Action<TokenBuilder> trueBuilder, Action<TokenBuilder>? falseBuilder = null)
		{
			var trueBranchBuilder = new TokenBuilder(ParserBuilder);
			trueBuilder(trueBranchBuilder);
			if (!trueBranchBuilder.CanBeBuilt)
				throw new ParserBuildingException("True branch token pattern cannot be empty.");

			var falseBranchBuilder = new TokenBuilder(ParserBuilder);
			if (falseBuilder != null)
			{
				falseBuilder(falseBranchBuilder);
				if (!falseBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("False branch token pattern cannot be empty.");
			}

			return Token(new BuildableIfTokenPattern
			{
				Condition = condition,
				TrueBranch = trueBranchBuilder.BuildingPattern.Value,
				FalseBranch = falseBranchBuilder.BuildingPattern ?? default
			});
		}
		
		/// <summary>
		/// Adds a token pattern that conditionally matches one of two patterns based on parser parameter.
		/// </summary>
		/// <param name="condition">The condition function that determines which branch to take.</param>
		/// <param name="trueBuilder">The builder action for the true branch.</param>
		/// <param name="falseBuilder">The builder action for the false branch.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if builder actions have not added any elements.</exception>
		public TokenBuilder If<T>(Func<T, bool> condition, Action<TokenBuilder> trueBuilder, Action<TokenBuilder>? falseBuilder = null)
		{
			var trueBranchBuilder = new TokenBuilder(ParserBuilder);
			trueBuilder(trueBranchBuilder);
			if (!trueBranchBuilder.CanBeBuilt)
				throw new ParserBuildingException("True branch token pattern cannot be empty.");

			var falseBranchBuilder = new TokenBuilder(ParserBuilder);
			if (falseBuilder != null)
			{
				falseBuilder(falseBranchBuilder);
				if (!falseBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("False branch token pattern cannot be empty.");
			}

			return Token(new BuildableIfTokenPattern
			{
				Condition = p => p is T t && condition(t),
				TrueBranch = trueBranchBuilder.BuildingPattern.Value,
				FalseBranch = falseBranchBuilder.BuildingPattern ?? default
			});
		}

		/// <summary>
		/// Adds a token pattern that matches one of multiple patterns based on parser parameter.
		/// </summary>
		/// <param name="selector">The selector function that determines which branch to take, returns index.</param>
		/// <param name="defaultBranch">The builder for the default branch.</param>
		/// <param name="branches">The builder actions for the branches.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Switch(Func<object?, int> selector, Action<TokenBuilder>? defaultBranch, params Action<TokenBuilder>[] branches)
		{
			var branchPatterns = new List<Or<string, BuildableTokenPattern>>();

			foreach (var branchBuilderAction in branches)
			{
				var branchBuilder = new TokenBuilder(ParserBuilder);
				branchBuilderAction(branchBuilder);
				if (!branchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Branch token pattern cannot be empty.");

				branchPatterns.Add(branchBuilder.BuildingPattern.Value);
			}

			Or<string, BuildableTokenPattern> defaultBranchToken = default;

			if (defaultBranch != null)
			{
				var defaultBranchBuilder = new TokenBuilder(ParserBuilder);
				defaultBranch(defaultBranchBuilder);
				if (!defaultBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Default branch token pattern cannot be empty.");
				defaultBranchToken = defaultBranchBuilder.BuildingPattern.Value;
			}

			return Token(new BuildableSwitchTokenPattern
			{
				Selector = selector,
				Branches = branchPatterns,
				DefaultBranch = defaultBranchToken
			});
		}

		/// <summary>
		/// Adds a token pattern that matches one of multiple patterns based on parser parameter.
		/// </summary>
		/// <param name="selector">The selector function that determines which branch to take, returns index.</param>
		/// <param name="defaultBranch">The builder for the default branch.</param>
		/// <param name="branches">The builder actions for the branches.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Switch<T>(Func<T, int> selector, Action<TokenBuilder>? defaultBranch, params Action<TokenBuilder>[] branches)
		{
			var branchPatterns = new List<Or<string, BuildableTokenPattern>>();

			foreach (var branchBuilderAction in branches)
			{
				var branchBuilder = new TokenBuilder(ParserBuilder);
				branchBuilderAction(branchBuilder);
				if (!branchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Branch token pattern cannot be empty.");

				branchPatterns.Add(branchBuilder.BuildingPattern.Value);
			}

			Or<string, BuildableTokenPattern> defaultBranchToken = default;

			if (defaultBranch != null)
			{
				var defaultBranchBuilder = new TokenBuilder(ParserBuilder);
				defaultBranch(defaultBranchBuilder);
				if (!defaultBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Default branch token pattern cannot be empty.");
				defaultBranchToken = defaultBranchBuilder.BuildingPattern.Value;
			}

			return Token(new BuildableSwitchTokenPattern
			{
				Selector = p => p is T t ? selector(t) : -1,
				Branches = branchPatterns,
				DefaultBranch = defaultBranchToken
			});
		}

		/// <summary>
		/// Adds a token pattern that matches one of multiple patterns based on parser parameter.
		/// </summary>
		/// <param name="defaultBranch">The builder for the default branch.</param>
		/// <param name="branches">The builder actions for the branches paired with conditions.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Switch(Action<TokenBuilder>? defaultBranch, params (Func<object?, bool>, Action<TokenBuilder>)[] branches)
		{
			var branchPatterns = new List<Or<string, BuildableTokenPattern>>();
			var conditions = new List<Func<object?, bool>>();

			foreach (var (condition, branchBuilderAction) in branches)
			{
				var branchBuilder = new TokenBuilder(ParserBuilder);
				branchBuilderAction(branchBuilder);
				if (!branchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Branch token pattern cannot be empty.");

				branchPatterns.Add(branchBuilder.BuildingPattern.Value);
				conditions.Add(condition);
			}

			Or<string, BuildableTokenPattern> defaultBranchToken = default;

			if (defaultBranch != null)
			{
				var defaultBranchBuilder = new TokenBuilder(ParserBuilder);
				defaultBranch(defaultBranchBuilder);
				if (!defaultBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Default branch token pattern cannot be empty.");
				defaultBranchToken = defaultBranchBuilder.BuildingPattern.Value;
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

			return Token(new BuildableSwitchTokenPattern
			{
				Selector = selector,
				Branches = branchPatterns,
				DefaultBranch = defaultBranchToken
			});
		}

		/// <summary>
		/// Adds a token pattern that matches one of multiple patterns based on parser parameter.
		/// </summary>
		/// <param name="defaultBranch">The builder for the default branch.</param>
		/// <param name="branches">The builder actions for the branches paired with conditions.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Switch<T>(Action<TokenBuilder>? defaultBranch, params (Func<T, bool>, Action<TokenBuilder>)[] branches)
		{
			var branchPatterns = new List<Or<string, BuildableTokenPattern>>();
			var conditions = new List<Func<T, bool>>();

			foreach (var (condition, branchBuilderAction) in branches)
			{
				var branchBuilder = new TokenBuilder(ParserBuilder);
				branchBuilderAction(branchBuilder);
				if (!branchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Branch token pattern cannot be empty.");

				branchPatterns.Add(branchBuilder.BuildingPattern.Value);
				conditions.Add(condition);
			}

			Or<string, BuildableTokenPattern> defaultBranchToken = default;

			if (defaultBranch != null)
			{
				var defaultBranchBuilder = new TokenBuilder(ParserBuilder);
				defaultBranch(defaultBranchBuilder);
				if (!defaultBranchBuilder.CanBeBuilt)
					throw new ParserBuildingException("Default branch token pattern cannot be empty.");
				defaultBranchToken = defaultBranchBuilder.BuildingPattern.Value;
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

			return Token(new BuildableSwitchTokenPattern
			{
				Selector = selector,
				Branches = branchPatterns,
				DefaultBranch = defaultBranchToken
			});
		}

		/// <summary>
		/// Adds a token pattern that matches one of multiple patterns based on parser parameter.
		/// </summary>
		/// <param name="branches">The builder actions for the branches paired with conditions.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Switch(params (Func<object?, bool>, Action<TokenBuilder>)[] branches)
		{
			return Switch(null, branches);
		}

		/// <summary>
		/// Adds a token pattern that matches one of multiple patterns based on parser parameter.
		/// </summary>
		/// <param name="branches">The builder actions for the branches paired with conditions.</param>
		/// <returns>Current instance for method chaining.</returns>
		/// <exception cref="ParserBuildingException">Thrown if any builder action have not added any elements.</exception>
		public TokenBuilder Switch<T>(params (Func<T, bool>, Action<TokenBuilder>)[] branches)
		{
			return Switch(null, branches);
		}
	}
}