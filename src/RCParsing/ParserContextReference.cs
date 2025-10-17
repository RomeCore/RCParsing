using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// A reference to <see cref="ParserContext"/>.
	/// </summary>
	/// <remarks>
	/// Used for reducing allocations and improve performance.
	/// </remarks>
	public class ParserContextReference
	{
		/// <summary>
		/// The context that this reference is pointing at.
		/// </summary>
		public readonly ParserContext context;

		/// <summary>
		/// Initializes a new instance of <see cref="ParserContextReference"/> class.
		/// </summary>
		/// <param name="context">The context to store.</param>
		public ParserContextReference(ParserContext context)
		{
			this.context = context;
		}

		public static implicit operator ParserContext(ParserContextReference reference)
		{
			return reference.context;
		}
		public static implicit operator ParserContextReference(ParserContext context)
		{
			return new ParserContextReference(context);
		}
	}
}