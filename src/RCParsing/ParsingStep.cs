using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents the type of a parsing step for walk trace.
	/// </summary>
	public enum ParsingStepType
	{
		/// <summary>
		/// Just began to parse a rule.
		/// </summary>
		Enter,

		/// <summary>
		/// Information about the parsing step.
		/// </summary>
		Info,

		/// <summary>
		/// Successfully finished parsing a rule.
		/// </summary>
		Success,

		/// <summary>
		/// Finished parsing a rule with failure.
		/// </summary>
		Fail
	}

	/// <summary>
	/// Represents a single step in the walk trace of a parser.
	/// </summary>
	public struct ParsingStep
	{
		/// <summary>
		/// The type of the parsing step.
		/// </summary>
		public ParsingStepType type;

		/// <summary>
		/// The ID of the parser rule that is being parsed.
		/// </summary>
		public int ruleId;

		/// <summary>
		/// The optional message associated with the parsing step.
		/// </summary>
		public string message;

		/// <summary>
		/// The current position in the input text where parsing is taking place.
		/// </summary>
		public int startIndex;

		/// <summary>
		/// The number of characters that have been parsed if successful, otherwise 0.
		/// </summary>
		public int length;
	}
}