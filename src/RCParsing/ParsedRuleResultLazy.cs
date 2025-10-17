using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using RCParsing.ParserRules;
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

		private bool _tokenCalculated;
		private ParsedTokenResult? _token;
		public override ParsedTokenResult? Token
		{
			get
			{
				if (_tokenCalculated)
					return _token;

				_tokenCalculated = true;
				if (IsToken)
				{
					var element = Result.element;
					var context = Context;
					return _token = new ParsedTokenResult(this, context, Result.element, TokenId);
				}
				return null;
			}
		}

		private string _text;
		public override string Text => _text ??= Context.input.Substring(Result.startIndex, Result.length);

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
		public override IReadOnlyList<ParsedRuleResultBase> Children => _childrenLazy ??=
			new LazyArray<ParsedRuleResultBase>(Result.children?.Count ?? 0, ChildrenFactory);

		private ParsedRuleResultBase ChildrenFactory(int index)
		{
			return new ParsedRuleResultLazy(Optimization, this, _ctx, Result.children[index]);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResultLazy"/> class.
		/// </summary>
		/// <param name="treeOptimization">The optimization flags that used to optimize the parse tree.</param>
		/// <param name="parent">The parent result of this rule, if any.</param>
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
			Result = treeOptimization == ParseTreeOptimization.None ? result
				: result.Optimized(Context, Optimization);
		}

		public override ParsedRuleResultBase Optimized(ParseTreeOptimization optimization = ParseTreeOptimization.Default)
		{
			return new ParsedRuleResultLazy(optimization, Parent, Context, Result);
		}

		public override ParsedRuleResultBase Updated(ParserContext newContext, ParsedRule newParsedRule)
		{
			var link = new ParserContextLink { context = newContext };
			return Updated(link, Parent, newParsedRule);
		}

		private ParsedRuleResultLazy Updated(ParserContextLink link,
			ParsedRuleResultBase parent, ParsedRule newParsedRule)
		{
			var result = new ParsedRuleResultLazy(Optimization, parent, link, newParsedRule);

			int version = Version;
			int newVersion = newParsedRule.version;

			if (version != newVersion)
			{
				// Invalidate cache if version changed.
				result._valueConstructed = false;
				result._value = null;
				result._tokenCalculated = false;
				result._token = null;

				if ((Result.children?.Count ?? 0) == (newParsedRule.children?.Count ?? 0))
					result._childrenLazy = _childrenLazy?.Converted(result.ChildrenFactory,
						(i, v) => ((ParsedRuleResultLazy)v).Updated(link, result, newParsedRule.children[i]));
				else
					result._childrenLazy = null;
			}
			else
			{
				result._valueConstructed = _valueConstructed;
				result._value = _value;
				result._tokenCalculated = _tokenCalculated;
				result._token = _token;
				result._childrenLazy = _childrenLazy?.WithFactory(result.ChildrenFactory);
			}

			return result;
		}
	}
}