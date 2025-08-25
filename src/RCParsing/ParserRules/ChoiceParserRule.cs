using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

		#region Optimization

		private Func<ParserContext, ParserContext, ParsedRule> parseFunction;
		private Func<ParserContext, ParsedRule>[] parseFunctions;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			parseFunctions = new Func<ParserContext, ParsedRule>[Choices.Length];

			for (int i = 0; i < Choices.Length; i++)
			{
				var id = Choices[i];
				var rule = GetRule(id);

				if (initFlags.HasFlag(ParserInitFlags.InlineRules) && rule.CanBeInlined)
					parseFunctions[i] = chCtx => rule.Parse(chCtx, chCtx);
				else
					parseFunctions[i] = chCtx => TryParseRule(id, chCtx);
			}

			parseFunction = (ctx, chCtx) =>
			{
				for (int i = 0; i < parseFunctions.Length; i++)
				{
					var parsedRule = parseFunctions[i](chCtx);

					if (parsedRule.success)
					{
						parsedRule.occurency = i;
						return ParsedRule.Rule(Id,
							parsedRule.startIndex,
							parsedRule.length,
							ParsedRuleChildUtils.Single(ref parsedRule),
							parsedRule.intermediateValue);
					}
				}

				RecordError(ref ctx, "Found no matching choice.");
				return ParsedRule.Fail;
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

		#endregion

		public override ParsedRule Parse(ParserContext context, ParserContext childContext)
		{
			return parseFunction(context, childContext);
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