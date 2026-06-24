using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents an exception that occurs during semantic analysis (e.g. transforming <see cref="ParsedRuleResultBase"/> into a value).
	/// </summary>
	public class SemanticException : Exception
	{
		/// <summary>
		/// Gets the parsed rule result associated with this semantic exception.
		/// </summary>
		public ParsedRuleResultBase AssociatedResult { get; }

		/// <summary>
		/// Gets the original exception message that have been passed to constructor.
		/// </summary>
		public string OriginalMessage { get; }

		/// <summary>
		/// Gets the children exceptions associated with this semantic exception.
		/// </summary>
		public IReadOnlyList<SemanticException> Children { get; }

		public SemanticException(ParsedRuleResultBase result, string message, Exception? inner = null) :
			base(FormatMessage(result, message, Enumerable.Empty<SemanticException>()), inner)
		{
			AssociatedResult = result;
			OriginalMessage = message;
			Children = Array.Empty<SemanticException>();
		}

		public SemanticException(ParsedRuleResultBase result, string message, params SemanticException[] children) :
			base(FormatMessage(result, message, children))
		{
			AssociatedResult = result;
			OriginalMessage = message;
			Children = children.AsReadOnlyList();
		}

		public SemanticException(ParsedRuleResultBase result, string message, Exception? inner, params SemanticException[] children) :
			base(FormatMessage(result, message, children), inner)
		{
			AssociatedResult = result;
			OriginalMessage = message;
			Children = children.AsReadOnlyList();
		}

		public SemanticException(ParsedRuleResultBase result, string message, IEnumerable<SemanticException> children) :
			base(FormatMessage(result, message, children))
		{
			AssociatedResult = result;
			OriginalMessage = message;
			Children = children.AsReadOnlyList();
		}

		public SemanticException(ParsedRuleResultBase result, string message, Exception? inner, IEnumerable<SemanticException> children) :
			base(FormatMessage(result, message, children), inner)
		{
			AssociatedResult = result;
			OriginalMessage = message;
			Children = children.AsReadOnlyList();
		}

		private static string FormatMessage(ParsedRuleResultBase result, string message, IEnumerable<SemanticException> children)
		{
			var sb = new StringBuilder();

			sb.Append("A semantic error occurred: ");
			sb.AppendLine(message);
			sb.AppendLine();

			var input = result.Context.input;
			if (result.Length is 0 or 1)
			{
				PositionalFormatter.Decompose(input, result.StartIndex,
					out int lineStart, out int lineLength, out int lineNumber, out int columnNumber, out int visualColumnNumber,
					result.Context.parser.MainSettings.tabSize);

				sb.AppendLine("Location:");

				string lineAndColumn = $"line {lineNumber}, column {columnNumber}";
				string pointerLine;
				if (visualColumnNumber <= lineAndColumn.Length + 2)
					pointerLine = new string(' ', visualColumnNumber - 1) + '^' + ' ' + lineAndColumn;
				else
					pointerLine = new string(' ', visualColumnNumber - 2 - lineAndColumn.Length) + lineAndColumn + ' ' + '^';

				sb.AppendLine(input.Substring(lineStart, lineLength));
				sb.AppendLine(pointerLine);
			}
			else
			{
				PositionalFormatter.Decompose(input, result.StartIndex,
					out int lineStart1, out int lineLength1, out int lineNumber1, out int columnNumber1, out int visualColumnNumber1,
					result.Context.parser.MainSettings.tabSize);

				PositionalFormatter.Decompose(input, result.EndIndex - 1,
					out int lineStart2, out int lineLength2, out int lineNumber2, out int columnNumber2, out int visualColumnNumber2,
					result.Context.parser.MainSettings.tabSize);

				if (lineNumber1 == lineNumber2)
				{
					sb.AppendLine("Location:");

					// At the same line
					string lineAndColumn = $"line {lineNumber1}, column {columnNumber1}, length {result.Length}";
					string pointerLine;
					if (visualColumnNumber1 <= lineAndColumn.Length + 2)
						pointerLine = new string(' ', visualColumnNumber1 - 1) + new string('^', result.Length) + ' ' + lineAndColumn;
					else
						pointerLine = new string(' ', visualColumnNumber1 - 2 - lineAndColumn.Length) + lineAndColumn + ' ' + new string('^', result.Length);

					sb.AppendLine(input.Substring(lineStart1, lineLength1));
					sb.AppendLine(pointerLine);
				}
				else
				{
					sb.AppendLine($"Location (from line {lineNumber1}, column {columnNumber1} to line {lineNumber2}, column {columnNumber2}; length {result.Length} characters):");

					int maxLineNumberLength = Math.Max(lineNumber1.ToString().Length, lineNumber2.ToString().Length);

					// At different lines
					sb.Append(lineNumber1.ToString().PadLeft(maxLineNumberLength) + ": ");
					sb.AppendLine(input.Substring(lineStart1, lineLength1));
					sb.Append(new string(' ', maxLineNumberLength + 2)); // + 2 from ": "
					sb.AppendLine(new string('^', lineLength1 - visualColumnNumber1 + 1).PadLeft(lineLength1));

					if (lineNumber1 + 1 != lineNumber2)
						sb.AppendLine("...");

					sb.Append(lineNumber2.ToString().PadLeft(maxLineNumberLength) + ": ");
					sb.AppendLine(input.Substring(lineStart2, lineLength2));
					sb.Append(new string(' ', maxLineNumberLength + 2));
					sb.AppendLine(new string('^', visualColumnNumber2));
				}
			}

			sb.AppendLine().AppendLine("The rule that failed:");
			sb.Append(result.Rule.ToString());

			var childList = children.ToList();
			if (childList.Count > 0)
			{
				sb.AppendLine().AppendLine("Children errors:");
				for (int i = 0; i < childList.Count; i++)
				{
					if (i < childList.Count - 1)
						sb.AppendLine(childList[i].OriginalMessage);
					else
						sb.Append(childList[i].OriginalMessage);
				}
			}

			return sb.ToString();
		}
	}
}