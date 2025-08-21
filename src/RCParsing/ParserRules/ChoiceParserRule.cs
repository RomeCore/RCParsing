using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that matches one of several alternatives.
	/// </summary>
	public class ChoiceParserRule : ParserRule
	{
		/// <summary>
		/// The rule ids that are being chosen from.
		/// </summary>
		public ImmutableArray<int> Choices { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceParserRule"/> class.
		/// </summary>
		/// <param name="parserRuleIds">The parser rules ids to choose from.</param>
		public ChoiceParserRule(IEnumerable<int> parserRuleIds)
		{
			Choices = parserRuleIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(parserRuleIds));
			if (Choices.IsEmpty)
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

		/// <summary>
		/// Map from first-char -> ordered candidate rule ids (preserving original Choices order).
		/// Built by Optimize().
		/// </summary>
		private Dictionary<char, ImmutableArray<int>>? _firstCharToChoicesMap;

		/// <summary>
		/// Ordered list of candidates to try when there is no current char (end-of-input) or char not found.
		/// These are the rules whose FirstChars == null (i.e. undetermined start).
		/// </summary>
		private ImmutableArray<int> _defaultCandidates = ImmutableArray<int>.Empty;

		/// <summary>
		/// Mapping ruleId -> original index (occurrence) in Choices to correctly set parsedRule.occurency.
		/// </summary>
		private ImmutableDictionary<int, int>? _choiceIndexMap;

		/// <summary>
		/// Build per-first-char candidate lists preserving original order of Choices.
		/// This is called during parser construction/optimization.
		/// </summary>
		protected override void Optimize()
		{
			// Build index map: ruleId => occurrence index
			var indexBuilder = ImmutableDictionary.CreateBuilder<int, int>();
			for (int i = 0; i < Choices.Length; i++)
				indexBuilder[Choices[i]] = i;
			_choiceIndexMap = indexBuilder.ToImmutable();

			// Collect FirstChars of each choice (may be null)
			var firstCharsByChoice = new HashSet<char>?[Choices.Length];
			HashSet<char> allChars = new();
			bool anyNullFirst = false;

			for (int i = 0; i < Choices.Length; i++)
			{
				var rule = GetRule(Choices[i]);
				var f = rule.FirstChars;
				if (f == null)
				{
					firstCharsByChoice[i] = null;
					anyNullFirst = true;
				}
				else
				{
					firstCharsByChoice[i] = new HashSet<char>(f);
					foreach (var ch in f)
						allChars.Add(ch);
				}
			}

			// Default candidates: rules with null FirstChars (they may need to be tried when char is absent
			// or the current char doesn't belong to any explicit set).
			var defaultCandidatesBuilder = ImmutableArray.CreateBuilder<int>();
			for (int i = 0; i < Choices.Length; i++)
			{
				if (firstCharsByChoice[i] == null)
					defaultCandidatesBuilder.Add(Choices[i]);
			}
			_defaultCandidates = defaultCandidatesBuilder.ToImmutable();

			// If there are no determinable first-chars at all and no null-firsts, nothing to optimize.
			// But we can still build map when at least one rule has concrete FirstChars.
			if (allChars.Count == 0 && !anyNullFirst)
			{
				_firstCharToChoicesMap = null; // nothing to optimize
				return;
			}

			// For each char in allChars, build the ordered candidate list:
			// iterate original Choices and add those that either have null FirstChars OR contain the char.
			var map = new Dictionary<char, ImmutableArray<int>>(allChars.Count);
			foreach (var ch in allChars)
			{
				var list = new List<int>(Choices.Length);
				for (int i = 0; i < Choices.Length; i++)
				{
					var fc = firstCharsByChoice[i];
					if (fc == null || fc.Contains(ch))
						list.Add(Choices[i]);
				}
				map[ch] = list.ToImmutableArray();
			}

			_firstCharToChoicesMap = map;
		}

		public override ParsedRule Parse(ParserContext context, ParserContext childContext)
		{
			// If optimization map is not built, fall back to the original sequential algorithm.
			if (_firstCharToChoicesMap == null || _choiceIndexMap == null)
			{
				int i = 0;
				foreach (var rule in Choices)
				{
					var parsedRule = TryParseRule(rule, childContext);

					if (parsedRule.success)
					{
						parsedRule.occurency = i;
						return ParsedRule.Rule(Id,
							parsedRule.startIndex,
							parsedRule.length,
							new List<ParsedRule> { parsedRule },
							parsedRule.intermediateValue);
					}

					i++;
				}

				RecordError(context, "Found no matching choice.");
				return ParsedRule.Fail;
			}

			// Optimized path: compute current char and pick candidate list.
			int pos = childContext.position;
			string input = childContext.str;
			ImmutableArray<int> candidates;

			if (pos >= input.Length)
			{
				// End of input: only rules with null FirstChars could possibly match
				candidates = _defaultCandidates;
			}
			else
			{
				char current = input[pos];
				if (_firstCharToChoicesMap.TryGetValue(current, out var list))
					candidates = list;
				else
					// No explicit candidate for this char — only _defaultCandidates remain.
					candidates = _defaultCandidates;
			}

			// Try each candidate in order; on first success, return with proper occurency index.
			foreach (var ruleId in candidates)
			{
				var parsedRule = TryParseRule(ruleId, childContext);
				if (parsedRule.success)
				{
					// Map ruleId -> its original index in Choices to preserve occurrence order
					if (_choiceIndexMap != null && _choiceIndexMap.TryGetValue(ruleId, out var occ))
						parsedRule.occurency = occ;
					return ParsedRule.Rule(Id,
						parsedRule.startIndex,
						parsedRule.length,
						new List<ParsedRule> { parsedRule },
						parsedRule.intermediateValue);
				}
			}

			RecordError(context, "Found no matching choice.");
			return ParsedRule.Fail;
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "Choice...";
			return $"Choice:\n" +
				string.Join("\n", Choices.Select(c => Parser.Rules[c].ToString(remainingDepth - 1)))
				.Indent("  ");
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