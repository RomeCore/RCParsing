using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents a base class for parsed rule as the Abstract Syntax Tree (AST).
	/// </summary>
	public abstract class ParsedRuleResultBase : IReadOnlyList<ParsedRuleResultBase>
	{
		/// <summary>
		/// Gets the parent result of this rule, if any.
		/// </summary>
		public abstract ParsedRuleResultBase? Parent { get; }

		/// <summary>
		/// Gets the parser context used for parsing.
		/// </summary>
		public abstract ParserContext Context { get; }

		/// <summary>
		/// Gets the parsed rule object containing the result of the parse.
		/// </summary>
		public abstract ParsedRule Result { get; }

		/// <summary>
		/// Gets the token result if the parsed result represents a token. Otherwise, returns null.
		/// </summary>
		public virtual ParsedTokenResult? Token => IsToken ?
			new ParsedTokenResult(this, Context, Result.element, Result.tokenId) : null;

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

		/// <summary>
		/// Gets the parsed input text that was captured.
		/// </summary>
		public virtual string Text => Context.input.Substring(Result.startIndex, Result.length);

		/// <summary>
		/// Gets the parsed input text that was captured as a span of characters.
		/// </summary>
		public ReadOnlySpan<char> Span => Context.input.AsSpan(Result.startIndex, Result.length);
		
		/// <summary>
		/// Gets the parsed input text that was captured as a memory of characters.
		/// </summary>
		public ReadOnlyMemory<char> Memory => Context.input.AsMemory(Result.startIndex, Result.length);

		/// <summary>
		/// Gets the parsed value associated with this rule.
		/// </summary>
		public virtual object? Value => Rule.ParsedValueFactory?.Invoke(this) ?? null;

		/// <summary>
		/// Gets the children results of this rule. Valid for parallel and sequence rules.
		/// </summary>
		public abstract IReadOnlyList<ParsedRuleResultBase> Children { get; }

		public virtual int Count => Result.children?.Count ?? 0;
		public virtual ParsedRuleResultBase this[int index] => Children[index];

		public virtual IEnumerator<ParsedRuleResultBase> GetEnumerator()
		{
			return Children.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Gets the text captured by child rule at the specific index.
		/// </summary>
		/// <returns>The text captured by child rule.</returns>
		public string GetText(int index) => this[index].Text;

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
		public T GetIntermediateValue<T>(int index) => (T)this[index].IntermediateValue;

		/// <summary>
		/// Tries to get the intermediate value associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this rule.</returns>
		public T? TryGetIntermediateValue<T>() => IntermediateValue is T result ? result : default;

		/// <summary>
		/// Tries to get the intermediate value associated with child rule at the specific index as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with child rule.</returns>
		public T? TryGetIntermediateValue<T>(int index)
			=> Count > index ? this[index].IntermediateValue is T result ? result : default : default;

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
		public T ConvertIntermediateValue<T>(int index) => (T)Convert.ChangeType(this[index].IntermediateValue, typeof(T));

		/// <summary>
		/// Gets the value associated with this rule as not-null object. If the value is null, throws an exception.
		/// </summary>
		/// <returns>The value associated with this rule.</returns>
		public object GetValue() => Value ?? throw new InvalidOperationException("ParsedRuleResult.Value is null");

		/// <summary>
		/// Gets the value associated with child rule at the specific index as not-null object. If the value is null, throws an exception.
		/// </summary>
		/// <returns>The value associated with child rule.</returns>
		public object GetValue(int index) => this[index].Value ?? throw new InvalidOperationException("ParsedRuleResult.Value is null");

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
		public T GetValue<T>(int index) => (T)this[index].Value;

		/// <summary>
		/// Tries to get the value associated with this rule as an instance of type <typeparamref name="T"/> or <see langword="default"/> value.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this rule.</returns>
		public T? TryGetValue<T>() => Value is T result ? result : default;

		/// <summary>
		/// Tries to get the value associated with child rule at the specific index as an instance of type <typeparamref name="T"/> or <see langword="default"/> value.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with child rule.</returns>
		public T? TryGetValue<T>(int index)
			=> Count > index ? this[index].Value is T result ? result : default : default;

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
		public T ConvertValue<T>(int index) => (T)Convert.ChangeType(this[index].Value, typeof(T));

		/// <summary>
		/// Gets the parsing parameter associated with this rule as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of parsing parameter to retrieve.</typeparam>
		/// <returns>The parsing parameter associated with this rule.</returns>
		public T GetParsingParameter<T>() => (T)ParsingParameter;

		/// <summary>
		/// Tries to get the parsing parameter associated with this rule as an instance of type <typeparamref name="T"/> or <see langword="default"/> value.
		/// </summary>
		/// <typeparam name="T">The type of parsing parameter to retrieve.</typeparam>
		/// <returns>The parsing parameter associated with this rule.</returns>
		public T? TryGetParsingParameter<T>() => ParsingParameter is T result ? result : default;

		/// <summary>
		/// Selects the children values array of this rule.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public object?[] SelectArray()
		{
			if (Count == 0)
				return Array.Empty<object?>();
			var result = new object?[Count];

			int i = 0;
			foreach (var child in this)
				result[i++] = child.Value;
			return result;
		}

		/// <summary>
		/// Selects the children values array of child rule at the specified index.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public object?[] SelectArray(int index)
		{
			return this[index].SelectArray();
		}

		/// <summary>
		/// Selects the children values of this rule.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public IEnumerable<object> SelectValues()
		{
			return this.Select(child => child.GetValue());
		}

		/// <summary>
		/// Selects the children values of child rule at the specified index.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public IEnumerable<object> SelectValues(int index)
		{
			return this[index].SelectValues();
		}

		/// <summary>
		/// Selects the casted children values of this rule.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public T[] SelectArray<T>()
		{
			var result = new T[Count];
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
			return this[index].SelectArray<T>();
		}

		/// <summary>
		/// Selects the casted children values of this rule.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public IEnumerable<T> SelectValues<T>()
		{
			return this.Select(child => child.GetValue<T>());
		}

		/// <summary>
		/// Selects the casted children values of child rule at the specified index.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public IEnumerable<T> SelectValues<T>(int index)
		{
			return this[index].SelectValues<T>();
		}

		/// <summary>
		/// Selects the children of this rule using a selector function.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <param name="selector">The selector function to apply to each child.</param>
		/// <returns>The selected values from the children.</returns>
		public T[] SelectArray<T>(Func<ParsedRuleResultBase, T> selector)
		{
			var result = new T[Count];
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
		public T[] SelectArray<T>(int index, Func<ParsedRuleResultBase, T> selector)
		{
			return this[index].SelectArray(selector);
		}

		/// <summary>
		/// Gets child parsed rules for this rule and joins them into a single collection recursively.
		/// </summary>
		/// <remarks>
		/// Used for making the AST flat.
		/// </remarks>
		/// <param name="maxDepth">The maximum depth to which child rules should be joined. If equal to zero, this element is returned. If -1, all child rules are joined recursively. Default is -1.</param>
		/// <returns>A collection of all child parsed rules. Returns this element if no children are present or the maximum depth is reached.</returns>
		public IEnumerable<ParsedRuleResultBase> GetJoinedChildren(int maxDepth = -1)
		{
			if (maxDepth == 0 || Count == 0)
				return this.WrapIntoEnumerable();

			return this.SelectMany(r => r.GetJoinedChildren(maxDepth - 1));
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
		public ParsedRuleResultBase Optimized(ParseTreeOptimization optimization = ParseTreeOptimization.Default)
		{
			return new ParsedRuleResultLazy(optimization, Parent, Context, Result);
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

			foreach (var child in this)
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