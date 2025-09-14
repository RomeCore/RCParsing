namespace RCParsing
{
	/// <summary>
	/// Represents an element that was expected during parsing.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ExpectedElement<T> where T : ParserElement
	{
		/// <summary>
		/// Gets the parser element that was expected.
		/// </summary>
		public T Element { get; }

		/// <summary>
		/// Gets the ID of the parser element that was expected.
		/// </summary>
		public int Id => Element.Id;

		/// <summary>
		/// Gets the last alias of the parser element that was expected.
		/// </summary>
		public string? Alias => Element.Aliases.Count > 0 ? Element.Aliases[0] : null;

		/// <summary>
		/// Gets the error message that was ommited when element was failed to parse.
		/// </summary>
		public string Message { get; }

		/// <summary>
		/// Gets the stack trace that describes the state of the parser when the expected element was encountered.
		/// </summary>
		public ParserStackTrace? StackTrace { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ExpectedElement{T}"/> class.
		/// </summary>
		/// <param name="element">The parser element that was expected.</param>
		/// <param name="message">The errors message that was ommited when element was failed to parse.</param>
		/// <param name="stackTrace">The stack trace that describes the state of the parser when the expected element was encountered.</param>
		public ExpectedElement(T element, string message, ParserStackTrace? stackTrace = null)
		{
			Element = element;
			Message = message;
			StackTrace = stackTrace;
		}

		/// <summary>
		/// Returns a string that represents the expected element.
		/// </summary>
		/// <returns>A string that represents the expected element.</returns>
		public override string ToString()
		{
			return Element.ToString();
		}
	}
}