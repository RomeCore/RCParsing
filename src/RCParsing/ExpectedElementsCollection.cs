using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using RCParsing.ParserRules;

namespace RCParsing
{
	/// <summary>
	/// The collection of parser elements that are expected at the specific error position.
	/// </summary>
	public class ExpectedElementsCollection : IReadOnlyList<ExpectedElement<ParserElement>>
	{
		/// <summary>
		/// Gets the collection of parser elements that are expected at the specific error position.
		/// </summary>
		public IReadOnlyList<ExpectedElement<ParserElement>> Elements { get; }

		private ExpectedTokensCollection _tokens;
		/// <summary>
		/// Gets a list of token patterns that are expected by this parser element.
		/// </summary>
		public ExpectedTokensCollection Tokens => _tokens ??= new ExpectedTokensCollection(Elements.Select(e =>
		{
			if (e.Element is TokenPattern p)
				return new ExpectedElement<TokenPattern>(p, e.Message, e.StackTrace);
			if (e.Element is TokenParserRule r)
				return new ExpectedElement<TokenPattern>(r.TokenPattern, e.Message, e.StackTrace);
			return null;
		}).Where(p => p != null).ToImmutableList());

		private ExpectedRulesCollection _rules;
		/// <summary>
		/// Gets the list of rules that are expected to be parsed.
		/// </summary>
		public ExpectedRulesCollection Rules => _rules ??= new ExpectedRulesCollection(Elements.Select(e =>
		{
			if (e.Element is TokenParserRule)
				return null;
			if (e.Element is ParserRule r)
				return new ExpectedElement<ParserRule>(r, e.Message, e.StackTrace);
			return null;
		}).Where(p => p != null).ToImmutableList());

		internal ExpectedElementsCollection(IEnumerable<(ParserElement, string, ParserStackTrace)> elements)
		{
			Elements = elements.Select(e => new ExpectedElement<ParserElement>(e.Item1, e.Item2, e.Item3)).ToImmutableList();
		}

		public int Count => Elements.Count;
		public ExpectedElement<ParserElement> this[int index] => Elements[index];
		public IEnumerator<ExpectedElement<ParserElement>> GetEnumerator() => Elements.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <remarks>
		/// Contains the list of expected parser elements. Each element is represented by its string representation.
		/// </remarks>
		/// <returns>A string that represents the current object with expected elements.</returns>
		public override string ToString()
		{
			return string.Join(Environment.NewLine, Elements.Select(e => e.Element.ToString()));
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <remarks>
		/// Contains the list of expected parser elements. Each element is represented by its string representation.
		/// </remarks>
		/// <returns>A string that represents the current object with expected elements.</returns>
		public string ToString(ErrorFormattingFlags flags)
		{
			if (flags.HasFlag(ErrorFormattingFlags.DisplayRules))
				return string.Join(Environment.NewLine, Elements.Select(e => e.Element.ToString()).OrderBy(v => v));
			return Tokens.ToString();
		}
	}
}