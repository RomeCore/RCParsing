using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	/// Represents the result of a parsed rule that stores calculated values that can be reused.
	/// </summary>
	public sealed class ParsedRuleResultLazy : ParsedRuleResultBase, IOptimizedParsedRuleResult
	{
		public ParseTreeOptimization Optimization { get; }
		public override ParsedRuleResultBase? Parent { get; }

		/// <summary>
		/// The immutable <see cref="ParserContext"/> reference.
		/// </summary>
		public ParserContextReference ContextReference { get; }

		public override ParserContext Context => ContextReference.context;

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

				_valueConstructed = true;
				_value = Rule.ParsedValueFactory?.Invoke(this);
				return _value;
			}
		}

		private LazyArray<ParsedRuleResultBase>? _childrenLazy;
		public override IReadOnlyList<ParsedRuleResultBase> Children => _childrenLazy ??=
			new LazyArray<ParsedRuleResultBase>(Result.children?.Count ?? 0, ChildrenFactory);

		private ParsedRuleResultBase ChildrenFactory(int index)
		{
			return new ParsedRuleResultLazy(Optimization, this, ContextReference, Result.children[index]);
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
			: this (treeOptimization, parent, new ParserContextReference(context), result)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResultLazy"/> class.
		/// </summary>
		/// <param name="treeOptimization">The optimization flags that used to optimize the parse tree.</param>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResultLazy(ParseTreeOptimization treeOptimization,
			ParsedRuleResultBase? parent, ParserContextReference context, ParsedRule result)
		{
			Optimization = treeOptimization;
			Parent = parent;
			ContextReference = context;
			Result = treeOptimization == ParseTreeOptimization.None ? result
				: result.Optimized(Context, Optimization);
		}

		public override ParsedRuleResultBase Optimized(ParseTreeOptimization optimization = ParseTreeOptimization.Default)
		{
			return new ParsedRuleResultLazy(optimization, Parent, Context, Result);
		}

		public override ParsedRuleResultBase Updated(ParserContext newContext, ParsedRule newParsedRule)
		{
			var link = new ParserContextReference(newContext);
			return Updated(link, Parent, newParsedRule);
		}

		private ParsedRuleResultLazy Updated(ParserContextReference reference,
			ParsedRuleResultBase parent, ParsedRule newParsedRule)
		{
			var result = new ParsedRuleResultLazy(Optimization, parent, reference, newParsedRule);

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
						(i, v) => ((ParsedRuleResultLazy)v).Updated(ContextReference, result, newParsedRule.children[i]));
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