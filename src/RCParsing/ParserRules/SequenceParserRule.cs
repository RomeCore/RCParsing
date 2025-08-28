using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a sequence of rules that must be parsed in order.
	/// </summary>
	public class SequenceParserRule : ParserRule
	{
		/// <summary>
		/// The rules ids that make up the sequence.
		/// </summary>
		public ImmutableArray<int> Rules { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SequenceParserRule"/> class.
		/// </summary>
		/// <param name="parserRules">The rules ids that make up the sequence.</param>
		public SequenceParserRule(IEnumerable<int> parserRules)
		{
			Rules = parserRules?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(parserRules));
			if (Rules.Length == 0)
				throw new ArgumentException("Sequence must have at least one rule");
		}

		protected override HashSet<char>? FirstCharsCore => GetRule(Rules[0]).FirstChars;



		#region Optimization

		private Func<ParserContext, ParserContext, ParsedRule> parseFunction;
		private Func<ParserContext, ParsedRule>[] parseFunctions;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			parseFunctions = new Func<ParserContext, ParsedRule>[Rules.Length];

			for (int i = 0; i < Rules.Length; i++)
			{
				var id = Rules[i];
				var rule = GetRule(id);

				if (initFlags.HasFlag(ParserInitFlags.InlineRules) && rule.CanBeInlined && i == 0)
					parseFunctions[i] = chCtx => rule.Parse(chCtx, chCtx);
				else
					parseFunctions[i] = chCtx => TryParseRule(id, chCtx);
			}

			parseFunction = (ctx, chCtx) =>
			{
				var startIndex = chCtx.position;
				ParsedRule[]? rules = null;

				for (int i = 0; i < parseFunctions.Length; i++)
				{
					var parsedRule = parseFunctions[i](chCtx);
					if (!parsedRule.success)
					{
						RecordError(ref ctx, "Failed to parse sequence rule.");
						return ParsedRule.Fail;
					}

					rules ??= new ParsedRule[Rules.Length];
					parsedRule.occurency = i;
					rules[i] = parsedRule;
					chCtx.position = parsedRule.startIndex + parsedRule.length;
					chCtx.passedBarriers = parsedRule.passedBarriers;
				}

				return ParsedRule.Rule(Id, startIndex, chCtx.position - startIndex, chCtx.passedBarriers, rules);
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
			return parseFunction.Invoke(context, childContext);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;

			if (remainingDepth <= 0)
				return $"Sequence{alias}...";

			return $"Sequence{alias}:\n" +
				string.Join("\n", Rules.Select(c => GetRule(c).ToString(remainingDepth - 1)))
				.Indent("  ");
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;

			if (remainingDepth <= 0)
				return $"Sequence{alias}...";

			return $"Sequence{alias}:\n" +
				string.Join("\n", Rules.Select(c =>
				{
					if (c == childIndex)
					{
						return GetRule(c).ToString(remainingDepth - 1) + " <-- here";
					}
					return GetRule(c).ToString(remainingDepth - 1);
				}))
				.Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return obj is SequenceParserRule rule &&
				   Rules.SequenceEqual(rule.Rules) &&
				   ParsedValueFactory == rule.ParsedValueFactory;
		}

		public override int GetHashCode()
		{
			int hashCode = 1930700721;
			hashCode = hashCode * -1521134295 + Rules.GetSequenceHashCode();
			hashCode = hashCode * -1521134295 + (ParsedValueFactory?.GetHashCode() ?? 0);
			return hashCode;
		}
	}
}