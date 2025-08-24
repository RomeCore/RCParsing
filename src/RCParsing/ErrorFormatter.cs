using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Provides methods for formatting parsing errors.
	/// </summary>
	public static class ErrorFormatter
	{
		/// <summary>
		/// Formats a parsing error for display.
		/// </summary>
		/// <param name="context">The parser context that used to configure the display and contains the input text.</param>
		/// <param name="error">The parsing error to format.</param>
		/// <returns>A formatted string representing the parsing error.</returns>
		public static string FormatError(ParserContext context, ParsingError error)
		{
			return FormatErrors(context, error);
		}

		/// <summary>
		/// Formats the parsing error sfor display.
		/// </summary>
		/// <param name="context">The parser context that used to configure the display and contains the input text.</param>
		/// <param name="errors">The parsing errors to format.</param>
		/// <returns>A formatted string representing the parsing error.</returns>
		public static string FormatErrors(ParserContext context, params ParsingError[] errors)
		{
			return FormatErrors(context, (IEnumerable<ParsingError>)errors);
		}

		/// <summary>
		/// Formats the parsing error sfor display.
		/// </summary>
		/// <param name="context">The parser context that used to configure the display and contains the input text.</param>
		/// <param name="errors">The parsing errors to format.</param>
		/// <returns>A formatted string representing the parsing error.</returns>
		public static string FormatErrors(ParserContext context, IEnumerable<ParsingError> errors)
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine("One or more errors occurred during parsing:").AppendLine();

			var groupedErrors = errors
				.GroupBy(e => e.position)
				.OrderByDescending(g => g.Key)
				.ToList();

			int max = context.settings.errorHandling.HasFlag(ParserErrorHandlingMode.DisplayExtended) ? 5 : 1;
			int last = Math.Min(groupedErrors.Count, max);

			for (int i = 0; i < last; i++)
			{
				var groupedError = groupedErrors[i];

				var position = groupedError.Key;
				var groupErrors = groupedError.ToList();

				if (!context.settings.errorHandling.HasFlag(ParserErrorHandlingMode.DisplayRules))
				{
					groupErrors.RemoveAll(e =>
					{
						ParserElement element = e.isToken
							? context.parser.TokenPatterns[e.elementId]
							: context.parser.Rules[e.elementId];
						return element is not TokenPattern && element is not TokenParserRule;
					});
				}

				var expected = groupErrors
					.Where(e => e.elementId >= 0)
					.Select(e => e.isToken
						? context.parser.TokenPatterns[e.elementId].ToString()
						: context.parser.Rules[e.elementId].ToString())
					.Distinct()
					.OrderBy(m => m)
					.ToList();

				if (context.settings.errorHandling.HasFlag(ParserErrorHandlingMode.DisplayMessages))
				{
					var msg = groupErrors
						.Select(e => e.message)
						.Where(m => !string.IsNullOrEmpty(m))
						.Distinct()
						.ToList();

					if (msg.Count > 0)
						sb.AppendLine(string.Join(" / ", msg)).AppendLine();
				}

				sb.AppendLine("The line where the error occurred:");
				sb.AppendLine(PositionalFormatter.Format(context.str, groupedError.Key));

				if (expected.Count > 0)
				{
					sb.AppendLine();

					string unexpected = $"'{GetCharacterDisplay(context.str, position)}' is unexpected character";
					string oneOf = expected.Count > 1 ? " one of" : "";

					if (expected.Count > 1 || expected.Sum(s => s.Length) > 30)
						sb.AppendLine($"{unexpected}, expected{oneOf}:\n" + string.Join("\n", expected).Indent("  "));
					else
						sb.AppendLine($"{unexpected}, expected{oneOf}: " + string.Join(", ", expected));
				}

				sb.Length -= Environment.NewLine.Length;

				if (i < last - 1)
					sb.AppendLine().AppendLine();
			}

			if (groupedErrors.Count > max)
				sb.AppendLine().AppendLine().Append("...and more errors omitted.");

			return sb.ToString();
		}

		private static string GetCharacterDisplay(string input, int position)
		{
			if (position == input.Length)
				return "end of file";

			var ch = input[position];

			return ch switch
			{
				'\t' => "tab",
				'\n' => "newline",
				'\r' => "return",
				' ' => "space",
				_ => ch.ToString()
			};
		}
	}
}