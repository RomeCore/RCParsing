using System;

namespace RCParsing
{
	/// <summary>
	/// Represents flags that control how parsing errors are formatted.
	/// </summary>
	[Flags]
	public enum ErrorFormattingFlags
	{
		/// <summary>
		/// Displays the brief error messages without any additional debugging information.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Displays what rules are expected when formatting errors for exceptions.
		/// </summary>
		DisplayRules = 1,

		/// <summary>
		/// Displays the hidden error messages when formatting errors for exceptions.
		/// </summary>
		DisplayMessages = 2,

		/// <summary>
		/// Displays more groups of errors (instead of a single group) when formatting errors for exceptions.
		/// </summary>
		MoreGroups = 4,
	}
}