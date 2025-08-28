using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using RCParsing.Tokenizers;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a builder for constructing tokenizers collection for a parser.
	/// </summary>
	public class ParserTokenizersBuilder
	{
		private readonly List<BarrierTokenizer> _tokenizers = new();

		/// <summary>
		/// Builds the tokenizers collection for the parser.
		/// </summary>
		/// <returns>The built tokenizers collection.</returns>
		public ImmutableArray<BarrierTokenizer> Build()
		{
			return _tokenizers.ToImmutableArray();
		}

		/// <summary>
		/// Adds a tokenizer to the collection.
		/// </summary>
		/// <param name="tokenizer">The tokenizer to add.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserTokenizersBuilder Add(BarrierTokenizer tokenizer)
		{
			_tokenizers.Add(tokenizer);
			return this;
		}

		/// <inheritdoc cref="AddIndent(int, IndentTokenizerMode, string, string, string?)"/>
		public ParserTokenizersBuilder AddIndent(string indentTokenName, string dedentTokenName, string? newlineTokenName = null)
		{
			return Add(new IndentTokenizer(indentTokenName, dedentTokenName, newlineTokenName));
		}

		/// <inheritdoc cref="AddIndent(int, IndentTokenizerMode, string, string, string?)"/>
		public ParserTokenizersBuilder AddIndent(int indentSize, string indentTokenName, string dedentTokenName, string? newlineTokenName = null)
		{
			return Add(new IndentTokenizer(indentSize, indentTokenName, dedentTokenName, newlineTokenName));
		}

		/// <inheritdoc cref="AddIndent(int, IndentTokenizerMode, string, string, string?)"/>
		public ParserTokenizersBuilder AddIndent(IndentTokenizerMode mode, string indentTokenName, string dedentTokenName, string? newlineTokenName = null)
		{
			return Add(new IndentTokenizer(mode, indentTokenName, dedentTokenName, newlineTokenName));
		}

		/// <summary>
		/// Adds an indent tokenizer to the collection.
		/// </summary>
		/// <param name="indentSize">The size of the indent/tab character in spaces.</param>
		/// <param name="mode">The mode used by this tokenizer. Determines how indentation is handled during tokenization.</param>
		/// <param name="indentTokenName">The name of the indent token.</param>
		/// <param name="dedentTokenName">The name of the dedent token.</param>
		/// <param name="newlineTokenName">The name of the newline token. If null, no newline token will be added. Default is null.</param>
		/// <returns>Current instance for method chaining.</returns>
		public ParserTokenizersBuilder AddIndent(int indentSize, IndentTokenizerMode mode, string indentTokenName, string dedentTokenName, string? newlineTokenName = null)
		{
			return Add(new IndentTokenizer(indentSize, mode, indentTokenName, dedentTokenName, newlineTokenName));
		}
	}
}