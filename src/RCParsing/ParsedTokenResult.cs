using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents the result of a parsed token.
	/// </summary>
	public class ParsedTokenResult
	{
		/// <summary>
		/// Gets the parent result of this rule, if any.
		/// </summary>
		public ParsedRuleResultBase? Parent { get; }

		/// <summary>
		/// Gets the parser context used for parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets the parsed token object containing the result of the parse.
		/// </summary>
		public ParsedElement Result { get; }

		/// <summary>
		/// Gets value indicating whether the parsing operation was successful.
		/// </summary>
		public bool Success => Result.success;

		/// <summary>
		/// Gets the unique identifier for the token that was parsed.
		/// </summary>
		public int TokenId { get; }

		/// <summary>
		/// Gets the parsed value associated with this token.
		/// </summary>
		public TokenPattern Token => Context.parser.TokenPatterns[TokenId];

		/// <summary>
		/// Gets the alias for the token pattern that was parsed. May be null if no alias is defined.
		/// </summary>
		public string TokenAlias => Token.Aliases.Count > 0 ? Token.Aliases[Token.Aliases.Count - 1] : null;

		/// <summary>
		/// Gets the aliases for the token pattern that was parsed.
		/// </summary>
		public ImmutableList<string> TokenAliases => Token.Aliases;

		/// <summary>
		/// Gets the starting index of the token in the input text.
		/// </summary>
		public int StartIndex => Result.startIndex;

		/// <summary>
		/// Gets the length of the token in the input text.
		/// </summary>
		public int Length => Result.length;

		/// <summary>
		/// Gets the intermediate value associated with this token.
		/// </summary>
		public object? IntermediateValue => Result.intermediateValue;

		private readonly Utils.LazyValue<string> _textLazy;
		/// <summary>
		/// Gets the parsed input text that was captured.
		/// </summary>
		public string Text => _textLazy.Value;

		/// <summary>
		/// Gets the parsed input text that was captured as a span of characters.
		/// </summary>
		public ReadOnlySpan<char> Span => Context.input.AsSpan(Result.startIndex, Result.length);

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedTokenResult"/> class.
		/// </summary>
		/// <param name="parent">The parent result of this rule, if any.</param>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="result">The parsed token object containing the result of the parse.</param>
		/// <param name="tokenId">The token ID for the parsed token result.</param>
		public ParsedTokenResult(ParsedRuleResultBase? parent, ParserContext context, ParsedElement result, int tokenId)
		{
			Parent = parent;
			Context = context;
			Result = result;
			TokenId = tokenId;

			_textLazy = new Utils.LazyValue<string>(() => Context.input.Substring(Result.startIndex, Result.length));
		}

		/// <summary>
		/// Gets the intermediate value associated with this token as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this token.</returns>
		public T GetIntermediateValue<T>() => (T)IntermediateValue;

		/// <summary>
		/// Tries to get the intermediate value associated with this token as an instance of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of value to retrieve.</typeparam>
		/// <returns>The intermediate value associated with this token.</returns>
		public T? TryGetIntermediateValue<T>() where T : class => IntermediateValue as T;

		/// <summary>
		/// Dumps the parsed token result to a string representation.
		/// </summary>
		/// <returns>A string representation of the parsed token result.</returns>
		public string Dump()
		{
			StringBuilder sb = new StringBuilder();

			string intermediateValueStr = IntermediateValue?.ToString() ?? "null";

			sb.AppendLine($"Token: {Token}, Captured Text: \"{Text}\"");
			sb.AppendLine($"Intermediate Value: {intermediateValueStr}");

			return sb.ToString();
		}

		public override string ToString()
		{
			return Text;
		}
	}
}