using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.TokenPatterns;

namespace RCParsing
{
	/// <summary>
	/// Represents a parsed element in the input text.
	/// </summary>
	public struct ParsedElement
	{
		/// <summary>
		/// The value indicates whether the parsing was successful.
		/// </summary>
		public bool success;

		/// <summary>
		/// The ID of the element that was parsed.
		/// </summary>
		public int elementId;

		/// <summary>
		/// The starting index of the element in the input text.
		/// </summary>
		public int startIndex;

		/// <summary>
		/// The length of the element in the input text.
		/// </summary>
		public int length;

		/// <summary>
		/// Gets the intermediate value associated with this element.
		/// </summary>
		/// <remarks>
		/// For <see cref="SequenceTokenPattern"/> or <see cref="RepeatTokenPattern"/> it will be <see langword="null"/>. <br/>
		/// For <see cref="ChoiceTokenPattern"/> it will be the selected inner value. <br/>
		/// For <see cref="OptionalTokenPattern"/> it will be the inner value if present, otherwise null.
		/// <para/>
		/// For leaf token implementations this may be, for example,
		/// <see cref="Match"/> for <see cref="RegexTokenPattern"/>. <br/>
		/// See remarks for specific implementations.
		/// </remarks>
		public object? intermediateValue;

		/// <summary>
		/// Initializes a new instance of the successful <see cref="ParsedElement"/> struct.
		/// </summary>
		/// <param name="elementId">The ID of the element that was parsed.</param>
		/// <param name="startIndex">The starting index of the element in the input text.</param>
		/// <param name="length">The length of the element in the input text.</param>
		/// <param name="intermediateValue">The intermediate value associated with this token.</param>
		public ParsedElement(int elementId, int startIndex, int length, object? intermediateValue = null)
		{
			this.success = true;
			this.elementId = elementId;
			this.startIndex = startIndex;
			this.length = length;
			this.intermediateValue = intermediateValue;
		}

		/// <summary>
		/// Gets a parsed element that represents failure.
		/// </summary>
		public static ParsedElement Fail { get; } = new ParsedElement
		{
			success = false,
			elementId = -1,
			startIndex = -1,
			length = 0,
			intermediateValue = null
		};
	}
}