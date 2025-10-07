using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Defines the behaviour of Choice parser rules and token patterns.
	/// </summary>
	public enum ChoiceMode
	{
		/// <summary>
		/// Returns the first succeed choice occurence.
		/// </summary>
		First,

		/// <summary>
		/// Returns the first shortest succeed choice occurence.
		/// </summary>
		Shortest,

		/// <summary>
		/// Returns the first longest succeed choice occurence.
		/// </summary>
		Longest
	}
}