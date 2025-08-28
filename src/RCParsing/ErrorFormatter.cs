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

			var flags = context.parser.MainSettings.errorFormattingFlags;
			int max = flags.HasFlag(ErrorFormattingFlags.MoreGroups) ? 5 : 1;
			int last = Math.Min(groupedErrors.Count, max);

			for (int i = 0; i < last; i++)
			{
				var groupedError = groupedErrors[i];

				var position = groupedError.Key;
				var groupErrors = groupedError.ToList();

				if (!flags.HasFlag(ErrorFormattingFlags.DisplayRules))
				{
					groupErrors.RemoveAll(e =>
					{
						if (e.elementId < 0)
							return false;
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

				if (expected.Count == 0 ||
					flags.HasFlag(ErrorFormattingFlags.DisplayMessages))
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

					if (context.barrierTokens.TryGetBarrierToken(position, context.passedBarriers, out var barrierToken))
					{
						unexpected = $"'{barrierToken.tokenAlias}' " +
							$"is unexpected barrier token or '{GetCharacterDisplay(context.str, position)}' " +
							$"is unexpected character";
					}

					string oneOf = expected.Count > 1 ? " one of" : "";

					if (expected.Count > 1 || expected.Sum(s => s.Length) > 30)
						sb.AppendLine($"{unexpected}, expected{oneOf}:\n" + string.Join("\n", expected).Indent("  "));
					else
						sb.AppendLine($"{unexpected}, expected{oneOf} " + string.Join(", ", expected));
				}

				HashSet<int> recodedElements = new HashSet<int>();

				foreach (var error in groupedError)
				{
					var topFrame = error.stackFrame;
					int prevStackRule = -1;

					if (topFrame != null && !recodedElements.Add(topFrame.ruleId))
					{
						sb.AppendLine();
						sb.Append($"[{context.parser.Rules[topFrame.ruleId].ToString(0)}] ");
						sb.AppendLine("Stack trace (top call recently):");
						prevStackRule = topFrame.ruleId;
						topFrame = topFrame.previous;

						int remainingFrames = 15;
						while (topFrame != null && remainingFrames-- >= 0)
						{
							recodedElements.Add(topFrame.ruleId);
							var rule = context.parser.Rules[topFrame.ruleId];
							sb.AppendLine("- " + rule.ToStackTraceString(1, prevStackRule)
								.Indent("  ", addIndentToFirstLine: false));
							prevStackRule = topFrame.ruleId;
							topFrame = topFrame.previous;
						}

						if (topFrame != null)
							sb.AppendLine("Stack trace truncated...");
					}
				}

				sb.Length -= Environment.NewLine.Length;

				if (i < last - 1)
					sb.AppendLine().AppendLine().Append("===== NEXT ERROR =====").AppendLine().AppendLine();
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
				'\t' => "tab (\\t)",
				'\n' => "newline (\\n)",
				'\r' => "return (\\r)",
				' ' => "space (' ')",
				_ => ch.ToString()
			};
		}
	}
}