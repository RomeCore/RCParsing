using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches text until the child pattern is found, capturing the text as intermediate value.
	/// </summary>
	public class TextUntilTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID of the stop condition.
		/// </summary>
		public int StopPattern { get; }

		/// <summary>
		/// Gets whether empty matches are allowed.
		/// </summary>
		public bool AllowEmpty { get; }

		/// <summary>
		/// Gets whether to consume the stop pattern.
		/// </summary>
		public bool ConsumeStop { get; }

		/// <summary>
		/// Gets whether to fail when end of input is reached.
		/// </summary>
		public bool FailOnEof { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="TextUntilTokenPattern"/> class.
		/// </summary>
		/// <param name="stopPattern">The token pattern ID of the stop condition.</param>
		/// <param name="allowEmpty">Whether empty matches are allowed.</param>
		/// <param name="consumeStop">Whether to consume the stop pattern.</param>
		/// <param name="failOnEof">Whether to fail when end of input is reached.</param>
		public TextUntilTokenPattern(int stopPattern, bool allowEmpty = true, bool consumeStop = false, bool failOnEof = false)
		{
			StopPattern = stopPattern;
			AllowEmpty = allowEmpty;
			ConsumeStop = consumeStop;
			FailOnEof = failOnEof;
		}

		protected override HashSet<char> FirstCharsCore => new HashSet<char>();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => AllowEmpty;

		private TokenPattern _stopPattern;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);
			_stopPattern = GetTokenPattern(StopPattern);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			int startPos = position;

			// Try to find the stop pattern
			while (position < barrierPosition)
			{
				var stopMatch = _stopPattern.Match(input, position, barrierPosition, parserParameter, false, ref furthestError);
				if (stopMatch.success)
				{
					// Found stop pattern
					if (!AllowEmpty && position == startPos)
					{
						if (position >= furthestError.position)
							furthestError = new ParsingError(startPos, 0, "Empty match is not allowed.", Id, true);
						return ParsedElement.Fail;
					}

					if (ConsumeStop)
					{
						position = stopMatch.startIndex + stopMatch.length;
					}

					if (calculateIntermediateValue)
					{
						string capturedText = input.Substring(startPos, position - startPos);
						return new ParsedElement(startPos, position - startPos, capturedText);
					}
					else
					{
						return new ParsedElement(startPos, position - startPos);
					}
				}

				position++;
			}

			// Reached end of input
			if (FailOnEof)
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(startPos, 0, "Stop pattern not found before end of input.", Id, true);
				return ParsedElement.Fail;
			}

			// EOF is acceptable, capture remaining text
			int finalTextLength = barrierPosition - startPos;

			if (!AllowEmpty && finalTextLength == 0)
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(startPos, 0, "Empty match is not allowed.", Id, true);
				return ParsedElement.Fail;
			}

			if (calculateIntermediateValue)
			{
				string capturedText = input.Substring(startPos, finalTextLength);
				return new ParsedElement(startPos, barrierPosition - startPos, capturedText);
			}
			else
			{
				return new ParsedElement(startPos, barrierPosition - startPos);
			}
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "text until...";

			string options = "";
			if (!AllowEmpty) options += " non empty";
			if (ConsumeStop) options += " consume";
			if (!FailOnEof) options += " allow eof";

			return $"text until{options}: {GetTokenPattern(StopPattern).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is TextUntilTokenPattern pattern &&
				   StopPattern == pattern.StopPattern &&
				   AllowEmpty == pattern.AllowEmpty &&
				   ConsumeStop == pattern.ConsumeStop &&
				   FailOnEof == pattern.FailOnEof;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + StopPattern.GetHashCode();
			hashCode = hashCode * 397 + AllowEmpty.GetHashCode();
			hashCode = hashCode * 397 + ConsumeStop.GetHashCode();
			hashCode = hashCode * 397 + FailOnEof.GetHashCode();
			return hashCode;
		}
	}
}