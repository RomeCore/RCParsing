using System;

namespace RCParsing
{
	/// <summary>
	/// Represents the flags that can be used to optimize the parse tree.
	/// </summary>
	[Flags]
	public enum ParseTreeOptimization
	{
		/// <summary>
		/// No optimization flags are set.
		/// </summary>
		None = 0,

		/// <summary>
		/// The default optimization flags are set. These include:
		/// <list type="bullet">
		/// <item>IgnorePureLiterals</item>
		/// <item>RemoveEmptyOrWhitespaceNodes</item>
		/// <item>MergeSingleChildRules</item>
		/// <item>RecalculateSpans</item>
		/// </list>
		/// </summary>
		Default = RemoveEmptyOrWhitespaceNodes | MergeSingleChildRules | TrimSpans,

		/// <summary>
		/// Removes pure literals (char and string) in the parse tree. Does not affects literal choices.
		/// </summary>
		RemovePureLiterals = 1,

		/// <summary>
		/// Removes empty nodes from the parse tree.
		/// </summary>
		RemoveEmptyNodes = 2,

		/// <summary>
		/// Removes whitespace nodes from the parse tree.
		/// </summary>
		RemoveWhitespaceNodes = 4,

		/// <summary>
		/// Removes whitespace and empty nodes from the parse tree.
		/// </summary>
		RemoveEmptyOrWhitespaceNodes = RemoveEmptyNodes | RemoveWhitespaceNodes,

		/// <summary>
		/// Merges single child rules into their parent recursively.
		/// </summary>
		MergeSingleChildRules = 8,

		/// <summary>
		/// Recalculates the start index and length of each node in the parse tree to remove the leading and trailing whitespace from each node's span.
		/// </summary>
		TrimSpans = 16,
	}
}