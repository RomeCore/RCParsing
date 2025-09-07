using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents the flags that can be used to optimize the parse tree.
	/// </summary>
	[Flags]
	public enum ParseTreeOptimization
	{
		/// <summary>
		/// No optimization flags are set.
		/// </summary>
		None = 0,

		/// <summary>
		/// The default optimization flags are set. These include:
		/// <list type="bullet">
		/// <item>IgnorePureLiterals</item>
		/// <item>RemoveEmptyOrWhitespaceNodes</item>
		/// <item>MergeSingleChildRules</item>
		/// <item>RecalculateSpans</item>
		/// </list>
		/// </summary>
		Default = RemoveEmptyOrWhitespaceNodes | MergeSingleChildRules | TrimSpans,

		/// <summary>
		/// Removes pure literals (char and string) in the parse tree. Does not affects literal choices.
		/// </summary>
		RemovePureLiterals = 1,

		/// <summary>
		/// Removes empty nodes from the parse tree.
		/// </summary>
		RemoveEmptyNodes = 2,

		/// <summary>
		/// Removes whitespace nodes from the parse tree.
		/// </summary>
		RemoveWhitespaceNodes = 4,

		/// <summary>
		/// Removes whitespace and empty nodes from the parse tree.
		/// </summary>
		RemoveEmptyOrWhitespaceNodes = RemoveEmptyNodes | RemoveWhitespaceNodes,

		/// <summary>
		/// Merges single child rules into their parent recursively.
		/// </summary>
		MergeSingleChildRules = 8,

		/// <summary>
		/// Recalculates the start index and length of each node in the parse tree to remove the leading and trailing whitespace from each node's span.
		/// </summary>
		TrimSpans = 16,
	}

	/// <summary>
	/// Represents the result of a parsed rule.
	/// </summary>
	/// <remarks>
	/// This is a entirely lazy wrapper around <see cref="ParsedRule"/>.
	/// </remarks>
	public sealed class ParsedRuleResultLazy : ParsedRuleResultBase
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

		public override ParsedRuleResultBase? Parent { get; }
		private readonly ParserContextLink _ctx;
		public override ParserContext Context => _ctx.context;
		public override ParsedRule Result { get; }

		private ParsedTokenResult? _token;
		public override ParsedTokenResult? Token => Result.isToken ? _token ??=
			new ParsedTokenResult(this, Context, Result.element, Result.tokenId) : null;

		private string _text;
		public override string Text => _text ??= Context.str.Substring(Result.startIndex, Result.length);

		private bool _valueConstructed;
		private object? _value;
		public override object? Value
		{
			get
			{
				if (_valueConstructed)
					return _value;

				_value = Rule.ParsedValueFactory?.Invoke(this) ?? null;
				_valueConstructed = true;
				return _value;
			}
		}

		private LazyArray<ParsedRuleResultBase>? _childrenLazy;
		public override IReadOnlyList<ParsedRuleResultBase> Children => _childrenLazy ??= new LazyArray<ParsedRuleResultBase>(Result.children?.Count ?? 0, i =>
		{
			return new ParsedRuleResultLazy(Optimization, this, _ctx, Result.children[i]);
		});

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResultLazy"/> class.
		/// </summary>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="treeOptimization">The optimization flags that used to optimize the parse tree.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResultLazy(ParseTreeOptimization treeOptimization,
			ParsedRuleResultBase? parent, ParserContext context, ParsedRule result)
			: this (treeOptimization, parent, new ParserContextLink { context = context }, result)
		{
		}
		
		private ParsedRuleResultLazy(ParseTreeOptimization treeOptimization,
			ParsedRuleResultBase? parent, ParserContextLink context, ParsedRule result)
		{
			Optimization = treeOptimization;
			Parent = parent;
			_ctx = context;
			Result = treeOptimization == ParseTreeOptimization.None ? result : Optimized(result, _ctx, Optimization);
		}

		private static ParsedRule Optimized(ParsedRule rule, ParserContextLink link, ParseTreeOptimization optimization)
		{
			if (rule.children == null || rule.children.Count == 0 || optimization == ParseTreeOptimization.None)
				return rule;

			var context = link.context;
			IEnumerable<ParsedRule> rawChildren = rule.children;

			if (optimization.HasFlag(ParseTreeOptimization.RemoveEmptyNodes))
				rawChildren = rawChildren.Where(c => c.length != 0);

			if (optimization.HasFlag(ParseTreeOptimization.RemoveWhitespaceNodes))
				rawChildren = rawChildren.Where(c => !context.str.AsSpan(c.startIndex, c.length).IsWhiteSpace());

			if (optimization.HasFlag(ParseTreeOptimization.RemovePureLiterals))
				rawChildren = rawChildren.Where(c => !(c.isToken && (
					context.parser.TokenPatterns[c.tokenId] is LiteralTokenPattern ||
					context.parser.TokenPatterns[c.tokenId] is LiteralCharTokenPattern)));

			if (optimization.HasFlag(ParseTreeOptimization.MergeSingleChildRules))
			{
				var children = rawChildren.ToList();
				if (children.Count == 1)
				{
					return Optimized(children[0], link, optimization);
				}
			}

			if (optimization.HasFlag(ParseTreeOptimization.TrimSpans))
			{
				var span = context.str.AsSpan(rule.startIndex, rule.length);
				
				int startIndex = 0;
				int length = span.Length;

				while (startIndex < span.Length && char.IsWhiteSpace(span[startIndex]))
					startIndex++;

				if (startIndex == span.Length)
				{
					rule.length = 0;
				}
				else
				{
					while (length > startIndex && char.IsWhiteSpace(span[length - 1]))
						length--;

					rule.startIndex += startIndex;
					rule.length = length - startIndex;
				}
			}

			rule.children = rawChildren.ToList();
			return rule;
		}
	}
}