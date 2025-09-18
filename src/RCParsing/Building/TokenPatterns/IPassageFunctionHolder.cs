using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.Building.TokenPatterns
{
	/// <summary>
	/// Represents a passage function holder, the buildable token pattern that can hold the passage function.
	/// </summary>
	public interface IPassageFunctionHolder
	{
		/// <summary>
		/// The function to pass the intermediate values from each pattern to the result intermediate value.
		/// </summary>
		public Func<IReadOnlyList<object?>, object?>? PassageFunction { get; set; }
	}
}