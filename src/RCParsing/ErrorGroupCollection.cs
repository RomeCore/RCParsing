using System;
using System.Collections;
using System.Collections.Generic;
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
		/// Gets the relevant error groups.
		/// </summary>
		/// <remarks>
		/// Error group is considered relevant when it has the furthest position 
		/// before the end or any error recovery point.
		/// </remarks>
		public IReadOnlyList<ErrorGroup> RelevantGroups { get; }

		/// <summary>
		/// Gets the list of error groups that were created during parsing.
		/// Each group contains a set of errors that occurred at the same position.
		/// </summary>
		public IReadOnlyList<ErrorGroup> Groups { get; }

		/// <summary>
		/// Gets the error group that contains the latest position in the input text where an error occurred.
		/// If no errors were found, returns <see langword="null"/>.
		/// </summary>
		public ErrorGroup? Last => Groups.Count > 0 ? Groups[Groups.Count - 1] : null;

		private IReadOnlyList<ErrorGroup> _reversedRelevant;
		/// <summary>
		/// Gets the list of relevant error groups in reverse order, meaning the first group is the one with the
		/// latest position in the input text where an error occurred, and so on.
		/// Each group contains a set of errors that occurred at the same position, but in reverse order from the original list.
		/// </summary>
		/// <remarks>
		/// Error group is considered relevant when it has the furthest position 
		/// before the end or any error recovery point.
		/// </remarks>
		public IReadOnlyList<ErrorGroup> ReversedRelevant => _reversedRelevant ??= new ReversedList<ErrorGroup>(RelevantGroups);

		private IReadOnlyList<ErrorGroup> _reversed;
		/// <summary>
		/// Gets the list of error groups in reverse order, meaning the first group is the one with the
		/// latest position in the input text where an error occurred, and so on.
		/// Each group contains a set of errors that occurred at the same position, but in reverse order from the original list.
		/// </summary>
		public IReadOnlyList<ErrorGroup> Reversed => _reversed ??= new ReversedList<ErrorGroup>(Groups);

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorGroupCollection"/> class.
		/// </summary>
		/// <param name="context">The parser context that was used during parsing.</param>
		/// <param name="errors">The list of parsing errors that occurred during parsing.</param>
		/// <param name="errorRecoveryIndices">A list of indices pointing to <paramref name="errors"/> when error recovery was triggered.</param>
		/// <param name="excludeLastRelevantGroup">
		/// Whether to exclude last relevant error group.
		/// Should be <see langword="true"/> when parsing was successful and last error group is not relevant at all.
		/// </param>
		public ErrorGroupCollection(ParserContext context, IEnumerable<ParsingError> errors,
			IEnumerable<int>? errorRecoveryIndices = null, bool excludeLastRelevantGroup = false)
		{
			Context = context;
			Errors = errors?.ToArray().AsReadOnlyList() ?? throw new ArgumentNullException(nameof(errors));

			var (groups, relGroups) = MakeGroups(context, errors,
				errorRecoveryIndices ?? Array.Empty<int>(), excludeLastRelevantGroup);
			Groups = groups;
			RelevantGroups = relGroups;
		}

		public int Count => Groups.Count;
		public ErrorGroup this[int index] => Groups[index];
		public IEnumerator<ErrorGroup> GetEnumerator() => Groups.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private static (IReadOnlyList<ErrorGroup>, IReadOnlyList<ErrorGroup>) MakeGroups(ParserContext context,
			IEnumerable<ParsingError> errors, IEnumerable<int> errorIndices, bool excludeLastRelevantGroup)
		{
			Dictionary<int, List<ParsingError>> groups = new();
			HashSet<int> relevantGroups = new();
			var sortedRecovery = errorIndices.OrderBy(i => i)
				.Append(context.errors.Count).Distinct().ToList();
			int maxPosBeforeRecovery = -1;
			int maxPos = 0;
			int index = 0;
			int recoveryPointer = 0;

			foreach (var error in errors)
			{
				if (recoveryPointer < sortedRecovery.Count && index == sortedRecovery[recoveryPointer])
				{
					if (maxPosBeforeRecovery >= 0)
						relevantGroups.Add(maxPosBeforeRecovery);

					maxPosBeforeRecovery = -1;
					recoveryPointer++;
				}

				if (!groups.TryGetValue(error.position, out var groupErrors))
					groups[error.position] = groupErrors = new List<ParsingError>();

				if (error.position > maxPosBeforeRecovery)
					maxPosBeforeRecovery = error.position;
				if (error.position > maxPos)
					maxPos = error.position;

				groups[error.position].Add(error);

				index++;
			}

			var retGroups = groups
				.OrderBy(g => g.Key)
				.Select(g => new ErrorGroup(context, g.Key, g.Value,
					excludeLastRelevantGroup
						? relevantGroups.Contains(g.Key) && g.Key != maxPos
						: relevantGroups.Contains(g.Key)))
				.AsReadOnlyCollection();

			return (retGroups, retGroups.Where(g => g.IsRelevant).AsReadOnlyCollection());
		}

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