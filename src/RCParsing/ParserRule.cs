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
		/// Advances the parser context to use for this and child elements.
		/// </summary>
		/// <remarks>
		/// For <paramref name="context"/> it updates the settings to use as local element. <br/>
		/// It makes a <paramref name="childContext"/> that have advanced recursion depth and settings for child elements.
		/// </remarks>
		public void AdvanceContext(ref ParserContext context, out ParserContext childContext)
		{
			childContext = context;

			if (Settings.isDefault)
			{
				childContext.settings = context.settings;
			}
			else
			{
				context.settings.Resolve(Settings, Parser.Settings, out var forLocal, out var forChildren);
				context.settings = forLocal;

				childContext.settings = forChildren;
			}

			childContext.recursionDepth++;
		}

		/// <summary>
		/// Tries to parse the input string using this rule.
		/// </summary>
		/// <param name="context">The local parser context to use for this element.</param>
		/// <param name="childContext">The parser context for the child elements.</param>
		/// <returns>The parsed rule containing the result of parsing.</returns>
		public abstract ParsedRule Parse(ParserContext context, ParserContext childContext);

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