using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a segment of a string.
	/// </summary>
	public struct StringSegment
	{
		/// <summary>
		/// Gets the source string that contains the segment.
		/// </summary>
		public readonly string Source { get; }

		/// <summary>
		/// Gets the starting index of the segment within the source string. The index is zero-based.
		/// </summary>
		public readonly int StartIndex { get; }

		/// <summary>
		/// Gets the length of the segment. The length is the number of characters from StartIndex to the end of the string.
		/// </summary>
		public readonly int Length { get; }

		/// <summary>
		/// Gets the end index of the segment. This is calculated as StartIndex + Length.
		/// </summary>
		public readonly int EndIndex => StartIndex + Length;

		/// <summary>
		/// Gets a span representing the segment.
		/// </summary>
		public readonly ReadOnlySpan<char> Span => Source.AsSpan(StartIndex, Length);

		private string? _slice;
		/// <summary>
		/// Gets a copy of the segment as a new string. This is cached for future use.
		/// </summary>
		public string Slice => _slice ??= Span.ToString();

		/// <summary>
		/// Initializes a new instance of the <see cref="StringSegment"/> struct.
		/// </summary>
		/// <param name="source">The source string that this segment belongs to.</param>
		/// <param name="startIndex">The starting index of the segment within the source string.</param>
		/// <param name="length">The length of the segment.</param>
		public StringSegment(string source, int startIndex, int length)
		{
			Source = source;
			StartIndex = startIndex;
			Length = length;
			_slice = null;
		}

		public override string ToString()
		{
			return Slice;
		}
	}
}