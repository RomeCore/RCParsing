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
			var groups = new ErrorGroupCollection(context, errors);
			return groups.ToString(context.parser.MainSettings.errorFormattingFlags);
		}
	}
}