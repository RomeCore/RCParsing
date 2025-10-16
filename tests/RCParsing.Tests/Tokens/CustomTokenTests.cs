using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests.Tokens
{
	public class CustomTokenTests
	{
		[Fact]
		public void OneChildMatching()
		{
			var builder = new ParserBuilder();

			string intermediateValue = "test";

			ParsedElement Match(CustomTokenPattern self, string input, int start, int end,
				object? parameter, bool calc, ref ParsingError ferr, TokenPattern[] children)
			{
				var res = children[0].Match(input, start, end, parameter, calc, ref ferr);
				res.intermediateValue = intermediateValue;
				return res;
			}

			builder.CreateToken("custom")
				.Custom(
					Match,
					b => b.Identifier()
				);

			var parser = builder.Build();

			var match = parser.TryMatchToken("custom", "ID");

			Assert.True(match.Success);
			Assert.Equal("test", match.IntermediateValue);
		}
	}
}