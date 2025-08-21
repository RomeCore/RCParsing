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

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserCache"/> class.
		/// </summary>
		public ParserCache()
		{
			_rules = new Dictionary<(int, int), ParsedRule>();
		}

		/// <summary>
		/// Adds a parsed rule to the cache.
		/// </summary>
		/// <param name="ruleId">The ID of the rule.</param>
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