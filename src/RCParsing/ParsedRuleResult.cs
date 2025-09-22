using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a parsed rule result that calculated entirely on-demand without storing values (unlike lazy version).
	/// </summary>
	public sealed class ParsedRuleResult : ParsedRuleResultBase
	{
		/// <summary>
		/// Link wrapper for context to reduce allocations.
		/// </summary>
		private class ParserContextLink
		{
			public ParserContext context;
		}

		public override ParsedRuleResultBase this[int index] =>
			new ParsedRuleResult(this, _ctx, Result.children[index]);
		public override int Count => Result.children?.Count ?? 0;
		public override IEnumerator<ParsedRuleResultBase> GetEnumerator()
		{
			return Result.children.Select(c =>
				new ParsedRuleResult(this, _ctx, c)).GetEnumerator();
		}

		public override ParsedRuleResultBase? Parent { get; }
		private readonly ParserContextLink _ctx;
		public override ParserContext Context => _ctx.context;
		public override ParsedRule Result { get; }
		public override IReadOnlyList<ParsedRuleResultBase> Children => this;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResult"/> class.
		/// </summary>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResult(ParsedRuleResultBase? parent, ParserContext context, ParsedRule result)
			: this(parent, new ParserContextLink { context = context }, result)
		{
		}

		private ParsedRuleResult(ParsedRuleResultBase? parent, ParserContextLink context, ParsedRule result)
		{
			Parent = parent;
			_ctx = context;
			Result = result;
		}

		public override ParsedRuleResultBase Updated(ParserContext newContext, ParsedRule newParsedRule)
		{
			return new ParsedRuleResult(Parent, newContext, newParsedRule);
		}
	}
}