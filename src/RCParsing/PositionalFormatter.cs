using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// The utility class for formatting string related to positions in a text.
	/// </summary>
	public static class PositionalFormatter
	{
		/// <summary>
		/// Decomposes a string into lines and calculates the line number and column of a given position.
		/// </summary>
		/// <param name="str">The string to decompose.</param>
		/// <param name="position">The position to decompose.</param>
		/// <param name="lineStart">The start index of the current line. Outputs the start index of the line containing the given position.</param>
		/// <param name="lineLength">The length of the current line. Outputs the length of the line containing the given position.</param>
		/// <param name="lineNumber">The number of line containing the given position as a 1-based index. Outputs the line number containing the given position.</param>
		/// <param name="column">The number of column containing the given position as a 1-based index. Outputs the column number containing the given position.</param>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the given position is out of range.</exception>
		public static void Decompose(string str, int position,
			out int lineStart, out int lineLength, out int lineNumber, out int column)
		{
			if (position < 0 || position > str.Length)
				throw new ArgumentOutOfRangeException(nameof(position), "Position must be within the bounds of the string.");

			int currentLineNumber = 1;
			int currentLineStart = 0;

			for (int i = 0; i < position; i++)
			{
				char c = str[i];

				if (c == '\r')
				{
					// If next character is \n, treat \r\n as one line break
					if (i + 1 < str.Length && str[i + 1] == '\n')
					{
						i++; // skip \n
					}

					currentLineNumber++;
					currentLineStart = i + 1;
				}
				else if (c == '\n')
				{
					currentLineNumber++;
					currentLineStart = i + 1;
				}
			}


			int currentLineEnd = str.Length;
			for (int i = position; i < str.Length; i++)
			{
				char c = str[i];
				if (c == '\r' || c == '\n')
				{
					currentLineEnd = i;
					break;
				}
			}

			string targetLine = str.Substring(currentLineStart, currentLineEnd - currentLineStart);

			int targetOffset = position - currentLineStart;
			if (targetOffset < 0)
				targetOffset = 0;

			lineStart = currentLineStart;
			lineLength = currentLineEnd - currentLineStart;
			lineNumber = currentLineNumber;
			column = targetOffset + 1;
		}

		/// <summary>
		/// Extracts a line containing a specified position in a text and formats it for display.
		/// </summary>
		/// <remarks>
		/// Useful for debugging and displaying errors in a user-friendly manner.
		/// </remarks>
		/// <param name="str">The input text.</param>
		/// <param name="position">The zero-based index of the character in the text.</param>
		/// <returns>
		/// A formatted string containing the line at the specified position
		/// along with line number and column information for the specified position.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified position is out of range for the input text.</exception>
		public static string Format(string str, int position)
		{
			Decompose(str, position, out int lineStart, out int lineLength, out int lineNumber, out int column);

			string lineAndColumn = $"line {lineNumber}, column {column}";

			string pointerLine;
			if (column <= lineAndColumn.Length + 2)
				pointerLine = new string(' ', column - 1) + '^' + ' ' + lineAndColumn;
			else
				pointerLine = new string(' ', column - 2 - lineAndColumn.Length) + lineAndColumn + ' ' + '^';

			return $"{str.Substring(lineStart, lineLength)}\n{pointerLine}";
		}
	}
}