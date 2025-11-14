using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents an interface for objects that need to be initialized after the <see cref="ParserBuilder"/> build process.
	/// </summary>
	public interface IInitializeAfterBuild
	{
		/// <summary>
		/// Initializes this object with the provided parser.
		/// </summary>
		/// <remarks>
		/// Initialization is performed after the <see cref="ParserBuilder"/> build process completes.
		/// </remarks>
		/// <param name="parser">The parser to use for initialization.</param>
		void Initialize(Parser parser);
	}
}