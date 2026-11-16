namespace RCParsing
{
	public struct PositionFormatInfo
	{
		/// <summary>
		/// The current position in the source text.
		/// </summary>
		public int Position { get; set; }

		/// <summary>
		/// The start character index of the current line in the source text.
		/// </summary>
		public int LineStart { get; set; }

		/// <summary>
		/// The character length of the current line.
		/// </summary>
		public int LineLength { get; set; }

		/// <summary>
		/// The number of line containing the given position as a 1-based index.
		/// </summary>
		public int LineNumber { get; set; }

		/// <summary>
		/// The number of character from the start of the line to the given position as a 1-based index.
		/// </summary>
		public int ColumnNumber { get; set; }

		/// <summary>
		/// The number of character from the start of the line to the given position as a 1-based index, considering tab characters as multiple spaces.
		/// </summary>
		public int VisualColumnNumber { get; set; }

		/// <summary>
		/// The character length of the current line, considering tab characters as multiple spaces.
		/// </summary>
		public int VisualLineLength { get; set; }
	}
}