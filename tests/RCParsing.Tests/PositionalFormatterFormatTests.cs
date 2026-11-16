using System;
using RCParsing;

namespace RCParsing.Tests
{
	public class PositionalFormatterFormatTests
	{
		private static string Caret(int spaces, int carets = 1) =>
			new string(' ', spaces) + new string('^', carets);

		private static string[] Lines(string formatted) =>
			formatted.Split([Environment.NewLine], StringSplitOptions.None);

		[Fact]
		public void TabsExpandInVisualColumnsButNotInCharacterColumns()
		{
			var inputStr = "\tabc";

			var info = PositionalFormatter.Decompose(inputStr, 1);

			Assert.Equal(0, info.LineStart);
			Assert.Equal(4, info.LineLength);          // '\t' counts as one character
			Assert.Equal(2, info.ColumnNumber);        // ...and as one column
			Assert.Equal(5, info.VisualColumnNumber);  // but as four visual columns
			Assert.Equal(7, info.VisualLineLength);    // '\t' + "abc" = 4 + 3
		}

		[Fact]
		public void TabInTheMiddleExpandsToTheNextTabStop()
		{
			var inputStr = "a\tb";

			var info = PositionalFormatter.Decompose(inputStr, 2);

			Assert.Equal(3, info.LineLength);
			Assert.Equal(3, info.ColumnNumber);
			Assert.Equal(5, info.VisualColumnNumber);
			Assert.Equal(5, info.VisualLineLength);
		}

		[Fact]
		public void VisualLineLengthMatchesCharacterLengthWithoutTabs()
		{
			var info = PositionalFormatter.Decompose("abc", 1);

			Assert.Equal(info.LineLength, info.VisualLineLength);
			Assert.Equal(info.ColumnNumber, info.VisualColumnNumber);
		}

		[Fact]
		public void FormatPlacesTheCaretUnderTheVisualColumn()
		{
			var lines = Lines(PositionalFormatter.Format("\tabc", 1));

			Assert.Equal("\tabc", lines[0]);
			Assert.Equal(Caret(4), lines[1]);
		}

		[Fact]
		public void FormatHonoursTabSize()
		{
			var lines = Lines(PositionalFormatter.Format("\tabc", 1, tabSize: 2));

			Assert.Equal(Caret(2), lines[1]);
		}

		[Fact]
		public void FormatPutsTheAnnotationAfterTheCaretWhenThereIsRoom()
		{
			var lines = Lines(PositionalFormatter.Format("abc", 1, cursorAnnotation: "here"));

			Assert.Equal("abc", lines[0]);
			Assert.Equal(Caret(1) + " here", lines[1]);
		}

		[Fact]
		public void SameLineSpanDrawsOneCaretPerCharacter()
		{
			var start = PositionalFormatter.Decompose("abcdef", 1);
			var end = PositionalFormatter.Decompose("abcdef", 3);

			var lines = Lines(PositionalFormatter.Format("abcdef", start, end));

			Assert.Equal("abcdef", lines[0]);
			Assert.Equal(Caret(1, 3), lines[1]);
		}

		/// <summary>
		/// The regression case: a span that starts on a tab-indented line used to subtract a visual column
		/// from a character length, and the caret count went negative inside the exception constructor.
		/// </summary>
		[Fact]
		public void MultilineSpanStartingOnATabIndentedLineDoesNotThrow()
		{
			var inputStr = "\t}\n\tx";
			var start = PositionalFormatter.Decompose(inputStr, 1);
			var end = PositionalFormatter.Decompose(inputStr, 4);

			var lines = Lines(PositionalFormatter.Format(inputStr, start, end));

			Assert.Equal("\t}", lines[0]);
			Assert.Equal(Caret(4), lines[1]);
			Assert.Equal("\tx", lines[2]);
			Assert.Equal(Caret(0, 5), lines[3]);
		}

		[Fact]
		public void MultilineSpanNumbersBothEndsWhenAsked()
		{
			var inputStr = "\t}\n\tx";
			var start = PositionalFormatter.Decompose(inputStr, 1);
			var end = PositionalFormatter.Decompose(inputStr, 4);

			var lines = Lines(PositionalFormatter.Format(inputStr, start, end, addMultilineNumber: true));

			Assert.Equal("1: \t}", lines[0]);
			Assert.Equal(Caret(7), lines[1]);
			Assert.Equal("2: \tx", lines[2]);
			Assert.Equal(Caret(3, 5), lines[3]);
		}

		[Fact]
		public void MultilineSpanSkipsIntermediateLines()
		{
			var inputStr = "a\nb\nc\nd";
			var start = PositionalFormatter.Decompose(inputStr, 0);
			var end = PositionalFormatter.Decompose(inputStr, 6);

			var lines = Lines(PositionalFormatter.Format(inputStr, start, end, addMultilineNumber: true));

			Assert.Equal("1: a", lines[0]);
			Assert.Equal("...", lines[2]);
			Assert.Equal("4: d", lines[3]);
		}

		[Fact]
		public void SpanStartingAtTheLineEndDrawsAnEmptyMarkerInsteadOfThrowing()
		{
			var inputStr = "ab\ncd";
			var start = PositionalFormatter.Decompose(inputStr, 2);
			var end = PositionalFormatter.Decompose(inputStr, 4);

			var lines = Lines(PositionalFormatter.Format(inputStr, start, end));

			Assert.Equal("ab", lines[0]);
			Assert.Equal(new string(' ', start.VisualLineLength), lines[1]);
			Assert.Equal("cd", lines[2]);
			Assert.Equal(Caret(0, 2), lines[3]);
		}

		[Fact]
		public void FormatThrowsWhenTheEndPrecedesTheStart()
		{
			var start = PositionalFormatter.Decompose("abc", 2);
			var end = PositionalFormatter.Decompose("abc", 1);

			Assert.Throws<ArgumentException>(() => PositionalFormatter.Format("abc", start, end));
		}

		[Fact]
		public void FormatThrowsOnAMultiCharacterCursor()
		{
			var info = PositionalFormatter.Decompose("abc", 1);

			Assert.Throws<ArgumentException>(() => PositionalFormatter.Format("abc", info, cursor: "^^"));
		}

		[Fact]
		public void FormatThrowsWhenThePositionIsOutOfBounds()
		{
			var info = PositionalFormatter.Decompose("abc", 1);
			info.Position = 10;

			Assert.Throws<ArgumentOutOfRangeException>(() => PositionalFormatter.Format("abc", info));
		}
	}
}
