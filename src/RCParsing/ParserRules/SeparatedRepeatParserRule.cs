using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
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

		protected override HashSet<char>? FirstCharsCore => MinCount == 0 ? null :
			GetRule(Rule).FirstChars;



		private Func<ParserContext, ParserContext, ParsedRule> parseFunction;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			parseFunction = (ctx, chCtx) =>
			{
				var elements = new List<ParsedRule>();
				var initialPosition = chCtx.position;

				// Try to parse the first element (if required - error if not found; if optional - may return empty result)
				var firstElement = TryParseRule(Rule, chCtx);
				if (!firstElement.success)
				{
					// No first element found
					// If minCount == 0 — OK: empty sequence (but first check if there's a separator at the start)
					if (MinCount == 0)
					{
						// If there is a separator at the start — this is an error (unexpected leading separator)
						var sepAtStart = TryParseRule(Separator, chCtx);
						if (sepAtStart.success)
						{
							RecordError(ref ctx, "Unexpected separator before any element.");
							return ParsedRule.Fail;
						}

						// No elements and no separator — return successful empty result
						return ParsedRule.Rule(Id, initialPosition, 0, elements);
					}
					else
					{
						// Minimum count > 0, but first element not found — explicit error
						RecordError(ref ctx, $"Expected at least {MinCount} repetitions of child rule, but found 0.");
						return ParsedRule.Fail;
					}
				}

				// We have the first element
				if (firstElement.length == 0)
				{
					RecordError(ref ctx, "Parsed child element has zero length, which is not allowed.");
					return ParsedRule.Fail;
				}

				firstElement.occurency = elements.Count;
				elements.Add(firstElement);
				chCtx.position = firstElement.startIndex + firstElement.length;

				// Parse "separator + element" until limit reached
				while (MaxCount == -1 || elements.Count < MaxCount)
				{
					var beforeSepPos = chCtx.position;

					// Try to parse the separator
					var parsedSep = TryParseRule(Separator, chCtx);
					if (!parsedSep.success)
						break; // no separator — end of sequence

					if (parsedSep.length == 0)
					{
						RecordError(ref ctx, "Parsed separator has zero length, which is not allowed.");
						return ParsedRule.Fail;
					}

					// Include separator in result if requested
					if (IncludeSeparatorsInResult)
						elements.Add(parsedSep);

					// Separator successfully parsed — position already updated inside TryParseRule, but update again for safety:
					chCtx.position = parsedSep.startIndex + parsedSep.length;

					// Try to parse the next element
					var nextElement = TryParseRule(Rule, chCtx);
					if (!nextElement.success)
					{
						// Separator was found, but next element is missing
						if (AllowTrailingSeparator)
						{
							// Trailing separator allowed — consider separator consumed and stop.
							// Keep childContext.position after separator as is and exit loop.
							break;
						}
						else
						{
							RecordError(ref ctx, "Expected element after separator, but found none.");
							return ParsedRule.Fail;
						}
					}

					if (nextElement.length == 0)
					{
						RecordError(ref ctx, "Parsed child element has zero length, which is not allowed.");
						return ParsedRule.Fail;
					}

					nextElement.occurency = elements.Count;
					elements.Add(nextElement);
					chCtx.position = nextElement.startIndex + nextElement.length;

					// loop continues — try to find next separator + element
				}

				// Check minimum count
				if (elements.Count < MinCount)
				{
					RecordError(ref ctx, $"Expected at least {MinCount} repetitions of child rule, but found {elements.Count}.");
					return ParsedRule.Fail;
				}

				return ParsedRule.Rule(Id, initialPosition, chCtx.position - initialPosition, elements);
			};

			if (initFlags.HasFlag(ParserInitFlags.EnableMemoization))
			{
				var previous = parseFunction;
				parseFunction = (ctx, chCtx) =>
				{
					if (ctx.cache.TryGetRule(Id, ctx.position, out var cachedResult))
						return cachedResult;
					cachedResult = previous(ctx, chCtx);
					ctx.cache.AddRule(Id, ctx.position, cachedResult);
					return cachedResult;
				};
			}
		}

		public override ParsedRule Parse(ParserContext context, ParserContext childContext)
		{
			return parseFunction(context, childContext);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}' " : string.Empty;

			string trailing = AllowTrailingSeparator ? " (allow trailing)" : "";
			if (remainingDepth <= 0)
				return $"SeparatedRepeat{alias}{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}{trailing}...";

			return $"SeparatedRepeat{alias}{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}{trailing}: " +
				   $"{GetRule(Rule).ToString(remainingDepth - 1)} sep {GetRule(Separator).ToString(remainingDepth - 1)}";
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}' " : string.Empty;

			string trailing = AllowTrailingSeparator ? " (allow trailing)" : "";
			if (remainingDepth <= 0)
				return $"SeparatedRepeat{alias}{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}{trailing}...";

			string mainPointer = childIndex == Rule ? " <--- here" : " ";
			string sepPointer = childIndex == Separator ? " <--- here" : "";

			return $"SeparatedRepeat{alias}{alias}{{{MinCount}..{(MaxCount == -1 ? "" : MaxCount)}}}{trailing}: " +
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