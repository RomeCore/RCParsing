using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RCParsing.TokenPatterns;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents a collection of parsing errors grouped by position in the input text.
	/// </summary>
	public class ErrorGroup
	{
		/// <summary>
		/// Gets the parser context used for parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets the parser used for parsing.
		/// </summary>
		public Parser Parser => Context.parser;

		/// <summary>
		/// Gets the input text that was parsed with error.
		/// </summary>
		public string Input => Context.input;

		/// <summary>
		/// Gets the position in the input text where the error occurred.
		/// </summary>
		public int Position { get; }

		/// <summary>
		/// Gets the list of parsing errors that occurred during parsing.
		/// </summary>
		public IReadOnlyList<ParsingError> Errors { get; }

		private string? _lineText = null;
		private string? _formattedLineText = null;
		private bool? _isRelevant = null;
		private int? _passedBarriers = null;

		private int _lineStart = -1;
		private int _lineLength = -1;
		private int _line = -1;
		private int _column = -1;
		private int _visualColumn = -1;

		private void Calculate()
		{
			PositionalFormatter.Decompose(Input, Position, out _lineStart, out _lineLength,
				out _line, out _column, out _visualColumn, Context.parser.MainSettings.tabSize);
		}

		/// <summary>
		/// Gets the text of the line that contains the error position.
		/// </summary>
		public string LineText
		{
			get
			{
				if (_lineText != null)
					return _lineText;
				if (_lineStart == -1)
					Calculate();

				return _lineText = Input.Substring(_lineStart, _lineLength);
			}
		}

		/// <summary>
		/// Gets the text of the line that contains the error position with additional visual cursor position information.
		/// </summary>
		public string FormattedLineText
		{
			get
			{
				if (_formattedLineText != null)
					return _formattedLineText;
				if (_lineStart == -1)
					Calculate();

				string lineAndColumn = $"line {_line}, column {_column}";

				string pointerLine;
				if (_visualColumn <= lineAndColumn.Length + 2)
					pointerLine = new string(' ', _visualColumn - 1) + '^' + ' ' + lineAndColumn;
				else
					pointerLine = new string(' ', _visualColumn - 2 - lineAndColumn.Length) + lineAndColumn + ' ' + '^';

				return _formattedLineText = $"{LineText}\n{pointerLine}";
			}
		}

		/// <summary>
		/// Gets a value indicating whether this error group is relevant. An error group is considered relevant if it occurred at a position where no successful parsing has been done yet.
		/// </summary>
		public bool IsRelevant => _isRelevant ??= !Context.successPositions[Position];

		/// <summary>
		/// Gets the number of barriers that were successfully parsed before encountering this error group.
		/// </summary>
		public int PassedBarriers => _passedBarriers ??= Errors.Max(e => e.passedBarriers);

		/// <summary>
		/// Gets the starting index of the line that contains the error position.
		/// </summary>
		public int LineStart
		{
			get
			{
				if (_lineStart == -1)
					Calculate();
				return _lineStart;
			}
		}

		/// <summary>
		/// Gets the length of the line that contains the error position.
		/// </summary>
		public int LineLength
		{
			get
			{
				if (_lineStart == -1)
					Calculate();
				return _lineLength;
			}
		}

		/// <summary>
		/// Gets the 1-based line index of the error position in the input text.
		/// </summary>
		public int Line
		{
			get
			{
				if (_lineStart == -1)
					Calculate();
				return _line;
			}
		}

		/// <summary>
		/// Gets the 1-based column index of the error position in the input text.
		/// </summary>
		public int Column
		{
			get
			{
				if (_lineStart == -1)
					Calculate();
				return _column;
			}
		}

		/// <summary>
		/// Gets the 1-based visual column index (counts tabs as 4 spaces instead of 1 space) of the error position in the input text.
		/// </summary>
		public int VisualColumn
		{
			get
			{
				if (_lineStart == -1)
					Calculate();
				return _visualColumn;
			}
		}

		private ExpectedElementsCollection? _expected = null;
		/// <summary>
		/// Gets the collection of expected elements that were failed during parsing.
		/// </summary>
		public ExpectedElementsCollection Expected
		{
			get
			{
				if (_expected != null)
					return _expected;

				return _expected = new ExpectedElementsCollection(Errors.Select(e =>
				{
					if (e.elementId == -1)
						return (null, null, null);
					if (e.isToken)
						return (Parser.TokenPatterns[e.elementId], e.message, e.stackFrame != null ?
							new ParserStackTrace(Context, e.stackFrame) : null);
					return ((ParserElement)Parser.Rules[e.elementId], e.message, e.stackFrame != null ?
						new ParserStackTrace(Context, e.stackFrame) : null);
				}).Where(p => p.Item1 != null).Distinct());
			}
		}

		private bool _unexpectedBarrierCalculated;
		private UnexpectedBarrierToken? _unexpectedBarrier = null;
		/// <summary>
		/// Gets the unexpected barrier token that was encountered during parsing.
		/// </summary>
		public UnexpectedBarrierToken? UnexpectedBarrier
		{
			get
			{
				if (!_unexpectedBarrierCalculated)
				{
					if (Context.barrierTokens.TryGetBarrierToken(Position, PassedBarriers, out var barrierToken))
						_unexpectedBarrier = new UnexpectedBarrierToken(Context, barrierToken);

					_unexpectedBarrierCalculated = true;
				}
				return _unexpectedBarrier;
			}
		}

		private IReadOnlyList<string>? _errorMessages = null;
		/// <summary>
		/// Gets the list of error messages that describes the errors that occurred during parsing.
		/// </summary>
		public IReadOnlyList<string> ErrorMessages
		{
			get
			{
				if (_errorMessages != null)
					return _errorMessages;
				return _errorMessages = Errors.Select(e => e.message).Where(p => p != null).Distinct().ToArray().AsReadOnlyList();
			}
		}

		internal ErrorGroup(ParserContext context, int position, IReadOnlyList<ParsingError> errors)
		{
			Context = context;
			Position = position;
			Errors = errors;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorGroup"/> class.
		/// </summary>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="position">The position in the input text where the error occurred.</param>
		/// <param name="errors">The list of parsing errors that occurred during parsing.</param>
		/// <exception cref="ArgumentException">Thrown if no errors are provided or if they do not match the specified position.</exception>
		public ErrorGroup(ParserContext context, int position, IEnumerable<ParsingError> errors)
		{
			Context = context;
			Position = position;
			Errors = errors.ToArray().AsReadOnlyList();

			if (Errors.Count == 0)
				throw new ArgumentException("No errors provided.", nameof(errors));

			if (!Errors.All(e => e.position == Position))
				throw new ArgumentException("Error positions do not match.", nameof(errors));
		}

		/// <summary>
		/// Returns a string representation of the error group using the default formatting flags.
		/// </summary>
		/// <returns>A string representation of the error group.</returns>
		public override string ToString()
		{
			return ToString(ErrorFormattingFlags.Default);
		}

		/// <summary>
		/// Returns a string representation of the error group using the specified formatting flags.
		/// </summary>
		/// <param name="flags">The formatting flags to use when generating the string representation.</param>
		/// <returns>A string representation of the error group.</returns>
		public string ToString(ErrorFormattingFlags flags)
		{
			var sb = new StringBuilder();

			if (Expected.Count == 0 ||
				flags.HasFlag(ErrorFormattingFlags.DisplayMessages))
			{
				if (ErrorMessages.Count > 0)
					sb.AppendLine(string.Join(" / ", ErrorMessages)).AppendLine();
			}

			sb.AppendLine("The line where the error occurred:");
			sb.AppendLine(FormattedLineText);

			if (Expected.Count > 0)
			{
				sb.AppendLine();
				var expected = flags.HasFlag(ErrorFormattingFlags.DisplayRules) ?
					Expected.Select(e => e.ToString()).ToList() : Expected.Tokens.Select(e => e.ToString()).ToList();
				var unexpected = $"'{GetCharacterDisplay(Input, Position, Context.maxPosition)}' is unexpected character";

				if (UnexpectedBarrier != null)
				{
					unexpected = $"'{UnexpectedBarrier.Alias}' " +
						$"is unexpected barrier token or '{GetCharacterDisplay(Context.input, Position, Context.maxPosition)}' " +
						$"is unexpected character";
				}

				string oneOf = expected.Count > 1 ? " one of" : "";

				if (expected.Count > 1 || expected.Sum(e => e.Length) > 40)
					sb.AppendLine($"{unexpected}, expected{oneOf}:\n" + string.Join("\n", expected).Indent("  "));
				else
					sb.AppendLine($"{unexpected}, expected{oneOf} " + string.Join(", ", expected));
			}

			HashSet<int> recodedElements = new HashSet<int>();

			foreach (var expectedElement in Expected)
			{
				if (recodedElements.Contains(expectedElement.Id))
					continue;

				var stackTrace = expectedElement.StackTrace?.ToString(15, recodedElements);

				if (!string.IsNullOrEmpty(stackTrace))
				{
					sb.AppendLine();
					sb.AppendLine(stackTrace);
				}
			}

			sb.Length -= Environment.NewLine.Length;

			return sb.ToString();
		}

		private static string GetCharacterDisplay(string input, int position, int maxPosition)
		{
			if (position >= maxPosition)
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