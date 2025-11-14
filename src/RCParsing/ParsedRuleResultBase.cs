using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents a <see cref="ParsedRuleResultBase"/> with applied optimization tree flags.
	/// </summary>
	public interface IOptimizedParsedRuleResult
	{
		/// <summary>
		/// Gets the optimization flags that used to optimize the parse tree.
		/// </summary>
		ParseTreeOptimization Optimization { get; }
	}

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
		public virtual ParsedTokenResult? Token
		{
			get
			{
				if (IsToken)
				{
					var element = Result.element;
					var context = Context;
					return new ParsedTokenResult(this, context, Result.element, TokenId);
				}
				return null;
			}
		}

		/// <summary>
		/// Gets value indicating whether the parsing operation was successful.
		/// </summary>
		public bool Success => Result.success;

		/// <summary>
		/// Gets value indicating whether the AST node represents a token.
		/// </summary>
		public bool IsToken => Context.parser.Rules[RuleId] is TokenParserRule;

		/// <summary>
		/// Gets the token pattern ID if this AST node represents a token.
		/// </summary>
		public int TokenId => Context.parser.Rules[RuleId] is TokenParserRule trule ? trule.TokenPatternId : -1;

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
		public IReadOnlyList<string> RuleAliases => Rule.Aliases;

		/// <summary>
		/// Gets the starting index of the AST node in the input text.
		/// </summary>
		public int StartIndex => Result.startIndex;

		/// <summary>
		/// Gets the characters length of the AST node in the input text.
		/// </summary>
		public int Length => Result.length;

		/// <summary>
		/// Gets the ending index of the AST node in the input text.
		/// </summary>
		public int EndIndex => Result.endIndex;

		/// <summary>
		/// Gets the occurency index in the parent choice, sequence or any repeat rule. -1 by default.
		/// </summary>
		public int Occurency => Result.occurency;

		/// <summary>
		/// Gets the version of the AST. Used for incremental parsing and tracking changes in the parse tree.
		/// </summary>
		public int Version => Result.version;

		/// <summary>
		/// Gets the intermediate value associated with this rule.
		/// </summary>
		public object? IntermediateValue => Result.intermediateValue;

		/// <summary>
		/// Gets the parsing parameter object that was passed to the parser during parsing.
		/// May be <see langword="null"/> if no parameter is passed.
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
		/// Gets the parsed value associated with this AST node, if any. Otherwise, returns <see langword="null"/>.
		/// </summary>
		/// <remarks>
		/// The value is determined by the parsed rule's ParsedValueFactory method, if any.
		/// Calculates lazily, then this property is called or when parsing was successful.
		/// </remarks>
		public virtual object? Value => Rule.ParsedValueFactory?.Invoke(this) ?? null;

		/// <summary>
		/// Gets the children results of this AST node, if any. Otherwise, returns an empty list.
		/// </summary>
		public abstract IReadOnlyList<ParsedRuleResultBase> Children { get; }

		/// <summary>
		/// Gets the number of children results of this AST node.
		/// </summary>
		public virtual int Count => Result.children?.Count ?? 0;

		/// <summary>
		/// Gets the child AST node at the specified index. Throws an exception if the index is out of range.
		/// </summary>
		/// <param name="index">The zero-based index of the child AST node to get.</param>
		/// <returns>The child AST node at the specified index.</returns>
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
		/// Creates a new parsed result with updated context and parsed rule.
		/// </summary>
		/// <param name="newContext">The new parser context to use for the parsed result.</param>
		/// <param name="newParsedRule">The new parsed rule to use for the parsed result.</param>
		/// <returns>A new parsed result with updated context and parsed rule.</returns>
		public abstract ParsedRuleResultBase Updated(ParserContext newContext, ParsedRule newParsedRule);

		/// <summary>
		/// Gets the text captured by child AST node at the specific index.
		/// </summary>
		/// <returns>The text captured by child AST node.</returns>
		public string GetText(int index) => this[index].Text;

		/// <summary>
		/// Gets the intermediate value associated with this AST node as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this AST node.</returns>
		public T GetIntermediateValue<T>() => (T)IntermediateValue;

		/// <summary>
		/// Gets the intermediate value associated with child AST node at the specific index as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with child AST node.</returns>
		public T GetIntermediateValue<T>(int index) => (T)this[index].IntermediateValue;

		/// <summary>
		/// Tries to get the intermediate value associated with this AST node as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this AST node.</returns>
		public T? TryGetIntermediateValue<T>() => IntermediateValue is T result ? result : default;

		/// <summary>
		/// Tries to get the intermediate value associated with child AST node at the specific index as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with child AST node.</returns>
		public T? TryGetIntermediateValue<T>(int index)
			=> Count > index ? this[index].IntermediateValue is T result ? result : default : default;

		/// <summary>
		/// Gets the intermediate value associated with this AST node converted to type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this AST node.</returns>
		public T ConvertIntermediateValue<T>() => (T)Convert.ChangeType(IntermediateValue, typeof(T));

		/// <summary>
		/// Gets the intermediate value associated with child AST node at the specific index converted to type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with child AST node.</returns>
		public T ConvertIntermediateValue<T>(int index) => (T)Convert.ChangeType(this[index].IntermediateValue, typeof(T));

		/// <summary>
		/// Gets the value associated with this AST node as not-null object. If the value is null, throws an exception.
		/// </summary>
		/// <returns>The value associated with this AST node.</returns>
		public object GetValue() => Value ?? throw new InvalidOperationException("ParsedRuleResult.Value is null");

		/// <summary>
		/// Gets the value associated with child AST node at the specific index as not-null object. If the value is null, throws an exception.
		/// </summary>
		/// <returns>The value associated with child AST node.</returns>
		public object GetValue(int index) => this[index].Value ?? throw new InvalidOperationException("ParsedRuleResult.Value is null");

		/// <summary>
		/// Gets the value associated with this AST node as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this AST node.</returns>
		public T GetValue<T>() => (T)Value;

		/// <summary>
		/// Gets the value associated with child AST node at the specific index as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with child AST node.</returns>
		public T GetValue<T>(int index) => (T)this[index].Value;

		/// <summary>
		/// Tries to get the value associated with this AST node as an instance of type <typeparamref name="T"/> or <see langword="default"/> value.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this AST node.</returns>
		public T? TryGetValue<T>()
			=> Value is T result ? result : default;

		/// <summary>
		/// Tries to get the value associated with child AST node at the specific index as an instance of type <typeparamref name="T"/> or <see langword="default"/> value.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with child AST node.</returns>
		public T? TryGetValue<T>(int index)
			=> Count > index ? this[index].Value is T result ? result : default : default;

		/// <summary>
		/// Tries to get the value associated with this AST node as an instance of type <typeparamref name="T"/> or <see langword="default"/> value.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this AST node.</returns>
		public T? TryGetNullableValue<T>() where T : struct
			=> Value is T result ? result : null;

		/// <summary>
		/// Tries to get the value associated with child AST node at the specific index as an instance of type <typeparamref name="T"/> or <see langword="default"/> value.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with child AST node.</returns>
		public T? TryGetNullableValue<T>(int index) where T : struct
			=> Count > index ? this[index].Value is T result ? result : null : null;

		/// <summary>
		/// Gets the value associated with this AST node converted to type <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>
		/// Value is converted via <see cref="Convert"/>.
		/// </remarks>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with this AST node.</returns>
		public T ConvertValue<T>() => (T)Convert.ChangeType(Value, typeof(T));

		/// <summary>
		/// Gets the value associated with child AST node at the specific index converted to type <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>
		/// Value is converted via <see cref="Convert"/>.
		/// </remarks>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The value associated with child AST node.</returns>
		public T ConvertValue<T>(int index) => (T)Convert.ChangeType(this[index].Value, typeof(T));

		/// <summary>
		/// Gets the parsing parameter associated with parser context as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of parsing parameter to retrieve.</typeparam>
		/// <returns>The parsing parameter associated with the parser context.</returns>
		public T GetParsingParameter<T>() => (T)ParsingParameter;

		/// <summary>
		/// Tries to get the parsing parameter associated with parser context as an instance of type <typeparamref name="T"/> or <see langword="default"/> value.
		/// </summary>
		/// <typeparam name="T">The type of parsing parameter to retrieve.</typeparam>
		/// <returns>The parsing parameter associated with the parser context.</returns>
		public T? TryGetParsingParameter<T>() => ParsingParameter is T result ? result : default;

		/// <summary>
		/// Selects the children values array of this AST node.
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
		/// Selects the children values array of child AST node at the specified index.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public object?[] SelectArray(int index)
		{
			return this[index].SelectArray();
		}

		/// <summary>
		/// Selects the children values of this AST node.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public IEnumerable<object> SelectValues()
		{
			return this.Select(child => child.GetValue());
		}

		/// <summary>
		/// Selects the children values of child AST node at the specified index.
		/// </summary>
		/// <returns>The values from the children.</returns>
		public IEnumerable<object> SelectValues(int index)
		{
			return this[index].SelectValues();
		}

		/// <summary>
		/// Selects the casted children values of this AST node.
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
		/// Selects the casted children values array of child AST node at the specified index.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public T[] SelectArray<T>(int index)
		{
			return this[index].SelectArray<T>();
		}

		/// <summary>
		/// Selects the casted children values of this AST node.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public IEnumerable<T> SelectValues<T>()
		{
			return this.Select(child => child.GetValue<T>());
		}

		/// <summary>
		/// Selects the casted children values of child AST node at the specified index.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve from the children.</typeparam>
		/// <returns>The casted values from the children.</returns>
		public IEnumerable<T> SelectValues<T>(int index)
		{
			return this[index].SelectValues<T>();
		}

		/// <summary>
		/// Selects the children of this AST node using a selector function.
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
		/// Selects the children of child AST node at the specified index using a selector function.
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
		/// Returns a optimized version of this AST node.
		/// </summary>
		/// <remarks>
		/// Optimizes the parse tree by applying the specified optimization flags.
		/// Note that this may affect the parsed value calculation. Mostly used for analysis purposes.
		/// </remarks>
		/// <param name="optimization">The optimization flags to apply.</param>
		/// <returns>An optimized version of this AST node.</returns>
		public abstract ParsedRuleResultBase Optimized(ParseTreeOptimization optimization = ParseTreeOptimization.Default);

		/// <summary>
		/// Creates error groups from stored errors in context. <br/>
		/// If parsing was successful, last error group will be excluded from relevant error groups.
		/// </summary>
		/// <returns>The error groups created from context-stored errors.</returns>
		public ErrorGroupCollection CreateErrorGroups()
		{
			var context = Context;
			return new ErrorGroupCollection(context, context.errors, context.errorRecoveryIndices, Success);
		}



		private static void ValidateReparseContext(ParserContext oldContext, ParserContext newContext)
		{
			if (oldContext.position != newContext.position)
				throw new InvalidOperationException("Cannot reparse the context with mismatched minimum positions.");
		}

		private static void ValidateReparseContextChange(ParserContext oldContext, ParserContext newContext, TextChange change)
		{
			var maxPosDelta = oldContext.maxPosition - newContext.maxPosition;
			var changeDelta = change.oldLength - change.newLength;
			if (maxPosDelta != changeDelta)
				throw new InvalidOperationException("Cannot reparse the context with mismatched maximum (input length) positions.");
		}

		/// <summary>
		/// Incrementally re-parses this AST node with new input.
		/// </summary>
		/// <param name="input">The new input to re-parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <returns>A new AST node with the updated input.</returns>
		public ParsedRuleResultBase Reparsed(string input, object? parameter = null)
		{
			var context = Context;
			var parser = context.parser;
			var newContext = parser.CreateContext(input, parameter);

			ValidateReparseContext(context, newContext);
			var change = new TextChange(context.input, newContext.input);
			ValidateReparseContextChange(context, newContext, change);

			var reparsed = parser.ParseIncrementally(Result, newContext, change);
			return Updated(newContext, reparsed);
		}

		/// <summary>
		/// Incrementally re-parses this AST node with new input.
		/// </summary>
		/// <param name="input">The new input to re-parse.</param>
		/// <param name="startIndex">The starting index of the new input.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <returns>A new AST node with the updated input.</returns>
		public ParsedRuleResultBase Reparsed(string input, int startIndex, object? parameter = null)
		{
			var context = Context;
			var parser = context.parser;
			var newContext = parser.CreateContext(input, startIndex, parameter);

			ValidateReparseContext(context, newContext);
			var change = new TextChange(context.input, newContext.input);
			ValidateReparseContextChange(context, newContext, change);

			var reparsed = parser.ParseIncrementally(Result, newContext, change);
			return Updated(newContext, reparsed);
		}

		/// <summary>
		/// Incrementally re-parses this AST node with new input.
		/// </summary>
		/// <param name="input">The new input to re-parse.</param>
		/// <param name="startIndex">The starting index of the new input.</param>
		/// <param name="length">The number of characters to parse from the new input.</param>
		/// <param name="parameter">Optional parameter to pass to the parser.</param>
		/// <returns>A new AST node with the updated input.</returns>
		public ParsedRuleResultBase Reparsed(string input, int startIndex, int length, object? parameter = null)
		{
			var context = Context;
			var parser = context.parser;
			var newContext = parser.CreateContext(input, startIndex, length, parameter);

			ValidateReparseContext(context, newContext);
			var change = new TextChange(context.input, newContext.input);
			ValidateReparseContextChange(context, newContext, change);

			var reparsed = parser.ParseIncrementally(Result, newContext, change);
			return Updated(newContext, reparsed);
		}

		/// <summary>
		/// Incrementally re-parses this AST node with new input.
		/// </summary>
		/// <param name="context">The new parser context to re-parse.</param>
		/// <returns>A new AST node with the updated input.</returns>
		public ParsedRuleResultBase Reparsed(ParserContext context)
		{
			var _context = Context;
			var parser = _context.parser;
			if (context.parser != _context.parser)
				throw new InvalidOperationException("Cannot reparse the context with different parsers.");

			ValidateReparseContext(context, context);
			var change = new TextChange(context.input, context.input);
			ValidateReparseContextChange(context, context, change);

			var reparsed = parser.ParseIncrementally(Result, context, change);
			return Updated(context, reparsed);
		}

		/// <summary>
		/// Incrementally re-parses this AST node with new input.
		/// </summary>
		/// <param name="context">The new parser context to re-parse.</param>
		/// <param name="change">Optional text change to apply during re-parsing.</param>
		/// <returns>A new AST node with the updated input.</returns>
		public ParsedRuleResultBase Reparsed(ParserContext context, TextChange change)
		{
			var _context = Context;
			var parser = _context.parser;
			if (context.parser != _context.parser)
				throw new InvalidOperationException("Cannot reparse the context with different parsers.");

			ValidateReparseContext(context, context);
			ValidateReparseContextChange(context, context, change);

			var reparsed = parser.ParseIncrementally(Result, context, change);
			return Updated(context, reparsed);
		}

		/// <summary>
		/// Throws a <see cref="ParsingException"/> if the parsing operation was not successful (<see cref="Success"/> is <see langword="false"/>).
		/// </summary>
		/// <exception cref="ParsingException">Thrown if the parsing operation was not successful.</exception>
		public void ThrowIfFailed()
		{
			if (!Success)
				throw new ParsingException(Context);
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