using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents flags that can be used to configure the behavior of a parser during initialization.
	/// </summary>
	[Flags]
	public enum ParserInitFlags
	{
		/// <summary>
		/// No special configuration. Default behavior.
		/// </summary>
		None = 0,

		/// <summary>
		/// Enables early checking based on the character at the current input position.
		/// </summary>
		/// <remarks>
		/// Can improve performance by avoiding unnecessary backtracking in some cases. However, it can also reduce helpful errors.
		/// </remarks>
		FirstCharacterMatch = 1 << 0,

		/// <summary>
		/// Enables inlining of rules during parser initialization to reduce abstraction levels if possible.
		/// </summary>
		/// <remarks>
		/// This can bypass the settings inheritance and caching mechanism. Use with caution as it may lead to unexpected behavior if not used properly.
		/// </remarks>
		InlineRules = 1 << 1,

		/// <summary>
		/// Enables memoization for caching results of sub-rules during parsing to avoid redundant computations.
		/// </summary>
		EnableMemoization = 1 << 2,

		/// <summary>
		/// Enables writing stack traces for debugging purposes during parsing. This can be useful to understand the flow of execution and identify potential issues.
		/// </summary>
		StackTraceWriting = 1 << 3,
	}
}