namespace RCParsing
{
	/// <summary>
	/// Represents a strategy for handling errors during parsing a specific rule.
	/// </summary>
	public enum ErrorRecoveryStrategy
	{
		/// <summary>
		/// No error recovery strategy is used. Parsing will stop at the first error encountered.
		/// </summary>
		None = 0,

		/// <summary>
		/// Tries to parse again and again until it succeeds or until it reaches the end of input.
		/// </summary>
		FindNext,

		/// <summary>
		/// Skips text until the next anchor is encountered. The anchor is defined by a token pattern or another rule.
		/// </summary>
		/// <remarks>
		/// Stops at the start index of first occurrence of an anchor, if any, then tries to parse again. If no anchor is found, parsing will stop at the first error encountered.
		/// </remarks>
		SkipUntilAnchor,

		/// <summary>
		/// Skips text after the next anchor is encountered. The anchor is defined by a token pattern or another rule.
		/// </summary>
		/// <remarks>
		/// Stops at the end index of first occurrence of an anchor, if any, then tries to parse again. If no anchor is found, parsing will stop at the first error encountered.
		/// </remarks>
		SkipAfterAnchor,
	}
}