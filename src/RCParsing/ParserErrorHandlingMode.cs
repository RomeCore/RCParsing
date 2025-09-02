namespace RCParsing
{
	/// <summary>
	/// Defines how parser elements should handle errors.
	/// </summary>
	public enum ParserErrorHandlingMode
	{
		/// <summary>
		/// Records errors into parsing context. The default mode.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Ignores any errors when parser elements trying to record them.
		/// </summary>
		NoRecord = 1,

		/// <summary>
		/// Throws any errors when parser elements trying to record them.
		/// </summary>
		Throw = 2
	}
}