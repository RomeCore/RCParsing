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
	public class ParsedRuleResult : IReadOnlyList<ParsedRuleResult>
	{
		/// <summary>
		/// Gets the optimization flags that used to optimize the parse tree.
		/// </summary>
		public ParseTreeOptimization Optimization { get; }

		/// <summary>
		/// Gets the parent result of this rule, if any.
		/// </summary>
		public ParsedRuleResult? Parent { get; }

		/// <summary>
		/// Gets the parser context used for parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets the parsed rule object containing the result of the parse.
		/// </summary>
		public ParsedRule Result { get; }

		private ParsedTokenResult? _token;
		/// <summary>
		/// Gets the token result if the parsed result represents a token. Otherwise, returns null.
		/// </summary>
		public ParsedTokenResult? Token => Result.isToken ? _token ??= new ParsedTokenResult(this, Context, Result.element) : null;

		/// <summary>
		/// Gets value indicating whether the parsing operation was successful.
		/// </summary>
		public bool Success => Result.success;

		/// <summary>
		/// Gets value indicating whether the parsed result represents a token.
		/// </summary>
		public bool IsToken => Result.isToken;

		/// <summary>
		/// Gets the unique identifier for the parser rule that was parsed.
		/// </summary>
		public int RuleId => Result.ruleId;

		/// <summary>
		/// Gets the parsed value associated with this rule.
		/// </summary>
		public ParserRule Rule => Context.parser.Rules[Result.ruleId];

		/// <summary>
		/// Gets the alias for the parser rule that was parsed. May be null if no alias is defined.
		/// </summary>
		public string RuleAlias => Rule.Aliases.Count > 0 ? Rule.Aliases[Rule.Aliases.Count - 1] : null;

		/// <summary>
		/// Gets the aliases for the parser rule that was parsed.
		/// </summary>
		public ImmutableList<string> RuleAliases => Rule.Aliases;

		/// <summary>
		/// Gets the starting index of the rule in the input text.
		/// </summary>
		public int StartIndex => Result.startIndex;

		/// <summary>
		/// Gets the length of the rule in the input text.
		/// </summary>
		public int Length => Result.length;

		/// <summary>
		/// Gets the occurency index in the parent choice, sequence or any repeat rule. -1 by default.
		/// </summary>
		public int Occurency => Result.occurency;

		/// <summary>
		/// Gets the intermediate value associated with this rule.
		/// </summary>
		public object? IntermediateValue => Result.intermediateValue;

		/// <summary>
		/// Gets the parsing parameter object that was passed to the parser during parsing. May be null if no parameter is passed.
		/// </summary>
		public object? ParsingParameter => Context.parserParameter;

		private string _text;
		/// <summary>
		/// Gets the parsed input text that was captured.
		/// </summary>
		public string Text => _text ??= Context.str.Substring(Result.startIndex, Result.length);

		/// <summary>
		/// Gets the parsed input text that was captured as a span of characters.
		/// </summary>
		public ReadOnlySpan<char> Span => Context.str.AsSpan(Result.startIndex, Result.length);

		private bool _valueConstructed;
		private object? _value;
		/// <summary>
		/// Gets the parsed value associated with this rule.
		/// </summary>
		public object? Value
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

		private LazyArray<ParsedRuleResult>? _childrenLazy;
		/// <summary>
		/// Gets the children results of this rule. Valid for parallel and sequence rules.
		/// </summary>
		public LazyArray<ParsedRuleResult> Children => _childrenLazy ??= new LazyArray<ParsedRuleResult>(Result.children?.Count ?? 0, i =>
		{
			return new ParsedRuleResult(Optimization, this, Context, Result.children[i]);
		});

		public int Count => Result.children.Count;
		public ParsedRuleResult this[int index] => Children[index];

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedRuleResult"/> class.
		/// </summary>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="treeOptimization">The optimization flags that used to optimize the parse tree.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed rule object containing the result of the parse.</param>
		public ParsedRuleResult(ParseTreeOptimization treeOptimization,
			ParsedRuleResult? parent, ParserContext context, ParsedRule result)
		{
			Optimization = treeOptimization;
			Parent = parent;
			Context = context;
			Result = treeOptimization == ParseTreeOptimization.None ? result : Optimized(result, context, Optimization);
		}

		private static ParsedRule Optimized(ParsedRule rule, ParserContext context, ParseTreeOptimization optimization)
		{
			if (rule.children == null || rule.children.Count == 0 || optimization == ParseTreeOptimization.None)
				return rule;

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
					return Optimized(children[0], context, optimization);
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

		/// <summary>
		/// Returns a optimized version of this parsed rule result.
		/// </summary>
		/// <remarks>
		/// Optimizes the parse tree by applying the specified optimization flags.
		/// Note that this may affect the parsed value calculation.
		/// </remarks>
		/// <param name="optimization">The optimization flags to apply.</param>
		/// <returns>An optimized version of this parsed rule result.</returns>
		public ParsedRuleResult Optimized(ParseTreeOptimization optimization = ParseTreeOptimization.Default)
		{
			return new ParsedRuleResult(optimization, Parent, Context, Result);
		}

		/// <summary>
		/// Gets the text captured by child rule at the specific index.
		/// </summary>
		/// <returns>The text captured by child rule.</returns>
		public string GetText(int index) => Children[index].Text;

		/// <summary>
		/// Gets the intermediate value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this rule.</returns>
		public T GetIntermediateValue<T>() => (T)IntermediateValue;

		/// <summary>
		/// Gets the intermediate value associated with child rule at the specific index as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with child rule.</returns>
		public T GetIntermediateValue<T>(int index) => (T)Children[index].IntermediateValue;

		/// <summary>
		/// Tries to get the intermediate value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this rule.</returns>
		public T? TryGetIntermediateValue<T>() where T : class => IntermediateValue as T;

		/// <summary>
		/// Tries to get the intermediate value associated with child rule at the specific index as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with child rule.</returns>
		public T? TryGetIntermediateValue<T>(int index) where T : class
			=> Children.Length > index ? Children[index].IntermediateValue as T : null;

		/// <summary>
		/// Gets the intermediate value associated with this rule converted to type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this rule.</returns>
		public T ConvertIntermediateValue<T>() => (T)Convert.ChangeType(IntermediateValue, typeof(T));

		/// <summary>
		/// Gets the intermediate value associated with child rule at the specific index converted to type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with child rule.</returns>
		public T ConvertIntermediateValue<T>(int index) => (T)Convert.ChangeType(Children[index].IntermediateValue, typeof(T));

		/// <summary>
		/// Gets the value associated with this rule as not-null object. If the value is null, throws an exception.
		/// </summary>
		/// <returns>The value associated with this rule.</returns>
		public object GetValue() => Value ?? throw new InvalidOperationException("ParsedRuleResult.Value is null");

		/// <summary>
		/// Gets the value associated with child rule at the specific index as not-null object. If the value is null, throws an exception.
		/// </summary>
		/// <returns>The value associated with child rule.</returns>
		public object GetValue(int index) => Children[index].Value ?? throw new InvalidOperationException("ParsedRuleResult.Value is null");

		/// <summary>
		/// Gets the value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this rule.</returns>
		public T GetValue<T>() => (T)Value;

		/// <summary>
		/// Gets the value associated with child rule at the specific index as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with child rule.</returns>
		public T GetValue<T>(int index) => (T)Children[index].Value;

		/// <summary>
		/// Tries to get the value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this rule.</returns>
		public T? TryGetValue<T>() where T : class => Value as T;

		/// <summary>
		/// Tries to get the value associated with child rule at the specific index as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with child rule.</returns>
		public T? TryGetValue<T>(int index) where T : class
			=> Children.Length > index ? Children[index].Value as T : null;

		/// <summary>
		/// Gets the value associated with this rule converted to type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this rule.</returns>
		public T ConvertValue<T>() => (T)Convert.ChangeType(Value, typeof(T));

		/// <summary>
		/// Gets the value associated with child rule at the specific index converted to type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with child rule.</returns>
		public T ConvertValue<T>(int index) => (T)Convert.ChangeType(Children[index].Value, typeof(T));

		/// <summary>
		/// Gets the parsing parameter associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of parsing parameter to retrieve.</typeparam>
		/// <returns>The parsing parameter associated with this rule.</returns>
		public T GetParsingParameter<T>() => (T)ParsingParameter;

		/// <summary>
		/// Tries to get the parsing parameter associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of parsing parameter to retrieve.</typeparam>
		/// <returns>The parsing parameter associated with this rule.</returns>
		public T? TryGetParsingParameter<T>() where T : class => ParsingParameter as T;

		/// <summary>
		/// Selects the children values array of this rule.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public object?[] SelectArray()
		{
			var result = new object?[Result.children?.Count ?? 0];
			if (result.Length == 0)
				return result;

			int i = 0;
			foreach (var child in Children)
				result[i++] = child.Value;
			return result;
		}

		/// <summary>
		/// Selects the children values array of child rule at the specified index.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public object?[] SelectArray(int index)
		{
			return Children[index].SelectArray();
		}

		/// <summary>
		/// Selects the children values of this rule.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public IEnumerable<object> SelectValues()
		{
			return Children.Select(child => child.GetValue());
		}

		/// <summary>
		/// Selects the children values of child rule at the specified index.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public IEnumerable<object> SelectValues(int index)
		{
			return Children[index].SelectValues();
		}

		/// <summary>
		/// Selects the casted children values of this rule.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public T[] SelectArray<T>()
		{
			var result = new T[Result.children?.Count ?? 0];
			if (result.Length == 0)
				return result;

			int i = 0;
			foreach (var child in Children)
				result[i++] = child.GetValue<T>();
			return result;
		}

		/// <summary>
		/// Selects the casted children values array of child rule at the specified index.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public T[] SelectArray<T>(int index)
		{
			return Children[index].SelectArray<T>();
		}

		/// <summary>
		/// Selects the casted children values of this rule.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public IEnumerable<T> SelectValues<T>()
		{
			return Children.Select(child => child.GetValue<T>());
		}

		/// <summary>
		/// Selects the casted children values of child rule at the specified index.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public IEnumerable<T> SelectValues<T>(int index)
		{
			return Children[index].SelectValues<T>();
		}

		/// <summary>
		/// Selects the children of this rule using a selector function.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <param name="selector">The selector function to apply to each child.</param>
		/// <returns>The selected values from the children.</returns>
		public T[] SelectArray<T>(Func<ParsedRuleResult, T> selector)
		{
			var result = new T[Result.children?.Count ?? 0];
			if (result.Length == 0)
				return result;

			int i = 0;
			foreach (var child in Children)
				result[i++] = selector(child);
			return result;
		}

		/// <summary>
		/// Selects the children of child rule at the specified index using a selector function.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The selected values from the children.</returns>
		public T[] SelectArray<T>(int index, Func<ParsedRuleResult, T> selector)
		{
			return Children[index].SelectArray(selector);
		}

		/// <summary>
		/// Gets child parsed rules for this rule and joins them into a single collection.
		/// </summary>
		/// <param name="maxDepth">The maximum depth to which child rules should be joined. If less than or equal to zero, this element is returned.</param>
		/// <returns>A collection of child parsed rules. Returns this element if no children are present or the maximum depth is reached.</returns>
		public IEnumerable<ParsedRuleResult> GetJoinedChildren(int maxDepth)
		{
			if (maxDepth <= 0 || (Result.children?.Count ?? 0) == 0)
				return this.WrapIntoEnumerable();

			return Children.SelectMany(r => r.GetJoinedChildren(maxDepth - 1));
		}

		public IEnumerator<ParsedRuleResult> GetEnumerator()
		{
			return Children.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return Children.GetEnumerator();
		}

		public string Dump(int maxDepth)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine($"Rule: {Rule.ToString(2)}");

			string valueStr = string.Empty;
			try
			{
				valueStr = Value?.ToString() ?? "null";
			}
			catch (Exception ex)
			{
				valueStr = $"Error {ex.GetType().Name}: {ex.Message}";
			}

			string intermediateValueStr = IntermediateValue?.ToString() ?? "null";

			if (IsToken)
				sb.AppendLine(Token.Dump());

			sb.AppendLine($"Value: {valueStr}");
			sb.AppendLine($"Intermediate Value: {intermediateValueStr}");

			foreach (var child in Children)
			{
				sb.AppendLine(child.Dump(maxDepth - 1).Indent("  "));
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return Text;
		}
	}
}