using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.Building.ErrorRecoveryStrategies
{
	/// <summary>
	/// A class that will be directly built using prepared error recovery strategy. This is a leaf node in the buildable tree.
	/// </summary>
	public class BuildableLeafErrorRecoveryStrategy : BuildableErrorRecoveryStrategy
	{
		/// <summary>
		/// Gets or sets the prepared error recovery strategy that will be returned by builder.
		/// </summary>
		public ErrorRecoveryStrategy? Strategy { get; set; }

		public override ErrorRecoveryStrategy BuildTyped(List<int>? ruleChildren, List<int>? tokenChildren, List<object?>? elementChildren)
		{
			return Strategy;
		}
	}
}