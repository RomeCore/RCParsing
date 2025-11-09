using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCParsing
{
#pragma warning disable IDE1006 // Naming Styles

	/// <summary>
	/// Represents a shared parser context part for parsing input text.
	/// </summary>
	public class SharedParserContext
	{
		/// <summary>
		/// Gets the input text being parsed.
		/// </summary>
		public readonly string input;

		/// <summary>
		/// The optional parameter passed to the parser. Can be used to pass additional information to the
		/// transformation functions, custom parser rules and token patterns.
		/// </summary>
		public readonly object? parserParameter;

		/// <summary>
		/// The parser object that is performing the parsing.
		/// </summary>
		public readonly Parser parser;

		/// <summary>
		/// A cache to store parsed results for reuse.
		/// </summary>
		public readonly ParserCache cache;

		/// <summary>
		/// A list to store any parsing errors encountered during the process.
		/// </summary>
		public readonly List<ParsingError> errors;

		/// <summary>
		/// A list of indices pointing to <see cref="errors"/> when error recovery was triggered.
		/// </summary>
		/// <remarks>
		/// When error recovery triggers, the <c>errors.Count</c> is added to this list.
		/// Used for retrieving the relevant error groups.
		/// </remarks>
		public readonly List<int> errorRecoveryIndices;

		/// <summary>
		/// A list of barrier tokens that are used to prevent parsing at certain positions.
		/// </summary>
		public readonly BarrierTokenCollection barrierTokens;

		/// <summary>
		/// A collection of walk trace entries that are used for debugging purposes.
		/// </summary>
		public readonly ParserWalkTrace walkTrace;

		/// <summary>
		/// Initializes a new instance of the <see cref="SharedParserContext"/> class.
		/// </summary>
		/// <param name="parser">The parser object that is performing the parsing.</param>
		/// <param name="str">The input string to parse.</param>
		/// <param name="parserParameter">The optional parameter passed to the parser. Can be used to pass additional information to the transformation functions, custom parser rules and token patterns.</param>
		/// <exception cref="ArgumentNullException"></exception>
		public SharedParserContext(Parser parser, string str, object? parserParameter = null)
		{
			this.input = str;
			this.parserParameter = parserParameter;
			this.parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.cache = new ParserCache();
			this.errors = new List<ParsingError>();
			this.errorRecoveryIndices = new List<int>();
			this.barrierTokens = new BarrierTokenCollection();
			this.walkTrace = new ParserWalkTrace(this);
		}
	}

	/// <summary>
	/// Represents a stack frame for the parser. Used to keep track of the current state during parsing.
	/// </summary>
	public class IntermediateParserStackFrame
	{
		/// <summary>
		/// Gets the previous stack frame, if any.
		/// </summary>
		public readonly IntermediateParserStackFrame? previous;

		/// <summary>
		/// Gets the rule ID that is currently being parsed.
		/// </summary>
		public readonly int ruleId;

		/// <summary>
		/// Gets the position where parsing of this element was started.
		/// </summary>
		public readonly int position;

		/// <summary>
		/// Gets the recursion depth at which the current parsing operation is taking place.
		/// </summary>
		public readonly int recursionDepth;

		/// <summary>
		/// Creates a new instance of the <see cref="IntermediateParserStackFrame"/> class.
		/// </summary>
		/// <param name="previous">The parser stack frame that is the previous one in the stack.</param>
		/// <param name="ruleId">The ID of the parser rule that is currently being parsed.</param>
		/// <param name="position">The position where parsing of this element was started.</param>
		public IntermediateParserStackFrame(IntermediateParserStackFrame? previous, int ruleId, int position)
		{
			this.previous = previous;
			this.ruleId = ruleId;
			this.position = position;
			this.recursionDepth = previous == null ? 0 : previous.recursionDepth + 1;
		}
	}

	/// <summary>
	/// Represents the context for parsing operations.
	/// </summary>
	public struct ParserContext
	{
		/// <summary>
		/// The input string to be parsed.
		/// </summary>
		public readonly string input => shared.input;

		/// <summary>
		/// The current position in the input string.
		/// </summary>
		public int position;

		/// <summary>
		/// Maximum position in the input string. Used to limit parsing.
		/// </summary>
		public int maxPosition;

		/// <summary>
		/// The count of passed barrier tokens.
		/// </summary>
		public int passedBarriers;

		/// <summary>
		/// Gets the top stack frame in the rule parsing stack, if any.
		/// </summary>
		public IntermediateParserStackFrame? topStackFrame;

		/// <summary>
		/// The shared parser context that is performing the parsing.
		/// </summary>
		public SharedParserContext shared;

		/// <summary>
		/// Gets the current recursion depth of the parsing operation if the stack tracing is enabled.
		/// </summary>
		public readonly int recursionDepth => topStackFrame == null ? 0 : topStackFrame.recursionDepth;

		/// <summary>
		/// The optional parameter passed to the parser. Can be used to pass additional information to the
		/// transformation functions, custom parser rules and token patterns.
		/// </summary>
		public readonly object? parserParameter => shared.parserParameter;

		/// <summary>
		/// The parser object that is performing the parsing.
		/// </summary>
		public readonly Parser parser => shared.parser;

		/// <summary>
		/// A cache to store parsed results for reuse.
		/// </summary>
		public readonly ParserCache cache => shared.cache;

		/// <summary>
		/// A list to store any parsing errors encountered during the process.
		/// </summary>
		public readonly List<ParsingError> errors => shared.errors;

		/// <summary>
		/// A list of indices pointing to <see cref="errors"/> when error recovery was triggered.
		/// </summary>
		/// <remarks>
		/// When error recovery triggers, the <c>errors.Count</c> is added to this list.
		/// Used for retrieving the relevant error groups.
		/// </remarks>
		public readonly List<int> errorRecoveryIndices => shared.errorRecoveryIndices;

		/// <summary>
		/// A list of barrier tokens that are used to prevent parsing at certain positions.
		/// </summary>
		public readonly BarrierTokenCollection barrierTokens => shared.barrierTokens;

		/// <summary>
		/// A collection of walk trace entries that are used for debugging purposes.
		/// </summary>
		public readonly ParserWalkTrace walkTrace => shared.walkTrace;

		/// <summary>
		/// Gets a summary of first 10 parsing errors encountered during the process.
		/// </summary>
		/// <remarks>
		/// Needed for debugging purposes.
		/// </remarks>
		public readonly string ErrorSummary => GetErrorSummary(10);

		/// <summary>
		/// Gets the text after the current position in the input string.
		/// </summary>
		/// <remarks>
		/// Needed for debugging purposes.
		/// </remarks>
		public string TextAfterPosition
		{
			get
			{
				var substring = this.input.Substring(this.position);
				if (substring.Length > 20)
					return substring.Substring(0, 20) + "...";
				return substring;
			}
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ParserContext"/> class.
		/// </summary>
		/// <param name="parser">The parser object that is performing the parsing.</param>
		/// <param name="str">The input string to be parsed.</param>
		/// <param name="parserParameter">Optional parameter that have been passed to the parser.</param>
		internal ParserContext(Parser parser, string str, object? parserParameter)
		{
			position = 0;
			maxPosition = str.Length;
			passedBarriers = 0;
			topStackFrame = null;
			shared = new SharedParserContext(parser, str, parserParameter);
		}

		/// <summary>
		/// Appends a new stack frame to the parser's stack. Used for tracking recursion depth and parsing rules.
		/// </summary>
		/// <param name="ruleId">The ID of the parser rule that is currently being parsed.</param>
		/// <param name="position">The position where parsing of rule was started.</param>
		public void AppendStackFrame(int ruleId, int position)
		{
			topStackFrame = new IntermediateParserStackFrame(topStackFrame, ruleId, position);
		}

		/// <summary>
		/// Records, ignores or throws an error based on settings.
		/// </summary>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		/// <param name="error">The parsing error to record.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void RecordError(ParserSettings settings, ParsingError error)
		{
			switch (settings.errorHandling)
			{
				case ParserErrorHandlingMode.Default:
					errors.Add(error);
					break;

				case ParserErrorHandlingMode.NoRecord:
					break;

				case ParserErrorHandlingMode.Throw:
					errors.Add(error);
					throw new ParsingException(this);
			}
		}

		/// <summary>
		/// Records, ignores or throws an error based on the current settings.
		/// </summary>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		/// <param name="message">The error message to record.</param>
		/// <param name="elementId">The ID of the element (rule or token) that caused the error or been expected at this position.</param>
		/// <param name="isToken">A value indicating whether the element that caused the error is a token.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void RecordError(ParserSettings settings, string? message = null, int elementId = -1, bool isToken = false)
		{
			RecordError(settings, new ParsingError(position, passedBarriers, message, elementId, isToken, topStackFrame));
		}

		/// <summary>
		/// Records, ignores or throws an error based on the current settings.
		/// </summary>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		/// <param name="position">The position in the input string where the error occurred.</param>
		/// <param name="message">The error message to record.</param>
		/// <param name="elementId">The ID of the element (rule or token) that caused the error or been expected at this position.</param>
		/// <param name="isToken">A value indicating whether the element that caused the error is a token.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void RecordError(ParserSettings settings, int position, string? message = null, int elementId = -1, bool isToken = false)
		{
			RecordError(settings, new ParsingError(position, passedBarriers, message, elementId, isToken, topStackFrame));
		}

		/// <summary>
		/// Records, ignores or throws an error based on the current settings.
		/// </summary>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		/// <param name="position">The position in the input string where the error occurred.</param>
		/// <param name="passedBarriers">The count of barriers that were successfully parsed before encountering this error.</param>
		/// <param name="message">The error message to record.</param>
		/// <param name="elementId">The ID of the element (rule or token) that caused the error or been expected at this position.</param>
		/// <param name="isToken">A value indicating whether the element that caused the error is a token.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public readonly void RecordError(ParserSettings settings, int position, int passedBarriers, string? message = null, int elementId = -1, bool isToken = false)
		{
			RecordError(settings, new ParsingError(position, passedBarriers, message, elementId, isToken, topStackFrame));
		}

		/// <summary>
		/// Records an index pointing to the current error count when error recovery was triggered.
		/// </summary>
		public void RecordErrorRecoveryIndex()
		{
			if (errors.Count > 0)
				errorRecoveryIndices.Add(errors.Count);
		}

		/// <summary>
		/// Returns a summary of all parsing errors encountered during the process, limited to a specified number of errors.
		/// </summary>
		/// <param name="maxErrors">The maximum number of errors to include in the summary.</param>
		/// <returns>A summary of the parsing errors.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown if the specified number of errors is negative.</exception>
		public readonly string GetErrorSummary(int maxErrors)
		{
			if (maxErrors < 0)
				throw new ArgumentOutOfRangeException(nameof(maxErrors));
			if (errors.Count == 0)
				return "No errors encountered.";

			ParserContext t = this;

			return $"Errors ({errors.Count} total):\n" +
				$"{string.Join("\n\n", errors.Take(maxErrors).Select(e => e.ToString(t)))}" +
				$"{(errors.Count > maxErrors ? $"\n\nand {errors.Count - maxErrors} more..." : "")}";
		}

		/// <summary>
		/// Creates error groups from stored parsing errors.
		/// </summary>
		/// <param name="excludeLastRelevantGroup">
		/// Whether to exclude last relevant error group.
		/// Should be <see langword="true"/> when parsing was successful and last error group is not relevant at all.
		/// </param>
		/// <returns>A collection of error groups.</returns>
		public readonly ErrorGroupCollection CreateErrorGroups(bool excludeLastRelevantGroup = false)
		{
			return new ErrorGroupCollection(this, errors, errorRecoveryIndices, excludeLastRelevantGroup);
		}
	}
}