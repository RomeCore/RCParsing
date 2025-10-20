namespace RCParsing
{
	/// <summary>
	/// Defines the advance behaviour of parser rules and token patterns.
	/// </summary>
	public enum AdvanceMode
	{
		/// <summary>
		/// Not advances position if element matches zero length.
		/// </summary>
		IgnoreEmpty,

		/// <summary>
		/// Advances position even if element matches zero length.
		/// </summary>
		AdvanceEmpty
	}
}