using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that matches one of several alternatives.
	/// </summary>
	public class ChoiceParserRule : ParserRule
	{
		private readonly int[] _choicesIds;

		/// <summary>
		/// The child element selection behaviour of this choice rule.
		/// </summary>
		public ChoiceMode Mode { get; }

		/// <summary>
		/// The rule ids that are being chosen from.
		/// </summary>
		public IReadOnlyList<int> Choices { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceParserRule"/> class.
		/// </summary>
		/// <param name="mode">The child element selection behaviour of this choice rule.</param>
		/// <param name="parserRuleIds">The parser rules ids to choose from.</param>
		public ChoiceParserRule(ChoiceMode mode, IEnumerable<int> parserRuleIds)
		{
			if (!Enum.IsDefined(typeof(ChoiceMode), mode))
				throw new ArgumentOutOfRangeException(nameof(mode));
			Mode = mode;

			_choicesIds = parserRuleIds?.ToArray() ?? throw new ArgumentNullException(nameof(parserRuleIds));
			Choices = _choicesIds.AsReadOnlyList();
			if (_choicesIds.Length == 0)
				throw new ArgumentException("At least one parser rule must be provided.", nameof(parserRuleIds));
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				var childFirstChars = Choices.Select(id => GetRule(id).FirstChars).ToArray();
				return new(childFirstChars.SelectMany(fcs => fcs).Distinct());
			}
		}
		protected override bool IsFirstCharDeterministicCore
		{
			get
			{
				var childDeterministic = Choices.Select(id => GetRule(id).IsFirstCharDeterministic).ToArray();
				return childDeterministic.All(o => o);
			}
		}
		protected override bool IsOptionalCore
		{
			get
			{
				var childOptionals = Choices.Select(id => GetRule(id).IsOptional).ToArray();
				return childOptionals.Any(o => o);
			}
		}



		#region Optimization

		private ParseDelegate parseFunction;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			var parseFunctions = new Func<ParserContext, ParserSettings, ParsedRule>[_choicesIds.Length];

			for (int i = 0; i < _choicesIds.Length; i++)
			{
				var id = Choices[i];
				var rule = GetRule(id);

				if (initFlags.HasFlag(ParserInitFlags.InlineRules) && rule.CanBeInlined)
				{
					parseFunctions[i] = (ctx, chStng) => rule.Parse(ctx, chStng, chStng);
				}
				else
				{
					parseFunctions[i] = (ctx, chStng) => TryParseRule(id, ctx, chStng);
				}
			}

			ParsedRule ParseFirst(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				for (int i = 0; i < parseFunctions.Length; i++)
				{
					var parsedRule = parseFunctions[i](context, childSettings);

					if (parsedRule.success)
					{
						parsedRule.occurency = i;
						return new ParsedRule(Id,
							parsedRule.startIndex,
							parsedRule.length,
							parsedRule.passedBarriers,
							parsedRule.intermediateValue,
							parsedRule);
					}
				}

				RecordError(ref context, ref settings, "Found no matching choice.");
				return ParsedRule.Fail;
			}

			ParsedRule ParseShortest(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				int shortestLen = int.MaxValue;
				ParsedRule shortest = ParsedRule.Fail;

				for (int i = 0; i < parseFunctions.Length; i++)
				{
					var parsedRule = parseFunctions[i](context, childSettings);

					if (parsedRule.success && parsedRule.length < shortestLen)
					{
						parsedRule.occurency = i;
						shortestLen = parsedRule.length;
						shortest = parsedRule;
					}
				}

				if (shortest.success)
				{
					return new ParsedRule(Id,
						shortest.startIndex,
						shortest.length,
						shortest.passedBarriers,
						shortest.intermediateValue,
						shortest);
				}

				RecordError(ref context, ref settings, "Found no matching choice.");
				return ParsedRule.Fail;
			}

			ParsedRule ParseLongest(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				int longestLen = int.MinValue;
				ParsedRule longest = ParsedRule.Fail;

				for (int i = 0; i < parseFunctions.Length; i++)
				{
					var parsedRule = parseFunctions[i](context, childSettings);

					if (parsedRule.success && parsedRule.length > longestLen)
					{
						parsedRule.occurency = i;
						longestLen = parsedRule.length;
						longest = parsedRule;
					}
				}

				if (longest.success)
				{
					return new ParsedRule(Id,
						longest.startIndex,
						longest.length,
						longest.passedBarriers,
						longest.intermediateValue,
						longest);
				}

				RecordError(ref context, ref settings, "Found no matching choice.");
				return ParsedRule.Fail;
			}

			if (!initFlags.HasFlag(ParserInitFlags.FirstCharacterMatch))
			{
				switch (Mode)
				{
					default:
					case ChoiceMode.First:
						parseFunction = ParseFirst;
						break;

					case ChoiceMode.Shortest:
						parseFunction = ParseShortest;
						break;

					case ChoiceMode.Longest:
						parseFunction = ParseLongest;
						break;
				}
			}
			else
			{
				var optimizedCandidates = new Func<ParserContext, ParserSettings, ParsedRule>[char.MaxValue + 1][];
				var nonDeterministic = new List<Func<ParserContext, ParserSettings, ParsedRule>>();

				foreach (var ch in FirstChars)
				{
					var choicesByChar = new List<Func<ParserContext, ParserSettings, ParsedRule>>();
					for (int i = 0; i < _choicesIds.Length; i++)
					{
						var id = Choices[i];
						var rule = GetRule(id);

						if (!rule.IsFirstCharDeterministic || rule.IsOptional || rule.FirstChars.Contains(ch))
							choicesByChar.Add(parseFunctions[i]);
					}
					optimizedCandidates[ch] = choicesByChar.ToArray();
				}

				for (int i = 0; i < _choicesIds.Length; i++)
				{
					var id = Choices[i];
					var rule = GetRule(id);
					if (!rule.IsFirstCharDeterministic || rule.IsOptional)
						nonDeterministic.Add(parseFunctions[i]);
				}

				var _nonDeterministic = nonDeterministic.ToArray();

				for (int c = 0; c < 0xffff + 1; c++)
				{
					if (optimizedCandidates[c] == null)
						optimizedCandidates[c] = _nonDeterministic;
				}

				ParsedRule ParseFirstLookahead(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
				{
					var candidates = context.position < context.maxPosition ?
						optimizedCandidates[context.input[context.position]] :
						_nonDeterministic;

					for (int i = 0; i < candidates.Length; i++)
					{
						var parsedRule = candidates[i](context, childSettings);

						if (parsedRule.success)
						{
							parsedRule.occurency = i;
							return new ParsedRule(Id,
								parsedRule.startIndex,
								parsedRule.length,
								parsedRule.passedBarriers,
								parsedRule.intermediateValue,
								parsedRule);
						}
					}

					RecordError(ref context, ref settings, "Found no matching choice.");
					return ParsedRule.Fail;
				}

				ParsedRule ParseShortestLookahead(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
				{
					var candidates = context.position < context.maxPosition ?
						optimizedCandidates[context.input[context.position]] :
						_nonDeterministic;

					int shortestLen = int.MaxValue;
					ParsedRule shortest = ParsedRule.Fail;

					for (int i = 0; i < candidates.Length; i++)
					{
						var parsedRule = candidates[i](context, childSettings);

						if (parsedRule.success && parsedRule.length < shortestLen)
						{
							parsedRule.occurency = i;
							shortestLen = parsedRule.length;
							shortest = parsedRule;
						}
					}

					if (shortest.success)
					{
						return new ParsedRule(Id,
							shortest.startIndex,
							shortest.length,
							shortest.passedBarriers,
							shortest.intermediateValue,
							shortest);
					}

					RecordError(ref context, ref settings, "Found no matching choice.");
					return ParsedRule.Fail;
				}

				ParsedRule ParseLongestLookahead(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
				{
					var candidates = context.position < context.maxPosition ?
						optimizedCandidates[context.input[context.position]] :
						_nonDeterministic;

					int longestLen = int.MinValue;
					ParsedRule longest = ParsedRule.Fail;

					for (int i = 0; i < candidates.Length; i++)
					{
						var parsedRule = candidates[i](context, childSettings);

						if (parsedRule.success && parsedRule.length > longestLen)
						{
							parsedRule.occurency = i;
							longestLen = parsedRule.length;
							longest = parsedRule;
						}
					}

					if (longest.success)
					{
						return new ParsedRule(Id,
							longest.startIndex,
							longest.length,
							longest.passedBarriers,
							longest.intermediateValue,
							longest);
					}

					RecordError(ref context, ref settings, "Found no matching choice.");
					return ParsedRule.Fail;
				}

				switch (Mode)
				{
					default:
					case ChoiceMode.First:
						parseFunction = ParseFirstLookahead;
						break;

					case ChoiceMode.Shortest:
						parseFunction = ParseShortestLookahead;
						break;

					case ChoiceMode.Longest:
						parseFunction = ParseLongestLookahead;
						break;
				}
			}

			parseFunction = WrapParseFunction(parseFunction, initFlags);
		}

		#endregion

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction(ref context, ref settings, ref childSettings);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;

			if (remainingDepth <= 0)
				return $"Choice{alias}...";

			return $"Choice{alias}:\n" +
				string.Join("\n", Choices.Select(c => Parser.Rules[c].ToString(remainingDepth - 1)))
				.Indent("  ");
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;

			if (remainingDepth <= 0)
				return $"Choice{alias}...";

			return $"Choice{alias}:\n" +
				string.Join("\n", Choices.Select(c =>
				{
					if (c == childIndex)
						return Parser.Rules[c].ToString(remainingDepth - 1) + " <-- here";
					return Parser.Rules[c].ToString(remainingDepth - 1);
				})).Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is ChoiceParserRule rule &&
				   Choices.SequenceEqual(rule.Choices);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Choices.GetSequenceHashCode();
			return hashCode;
		}
	}
}