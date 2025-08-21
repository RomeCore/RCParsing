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



		public override ParsedRule Parse(ParserContext context, ParserContext childContext)
		{
			var startIndex = childContext.position;
			var rules = new List<ParsedRule>();
			int i = 0;

			foreach (var rule in Rules)
			{
				var parsedRule = TryParseRule(rule, childContext);
				if (!parsedRule.success)
				{
					RecordError(context, "Failed to parse sequence rule.");
					return ParsedRule.Fail;
				}

				parsedRule.occurency = i++;
				rules.Add(parsedRule);
				childContext.position = parsedRule.startIndex + parsedRule.length;
			}

			return ParsedRule.Rule(Id, startIndex, childContext.position - startIndex, rules);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "Sequence...";
			return $"Sequence:\n" +
				string.Join("\n", Rules.Select(c => GetRule(c).ToString(remainingDepth - 1)))
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
			hashCode = hashCode * -1521134295 + ParsedValueFactory.GetHashCode();
			return hashCode;
		}
	}
}