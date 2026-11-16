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
				var info = PositionalFormatter.Decompose(input, result.StartIndex, result.Context.parser.MainSettings.tabSize);
				sb.AppendLine("Location:");
				string annotation = $"line {info.LineNumber}, column {info.ColumnNumber}";
				sb.AppendLine(PositionalFormatter.Format(input, info, cursorAnnotation: annotation));
			}
			else
			{
				var startInfo = PositionalFormatter.Decompose(input, result.StartIndex, result.Context.parser.MainSettings.tabSize);
				var endInfo = PositionalFormatter.Decompose(input, result.EndIndex - 1, result.Context.parser.MainSettings.tabSize);

				if (startInfo.LineNumber == endInfo.LineNumber)
				{
					// At the same line

					sb.AppendLine("Location:");
					string annotation = $"line {startInfo.LineNumber}, column {startInfo.ColumnNumber}, length {result.Length}";
					sb.AppendLine(PositionalFormatter.Format(input, startInfo, endInfo, cursorAnnotation: annotation));
				}
				else
				{
					// At different lines

					sb.AppendLine($"Location (from line {startInfo.LineNumber}, column {startInfo.ColumnNumber} to line {endInfo.LineNumber}, " +
						$"column {endInfo.ColumnNumber}; length {result.Length} characters):");
					sb.AppendLine(PositionalFormatter.Format(input, startInfo, endInfo, addMultilineNumber: true));
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