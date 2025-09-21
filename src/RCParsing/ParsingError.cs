using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a parsing error encountered during the parsing of input string.
	/// </summary>
	public struct ParsingError : IEquatable<ParsingError>
	{
		/// <summary>
		/// Gets the position in the input string where the error occurred.
		/// </summary>
		public int position;

		/// <summary>
		/// Gets the count of barriers that were successfully parsed before encountering this error.
		/// </summary>
		public int passedBarriers;

		/// <summary>
		/// Gets an optional description of the parsing error.
		/// </summary>
		public string? message;

		/// <summary>
		/// Gets the ID of the element (rule or token) that caused the error or been expected at this position.
		/// </summary>
		public int elementId;

		/// <summary>
		/// Gets a value indicating whether the element that caused the error is a token.
		/// </summary>
		public bool isToken;

		/// <summary>
		/// Gets the stack frame that describes the state of the parser when the error occurred.
		/// </summary>
		public IntermediateParserStackFrame? stackFrame;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingError"/> struct.
		/// </summary>
		/// <param name="position">The position in the input string where the error occurred.</param>
		/// <param name="passedBarriers">The count of barriers that were successfully parsed before encountering this error.</param>
		/// <param name="message">An optional description of the parsing error.</param>
		/// <param name="elementId">The ID of the element (rule or token) that caused the error or been expected at this position.</param>
		/// <param name="isToken">A value indicating whether the element that caused the error is a token.</param>
		/// <param name="stackFrame">The stack frame that describes the state of the parser when the error occurred.</param>
		public ParsingError(int position, int passedBarriers, string? message = null, int elementId = -1, bool isToken = false, IntermediateParserStackFrame? stackFrame = null)
		{
			this.position = position;
			this.passedBarriers = passedBarriers;
			this.message = message;
			this.elementId = elementId;
			this.isToken = isToken;
			this.stackFrame = stackFrame;
		}

		/// <summary>
		/// Returns a string that represents the parsing error with pretty formatted
		/// target line with line and column number informations.
		/// </summary>
		/// <param name="context">The parser context used for formatting.</param>
		/// <returns>A string that represents the parsing error.</returns>
		public string ToString(ParserContext context)
		{
			return ErrorFormatter.FormatError(context, this);
		}

		/// <summary>
		/// Converts the parsing error to a <see cref="ParsingException"/> with additional information from the provided <see cref="ParserContext"/>.
		/// </summary>
		/// <param name="context">The parser context to use for additional information.</param>
		/// <returns>An instance of <see cref="ParsingException"/> containing message, position and formatted input text.</returns>
		public ParsingException ToException(ParserContext context)
		{
			return new ParsingException(context, this);
		}

		/// <summary>
		/// Gets the empty instance of <see cref="ParsingError"/>.
		/// </summary>
		public static readonly ParsingError Empty = new ParsingError(-1, 0);

		public override string ToString()
		{
			string target = string.Empty;
			if (elementId != -1)
				target = isToken ? $"token::{elementId}" : $"rule::{elementId}";
			string msg = string.Empty;
			if (message != null)
				msg = $", message::{message}";
			return $"[{position}], expected a {target}{msg}";
		}

		public override bool Equals(object obj)
		{
			return obj is ParsingError other && Equals(other);
		}

		public bool Equals(ParsingError other)
		{
			return position == other.position &&
				   message == other.message &&
				   elementId == other.elementId &&
				   isToken == other.isToken &&
				   stackFrame == other.stackFrame;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 397 + position.GetHashCode();
			hash = hash * 397 + (message?.GetHashCode() ?? 0);
			hash = hash * 397 + elementId.GetHashCode();
			hash = hash * 397 + isToken.GetHashCode();
			hash = hash * 397 + (stackFrame?.GetHashCode() ?? 0);
			return hash;
		}

		public static bool operator ==(ParsingError left, ParsingError right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ParsingError left, ParsingError right)
		{
			return !(left == right);
		}
	}
}