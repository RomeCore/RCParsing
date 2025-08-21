using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a parsing error encountered during the parsing of input string.
	/// </summary>
	public readonly struct ParsingError : IEquatable<ParsingError>
	{
		/// <summary>
		/// Gets the position in the input string where the error occurred.
		/// </summary>
		public readonly int position;

		/// <summary>
		/// Gets the recursion depth at which the error occurred.
		/// </summary>
		public readonly int depth;

		/// <summary>
		/// Gets an optional description of the parsing error.
		/// </summary>
		public readonly string? message;

		/// <summary>
		/// Gets the ID of the element (rule or token) that caused the error or been expected at this position.
		/// </summary>
		public readonly int elementId;

		/// <summary>
		/// Gets a value indicating whether the element that caused the error is a token.
		/// </summary>
		public readonly bool isToken;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParsingError"/> struct.
		/// </summary>
		/// <param name="position">The position in the input string where the error occurred.</param>
		/// <param name="depth">The recursion depth at which the error occurred.</param>
		/// <param name="message">An optional description of the parsing error.</param>
		/// <param name="elementId">The ID of the element (rule or token) that caused the error or been expected at this position.</param>
		/// <param name="isToken">A value indicating whether the element that caused the error is a token.</param>
		public ParsingError(int position, int depth, string? message = null, int elementId = -1, bool isToken = false)
		{
			this.position = position;
			this.depth = depth;
			this.message = message;
			this.elementId = elementId;
			this.isToken = isToken;
		}

		/// <summary>
		/// Returns a string that represents the parsing error with pretty formatted
		/// target line with line and column number informations.
		/// </summary>
		/// <param name="context">The parser context used for formatting.</param>
		/// <returns>A string that represents the parsing error.</returns>
		public string ToString(ParserContext context)
		{
			string msg = message != null ? $"{message}\n" : string.Empty;
			string elem = elementId != -1 ? $"Failed to parse/expected {(isToken ? context.parser.TokenPatterns[elementId] : context.parser.Rules[elementId])}\n" : string.Empty;

			if (string.IsNullOrEmpty(msg) && string.IsNullOrEmpty(elem))
				msg = "Unknown error\n";

			return $"{msg}{elem}{PositionalFormatter.Format(context.str, position)}";
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

		public override string ToString()
		{
			string target = string.Empty;
			if (elementId != -1)
				target = isToken ? $"token::{elementId}" : $"rule::{elementId}";
			string msg = string.Empty;
			if (message != null)
				msg = $", message::{message}";
			return $"[{position}::depth={depth}], expected a {target}{msg}";
		}

		public override bool Equals(object obj)
		{
			return obj is ParsingError other && Equals(other);
		}

		public bool Equals(ParsingError other)
		{
			return position == other.position &&
				   depth == other.depth &&
				   message == other.message &&
				   elementId == other.elementId &&
				   isToken == other.isToken;
		}

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + position.GetHashCode();
			hash = hash * 23 + depth.GetHashCode();
			hash = hash * 23 + (message?.GetHashCode() ?? 0);
			hash = hash * 23 + elementId.GetHashCode();
			hash = hash * 23 + isToken.GetHashCode();
			return hash;
		}
	}
}