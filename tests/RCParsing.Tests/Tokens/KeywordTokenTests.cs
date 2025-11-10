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

			Assert.False(parser.MatchesToken("1", "KeYwOrDd"));
			Assert.False(parser.MatchesToken("1", "Key"));
			Assert.False(parser.MatchesToken("1", "Aaa"));
		}

		[Fact]
		public void Choice_CaseInsensitiveMatching()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("1")
				.KeywordChoiceIgnoreCase("Keyword", "Another");

			var parser = builder.Build();

			Assert.True(parser.MatchesToken("1", "keyword", out var matchedLen));
			Assert.Equal(7, matchedLen);

			Assert.True(parser.MatchesToken("1", "KEYWORD", out matchedLen));
			Assert.Equal(7, matchedLen);

			Assert.True(parser.MatchesToken("1", "KeYwOrD", out matchedLen));
			Assert.Equal(7, matchedLen);

			Assert.True(parser.MatchesToken("1", "another", out matchedLen));
			Assert.Equal(7, matchedLen);

			Assert.True(parser.MatchesToken("1", "ANOTHER", out matchedLen));
			Assert.Equal(7, matchedLen);

			Assert.True(parser.MatchesToken("1", "AnOtHeR", out matchedLen));
			Assert.Equal(7, matchedLen);

			Assert.False(parser.MatchesToken("1", "KeYwOrDd"));
			Assert.False(parser.MatchesToken("1", "another_"));
			Assert.False(parser.MatchesToken("1", "Aaa"));
		}

		[Fact]
		public void CaseInsensitiveMatching_UnicodeLanguages()
		{
			var builder = new ParserBuilder();

			// Test various Unicode languages and scripts
			builder.CreateToken("1").UnicodeKeywordIgnoreCase("Привет"); // Russian
			builder.CreateToken("2").UnicodeKeywordIgnoreCase("Γειάσου"); // Greek
			builder.CreateToken("3").UnicodeKeywordIgnoreCase("こんにちは"); // Japanese Hiragana
			builder.CreateToken("4").UnicodeKeywordIgnoreCase("你好"); // Chinese
			builder.CreateToken("5").UnicodeKeywordIgnoreCase("مرحبا"); // Arabic
			builder.CreateToken("6").UnicodeKeywordIgnoreCase("שלום"); // Hebrew
			builder.CreateToken("7").UnicodeKeywordIgnoreCase("नमस्ते"); // Hindi (Devanagari)
			builder.CreateToken("8").UnicodeKeywordIgnoreCase("안녕하세요"); // Korean

			var parser = builder.Build();

			// Russian - Cyrillic
			Assert.True(parser.MatchesToken("1", "привет", out var matchedLen));
			Assert.Equal(6, matchedLen); // 6 characters in "Привет"
			Assert.True(parser.MatchesToken("1", "ПРИВЕТ", out matchedLen));
			Assert.Equal(6, matchedLen);
			Assert.True(parser.MatchesToken("1", "ПрИвЕт", out matchedLen));
			Assert.Equal(6, matchedLen);

			// Greek
			Assert.True(parser.MatchesToken("2", "γειάσου", out matchedLen));
			Assert.Equal(7, matchedLen);
			Assert.True(parser.MatchesToken("2", "ΓΕΙΆΣΟΥ", out matchedLen));
			Assert.Equal(7, matchedLen);

			// Japanese Hiragana (should be case-sensitive by nature, but test the API)
			Assert.True(parser.MatchesToken("3", "こんにちは", out matchedLen));
			Assert.Equal(5, matchedLen);

			// Chinese (should be case-sensitive by nature)
			Assert.True(parser.MatchesToken("4", "你好", out matchedLen));
			Assert.Equal(2, matchedLen);

			// Arabic (should be case-sensitive by nature)
			Assert.True(parser.MatchesToken("5", "مرحبا", out matchedLen));
			Assert.Equal(5, matchedLen);

			// Hebrew (should be case-sensitive by nature)
			Assert.True(parser.MatchesToken("6", "שלום", out matchedLen));
			Assert.Equal(4, matchedLen);

			// Hindi - Devanagari
			Assert.True(parser.MatchesToken("7", "नमस्ते", out matchedLen));
			Assert.Equal(6, matchedLen);

			// Korean - Hangul
			Assert.True(parser.MatchesToken("8", "안녕하세요", out matchedLen));
			Assert.Equal(5, matchedLen);
		}

		[Fact]
		public void Choice_CaseInsensitiveMatching_UnicodeLanguages()
		{
			var builder = new ParserBuilder();

			// Test choices with mixed Unicode languages
			builder.CreateToken("1").UnicodeKeywordChoiceIgnoreCase(("Привет", 1), ("Hello", 2), ("Γειάσου", 3));
			builder.CreateToken("2").UnicodeKeywordChoiceIgnoreCase("こんにちは", "안녕하세요", "नमस्ते");
			builder.CreateToken("3").UnicodeKeywordChoiceIgnoreCase("مرحبا", "שלום", "你好");

			var parser = builder.Build();

			// Mixed languages - token 1
			Assert.True(parser.MatchesToken("1", "привет", out var matchedLen));
			Assert.Equal(6, matchedLen);
			Assert.True(parser.MatchesToken("1", "ПРИВЕТ", out matchedLen));
			Assert.Equal(6, matchedLen);
			Assert.True(parser.MatchesToken("1", "hello", out matchedLen));
			Assert.Equal(5, matchedLen);
			Assert.True(parser.MatchesToken("1", "HELLO", out matchedLen));
			Assert.Equal(5, matchedLen);
			Assert.True(parser.MatchesToken("1", "γειάσου", out matchedLen));
			Assert.Equal(7, matchedLen);
			Assert.True(parser.MatchesToken("1", "ΓΕΙΆΣΟΥ", out matchedLen));
			Assert.Equal(7, matchedLen);

			// Mixed languages - token 2
			Assert.True(parser.MatchesToken("2", "こんにちは", out matchedLen));
			Assert.Equal(5, matchedLen);
			Assert.True(parser.MatchesToken("2", "안녕하세요", out matchedLen));
			Assert.Equal(5, matchedLen);
			Assert.True(parser.MatchesToken("2", "नमस्ते", out matchedLen));
			Assert.Equal(6, matchedLen);

			// Mixed languages - token 3
			Assert.True(parser.MatchesToken("3", "مرحبا", out matchedLen));
			Assert.Equal(5, matchedLen);
			Assert.True(parser.MatchesToken("3", "שלום", out matchedLen));
			Assert.Equal(4, matchedLen);
			Assert.True(parser.MatchesToken("3", "你好", out matchedLen));
			Assert.Equal(2, matchedLen);

			// Negative cases
			Assert.False(parser.MatchesToken("1", "приветище")); // Too long
			Assert.False(parser.MatchesToken("1", "Hell")); // Too short
			Assert.False(parser.MatchesToken("1", "γεια")); // Wrong word
		}
	}
}