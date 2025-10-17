using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.ParserRules;

namespace RCParsing.Tests.Rules
{
	public class CustomRuleTests
	{
		[Fact]
		public void OneChildMatching()
		{
			var builder = new ParserBuilder();

			ParsedRule Parse(CustomParserRule self, ParserContext ctx, ParserSettings settings,
				ParserSettings childSettings, int[] childrenIds)
			{
				var res = self.ParseRule(childrenIds[0], ctx, childSettings);
				res.length -= (int)ctx.parserParameter!;
				return new ParsedRule(self.Id, res);
			}

			builder.CreateRule("custom")
				.Custom(
					Parse,
					b => b.Identifier()
				);

			var parser = builder.Build();

			var result = parser.ParseRule("custom", "IDD", parameter: 1);

			Assert.Equal(2, result.Length);
			Assert.Equal("ID", result.Text);
		}
	}
}