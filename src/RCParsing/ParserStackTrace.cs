using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents a stack trace for the parser.
	/// </summary>
	public class ParserStackTrace : IReadOnlyList<ParserStackFrame>
	{
		/// <summary>
		/// Gets the parser context used for parsing.
		/// </summary>
		public ParserContext Context { get; }

		/// <summary>
		/// Gets the top stack frame in the stack.
		/// </summary>
		public ParserStackFrame TopFrame { get; }

		/// <summary>
		/// Gets the list of stack frames in the stack. Top frames goes first.
		/// </summary>
		public IReadOnlyList<ParserStackFrame> Frames { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserStackTrace"/> class.
		/// </summary>
		/// <param name="context">The parser context used for parsing.</param>
		/// <param name="topFrame">The top stack frame in the stack.</param>
		public ParserStackTrace(ParserContext context, ParserStackFrame topFrame)
		{
			Context = context;
			TopFrame = topFrame ?? throw new ArgumentNullException(nameof(topFrame));

			var frames = ImmutableList.CreateBuilder<ParserStackFrame>();
			while (topFrame != null)
			{
				frames.Add(topFrame);
				topFrame = topFrame.previous;
			}
			Frames = frames.ToImmutable();
		}

		public int Count => Frames.Count;
		public ParserStackFrame this[int index] => Frames[index];
		public IEnumerator<ParserStackFrame> GetEnumerator() => Frames.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// Returns a string that represents the current stack trace.
		/// </summary>
		/// <returns>A string that represents the current stack trace.</returns>
		public override string ToString()
		{
			return ToString(15, null);
		}

		/// <summary>
		/// Returns a string that represents the current stack trace.
		/// </summary>
		/// <returns>A string that represents the current stack trace.</returns>
		public string ToString(int maxFrames, HashSet<int>? elementsToRecord = null)
		{
			var topFrame = TopFrame;
			int prevStackRule;

			if (topFrame != null)
			{
				var sb = new StringBuilder();
				sb.Append($"[{Context.parser.Rules[topFrame.ruleId].ToString(0)}] ");
				sb.AppendLine("Stack trace (top call recently):");
				elementsToRecord?.Add(topFrame.ruleId);
				prevStackRule = topFrame.ruleId;
				topFrame = topFrame.previous;

				while (topFrame != null && maxFrames-- >= 0)
				{
					elementsToRecord?.Add(topFrame.ruleId);
					var rule = Context.parser.Rules[topFrame.ruleId];
					sb.AppendLine("- " + rule.ToStackTraceString(1, prevStackRule)
						.Indent("  ", addIndentToFirstLine: false));
					prevStackRule = topFrame.ruleId;
					topFrame = topFrame.previous;
				}

				if (topFrame != null)
					sb.AppendLine("Stack trace truncated...");

				return sb.ToString();
			}

			return string.Empty;
		}
	}
}