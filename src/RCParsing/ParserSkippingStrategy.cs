namespace RCParsing
{
	/// <summary>
	/// Defines how parser should handle skipping of rules.
	/// </summary>
	public enum ParserSkippingStrategy
	{
		/// <summary>
		/// Parser will always ignore the skip-rule and try to parse the target rule. Default behavior.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Parser will try to skip the skip-rule once before parsing the target rule.
		/// </summary>
		SkipBeforeParsing,

		/// <summary>
		/// Parser will repeatedly attempt to skip the skip-rule in an interleaved manner with the target rule,
		/// until the target rule succeeds or both skip and target rules fail. This allows incremental consumption of skip content.
		/// </summary>
		SkipBeforeParsingLazy,

		/// <summary>
		/// Parser will greedily skip the skip-rule as many times as possible before attempting to parse the target rule.
		/// </summary>
		SkipBeforeParsingGreedy,

		/// <summary>
		/// Parser will first attempt to parse the target rule; if parsing fails, it will skip the skip-rule once
		/// and then retry parsing the target rule.
		/// </summary>
		/// <remarks>
		/// Works slower sometimes but allows to use rules that conflict with skip-rules.
		/// </remarks>
		TryParseThenSkip,

		/// <summary>
		/// Parser will attempt to parse the target rule; if parsing fails, it will alternately try to skip the skip-rule
		/// and parse the target rule repeatedly until the target rule succeeds or both fail.
		/// </summary>
		TryParseThenSkipLazy,

		/// <summary>
		/// Parser will attempt to parse the target rule; if parsing fails, it will greedily skip the skip-rule as many times
		/// as possible and then retry parsing the target rule.
		/// </summary>
		TryParseThenSkipGreedy
	}
}