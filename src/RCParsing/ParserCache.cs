using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a cache for parsed data to improve performance.
	/// </summary>
	public class ParserCache
	{
		private struct RuleInfoTuple : IEquatable<RuleInfoTuple>
		{
			public int ruleId, position, passedBarriers;

			public readonly override int GetHashCode()
			{
				unchecked
				{
					return ruleId * 397 + position + passedBarriers * 397 * 397;
				}
			}
			public readonly override bool Equals(object? obj)
			{
				return obj is RuleInfoTuple tuple && Equals(tuple);
			}
			public readonly bool Equals(RuleInfoTuple other)
			{
				return ruleId == other.ruleId && position == other.position && passedBarriers == other.passedBarriers;
			}
		}

		private readonly Dictionary<RuleInfoTuple, ParsedRule> _rules;
		private readonly Dictionary<RuleInfoTuple, int> _begginningRules;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserCache"/> class.
		/// </summary>
		public ParserCache()
		{
			_rules = new();
			_begginningRules = new();
		}

		/// <summary>
		/// Tries to begin a new rule parsing at the specified position.
		/// </summary>
		/// <remarks>
		/// Useful for avoiding infinite recursion when parsing certain rules.
		/// </remarks>
		/// <param name="ruleId">The ID of the rule being parsed.</param>
		/// <param name="position">The starting position of the rule in the input text.</param>
		/// <param name="passedBarriers">The number of barrier tokens that have been passed.</param>
		/// <returns><see langword="true"/> if the rule parsing can be started; otherwise, <see langword="false"/>.</returns>
		public bool TryBeginRule(int ruleId, int position, int passedBarriers)
		{
			var tuple = new RuleInfoTuple { ruleId = ruleId, position = position, passedBarriers = passedBarriers };
			if (_begginningRules.TryGetValue(tuple, out var count))
			{
				if (count >= 2)
				{
					_begginningRules[tuple] = 0;
					return false;
				}
			}
			else
			{
				count = 0;
			}
			_begginningRules[tuple] = count + 1;
			return true;
		}

		/// <summary>
		/// Adds a parsed rule to the cache.
		/// </summary>
		/// <param name="ruleId">The ID of the rule being parsed.</param>
		/// <param name="position">The starting position of the rule in the input text.</param>
		/// <param name="passedBarriers">The number of barrier tokens that have been passed.</param>
		/// <param name="rule">The parsed rule to add.</param>
		public void AddRule(int ruleId, int position, int passedBarriers, ParsedRule rule)
		{
			var tuple = new RuleInfoTuple { ruleId = ruleId, position = position, passedBarriers = passedBarriers };
			_rules[tuple] = rule;
		}

		/// <summary>
		/// Retrieves a parsed rule from the cache.
		/// </summary>
		/// <param name="ruleId">The ID of the rule being parsed.</param>
		/// <param name="position">The starting position of the rule in the input text.</param>
		/// <param name="passedBarriers">The number of barrier tokens that have been passed.</param>m>
		/// <param name="rule">The parsed rule if found; otherwise, default.</param>
		/// <returns><see langword="true"/> if the parsed rule is found; otherwise, <see langword="false"/>.</returns>
		public bool TryGetRule(int ruleId, int position, int passedBarriers, out ParsedRule rule)
		{
			var tuple = new RuleInfoTuple { ruleId = ruleId, position = position, passedBarriers = passedBarriers };
			return _rules.TryGetValue(tuple, out rule);
		}
	}
}