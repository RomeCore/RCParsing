using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a parser rule that can be used to parse input strings.
	/// </summary>
	public abstract class ParserRule : ParserElement
	{
		/// <summary>
		/// Gets the parsed value factory associated with this rule.
		/// </summary>
		public Func<ParsedRuleResult, object?>? ParsedValueFactory { get; internal set; } = null;

		/// <summary>
		/// Gets the local settings for this parser rule with each setting configurable by override modes.
		/// </summary>
		public ParserLocalSettings Settings { get; internal set; } = default;

		/// <summary>
		/// Gets a value indicating whether this rule can be used directly without using parser.
		/// </summary>
		public virtual bool CanBeInlined => Settings.isDefault;




		private bool _writeStackTrace = false;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			if (initFlags.HasFlag(ParserInitFlags.StackTraceWriting))
				_writeStackTrace = true;
		}

		/// <summary>
		/// Advances the parser context to use for this and child elements.
		/// </summary>
		public void AdvanceContext(ref ParserContext context, out ParserContext childContext)
		{
			if (_writeStackTrace)
				context.AppendStackFrame(Id);
			childContext = context;

			if (!Settings.isDefault)
			{
				context.settings.Resolve(Settings, Parser.GlobalSettings, out var forLocal, out var forChildren);
				context.settings = forLocal;

				childContext.settings = forChildren;
			}
		}

		/// <summary>
		/// Tries to parse the input string using this rule.
		/// </summary>
		/// <param name="context">The local parser context to use for this element.</param>
		/// <param name="childContext">The parser context for the child elements.</param>
		/// <returns>The parsed rule containing the result of parsing.</returns>
		public abstract ParsedRule Parse(ParserContext context, ParserContext childContext);

		/// <summary>
		/// Converts this parser rule to a stack trace string for debugging purposes.
		/// </summary>
		/// <param name="remainingDepth">The remaining depth of the stack trace to include. Default is 2.</param>
		/// <param name="childIndex">The index of the child element to include in the stack trace. Default is -1.</param>
		/// <returns>A stack trace string representing the rule with '&lt;-- here' pointers.</returns>
		public abstract string ToStackTraceString(int remainingDepth, int childIndex);

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is ParserRule other &&
				   Equals(ParsedValueFactory, other.ParsedValueFactory) &&
				   Settings == other.Settings;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode *= (ParsedValueFactory?.GetHashCode() ?? 0) * 23 + 397;
			hashCode *= Settings.GetHashCode() * 31 + 397;
			return hashCode;
		}
	}
}