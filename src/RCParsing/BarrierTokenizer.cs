using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a tokenizer that emits barrier tokens for the given input text.
	/// </summary>
	public abstract class BarrierTokenizer
	{
		/// <summary>
		/// Gets the aliases of the barrier token patterns used by this tokenizer.
		/// </summary>
		public abstract IEnumerable<string> BarrierAliases { get; }

		/// <summary>
		/// Emits barrier tokens for the given input text.
		/// </summary>
		/// <param name="context">The parser context to use for tokenization.</param>
		/// <returns>A list of barrier tokens emitted by this tokenizer.</returns>
		public abstract IEnumerable<BarrierToken> Tokenize(ParserContext context);
	}
}