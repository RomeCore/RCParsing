using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCParsing.Tests.Tokens
{
	public class KeywordTokenTests
	{
		[Fact]
		public void CaseInsensitiveMatching()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("1")
				.KeywordIgnoreCase("Keyword");

			var parser = builder.Build();

			Assert.True(parser.MatchesToken("1", "keyword", out var matchedLen));
			Assert.Equal(7, matchedLen);

			Assert.True(parser.MatchesToken("1", "KEYWORD", out matchedLen));
			Assert.Equal(7, matchedLen);

			Assert.True(parser.MatchesToken("1", "KeYwOrD", out matchedLen));
			Assert.Equal(7, matchedLen);
		}
	}
}