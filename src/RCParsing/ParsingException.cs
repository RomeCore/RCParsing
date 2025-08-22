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
			return ErrorFormatter.FormatErrors(context, errors);
		}
	}
}