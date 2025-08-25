using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	public class VirtualTokenPattern : TokenPattern
	{
		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter)
		{
			throw new InvalidOperationException("This pattern is not meant to be used directly. Should be used from a parent rule (not token pattern).");
		}

		public override string ToStringOverride(int remainingDepth)
		{
			throw new NotImplementedException();
		}
	}
}