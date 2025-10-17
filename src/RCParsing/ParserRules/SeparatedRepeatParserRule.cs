using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// A parser rule that repeats a specified element rule with a separator rule between elements.
	/// </summary>
	public class SeparatedRepeatParserRule : ParserRule
	{
		/// <summary>
		/// Gets the element rule ID.
		/// </summary>
		public int Rule { get; }

		/// <summary>
		/// Gets the separator rule ID.
		/// </summary>
		public int Separator { get; }

		/// <summary>
		/// Gets the minimum number of elements.
		/// </summary>
		public int MinCount { get; }

		/// <summary>
		/// Gets the maximum number of elements, or -1 for no limit.
		/// </summary>
		public int MaxCount { get; }

		/// <summary>
		/// Gets whether a trailing separator without a following element is allowed.
		/// </summary>
		public bool AllowTrailingSeparator { get; }

		/// <summary>
		/// Gets whether separators should be included in the result children rules.
		/// </summary>
		public bool IncludeSeparatorsInResult { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="SeparatedRepeatParserRule"/> class.
		/// </summary>
		/// <param name="rule">The ID of the rule to repeat.</param>
		/// <param name="separatorRule">The ID of the rule to use as a separator.</param>
		/// <param name="minCount">The minimum number of elements.</param>
		/// <param name="maxCount">The maximum number of elements, or -1 for no limit.</param>
		/// <param name="allowTrailingSeparator">Whether a trailing separator without a following element is allowed.</param>
		/// <param name="includeSeparatorsInResult">Whether separators should be included in the result children rules.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="minCount"/> is less than 0 or less than <paramref name="maxCount"/> if specified.</exception>
		public SeparatedRepeatParserRule(
			int rule,
			int separatorRule,
			int minCount,
			int maxCount,
			bool allowTrailingSeparator = false,
			bool includeSeparatorsInResult = false)
		{
			if (minCount < 0)
				throw new ArgumentOutOfRangeException(nameof(minCount), "minCount must be >= 0");

			if (maxCount < minCount && maxCount >= 0)
				throw new ArgumentOutOfRangeException(nameof(maxCount), "maxCount must be >= minCount or -1");

			Rule = rule;
			Separator = separatorRule;
			MinCount = minCount;
			MaxCount = Math.Max(maxCount, -1);
			AllowTrailingSeparator = allowTrailingSeparator;
			IncludeSeparatorsInResult = includeSeparatorsInResult;
		}

		protected override HashSet<char> FirstCharsCore => GetRule(Rule).FirstChars;
		protected override bool IsFirstCharDeterministicCore => GetRule(Rule).IsFirstCharDeterministic;
		protected override bool IsOptionalCore => MinCount == 0 || GetRule(Rule).IsOptional;



		private ParseDelegate parseFunction;
		private ParserRule rule;
		private bool canRuleBeInlined;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			rule = GetRule(Rule);
			canRuleBeInlined = rule.CanBeInlined && (initFlags & ParserInitFlags.InlineRules) != 0;

			ParsedRule Parse(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				var initialPosition = context.position;

				// Try to parse the first element (if required - error if not found; if optional - may return empty result)
				ParsedRule firstElement;
				if (canRuleBeInlined)
					firstElement = rule.Parse(context, childSettings, childSettings);
				else
					firstElement = TryParseRule(Rule, context, childSettings);

				if (!firstElement.success)
				{
					// No first element found
					// If minCount == 0 — OK: empty sequence
					if (MinCount == 0)
					{
						return new ParsedRule(Id, initialPosition, 0, context.passedBarriers, Array.Empty<ParsedRule>());
					}
					else
					{
						// Minimum count > 0, but first element not found — explicit error
						RecordError(ref context, ref settings, $"Expected at least {MinCount} repetitions of child rule, but found 0.");
						return ParsedRule.Fail;
					}
				}

				// We have the first element
				if (firstElement.length == 0)
				{
					RecordError(ref context, ref settings, "Parsed child element has zero length, which is not allowed.");
					return ParsedRule.Fail;
				}

				var elements = new List<ParsedRule>();
				int count = 1;
				firstElement.occurency = elements.Count;
				elements.Add(firstElement);
				context.position = firstElement.startIndex + firstElement.length;
				context.passedBarriers = firstElement.passedBarriers;

				// Parse "separator + element" until limit reached
				while (MaxCount == -1 || count < MaxCount)
				{
					var beforeSepPos = context.position;

					// Try to parse the separator
					var parsedSep = TryParseRule(Separator, context, childSettings);
					if (!parsedSep.success || parsedSep.length == 0)
						break; // no separator — end of sequence

					/*if (parsedSep.length == 0)
					{
						RecordError(ref context, ref settings, "Parsed separator has zero length, which is not allowed.");
						return ParsedRule.Fail;
					}*/

					// Separator successfully parsed — position already updated inside TryParseRule, but update again for safety:
					parsedSep.occurency = count;
					int postionBeforeSep = context.position;
					context.position = parsedSep.startIndex + parsedSep.length;
					context.passedBarriers = parsedSep.passedBarriers;

					// Include separator in result if requested
					if (IncludeSeparatorsInResult)
						elements.Add(parsedSep);

					// Try to parse the next element
					var nextElement = TryParseRule(Rule, context, childSettings);
					if (!nextElement.success || nextElement.length == 0)
					{
						// If element parse was not success, finish the parsing and (optionally) remove separator
						if (!AllowTrailingSeparator)
						{
							context.position = postionBeforeSep;
							elements.RemoveAt(elements.Count - 1);
						}
						break;
					}

					/*if (nextElement.length == 0)
					{
						RecordError(ref context, ref settings, "Parsed child element has zero length, which is not allowed.");
						return ParsedRule.Fail;
					}*/

					nextElement.occurency = count;
					count++;
					elements.Add(nextElement);
					context.position = nextElement.startIndex + nextElement.length;
					context.passedBarriers = nextElement.passedBarriers;

					// loop continues — try to find next separator + element
				}

				// Check minimum count
				if (count < MinCount)
				{
					RecordError(ref context, ref settings, $"Expected at least {MinCount} repetitions of child rule, but found {elements.Count}.");
					return ParsedRule.Fail;
				}

				return new ParsedRule(Id, initialPosition, context.position - initialPosition,
					context.passedBarriers, elements);
			};

			parseFunction = Parse;

			parseFunction = WrapParseFunction(parseFunction, initFlags);
		}

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction(ref context, ref settings, ref childSettings);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}' " : string.Empty;

			string trailing = AllowTrailingSeparator ? " (allow trailing)" : "";
			if (remainingDepth <= 0)
				return $"SeparatedRepeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]{trailing}...";

			return $"SeparatedRepeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]{trailing}: " +
				   $"{GetRule(Rule).ToString(remainingDepth - 1)} sep {GetRule(Separator).ToString(remainingDepth - 1)}";
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}' " : string.Empty;

			string trailing = AllowTrailingSeparator ? " (allow trailing)" : "";
			if (remainingDepth <= 0)
				return $"SeparatedRepeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]{trailing}...";

			string mainPointer = childIndex == Rule ? " <-- here" : " ";
			string sepPointer = childIndex == Separator ? " <-- here" : "";

			return $"SeparatedRepeat{alias}[{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}]{trailing}: " +
				   $"{GetRule(Rule).ToString(remainingDepth - 1)}{mainPointer}\n" +
				   $"sep {GetRule(Separator).ToString(remainingDepth - 1)}{sepPointer}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SeparatedRepeatParserRule rule &&
				   Rule == rule.Rule &&
				   Separator == rule.Separator &&
				   MinCount == rule.MinCount &&
				   MaxCount == rule.MaxCount &&
				   AllowTrailingSeparator == rule.AllowTrailingSeparator &&
				   IncludeSeparatorsInResult == rule.IncludeSeparatorsInResult;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Rule.GetHashCode();
			hashCode = hashCode * -1521134295 + Separator.GetHashCode();
			hashCode = hashCode * -1521134295 + MinCount.GetHashCode();
			hashCode = hashCode * -1521134295 + MaxCount.GetHashCode();
			hashCode = hashCode * -1521134295 + AllowTrailingSeparator.GetHashCode();
			hashCode = hashCode * -1521134295 + IncludeSeparatorsInResult.GetHashCode();
			return hashCode;
		}
	}
}