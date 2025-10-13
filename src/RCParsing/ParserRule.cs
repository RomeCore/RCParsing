using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace RCParsing
{
	/// <summary>
	/// The delegate used for inner parser rules optimizations.
	/// </summary>
	/// <param name="ctx">The context used for parsing.</param>
	/// <param name="settings">The settings to apply for current element.</param>
	/// <param name="childSettings">The settings to propagate for children elements.</param>
	/// <returns>The parsed rule result.</returns>
	public delegate ParsedRule ParseDelegate(ref ParserContext ctx, ref ParserSettings settings, ref ParserSettings childSettings);



	/// <summary>
	/// Represents a parser rule that can be used to parse input strings.
	/// </summary>
	public abstract class ParserRule : ParserElement
	{
		/// <summary>
		/// Gets the parsed value factory associated with this rule.
		/// </summary>
		public Func<ParsedRuleResultBase, object?>? ParsedValueFactory { get; internal set; } = null;

		/// <summary>
		/// Gets the local settings for this parser rule with each setting configurable by override modes.
		/// </summary>
		public ParserLocalSettings Settings { get; internal set; } = default;

		/// <summary>
		/// Gets the error recovery strategy associated with this rule.
		/// </summary>
		public ErrorRecovery ErrorRecovery { get; internal set; } = default;

		/// <summary>
		/// Gets a value indicating whether this rule can be used directly without using parser.
		/// </summary>
		public virtual bool CanBeInlined => Settings.isDefault &&
			ErrorRecovery.strategy == ErrorRecoveryStrategy.None;

		/// <summary>
		/// Gets a value indicating whether this rule can recover from errors.
		/// </summary>
		public bool CanRecover { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this rule writes stack traces to the current context for debugging purposes.
		/// </summary>
		public bool WritesStackTrace { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this rule records the walk trace to the context.
		/// </summary>
		public bool RecordsWalkTrace { get; private set; }



		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);

			if (initFlags.HasFlag(ParserInitFlags.StackTraceWriting))
				WritesStackTrace = true;
			if (initFlags.HasFlag(ParserInitFlags.WalkTraceRecording))
				RecordsWalkTrace = true;
		}

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			CanRecover = ErrorRecovery.strategy != ErrorRecoveryStrategy.None;
		}

		/// <summary>
		/// Wraps parse function to implement memoization and other things.
		/// </summary>
		/// <param name="toWrap">The function to wrap.</param>
		/// <param name="flags">The parser initialization flags.</param>
		/// <returns>The wrapped or original function.</returns>
		protected ParseDelegate WrapParseFunction(ParseDelegate toWrap, ParserInitFlags flags)
		{
			if (flags.HasFlag(ParserInitFlags.EnableMemoization))
			{
				var prev = toWrap;
				ParsedRule ParseMemoized(ref ParserContext ctx, ref ParserSettings stng, ref ParserSettings chStng)
				{
					if (ctx.cache.TryGetRule(Id, ctx.position, ctx.passedBarriers, out var cachedResult))
						return cachedResult;
					/*
					if (!ctx.cache.TryBeginRule(Id, ctx.position))
						return ParsedRule.Fail;
					*/
					int position = ctx.position;
					cachedResult = prev(ref ctx, ref stng, ref chStng);
					ctx.cache.AddRule(Id, position, ctx.passedBarriers, cachedResult);
					return cachedResult;
				}
				toWrap = ParseMemoized;
			}
			
			if (flags.HasFlag(ParserInitFlags.WalkTraceRecording))
			{
				var prev = toWrap;
				ParsedRule ParseWithWalkTrace(ref ParserContext ctx, ref ParserSettings stng, ref ParserSettings chStng)
				{
					var position = ctx.position;

					ctx.walkTrace.Record(new ParsingStep
					{
						type = ParsingStepType.Enter,
						startIndex = position,
						length = 0,
						ruleId = Id
					});

					var parsedRule = prev(ref ctx, ref stng, ref chStng);

					if (parsedRule.success)
					{
						ctx.walkTrace.Record(new ParsingStep
						{
							type = ParsingStepType.Success,
							startIndex = parsedRule.startIndex,
							length = parsedRule.length,
							ruleId = Id
						});
					}
					else
					{
						ctx.walkTrace.Record(new ParsingStep
						{
							type = ParsingStepType.Fail,
							startIndex = position,
							length = 0,
							ruleId = Id
						});
					}

					return parsedRule;
				}
				toWrap = ParseWithWalkTrace;
			}

			return toWrap;
		}

		/// <summary>
		/// Advances the parser context to use for this and child elements.
		/// </summary>
		public void AdvanceContext(ref ParserContext context, ref ParserSettings settings, out ParserSettings childSettings)
		{
			if (WritesStackTrace)
				context.AppendStackFrame(Id, context.position);

			if (Settings.isDefault)
			{
				childSettings = settings;
			}
			else
			{
				settings.Resolve(Settings, Parser.GlobalSettings, out var forLocal, out var forChildren);
				settings = forLocal;
				childSettings = forChildren;
			}
		}

		/// <summary>
		/// Tries to parse the input string using this rule.
		/// </summary>
		/// <param name="context">The local parser context to use for this element.</param>
		/// <param name="settings">The settings to use for this element.</param>
		/// <param name="childSettings">The settings to use for child elements.</param>
		/// <returns>The parsed rule containing the result of parsing.</returns>
		public abstract ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings);

		/// <inheritdoc cref="ParseIncrementally(ParserContext, ParserSettings, ParserSettings, ParsedRule, TextChange, int)"/>
		internal ParsedRule ParseIncrementallyInternal(ParserContext context, ParserSettings settings,
			ParserSettings childSettings, ParsedRule node, TextChange change, int newVersion)
		{
			return ParseIncrementally(context, settings, childSettings, node, change, newVersion);
		}

		/// <summary>
		/// Parses the AST node incrementally (reparses only the changed parts of text).
		/// </summary>
		/// <remarks>
		/// Called internally in the <see cref="Parser"/> inside an one of <see cref="ParsedRuleResultBase.Reparsed(ParserContext)"/> overloads. <br/>
		/// Called when text change captures more than one child nodes or more than zero partial child nodes. <br/>
		/// Default implementation just returns <see cref="ParsedRule.Fail"/>, meaning that no incremental algorithm is specified.
		/// </remarks>
		/// <param name="context">The new context used for reparse.</param>
		/// <param name="settings">The settings used for this element.</param>
		/// <param name="childSettings">The settings that should be used for child element.</param>
		/// <param name="node">The AST node to reparse.</param>
		/// <param name="change">The text change struture containing ranges of text changes.</param>
		/// <param name="newVersion">The new version that new changed nodes should be assigned.</param>
		/// <returns>The reparsed AST node or <see cref="ParsedRule.Fail"/> to mark node to be entirely reparsed.</returns>
		protected virtual ParsedRule ParseIncrementally(ParserContext context, ParserSettings settings,
			ParserSettings childSettings, ParsedRule node, TextChange change, int newVersion)
		{
			return ParsedRule.Fail;
		}

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
				   Settings == other.Settings &&
				   ErrorRecovery == other.ErrorRecovery;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + ParsedValueFactory?.GetHashCode() ?? 0;
			hashCode = hashCode * 397 + Settings.GetHashCode();
			hashCode = hashCode * 397 + ErrorRecovery.GetHashCode();
			return hashCode;
		}
	}
}