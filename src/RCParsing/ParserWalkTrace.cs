using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a trace of parsing steps taken by the parser.
	/// </summary>
	public class ParserWalkTrace : IReadOnlyList<ParsingStep>
	{
		private readonly SharedParserContext _sharedContext;
		private readonly List<ParsingStep> _steps = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserWalkTrace"/> class.
		/// </summary>
		/// <param name="context">The parser context that is performing the parsing.</param>
		public ParserWalkTrace(SharedParserContext context)
		{
			_sharedContext = context;
		}

		/// <summary>
		/// Adds a parsing step to the trace.
		/// </summary>
		/// <param name="step">The parsing step to record.</param>
		public void Record(ParsingStep step)
		{
			_steps.Add(step);
		}

		/// <summary>
		/// Renders the trace as a formatted string.
		/// </summary>
		/// <param name="maxCount">The maximum number of steps to include in the output. Default is -1 (all steps).</param>
		/// <returns></returns>
		public string Render(int maxCount = -1)
		{
			if (_steps.Count == 0 || maxCount == 0)
				return string.Empty;

			int startStepIndex = maxCount < 0 ? 0 : Math.Max(_steps.Count - maxCount, 0);

			var sb = new StringBuilder();

			if (startStepIndex > 0)
				sb.AppendLine($"... {startStepIndex} hidden parsing steps. Total: {_steps.Count} ...");

			for (int i = startStepIndex; i < _steps.Count; i++)
				sb.AppendLine(RenderStep(i));

			sb.AppendLine().Append("... End of walk trace ...");

			return sb.ToString();
		}

		/// <summary>
		/// Renders a single parsing step as a formatted string.
		/// </summary>
		/// <param name="index">The index of the parsing step to render.</param>
		/// <returns></returns>
		public string RenderStep(int index)
		{
			var step = _steps[index];
			var rule = _sharedContext.parser.GetRule(step.ruleId);

			var type = step.type switch
			{
				ParsingStepType.Enter =>	"[ENTER]",
				ParsingStepType.Info =>		"[INFO]",
				ParsingStepType.Success =>	"[SUCCESS]",
				ParsingStepType.Fail =>		"[FAIL]",
				_ =>						"[UNKNOWN]",
			};

			string rulePrev = rule.Alias is string alias ? $"'{alias}'" : rule.ToStringOverride(0);

			string positionInfo = $"pos:{step.startIndex} ".PadRight(10);

			string textPreview = string.Empty;
			if (step.length > 0)
				textPreview = $" matched: '{GetTextPreview(step.startIndex, step.length)}' [{step.length} chars]";
			else if (step.type == ParsingStepType.Fail)
				textPreview = $" failed to match: '{GetTextPreview(step.startIndex, 15)}...'";

				string message = string.Empty;
			if (!string.IsNullOrEmpty(step.message))
				message = " " + step.message;

			return type.PadRight(10) + positionInfo + rulePrev + textPreview + message;
		}

		private string GetTextPreview(int start, int length)
		{
			if (length == 0)
				return string.Empty;

			int maxPrefix = 15, maxSuffix = 10, maxPreview = maxPrefix + maxSuffix + 5;
			var input = _sharedContext.input;

			var text = start + length <= input.Length
				? input.Substring(start, Math.Min(length, input.Length - start))
				: input.Substring(start, input.Length - start);

			if (text.Length > maxPreview)
				text = text.Substring(0, maxPrefix) + " ..... " + text.Substring(text.Length - maxSuffix);

			// Escape control characters for better display
			text = text.Replace("\r", "\\r")
						.Replace("\n", "\\n")
						.Replace("\t", "\\t");

			return text;
		}



		public int Count => _steps.Count;
		public ParsingStep this[int index] => _steps[index];
		public IEnumerator<ParsingStep> GetEnumerator() => _steps.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _steps.GetEnumerator();
	}
}