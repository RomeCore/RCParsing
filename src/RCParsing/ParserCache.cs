using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a cache for parsed data to improve performance.
	/// </summary>
	public class ParserCache
	{
		private readonly Dictionary<(int, int), ParsedRule> _rules;
		private readonly Dictionary<(int, int), int> _begginningRules;

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
		/// <returns><see langword="true"/> if the rule parsing can be started; otherwise, <see langword="false"/>.</returns>
		public bool TryBeginRule(int ruleId, int position)
		{
			if (_begginningRules.TryGetValue((ruleId, position), out var count))
			{
				if (count >= 2)
				{
					_begginningRules[(ruleId, position)] = 0;
					return false;
				}
			}
			else
			{
				count = 0;
			}
			_begginningRules[(ruleId, position)] = count + 1;
			return true;
		}

		/// <summary>
		/// Adds a parsed rule to the cache.
		/// </summary>
		/// <param name="ruleId">The ID of the rule being parsed.</param>
		/// <param name="position">The position of the rule.</param>
		/// <param name="rule">The parsed rule to add.</param>
		public void AddRule(int ruleId, int position, ParsedRule rule)
		{
			_rules[(ruleId, position)] = rule;
		}

		/// <summary>
		/// Retrieves a parsed rule from the cache.
		/// </summary>
		/// <param name="ruleId">The ID of the rule.</param>
		/// <param name="position"> The position of the rule. </param>
		/// <param name="rule">The parsed rule if found; otherwise, null.</param>
		/// <returns><see langword="true"/> if the parsed rule is found; otherwise, <see langword="false"/>.</returns>
		public bool TryGetRule(int ruleId, int position, out ParsedRule rule)
		{
			return _rules.TryGetValue((ruleId, position), out rule);
		}
	}
}