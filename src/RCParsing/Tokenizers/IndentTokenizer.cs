using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.Tokenizers
{
	/// <summary>
	/// The mode for the <see cref="IndentTokenizer"/>. Determines how indentation is handled during tokenization.
	/// </summary>
	public enum IndentTokenizerMode
	{
		/// <summary>
		/// Strict mode enforces strict indentation rules, emitting an error if the indentation level
		/// changes by not equal to a multiple of the base indentation level.
		/// This mode is more rigid and requires consistent indentation throughout the input.
		/// </summary>
		Strict,

		/// <summary>
		/// Soft mode handles indentation by emitting a barrier token when the indentation level changes.
		/// This mode is lenient and does not enforce strict indentation rules.
		/// </summary>
		Soft,

		/// <summary>
		/// Hybrid mode requires a minimum indentation delta of base indentation level to be considered as a change,
		/// but allows for small indentation changes in a range of multiple of the base indentation level.
		/// </summary>
		Hybrid
	}

	/// <summary>
	/// Represents an indent tokenizer that emits barrier tokens for the given input text.
	/// </summary>
	/// <remarks>
	/// Emits the Python-like indent and dedent tokens.
	/// </remarks>
	public class IndentTokenizer : BarrierTokenizer
	{
		/// <summary>
		/// Gets the indent/tab size used by this tokenizer.
		/// </summary>
		public int IndentSize { get; }

		/// <summary>
		/// Gets the mode used by this tokenizer. Determines how indentation is handled during tokenization.
		/// </summary>
		public IndentTokenizerMode Mode { get; }

		/// <summary>
		/// Gets the name of the indent token.
		/// </summary>
		public string IndentTokenName { get; }

		/// <summary>
		/// Gets the name of the dedent token.
		/// </summary>
		public string DedentTokenName { get; }

		/// <summary>
		/// Gets the name of the newline token.
		/// </summary>
		public string? NewlineTokenName { get; }

		public override IEnumerable<string> BarrierAliases => NewlineTokenName != null
			? new[] { IndentTokenName, DedentTokenName, NewlineTokenName }
			: new[] { IndentTokenName, DedentTokenName };

		/// <inheritdoc cref="IndentTokenizer.IndentTokenizer(int, IndentTokenizerMode, string, string, string?)"/>
		public IndentTokenizer(string indentTokenName, string dedentTokenName, string? newlineTokenName = null)
		{
			IndentSize = 4;
			Mode = IndentTokenizerMode.Hybrid;
			IndentTokenName = indentTokenName;
			DedentTokenName = dedentTokenName;
			NewlineTokenName = newlineTokenName;
		}

		/// <inheritdoc cref="IndentTokenizer.IndentTokenizer(int, IndentTokenizerMode, string, string, string?)"/>
		public IndentTokenizer(int indentSize, string indentTokenName, string dedentTokenName, string? newlineTokenName = null)
		{
			IndentSize = indentSize;
			Mode = IndentTokenizerMode.Hybrid;
			IndentTokenName = indentTokenName;
			DedentTokenName = dedentTokenName;
			NewlineTokenName = newlineTokenName;
		}

		/// <inheritdoc cref="IndentTokenizer.IndentTokenizer(int, IndentTokenizerMode, string, string, string?)"/>
		public IndentTokenizer(IndentTokenizerMode mode, string indentTokenName, string dedentTokenName, string? newlineTokenName = null)
		{
			IndentSize = 4;
			Mode = mode;
			IndentTokenName = indentTokenName;
			DedentTokenName = dedentTokenName;
			NewlineTokenName = newlineTokenName;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="IndentTokenizer"/> class.
		/// </summary>
		/// <param name="indentSize">The size of the indent/tab character in spaces.</param>
		/// <param name="mode">The mode used by this tokenizer. Determines how indentation is handled during tokenization.</param>
		/// <param name="indentTokenName">The name of the indent token.</param>
		/// <param name="dedentTokenName">The name of the dedent token.</param>
		/// <param name="newlineTokenName">The name of the newline token.</param>
		public IndentTokenizer(int indentSize, IndentTokenizerMode mode, string indentTokenName, string dedentTokenName, string? newlineTokenName = null)
		{
			IndentSize = indentSize;
			Mode = mode;
			IndentTokenName = indentTokenName;
			DedentTokenName = dedentTokenName;
			NewlineTokenName = newlineTokenName;
		}



		private IEnumerable<BarrierToken> TokenizeStrict(ParserContext context)
		{
			string input = context.str ?? string.Empty;
			int length = input.Length;
			int currentIndent = 0;

			while (context.position < length)
			{
				var (col, contentStart, newlineStart, newlineLen, isBlankLine) = ParseLine(input, context.position, length);

				if (!isBlankLine)
				{
					// Strict: must be multiple of base indent
					if (col % IndentSize != 0)
					{
						throw new ParsingException(context,
							$"Indentation must be multiple of {IndentSize} in strict mode. " +
							$"Found: {col} spaces.", contentStart);
					}

					int newIndent = col / IndentSize;
					int delta = newIndent - currentIndent;
					currentIndent = newIndent;

					while (delta > 0)
					{
						yield return new BarrierToken(contentStart, 0, IndentTokenName);
						delta--;
					}
					while (delta < 0)
					{
						yield return new BarrierToken(contentStart, 0, DedentTokenName);
						delta++;
					}
				}

				if (newlineStart < length)
				{
					if (NewlineTokenName != null)
						yield return new BarrierToken(newlineStart, newlineLen, NewlineTokenName);
					context.position = newlineStart + newlineLen;
				}
				else
				{
					context.position = length;
				}
			}

			// Emit final DEDENTs at EOF
			int eofPos = length;
			while (currentIndent > 0)
			{
				yield return new BarrierToken(eofPos, 0, DedentTokenName);
				currentIndent--;
			}
		}

		private IEnumerable<BarrierToken> TokenizeSoft(ParserContext context)
		{
			string input = context.str ?? string.Empty;
			int length = input.Length;

			var indentStack = new Stack<int>();
			indentStack.Push(0);

			while (context.position < length)
			{
				var (col, contentStart, newlineStart, newlineLen, isBlankLine) = ParseLine(input, context.position, length);

				if (!isBlankLine)
				{
					int currentIndent = indentStack.Peek();

					if (col > currentIndent)
					{
						indentStack.Push(col);
						yield return new BarrierToken(contentStart, 0, IndentTokenName);
					}
					else if (col < currentIndent)
					{
						while (indentStack.Count > 0 && indentStack.Peek() > col)
						{
							indentStack.Pop();
							yield return new BarrierToken(contentStart, 0, DedentTokenName);
						}
					}
				}

				if (newlineStart < length)
				{
					if (NewlineTokenName != null)
						yield return new BarrierToken(newlineStart, newlineLen, NewlineTokenName);
					context.position = newlineStart + newlineLen;
				}
				else
				{
					context.position = length;
				}
			}

			// Emit final DEDENTs at EOF
			int eofPos = length;
			for (int k = indentStack.Count - 1; k > 0; k--)
			{
				yield return new BarrierToken(eofPos, 0, DedentTokenName);
			}
		}

		private IEnumerable<BarrierToken> TokenizeHybrid(ParserContext context)
		{
			string input = context.str ?? string.Empty;
			int length = input.Length;
			int currentIndent = 0;

			while (context.position < length)
			{
				var (col, contentStart, newlineStart, newlineLen, isBlankLine) = ParseLine(input, context.position, length);

				if (!isBlankLine)
				{
					int newIndent = col / IndentSize;
					int delta = newIndent - currentIndent;
					currentIndent = newIndent;

					while (delta > 0)
					{
						yield return new BarrierToken(contentStart, 0, IndentTokenName);
						delta--;
					}
					while (delta < 0)
					{
						yield return new BarrierToken(contentStart, 0, DedentTokenName);
						delta++;
					}
				}

				if (newlineStart < length)
				{
					if (NewlineTokenName != null)
						yield return new BarrierToken(newlineStart, newlineLen, NewlineTokenName);
					context.position = newlineStart + newlineLen;
				}
				else
				{
					context.position = length;
				}
			}

			// Emit final DEDENTs at EOF
			int eofPos = length;
			while (currentIndent > 0)
			{
				yield return new BarrierToken(eofPos, 0, DedentTokenName);
				currentIndent--;
			}
		}

		private (int col, int contentStart, int newlineStart, int newlineLen, bool isBlankLine)
			ParseLine(string input, int start, int length)
		{
			int col = 0;
			int j = start;

			// Compute indentation
			while (j < length)
			{
				char c = input[j];
				if (c == ' ')
				{
					col++; j++; continue;
				}
				if (c == '\t')
				{
					int add = IndentSize - (col % IndentSize);
					col += add;
					j++; continue;
				}
				if (c == '\r' || c == '\n')
					break;
				break;
			}

			int contentStart = j;

			// Find end of line
			int m = j;
			while (m < length && input[m] != '\r' && input[m] != '\n') m++;

			bool isBlankLine = (contentStart >= length) ||
							  (contentStart < length &&
							   (input[contentStart] == '\r' || input[contentStart] == '\n'));

			int newlineStart = m;
			int newlineLen = 0;

			if (m < length)
			{
				newlineLen = 1;
				if (input[m] == '\r' && m + 1 < length && input[m + 1] == '\n')
					newlineLen = 2;
			}

			return (col, contentStart, newlineStart, newlineLen, isBlankLine);
		}



		/// <summary>
		/// Tokenize input producing barrier tokens: INDENT, DEDENT and optionally NEWLINE.
		/// </summary>
		public override IEnumerable<BarrierToken> Tokenize(ParserContext context)
		{
			switch (Mode)
			{
				case IndentTokenizerMode.Strict:
					return TokenizeStrict(context);
				case IndentTokenizerMode.Soft:
					return TokenizeSoft(context);
				case IndentTokenizerMode.Hybrid:
					return TokenizeHybrid(context);

				default:
					throw new ArgumentOutOfRangeException(nameof(Mode), Mode, "Invalid mode specified.");
			}
		}
	}
}