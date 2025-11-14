using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.Building.SkipStrategies
{
	/// <summary>
	/// A class that will be directly built using prepared skip strategy. This is a leaf node in the buildable tree.
	/// </summary>
	public class BuildableLeafSkipStrategy : BuildableSkipStrategy
	{
		/// <summary>
		/// Gets or sets the skip strategy.
		/// </summary>
		public SkipStrategy? Strategy { get; set; }

		public override SkipStrategy BuildTyped(List<int>? ruleChildren, List<int>? tokenChildren, List<object?>? elementChildren)
		{
			return Strategy;
		}
	}
}