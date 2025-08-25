using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCParsing.Utils
{
	/// <summary>
	/// The utility methods for working with parsed rule's child collections.
	/// </summary>
	public static class ParsedRuleChildUtils
	{
		private class EmptyParsedRules : IReadOnlyList<ParsedRule>
		{
			public ParsedRule this[int index] => throw new ArgumentOutOfRangeException();
			public int Count => 0;

			public IEnumerator<ParsedRule> GetEnumerator()
			{
				return Enumerable.Empty<ParsedRule>().GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		private class SingleParsedRules : IReadOnlyList<ParsedRule>
		{
			public ParsedRule rule;

			public ParsedRule this[int index] => rule;
			public int Count => 1;

			public IEnumerator<ParsedRule> GetEnumerator()
			{
				yield return rule;
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>
		/// Gets an empty parsed rules list. This is a read-only collection that cannot be modified.
		/// </summary>
		public static readonly IReadOnlyList<ParsedRule> empty = new EmptyParsedRules();

		/// <summary>
		/// Gets a parsed rules list with a single element. This is a read-only collection that cannot be modified.
		/// </summary>
		/// <param name="rule">The parsed rule to include in the list.</param>
		/// <returns>A read-only collection containing a single parsed rule.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IReadOnlyList<ParsedRule> Single(ref ParsedRule rule)
		{
			return new SingleParsedRules { rule = rule };
		}
	}
}