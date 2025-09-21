using System;
using System.Collections;
using System.Collections.Generic;
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
		public ParserStackTrace(ParserContext context, IntermediateParserStackFrame topFrame)
		{
			Context = context;
			TopFrame = new(this, topFrame ?? throw new ArgumentNullException(nameof(topFrame)));

			var frames = new List<ParserStackFrame>();
			var _topFrame = TopFrame;
			while (_topFrame != null)
			{
				frames.Add(_topFrame);
				_topFrame = _topFrame.Previous;
			}
			Frames = frames.AsReadOnlyList();
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
				sb.Append($"[{Context.parser.Rules[topFrame.RuleId].ToString(0)}] ");
				sb.AppendLine("Stack trace (top call recently):");
				elementsToRecord?.Add(topFrame.RuleId);
				prevStackRule = topFrame.RuleId;
				topFrame = topFrame.Previous;

				while (topFrame != null && maxFrames-- >= 0)
				{
					elementsToRecord?.Add(topFrame.RuleId);
					sb.AppendLine("- " + topFrame.Rule.ToStackTraceString(1, prevStackRule)
						.Indent("  ", addIndentToFirstLine: false));
					prevStackRule = topFrame.RuleId;
					topFrame = topFrame.Previous;
				}

				if (topFrame != null)
					sb.AppendLine("Stack trace truncated...");

				return sb.ToString();
			}

			return string.Empty;
		}
	}
}