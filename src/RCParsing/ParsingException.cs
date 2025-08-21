using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using RCParsing.ParserRules;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents an exception that occurs during parsing.
	/// </summary>
	public class ParsingException : Exception
	{
		/// <summary>
		/// Gets the parser context that was used during parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets a list of errors that occurred during parsing.
		/// </summary>
		public ImmutableList<ParsingError> Errors { get; }

		/// <summary>
		/// Gets the last error message during parsing.
		/// </summary>
		public string ErrorMessage { get; }

		/// <summary>
		/// Gets the list of original messages of the exception.
		/// </summary>
		public ImmutableList<string> ErrorMessages { get; }

		/// <summary>
		/// Gets the last position in the input where the error occurred.
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Gets the positions in the input where the errors occurred.
		/// </summary>
		public ImmutableList<int> Positions { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		/// <param name="message">The error message.</param>
		public ParsingException(ParserContext context, string message) :
			base(FormatMessage(context, ErrorFromContextAndMessage(context, message)))
		{
			Context = context;
			ErrorMessages = ImmutableList.Create(message);
			ErrorMessage = message;
			Positions = ImmutableList.Create(context.position);
			Position = context.position;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		/// <param name="errors">The list of parsing errors that occurred.</param>
		public ParsingException(ParserContext context, params ParsingError[] errors) :
			base(FormatMessage(context, errors))
		{
			if (errors == null)
				throw new ArgumentNullException(nameof(errors));
			if (!errors.Any())
				throw new ArgumentException("At least one error must be provided.", nameof(errors));

			Context = context;
			ErrorMessages = errors.Select(e => e.message).ToImmutableList();
			ErrorMessage = ErrorMessages[ErrorMessages.Count - 1];
			Positions = errors.Select(e => e.position).ToImmutableList();
			Position = Positions[Positions.Count - 1];
		}

		private static ParsingError ErrorFromContextAndMessage(ParserContext context, string message)
		{
			return new ParsingError(context.position, context.recursionDepth, message);
		}

		private static string FormatMessage(ParserContext context, ParsingError error)
		{
			return error.ToString(context);
		}

		private static string FormatMessage(ParserContext context, params ParsingError[] errors)
		{
			if (errors.Length == 1)
				return FormatMessage(context, errors[0]);

			StringBuilder sb = new StringBuilder();
			const int max = 3;

			sb.AppendLine("Multiple errors occurred during parsing:").AppendLine();

			var groupedErrors = errors
				.GroupBy(e => e.position)
				.OrderByDescending(g => g.Key)
				.ToList();

			int last = Math.Min(groupedErrors.Count, max);

			for (int i = 0; i < last; i++)
			{
				var groupedError = groupedErrors[i];

				var position = groupedError.Key;
				var groupErrors = groupedError.ToList();

				if (groupErrors.Count > 7)
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

				var msg = groupErrors
					.Select(e => e.message)
					.Where(m => !string.IsNullOrEmpty(m))
					.Distinct()
					.ToList();

				if (msg.Count > 0)
					sb.AppendLine(string.Join(" / ", msg)).AppendLine();

				sb.AppendLine("The line where the error occurred:");
				sb.AppendLine(PositionalFormatter.Format(context.str, groupedError.Key));

				if (expected.Count > 0)
				{
					sb.AppendLine();
					string oneOf = msg.Count > 1 ? " one of" : "";
					if (expected.Any(e => e.Contains('\n') || e.Contains('\r')) || expected.Sum(s => s.Length) > 30)
						sb.AppendLine($"Expected{oneOf}:\n" + string.Join("\n", expected).Indent("  "));
					else
						sb.AppendLine($"Expected{oneOf}: " + string.Join(", ", expected));
				}

				sb.Length--;

				if (i < last - 1)
					sb.AppendLine().AppendLine();
			}

			if (groupedErrors.Count > max)
				sb.AppendLine().AppendLine().Append("...and more errors omitted.");

			return sb.ToString().Indent("  ", addIndentToFirstLine: false);
		}
	}
}