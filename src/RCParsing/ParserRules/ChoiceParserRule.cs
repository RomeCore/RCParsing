using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that matches one of several alternatives.
	/// </summary>
	public class ChoiceParserRule : ParserRule
	{
		private int[] _choicesIds;

		/// <summary>
		/// The rule ids that are being chosen from.
		/// </summary>
		public IReadOnlyList<int> Choices { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceParserRule"/> class.
		/// </summary>
		/// <param name="parserRuleIds">The parser rules ids to choose from.</param>
		public ChoiceParserRule(IEnumerable<int> parserRuleIds)
		{
			_choicesIds = parserRuleIds?.ToArray() ?? throw new ArgumentNullException(nameof(parserRuleIds));
			Choices = _choicesIds.AsReadOnlyList();
			if (_choicesIds.Length == 0)
				throw new ArgumentException("At least one parser rule must be provided.", nameof(parserRuleIds));
		}

		protected override HashSet<char>? FirstCharsCore
		{
			get
			{
				var childFirstChars = Choices.Select(id => GetRule(id).FirstChars).ToArray();
				if (childFirstChars.Any(fcs => fcs == null))
					return null;
				return new(childFirstChars.SelectMany(fcs => fcs!).Distinct());
			}
		}



		#region Optimization

		private ParseDelegate parseFunction;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			if (!initFlags.HasFlag(ParserInitFlags.FirstCharacterMatch))
			{
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

				ParsedRule Parse(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
				{
					for (int i = 0; i < parseFunctions.Length; i++)
					{
						var parsedRule = parseFunctions[i](context, childSettings);

						if (parsedRule.success)
						{
							parsedRule.occurency = i;
							return ParsedRule.Rule(Id,
								parsedRule.startIndex,
								parsedRule.length,
								parsedRule.passedBarriers,
								ParsedRuleChildUtils.Single(ref parsedRule),
								parsedRule.intermediateValue);
						}
					}

					RecordError(ref context, ref settings, "Found no matching choice.");
					return ParsedRule.Fail;
				}

				parseFunction = Parse;
			}
			else
			{
				var candidatesByFirstChar = new Dictionary<char, Func<ParserContext, ParserSettings, ParsedRule>[]>();
				var _nonDeterministicCandidates = new List<Func<ParserContext, ParserSettings, ParsedRule>>();

				if (FirstChars != null)
					foreach (var ch in FirstChars)
					{
						var rules = new List<Func<ParserContext, ParserSettings, ParsedRule>>();
						foreach (var choice in Choices)
						{
							int id = choice;
							var rule = GetRule(id);
							if (rule.FirstChars == null || rule.FirstChars.Contains(ch))
							{
								if (initFlags.HasFlag(ParserInitFlags.InlineRules) && rule.CanBeInlined)
									rules.Add((ctx, chStng) => rule.Parse(ctx, chStng, chStng));
								else
									rules.Add((ctx, chStng) => TryParseRule(id, ctx, chStng));
							}
							candidatesByFirstChar[ch] = rules.ToArray();
						}
					}

				foreach (var choice in Choices)
				{
					int id = choice;
					var rule = GetRule(id);
					if (rule.FirstChars == null)
					{
						if (initFlags.HasFlag(ParserInitFlags.InlineRules) && rule.CanBeInlined)
							_nonDeterministicCandidates.Add((ctx, chStng) => rule.Parse(ctx, chStng, chStng));
						else
							_nonDeterministicCandidates.Add((ctx, chStng) => TryParseRule(id, ctx, chStng));
					}
				}

				var nonDeterministicCandidates = _nonDeterministicCandidates.ToArray();

				ParsedRule Parse(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
				{
					if (!candidatesByFirstChar.TryGetValue(context.input[context.position], out var candidates))
						candidates = nonDeterministicCandidates;

					for (int i = 0; i < candidates.Length; i++)
					{
						var parsedRule = candidates[i](context, childSettings);

						if (parsedRule.success)
						{
							parsedRule.occurency = i;
							return ParsedRule.Rule(Id,
								parsedRule.startIndex,
								parsedRule.length,
								parsedRule.passedBarriers,
								ParsedRuleChildUtils.Single(ref parsedRule),
								parsedRule.intermediateValue);
						}
					}

					RecordError(ref context, ref settings, "Found no matching choice.");
					return ParsedRule.Fail;
				}

				parseFunction = Parse;
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