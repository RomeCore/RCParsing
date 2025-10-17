﻿using System;
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

		public override ParsedRuleResultBase Optimized(ParseTreeOptimization optimization = ParseTreeOptimization.Default)
		{
			return new ParsedRuleResultOptimized(optimization, Parent, Context, Result);
		}

		public override ParsedRuleResultBase Updated(ParserContext newContext, ParsedRule newParsedRule)
		{
			return new ParsedRuleResult(Parent, newContext, newParsedRule);
		}
	}

	/// <summary>
	/// Represents a parsed rule result that calculated entirely on-demand without storing values (unlike lazy version).
	/// </summary>
	/// <remarks>
	/// Also stores optimization flags.
	/// </remarks>
	public sealed class ParsedRuleResultOptimized : ParsedRuleResultBase
	{
		/// <summary>
		/// Link wrapper for context to reduce allocations.
		/// </summary>
		private class ParserContextLink
		{
			public ParserContext context;
		}

		/// <summary>
		/// Gets the optimization flags that used to optimize the parse tree.
		/// </summary>
		public ParseTreeOptimization Optimization { get; }

		public override ParsedRuleResultBase this[int index] =>
			new ParsedRuleResultOptimized(Optimization, this, _ctx, Result.children[index]);
		public override int Count => Result.children?.Count ?? 0;
		public override IEnumerator<ParsedRuleResultBase> GetEnumerator()
		{
			return Result.children.Select(c =>
				new ParsedRuleResultOptimized(Optimization, this, _ctx, c)).GetEnumerator();
		}

		public override ParsedRuleResultBase? Parent { get; }
		private readonly ParserContextLink _ctx;
		public override ParserContext Context => _ctx.context;
		public override ParsedRule Result { get; }
		public override IReadOnlyList<ParsedRuleResultBase> Children => this;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResultOptimized"/> class.
		/// </summary>
		/// <param name="treeOptimization">The optimization flags that used to optimize the parse tree.</param>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResultOptimized(ParseTreeOptimization treeOptimization,
			ParsedRuleResultBase? parent, ParserContext context, ParsedRule result)
			: this(treeOptimization, parent, new ParserContextLink { context = context }, result)
		{
		}

		private ParsedRuleResultOptimized(ParseTreeOptimization treeOptimization,
			ParsedRuleResultBase? parent, ParserContextLink context, ParsedRule result)
		{
			Optimization = treeOptimization;
			Parent = parent;
			_ctx = context;
			Result = treeOptimization == ParseTreeOptimization.None ? result
				: result.Optimized(context.context, treeOptimization);
		}

		public override ParsedRuleResultBase Optimized(ParseTreeOptimization optimization = ParseTreeOptimization.Default)
		{
			return new ParsedRuleResultOptimized(optimization, Parent, Context, Result);
		}

		public override ParsedRuleResultBase Updated(ParserContext newContext, ParsedRule newParsedRule)
		{
			return new ParsedRuleResultOptimized(Optimization, Parent, newContext, newParsedRule);
		}
	}
}