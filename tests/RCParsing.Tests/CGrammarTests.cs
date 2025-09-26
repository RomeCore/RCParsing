using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Tests.C;

namespace RCParsing.Tests
{
	public class CGrammarTests
	{
		[Fact]
		public void SimpleC()
		{
			string input =
			"""
			int func(double) {
				return 0.0;
			}

			void main(int) {
				return 2 + 2;
			}
			""";

			var parser = CParser.CreateParser();

			var ast = parser.Parse(input);
		}
	}
}