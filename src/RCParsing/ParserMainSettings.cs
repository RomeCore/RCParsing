using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// A type of AST that will be returned from parser.
	/// </summary>
	public enum ParserASTType
	{
		/// <summary>
		/// The default type, AST that does not store values and calulates them every time when called.
		/// </summary>
		Lightweight,

		/// <summary>
		/// The lazy AST that store calculated values.
		/// </summary>
		Lazy
	}

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
		/// The AST type that produces the parser on successful parsing. Can be either lazy or lightweight.
		/// </summary>
		public ParserASTType astType;

		/// <summary>
		/// Whether to record skipped rules during parsing. Useful for debugging and syntax highlighting.
		/// </summary>
		public bool recordSkippedRules;

		/// <summary>
		/// Size of tabs in spaces, if default or equal to 0, the 4 will be applied.
		/// </summary>
		public int tabSize;
	}
}