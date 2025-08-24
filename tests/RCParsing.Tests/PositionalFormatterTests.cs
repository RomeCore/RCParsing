using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing;

namespace RCParsing.Tests.Parsing
{
	public class PositionalFormatterTests
	{
		[Fact]
		public void SimpleTest()
		{
			var inputStr = "line1\nline2 abc\nline3";

			PositionalFormatter.Decompose(inputStr, 9,
				out var lineStart, out var lineLength, out var lineNumber, out var column, out _);

			Assert.Equal(6, lineStart);    // start of "line2 abc\n"
			Assert.Equal(9, lineLength);   // "line2 abc"
			Assert.Equal(2, lineNumber);   // second line
			Assert.Equal(4, column);       // index 9 points to 'e' in "line2"
		}

		[Fact]
		public void StartOfFirstLine()
		{
			var inputStr = "abc\ndef";
			PositionalFormatter.Decompose(inputStr, 0,
				out var lineStart, out var lineLength, out var lineNumber, out var column, out _);

			Assert.Equal(0, lineStart);
			Assert.Equal(3, lineLength);
			Assert.Equal(1, lineNumber);
			Assert.Equal(1, column);
		}

		[Fact]
		public void EndOfLine()
		{
			var inputStr = "abc\ndef";
			PositionalFormatter.Decompose(inputStr, 3,
				out var lineStart, out var lineLength, out var lineNumber, out var column, out _);

			Assert.Equal(0, lineStart);   // still on line 1
			Assert.Equal(3, lineLength);  // "abc"
			Assert.Equal(1, lineNumber);
			Assert.Equal(4, column);      // column starts at 1
		}

		[Fact]
		public void PositionInCarriageReturnNewline()
		{
			var inputStr = "abc\r\ndef";
			PositionalFormatter.Decompose(inputStr, 5,
				out var lineStart, out var lineLength, out var lineNumber, out var column, out _);

			Assert.Equal(5, lineStart);   // "def" starts at 5
			Assert.Equal(3, lineLength);  // "def"
			Assert.Equal(2, lineNumber);
			Assert.Equal(1, column);      // start of second line
		}

		[Fact]
		public void PositionAtLastCharacter()
		{
			var inputStr = "abc\ndef";
			PositionalFormatter.Decompose(inputStr, 6,
				out var lineStart, out var lineLength, out var lineNumber, out var column, out _);

			Assert.Equal(4, lineStart);   // "def" starts at index 4
			Assert.Equal(3, lineLength);
			Assert.Equal(2, lineNumber);
			Assert.Equal(3, column);      // points to 'f'
		}

		[Fact]
		public void SingleLineInput()
		{
			var inputStr = "just one line";
			PositionalFormatter.Decompose(inputStr, 5,
				out var lineStart, out var lineLength, out var lineNumber, out var column, out _);

			Assert.Equal(0, lineStart);
			Assert.Equal(13, lineLength);
			Assert.Equal(1, lineNumber);
			Assert.Equal(6, column);
		}

		[Fact]
		public void NewlineOnlyInput()
		{
			var inputStr = "\n";
			PositionalFormatter.Decompose(inputStr, 0,
				out var lineStart, out var lineLength, out var lineNumber, out var column, out _);

			Assert.Equal(0, lineStart);
			Assert.Equal(0, lineLength);
			Assert.Equal(1, lineNumber);
			Assert.Equal(1, column);
		}

		[Fact]
		public void ThrowsIfPositionIsOutOfBounds()
		{
			var inputStr = "abc";
			Assert.Throws<ArgumentOutOfRangeException>(() =>
				PositionalFormatter.Decompose(inputStr, 5,
					out _, out _, out _, out _, out _));
		}
	}

}