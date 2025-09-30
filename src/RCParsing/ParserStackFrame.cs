namespace RCParsing
{
	/// <summary>
	/// The stack frame in the parser stack trace used for inspecting.
	/// </summary>
	public class ParserStackFrame
	{
		/// <summary>
		/// Gets the parent stack trace.
		/// </summary>
		public ParserStackTrace Trace { get; }

		/// <summary>
		/// Gets the intermediate frame that was created when parsing.
		/// </summary>
		public IntermediateParserStackFrame Frame { get; }

		private bool _previousCalculated;
		private ParserStackFrame? _previous;
		/// <summary>
		/// Gets the previous stack frame, if any.
		/// </summary>
		public ParserStackFrame? Previous
		{
			get
			{
				if (_previousCalculated)
					return _previous;
				_previousCalculated = true;
				return _previous = Frame.previous == null ? null :
					new ParserStackFrame(Trace, Frame.previous);
			}
		}

		/// <summary>
		/// Gets the recursion depth of this stack frame.
		/// </summary>
		public int RecursionDepth => Frame.recursionDepth;
		
		/// <summary>
		/// Gets the rule ID that associated with this stack frame.
		/// </summary>
		public int RuleId => Frame.ruleId;

		/// <summary>
		/// Gets the position where parsing of rule has started.
		/// </summary>
		public int Position => Frame.position;

		private ParserRule? _rule;
		/// <summary>
		/// Gets the rule that associated with this stack frame.
		/// </summary>
		public ParserRule Rule => _rule ??= Trace.Context.parser.GetRule(Frame.ruleId);

		/// <summary>
		/// Initializes a new instance of <see cref="ParserStackFrame"/>.
		/// </summary>
		/// <param name="trace">The parent stack trace.</param>
		/// <param name="frame">The intermediate frame that was created when parsing.</param>
		public ParserStackFrame(ParserStackTrace trace, IntermediateParserStackFrame frame)
		{
			Trace = trace;
			Frame = frame;
		}
	}
}