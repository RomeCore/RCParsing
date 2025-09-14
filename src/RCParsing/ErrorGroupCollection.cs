using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents a collection of error groups that was occurred during parsing.
	/// </summary>
	public class ErrorGroupCollection : IReadOnlyList<ErrorGroup>
	{
		/// <summary>
		/// Gets the parser context that was used during parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets the parser were used for parsing.
		/// </summary>
		public Parser Parser => Context.parser;

		/// <summary>
		/// Gets the input text that was parsed with errors.
		/// </summary>
		public string Input => Context.input;

		/// <summary>
		/// Gets the list of parsing errors that occurred during parsing.
		/// </summary>
		public IReadOnlyList<ParsingError> Errors { get; }

		/// <summary>
		/// Gets the list of error groups that were created during parsing.
		/// Each group contains a set of errors that occurred at the same position.
		/// </summary>
		public IReadOnlyList<ErrorGroup> Groups { get; }

		private IReadOnlyList<ErrorGroup>? _relevantGroups;
		/// <summary>
		/// Gets the list of error groups that are relevant to the current parsing context.
		/// These are the groups that contain errors at positions that have not been successfully parsed yet.
		/// </summary>
		public IReadOnlyList<ErrorGroup> RelevantGroups => _relevantGroups ??= Groups.Where(g => g.IsRelevant).ToImmutableList();

		/// <summary>
		/// Gets the error group that contains the latest position in the input text where an error occurred.
		/// If no errors were found, returns <see langword="null"/>.
		/// </summary>
		public ErrorGroup? Last => Groups.Count > 0 ? Groups[Groups.Count - 1] : null;

		/// <summary>
		/// Gets the relevant error group that contains the latest position in the input text where an error occurred.
		/// If no errors were found, returns <see langword="null"/>.
		/// </summary>
		public ErrorGroup? LastRelevant => RelevantGroups.Count > 0 ? RelevantGroups[RelevantGroups.Count - 1] : null;

		private IReadOnlyList<ErrorGroup> _reversed;
		/// <summary>
		/// Gets the list of error groups in reverse order, meaning the first group is the one with the
		/// latest position in the input text where an error occurred, and so on.
		/// Each group contains a set of errors that occurred at the same position, but in reverse order from the original list.
		/// </summary>
		public IReadOnlyList<ErrorGroup> Reversed => _reversed ??= new ReversedList<ErrorGroup>(Groups);

		private IReadOnlyList<ErrorGroup> _relevantReversed;
		/// <summary>
		/// Gets the list of relevant error groups in reverse order, meaning the first group is the one with the
		/// latest position in the input text where an error occurred, and so on.
		/// Each group contains a set of errors that occurred at the same position, but in reverse order from the original list.
		/// These are the groups that contain errors at positions that have not been successfully parsed yet.
		/// </summary>
		public IReadOnlyList<ErrorGroup> RelevantReversed => _relevantReversed ??= new ReversedList<ErrorGroup>(RelevantGroups);

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorGroupCollection"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		/// <param name="errors">The list of parsing errors that occurred during parsing.</param>
		public ErrorGroupCollection(ParserContext context, IEnumerable<ParsingError> errors)
		{
			Context = context;
			Errors = errors?.ToImmutableList() ?? throw new ArgumentNullException(nameof(errors));

			Groups = errors.GroupBy(v => v.position).OrderBy(v => v.Key).Select(v =>
			{
				return new ErrorGroup(context, v.Key, v.ToImmutableList());
			}).ToImmutableList();
		}

		public int Count => Groups.Count;
		public ErrorGroup this[int index] => Groups[index];
		public IEnumerator<ErrorGroup> GetEnumerator() => Groups.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Returns a string representation of the error group collection using the default formatting flags.
		/// </summary>
		/// <returns>A string representation of the error group collection.</returns>
		public override string ToString()
		{
			return ToString(ErrorFormattingFlags.Default);
		}

		/// <summary>
		/// Returns a string representation of the error group collection using the specified formatting flags.
		/// </summary>
		/// <param name="flags">The formatting flags to use when generating the string representation.</param>
		/// <returns>A string representation of the error group collection.</returns>
		public string ToString(ErrorFormattingFlags flags)
		{
			return ToString(flags, flags.HasFlag(ErrorFormattingFlags.MoreGroups) ? 5 : 1);
		}

		/// <summary>
		/// Returns a string representation of the error group collection using the specified formatting flags and maximum number of groups to display.
		/// </summary>
		/// <param name="flags">The formatting flags to use when generating the string representation.</param>
		/// <param name="maxGroups">The maximum number of groups to display in the string representation.</param>
		/// <returns>A string representation of the error group collection.</returns>
		public string ToString(ErrorFormattingFlags flags, int maxGroups)
		{
			if (Groups.Count == 0)
				return string.Empty;

			var sb = new StringBuilder();

			maxGroups = Math.Min(maxGroups, Groups.Count);
			for (int i = 0; i < maxGroups; i++)
			{
				var group = Reversed[i];

				sb.AppendLine(group.ToString(flags));

				if (i < maxGroups - 1)
					sb.AppendLine().AppendLine().Append("===== NEXT ERROR =====").AppendLine().AppendLine();
			}

			if (maxGroups < Groups.Count)
				sb.AppendLine().Append("... and more errors omitted");
			else
				sb.Length -= Environment.NewLine.Length;

			return sb.ToString();
		}
	}
}