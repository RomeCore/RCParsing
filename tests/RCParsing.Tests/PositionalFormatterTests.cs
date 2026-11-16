using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing;

namespace RCParsing.Tests
{
	public class PositionalFormatterTests
	{
		[Fact]
		public void SimpleTest()
		{
			var inputStr = "line1\nline2 abc\nline3";

			var info = PositionalFormatter.Decompose(inputStr, 9);

			Assert.Equal(6, info.LineStart);    // start of "line2 abc\n"
			Assert.Equal(9, info.LineLength);   // "line2 abc"
			Assert.Equal(2, info.LineNumber);   // second line
			Assert.Equal(4, info.ColumnNumber); // index 9 points to 'e' in "line2"
		}

		[Fact]
		public void StartOfFirstLine()
		{
			var inputStr = "abc\ndef";

			var info = PositionalFormatter.Decompose(inputStr, 0);

			Assert.Equal(0, info.LineStart);
			Assert.Equal(3, info.LineLength);
			Assert.Equal(1, info.LineNumber);
			Assert.Equal(1, info.ColumnNumber);
		}

		[Fact]
		public void EndOfLine()
		{
			var inputStr = "abc\ndef";

			var info = PositionalFormatter.Decompose(inputStr, 3);

			Assert.Equal(0, info.LineStart);    // still on line 1
			Assert.Equal(3, info.LineLength);   // "abc"
			Assert.Equal(1, info.LineNumber);
			Assert.Equal(4, info.ColumnNumber); // column starts at 1
		}

		[Fact]
		public void PositionInCarriageReturnNewline()
		{
			var inputStr = "abc\r\ndef";

			var info = PositionalFormatter.Decompose(inputStr, 5);

			Assert.Equal(5, info.LineStart);    // "def" starts at 5
			Assert.Equal(3, info.LineLength);   // "def"
			Assert.Equal(2, info.LineNumber);
			Assert.Equal(1, info.ColumnNumber); // start of second line
		}

		[Fact]
		public void PositionAtLastCharacter()
		{
			var inputStr = "abc\ndef";

			var info = PositionalFormatter.Decompose(inputStr, 6);

			Assert.Equal(4, info.LineStart);    // "def" starts at index 4
			Assert.Equal(3, info.LineLength);
			Assert.Equal(2, info.LineNumber);
			Assert.Equal(3, info.ColumnNumber); // points to 'f'
		}

		[Fact]
		public void SingleLineInput()
		{
			var inputStr = "just one line";

			var info = PositionalFormatter.Decompose(inputStr, 5);

			Assert.Equal(0, info.LineStart);
			Assert.Equal(13, info.LineLength);
			Assert.Equal(1, info.LineNumber);
			Assert.Equal(6, info.ColumnNumber);
		}

		[Fact]
		public void NewlineOnlyInput()
		{
			var inputStr = "\n";

			var info = PositionalFormatter.Decompose(inputStr, 0);

			Assert.Equal(0, info.LineStart);
			Assert.Equal(0, info.LineLength);
			Assert.Equal(1, info.LineNumber);
			Assert.Equal(1, info.ColumnNumber);
		}

		[Fact]
		public void ThrowsIfPositionIsOutOfBounds()
		{
			var inputStr = "abc";
			Assert.Throws<ArgumentOutOfRangeException>(() =>
				PositionalFormatter.Decompose(inputStr, 5));
		}
	}

}