using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents the main settings used by the parser itself, not related to any specific rule or token pattern.
	/// </summary>
	public struct ParserMainSettings
	{
		/// <summary>
		/// The error formatting flags that control how parsing errors are formatted when throwing exceptions.
		/// </summary>
		public ErrorFormattingFlags errorFormattingFlags;

		/// <summary>
		/// The AST factory that is used by parser to produce usable AST.
		/// </summary>
		public Func<ParserContext, ParsedRule, ParsedRuleResultBase>? astFactory;

		/// <summary>
		/// Whether to record skipped rules during parsing. Useful for debugging and syntax highlighting.
		/// </summary>
		public bool recordSkippedRules;

		/// <summary>
		/// Size of tabs in spaces, if default or equal to 0, the 4 will be applied.
		/// </summary>
		public int tabSize;

		/// <summary>
		/// Maximum number of last steps that will be displayed in error messages.
		/// </summary>
		public int maxWalkStepsDisplay;

		/// <summary>
		/// Whether to use the optimized skip whitespace mode, where parser directly skips whitespaces before parsing rules. <br/>
		/// This mode prevents any other skip rules, strategies, barriers calculation and recording.
		/// </summary>
		public bool useOptimizedWhitespaceSkip;
	}
}