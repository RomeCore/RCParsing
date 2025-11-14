using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RCParsing
{
	/// <summary>
	/// Represents the result of a parsed rule that calculates all values on creation.
	/// </summary>
	public sealed class ParsedRuleResultPrecalculated : ParsedRuleResultBase, IOptimizedParsedRuleResult
	{
		/// <summary>
		/// Gets the optimization flags that used to optimize the parse tree.
		/// </summary>
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

		public override object? Value { get; }
		public override IReadOnlyList<ParsedRuleResultBase> Children { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResultPrecalculated"/> class.
		/// </summary>
		/// <param name="treeOptimization">The optimization flags that used to optimize the parse tree.</param>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResultPrecalculated(ParseTreeOptimization treeOptimization,
			ParsedRuleResultBase? parent, ParserContext context, ParsedRule result)
			: this(treeOptimization, parent, new ParserContextReference(context), result)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResultPrecalculated"/> class.
		/// </summary>
		/// <param name="treeOptimization">The optimization flags that used to optimize the parse tree.</param>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResultPrecalculated(ParseTreeOptimization treeOptimization,
			ParsedRuleResultBase? parent, ParserContextReference context, ParsedRule result)
		{
			Optimization = treeOptimization;
			Parent = parent;
			ContextReference = context;
			Result = result = treeOptimization == ParseTreeOptimization.None ? result
				: result.Optimized(Context, treeOptimization);

			var children = new ParsedRuleResultBase[result.children?.Count ?? 0];

			if (result.children != null)
			{
				for (int i = 0; i < result.children.Count; i++)
				{
					var child = result.children[i];
					children[i] = new ParsedRuleResultPrecalculated(treeOptimization, this, context, child);
				}
			}

			Children = new ReadOnlyCollection<ParsedRuleResultBase>(children);
			Value = Rule.ParsedValueFactory?.Invoke(this);
		}

		private ParsedRuleResultPrecalculated(ParsedRuleResultBase old,
			ParsedRuleResultBase parent, ParserContextReference context, ParsedRule newResult)
		{
			if (old is IOptimizedParsedRuleResult oldOpt)
				Optimization = oldOpt.Optimization;
			else
				Optimization = ParseTreeOptimization.None;
			Parent = parent;
			ContextReference = context;
			Result = newResult = Optimization == ParseTreeOptimization.None ? newResult
				: newResult.Optimized(Context, Optimization);

			if (old.Version == newResult.version)
			{
				Value = old.Value;
				Children = old.Children;
			}
			else
			{
				var children = new ParsedRuleResultBase[newResult.children?.Count ?? 0];

				if (newResult.children != null)
				{
					if ((old.Count) == children.Length)
					{
						for (int i = 0; i < newResult.children.Count; i++)
						{
							var oldChild = old.Children[i];
							var child = newResult.children[i];
							children[i] = new ParsedRuleResultPrecalculated(oldChild, this, context, child);
						}
					}
					else
					{
						for (int i = 0; i < newResult.children.Count; i++)
						{
							var child = newResult.children[i];
							children[i] = new ParsedRuleResultPrecalculated(Optimization, this, context, child);
						}
					}
				}

				Children = new ReadOnlyCollection<ParsedRuleResultBase>(children);
				Value = Rule.ParsedValueFactory?.Invoke(this);
			}
		}

		public override ParsedRuleResultBase Optimized(ParseTreeOptimization optimization = ParseTreeOptimization.Default)
		{
			return new ParsedRuleResultLazy(optimization, Parent, Context, Result);
		}

		public override ParsedRuleResultBase Updated(ParserContext newContext, ParsedRule newParsedRule)
		{
			var link = new ParserContextReference(newContext);
			return new ParsedRuleResultPrecalculated(this, Parent, link, newParsedRule);
		}
	}
}