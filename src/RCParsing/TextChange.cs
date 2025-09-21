using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a change in the input text.
	/// </summary>
	/// <remarks>
	/// Used for incremental parsing.
	/// </remarks>
	public readonly struct TextChange
	{
		/// <summary>
		/// Gets the start index of the change in the input text.
		/// </summary>
		public readonly int startIndex;

		/// <summary>
		/// Gets the old length of the change in the input text.
		/// </summary>
		public readonly int oldLength;

		/// <summary>
		/// Gets the new length of the change in the input text.
		/// </summary>
		public readonly int newLength;

		/// <summary>
		/// Gets the resulting text after applying the change.
		/// </summary>
		public readonly string resultingText;

		/// <summary>
		/// Initializes a new instance of the <see cref="TextChange"/> struct.
		/// </summary>
		/// <param name="startIndex">The start index of the change in the input text.</param>
		/// <param name="oldLength">The old length of the change in the input text.</param>
		/// <param name="newLength">The new length of the change in the input text.</param>
		/// <param name="resultingText">The resulting text after applying the change.</param>
		public TextChange(int startIndex, int oldLength, int newLength, string resultingText)
		{
			this.startIndex = startIndex;
			this.oldLength = oldLength;
			this.newLength = newLength;
			this.resultingText = resultingText;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TextChange"/> struct.
		/// </summary>
		/// <param name="oldText">The old text before the change.</param>
		/// <param name="startIndex">The start index of the change in the input text.</param>
		/// <param name="oldLength">The old length of the change in the input text.</param>
		/// <param name="newDelta">The new text to insert at the change index.</param>
		public TextChange(string oldText, int startIndex, int oldLength, string newDelta)
		{
			this.startIndex = startIndex;
			this.oldLength = oldLength;
			this.newLength = newDelta.Length;

			var sb = new StringBuilder();
			sb.Append(oldText, 0, startIndex);
			sb.Append(newDelta);
			sb.Append(oldText, startIndex + oldLength, oldText.Length - (startIndex + oldLength));
			this.resultingText = sb.ToString();
		}
	}
}