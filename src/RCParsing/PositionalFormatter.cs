using System;
using System.Globalization;
using System.Text;
using RCParsing.Utils;

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
		/// <param name="tabSize">The size of a tab character.</param>
		/// <returns>The line number and column of the given position with the other information given.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the given position is out of range.</exception>
		public static PositionFormatInfo Decompose(string str, int position, int tabSize = 4)
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

			var result = new PositionFormatInfo
			{
				Position = position,
				LineStart = currentLineStart,
				LineLength = currentLineEnd - currentLineStart,
				LineNumber = currentLineNumber,
				ColumnNumber = targetOffset + 1
			};

			int visualColumnNumber = 0;
			int visualLineLength = 0;
			for (int i = currentLineStart; i < currentLineEnd; i++)
			{
				if (str[i] == '\t')
				{
					if (i < currentLineStart + targetOffset)
						visualColumnNumber += tabSize - (visualColumnNumber % tabSize);
					visualLineLength += tabSize - (visualLineLength % tabSize);
				}
				else
				{
					if (i < currentLineStart + targetOffset)
						visualColumnNumber += 1;
					visualLineLength += 1;
				}
			}
			result.VisualColumnNumber = visualColumnNumber + 1;
			result.VisualLineLength = visualLineLength;

			return result;
		}

		/// <summary>
		/// Extracts a line containing a specified position in a text and formats it for display.
		/// </summary>
		/// <remarks>
		/// Useful for debugging and displaying errors in a user-friendly manner.
		/// </remarks>
		/// <param name="str">The input text.</param>
		/// <param name="position">The zero-based index of the character in the text.</param>
		/// <param name="tabSize">The size of a tab character.</param>
		/// <param name="cursor">The cursor string to indicate the position. Must be a single character.</param>
		/// <param name="cursorAnnotation">The annotation to add to the line along with cursor.</param>
		/// <returns>
		/// A formatted string containing the line at the specified position
		/// along with line number and column information for the specified position.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified position is out of range for the input text.</exception>
		public static string Format(string str, int position, int tabSize = 4,
			string cursor = "^", string? cursorAnnotation = null)
		{
			var info = Decompose(str, position, tabSize);
			return Format(str, info, cursor, cursorAnnotation);
		}

		/// <summary>
		/// Extracts a line containing a specified position in a text and formats it for display.
		/// </summary>
		/// <remarks>
		/// Useful for debugging and displaying errors in a user-friendly manner.
		/// </remarks>
		/// <param name="str">The input text.</param>
		/// <param name="position">The zero-based index of the character in the text.</param>
		/// <param name="length">The length of the line to extract.</param>
		/// <param name="tabSize">The size of a tab character.</param>
		/// <param name="addMultilineNumber">If true, adds line number information for the start and end positions if positions spans multiple lines.</param>
		/// <param name="cursor">The cursor string to indicate the position. Must be a single character.</param>
		/// <param name="cursorAnnotation">The annotation to add to the line along with cursor.</param>
		/// <returns>
		/// A formatted string containing the line at the specified position
		/// along with line number and column information for the specified position.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified position is out of range for the input text.</exception>
		public static string Format(string str, int position, int length, int tabSize = 4,
			bool addMultilineNumber = false, string cursor = "^", string? cursorAnnotation = null)
		{
			if (length == 0 || length == 1)
				return Format(str, position, tabSize: tabSize, cursor, cursorAnnotation);

			var info1 = Decompose(str, position, tabSize);
			var info2 = Decompose(str, position + length - 1, tabSize);
			return Format(str, info1, info2, addMultilineNumber, cursor, cursorAnnotation);
		}

		/// <summary>
		/// Extracts a line containing a specified position in a text and formats it for display.
		/// </summary>
		/// <remarks>
		/// Useful for debugging and displaying errors in a user-friendly manner.
		/// </remarks>
		/// <param name="str">The input text.</param>
		/// <param name="info">The position information used to format the line.</param>
		/// <param name="cursor">The cursor string to indicate the position. Must be a single character.</param>
		/// <param name="cursorAnnotation">The annotation to add to the line along with cursor.</param>
		/// <returns>
		/// A formatted string containing the line at the specified position with cursor and optional annotation attached:
		/// <code>
		/// Some source line...
		///      ^ error is here
		/// </code>
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified position is out of range for the input text.</exception>
		public static string Format(string str, PositionFormatInfo info, string cursor = "^", string? cursorAnnotation = null)
		{
			if (info.Position < 0 || info.Position > str.Length)
				throw new ArgumentOutOfRangeException(nameof(info), "Position must be within the bounds of the string.");

			if (cursor?.Length != 1)
				throw new ArgumentException("Cursor must be a single character.", nameof(cursor));

			if (string.IsNullOrEmpty(cursorAnnotation))
				return string.Concat(str.Substring(info.LineStart, info.LineLength),
					Environment.NewLine, new string(' ', info.VisualColumnNumber - 1), cursor);

			string pointerLine;
			if (info.VisualColumnNumber <= cursorAnnotation.Length + 2)
				pointerLine = new string(' ', info.VisualColumnNumber - 1) + cursor + ' ' + cursorAnnotation;
			else
				pointerLine = new string(' ', info.VisualColumnNumber - 2 - cursorAnnotation.Length) + cursorAnnotation + ' ' + cursor;

			return string.Concat(str.Substring(info.LineStart, info.LineLength), Environment.NewLine, pointerLine);
		}

		/// <summary>
		/// Extracts a line containing a specified position in a text and formats it for display.
		/// </summary>
		/// <remarks>
		/// Useful for debugging and displaying errors in a user-friendly manner.
		/// </remarks>
		/// <param name="str">The input text.</param>
		/// <param name="startInfo">The starting position information used to format the line.</param>
		/// <param name="endInfo">The ending position information used to format the line.</param>
		/// <param name="addMultilineNumber">If true, adds line number information for the start and end positions if positions spans multiple lines.</param>
		/// <param name="cursor">The cursor string to indicate the position. Must be a single character.</param>
		/// <param name="cursorAnnotation">The annotation to add to the line along with cursor.</param>
		/// <returns>
		/// A formatted string containing the line at the specified position
		/// along with line number and column information for the specified position.
		/// </returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified position is out of range for the input text.</exception>
		public static string Format(string str, PositionFormatInfo startInfo,
			PositionFormatInfo endInfo, bool addMultilineNumber = false, string cursor = "^", string? cursorAnnotation = null)
		{
			if (startInfo.Position < 0 || startInfo.Position > str.Length)
				throw new ArgumentOutOfRangeException(nameof(startInfo), "Start position must be within the bounds of the string.");
			if (endInfo.Position < 0 || endInfo.Position > str.Length)
				throw new ArgumentOutOfRangeException(nameof(endInfo), "End position must be within the bounds of the string.");
			if (startInfo.Position > endInfo.Position)
				throw new ArgumentException("Start position must be less than or equal to end position.", nameof(startInfo));

			if (startInfo.Position == endInfo.Position)
				return Format(str, startInfo, cursor, cursorAnnotation);

			int length = endInfo.Position - startInfo.Position + 1;

			if (startInfo.LineStart == endInfo.LineStart)
			{
				// At the same line

				if (string.IsNullOrEmpty(cursorAnnotation))
					return string.Concat(str.Substring(startInfo.LineStart, startInfo.LineLength),
						Environment.NewLine, new string(' ', startInfo.VisualColumnNumber - 1), new string(cursor[0], length));

				string pointerLine;
				if (startInfo.VisualColumnNumber <= cursorAnnotation.Length + 2)
					pointerLine = new string(' ', startInfo.VisualColumnNumber - 1) + new string(cursor[0], length) + ' ' + cursorAnnotation;
				else
					pointerLine = new string(' ', startInfo.VisualColumnNumber - 2 - cursorAnnotation.Length) + cursorAnnotation + ' ' + new string(cursor[0], length);

				return string.Concat(str.Substring(startInfo.LineStart, startInfo.LineLength), Environment.NewLine, pointerLine);
			}
			else
			{
				// At different lines

				var sb = new StringBuilder();

				if (addMultilineNumber)
				{
					var lineNumberStart = startInfo.LineNumber.ToString(CultureInfo.InvariantCulture);
					var lineNumberEnd = endInfo.LineNumber.ToString(CultureInfo.InvariantCulture);
					int maxLineNumberLength = Math.Max(lineNumberStart.Length, lineNumberEnd.Length);

					sb.Append(lineNumberStart.PadLeft(maxLineNumberLength) + ": ");
					sb.AppendLine(str.Substring(startInfo.LineStart, startInfo.LineLength));
					sb.Append(new string(' ', maxLineNumberLength + 2)); // + 2 from ": "
					sb.AppendLine(new string(cursor[0], startInfo.VisualLineLength - startInfo.VisualColumnNumber + 1)
						.PadLeft(startInfo.VisualLineLength));

					if (startInfo.LineNumber + 1 != endInfo.LineNumber)
						sb.AppendLine("...");

					sb.Append(lineNumberEnd.ToString().PadLeft(maxLineNumberLength) + ": ");
					sb.AppendLine(str.Substring(endInfo.LineStart, endInfo.LineLength));
					sb.Append(new string(' ', maxLineNumberLength + 2));
					sb.Append(new string(cursor[0], endInfo.VisualColumnNumber));

					if (!string.IsNullOrEmpty(cursorAnnotation))
						sb.Append(' ').Append(cursorAnnotation);
				}
				else
				{
					sb.AppendLine(str.Substring(startInfo.LineStart, startInfo.LineLength));
					sb.AppendLine(new string(cursor[0], startInfo.VisualLineLength - startInfo.VisualColumnNumber + 1)
						.PadLeft(startInfo.VisualLineLength));

					if (startInfo.LineNumber + 1 != endInfo.LineNumber)
						sb.AppendLine("...");

					sb.AppendLine(str.Substring(endInfo.LineStart, endInfo.LineLength));
					sb.Append(new string(cursor[0], endInfo.VisualColumnNumber));

					if (!string.IsNullOrEmpty(cursorAnnotation))
						sb.Append(' ').Append(cursorAnnotation);
				}

				return sb.ToString();
			}
		}
	}
}