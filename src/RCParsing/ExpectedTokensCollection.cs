using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RCParsing
{
	/// <summary>
	/// Represents a collection of expected token patterns in the group of errors.
	/// </summary>
	public class ExpectedTokensCollection : IReadOnlyList<ExpectedElement<TokenPattern>>
	{
		/// <summary>
		/// Gets the collection of expected token patterns.
		/// </summary>
		public IReadOnlyList<ExpectedElement<TokenPattern>> TokenPatterns { get; }

		internal ExpectedTokensCollection(IReadOnlyList<ExpectedElement<TokenPattern>> tokenPatterns)
		{
			TokenPatterns = tokenPatterns;
		}

		public int Count => TokenPatterns.Count;
		public ExpectedElement<TokenPattern> this[int index] => TokenPatterns[index];
		public IEnumerator<ExpectedElement<TokenPattern>> GetEnumerator() => TokenPatterns.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <remarks>
		/// Contains the list of expected token patterns. Each element is represented by its string representation.
		/// </remarks>
		/// <returns>A string that represents the current object with expected elements.</returns>
		public override string ToString()
		{
			return string.Join(Environment.NewLine, TokenPatterns.Select(e => e.Element.ToString()).OrderBy(v => v));
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <remarks>
		/// Contains the list of expected token patterns. Each element is represented by its string representation.
		/// </remarks>
		/// <returns>A string that represents the current object with expected elements.</returns>
		public string ToString(ErrorFormattingFlags flags)
		{
			if (flags.HasFlag(ErrorFormattingFlags.OnlyNamedElements))
				return string.Join(Environment.NewLine, TokenPatterns.Where(e => e.Alias != null)
					.Select(e => e.Element.ToString()).OrderBy(v => v));
			return string.Join(Environment.NewLine, TokenPatterns.Select(e => e.Element.ToString()).OrderBy(v => v));
		}
	}
}