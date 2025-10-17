using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a sequence of rules that must be parsed in order.
	/// </summary>
	public class SequenceParserRule : ParserRule
	{
		private readonly int[] _rules;
		
		/// <summary>
		/// The rules ids that make up the sequence.
		/// </summary>
		public IReadOnlyList<int> Rules { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SequenceParserRule"/> class.
		/// </summary>
		/// <param name="parserRules">The rules ids that make up the sequence.</param>
		public SequenceParserRule(IEnumerable<int> parserRules)
		{
			_rules = parserRules?.ToArray() ?? throw new ArgumentNullException(nameof(parserRules));
			Rules = _rules.AsReadOnlyList();
			if (_rules.Length == 0)
				throw new ArgumentException("Sequence must have at least one rule");
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				HashSet<char> firstChars = new();
				for (int i = 0; i < Rules.Count; i++)
				{
					var rule = GetRule(Rules[i]);
					foreach (var ch in rule.FirstChars)
						firstChars.Add(ch);
					if (!rule.IsOptional)
						break;
				}
				return firstChars;
			}
		}
		protected override bool IsFirstCharDeterministicCore
		{
			get
			{
				bool isDeterministic = true;
				for (int i = 0; i < Rules.Count; i++)
				{
					var rule = GetRule(Rules[i]);
					isDeterministic = isDeterministic && rule.IsFirstCharDeterministic;
					if (!rule.IsOptional)
						break;
				}
				return isDeterministic;
			}
		}
		protected override bool IsOptionalCore => Rules.All(i => GetRule(i).IsOptional);



		#region Optimization

		private ParseDelegate parseFunction;
		private Func<ParserContext, ParserSettings, ParsedRule>[] parseFunctions;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			parseFunctions = new Func<ParserContext, ParserSettings, ParsedRule>[_rules.Length];

			for (int i = 0; i < _rules.Length; i++)
			{
				var id = Rules[i];
				var rule = GetRule(id);

				if (initFlags.HasFlag(ParserInitFlags.InlineRules) && rule.CanBeInlined && i == 0)
					parseFunctions[i] = (ctx, chStng) => rule.Parse(ctx, chStng, chStng);
				else
					parseFunctions[i] = (ctx, chStng) => TryParseRule(id, ctx, chStng);
			}

			ParsedRule Parse(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				var startIndex = context.position;
				ParsedRule[]? rules = null;

				for (int i = 0; i < parseFunctions.Length; i++)
				{
					var parsedRule = parseFunctions[i](context, childSettings);
					if (!parsedRule.success)
					{
						RecordError(ref context, ref settings, "Failed to parse sequence rule.");
						return ParsedRule.Fail;
					}

					rules ??= new ParsedRule[_rules.Length];
					parsedRule.occurency = i;
					rules[i] = parsedRule;
					context.position = parsedRule.startIndex + parsedRule.length;
					context.passedBarriers = parsedRule.passedBarriers;
				}

				return new ParsedRule(Id, startIndex, context.position - startIndex,
					context.passedBarriers, rules);
			};

			parseFunction = Parse;

			parseFunction = WrapParseFunction(parseFunction, initFlags);
		}

		#endregion

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction.Invoke(ref context, ref settings, ref childSettings);
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