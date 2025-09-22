using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
		public IReadOnlyList<ParsingError> Errors { get; }

		/// <summary>
		/// Gets the last error message during parsing.
		/// </summary>
		public string LastErrorMessage { get; }

		/// <summary>
		/// Gets the last position in the input where the error occurred.
		/// </summary>
		public int LastPosition { get; }

		/// <summary>
		/// Gets the groups of errors that occurred during parsing.
		/// </summary>
		/// <remarks>
		/// Groups errors by their position in the input.
		/// </remarks>
		public ErrorGroupCollection Groups { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		/// <param name="message">The error message.</param>
		public ParsingException(ParserContext context, string message) :
			base(FormatMessage(context, out var groups,
				new ParsingError[] { CreateError(message, context.position, out var error) }))
		{
			Context = context;
			Errors = new ParsingError[] { error }.AsReadOnlyList();
			LastErrorMessage = message;
			LastPosition = context.position;
			Groups = groups;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		/// <param name="message">The error message.</param>
		/// <param name="position">The position in the input where the error occurred.</param>
		public ParsingException(ParserContext context, string message, int position) :
			base(FormatMessage(context, out var groups,
				new ParsingError[] { CreateError(message, position, out var error) }))
		{
			Context = context;
			Errors = new ParsingError[] { error }.AsReadOnlyList();
			LastErrorMessage = message;
			LastPosition = position;
			Groups = groups;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		public ParsingException(ParserContext context) :
			this(context, context.errors)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		/// <param name="errors">The list of parsing errors that occurred.</param>
		public ParsingException(ParserContext context, params ParsingError[] errors) :
			this(context, (IReadOnlyList<ParsingError>)errors)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingException"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		/// <param name="errors">The list of parsing errors that occurred.</param>
		public ParsingException(ParserContext context, IReadOnlyList<ParsingError> errors) :
			base(FormatMessage(context, out var groups, errors))
		{
			if (errors == null)
				throw new ArgumentNullException(nameof(errors));

			Context = context;
			Groups = groups;
			Errors = Groups.Errors;

			var errorMessages = Groups.Last?.ErrorMessages ?? Array.Empty<string>();
			LastErrorMessage = errorMessages.Count > 0 ? errorMessages[errorMessages.Count - 1] : string.Empty;
			LastPosition = Groups.Last?.Position ?? context.position;
		}

		private static ParsingError CreateError(string message, int position, out ParsingError error)
		{
			return error = new ParsingError(position, 0, message);
		}

		private static string FormatMessage(ParserContext context, out ErrorGroupCollection groups, IReadOnlyList<ParsingError> errors)
		{
			groups = new ErrorGroupCollection(context, errors);

			var flags = context.parser.MainSettings.errorFormattingFlags;
			int maxGroups = Math.Min(flags == ErrorFormattingFlags.MoreGroups ? 5 : 1, groups.Count);

			if (maxGroups == 0)
				return "Unknown error.";

			string header = maxGroups > 1
				? "One or more errors occured during parsing:"
				: "An error occurred during parsing:";

			var sb = new StringBuilder();

			sb.AppendLine(header).AppendLine();
			sb.Append(groups.ToString(flags, maxGroups));

			if (context.walkTrace.Count > 0)
				sb.AppendLine().AppendLine().AppendLine("Walk Trace:")
					.AppendLine().Append(context.walkTrace.Render(maxCount:
						context.parser.MainSettings.maxWalkStepsDisplay));

			return sb.ToString();
		}
	}
}