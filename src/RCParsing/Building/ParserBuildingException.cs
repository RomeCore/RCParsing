using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents an exception that occurs during the building of a parser.
	/// </summary>
	public class ParserBuildingException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserBuildingException"/> class with the specified error message.
		/// </summary>
		/// <param name="message">The error message that explains the reason for the exception.</param>
		public ParserBuildingException(string message) : base(message)
		{
		}
	}
}