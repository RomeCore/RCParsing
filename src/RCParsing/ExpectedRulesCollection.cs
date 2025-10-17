using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace RCParsing
{
	/// <summary>
	/// Represents a collection of expected token patterns in the group of errors.
	/// </summary>
	public class ExpectedRulesCollection : IReadOnlyList<ExpectedElement<ParserRule>>
	{
		/// <summary>
		/// Gets the collection of expected token patterns.
		/// </summary>
		public IReadOnlyList<ExpectedElement<ParserRule>> Rules { get; }

		internal ExpectedRulesCollection(IReadOnlyList<ExpectedElement<ParserRule>> rules)
		{
			Rules = rules;
		}

		public int Count => Rules.Count;
		public ExpectedElement<ParserRule> this[int index] => Rules[index];
		public IEnumerator<ExpectedElement<ParserRule>> GetEnumerator() => Rules.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <remarks>
		/// Contains the list of expected parser rules. Each element is represented by its string representation.
		/// </remarks>
		/// <returns>A string that represents the current object with expected elements.</returns>
		public override string ToString()
		{
			return string.Join(Environment.NewLine, Rules.Select(e => e.Element.ToString()).OrderBy(v => v));
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <remarks>
		/// Contains the list of expected parser rules. Each element is represented by its string representation.
		/// </remarks>
		/// <returns>A string that represents the current object with expected elements.</returns>
		public string ToString(ErrorFormattingFlags flags)
		{
			if (flags.HasFlag(ErrorFormattingFlags.OnlyNamedElements))
				return string.Join(Environment.NewLine, Rules.Where(e => e.Alias != null)
					.Select(e => e.Element.ToString()).OrderBy(v => v));
			return string.Join(Environment.NewLine, Rules.Select(e => e.Element.ToString()).OrderBy(v => v));
		}
	}
}