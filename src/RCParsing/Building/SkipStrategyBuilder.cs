using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.Building
{
	/// <summary>
	/// Represents a buildr for skip strategies.
	/// </summary>
	public class SkipStrategyBuilder
	{
		/// <summary>
		/// Gets or sets the skip strategy being built.
		/// </summary>
		public BuildableSkipStrategy? BuildingSkipStrategy { get; set; }
	}
}