using System;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches a single element
	/// and captures its matched substring as intermediate value,
	/// with optional trimming of characters from the start and end.
	/// </summary>
	public class CaptureTextTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID of the child that should be parsed.
		/// </summary>
		public int Child { get; }

		/// <summary>
		/// Gets the number of characters to trim from the start of the matched substring.
		/// </summary>
		public int TrimStart { get; }

		/// <summary>
		/// Gets the number of characters to trim from the end of the matched substring.
		/// </summary>
		public int TrimEnd { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="CaptureTextTokenPattern"/> class.
		/// </summary>
		/// <param name="child">The token pattern ID of the child element that must be matched.</param>
		/// <param name="trimStart">The number of characters to trim from the start of the captured text.</param>
		/// <param name="trimEnd">The number of characters to trim from the end of the captured text.</param>
		public CaptureTextTokenPattern(int child, int trimStart = 0, int trimEnd = 0)
		{
			if (trimStart < 0) throw new ArgumentOutOfRangeException(nameof(trimStart));
			if (trimEnd < 0) throw new ArgumentOutOfRangeException(nameof(trimEnd));

			Child = child;
			TrimStart = trimStart;
			TrimEnd = trimEnd;
		}



		private TokenPattern _child;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);
			_child = GetTokenPattern(Child);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			var initialPosition = position;
			var child = _child.Match(input, position, barrierPosition, parserParameter, false);
			if (!child.success)
				return ParsedElement.Fail;

			position = child.startIndex + child.length;

			object? value = null;
			if (calculateIntermediateValue)
			{
				int trimStart = Math.Min(TrimStart, child.length);
				int trimEnd = Math.Min(TrimEnd, child.length - trimStart);

				if (child.length - trimStart - trimEnd > 0)
				{
					value = input.Substring(child.startIndex + trimStart, child.length - trimStart - trimEnd);
				}
				else
				{
					value = string.Empty;
				}
			}

			return new ParsedElement(initialPosition, position - initialPosition, value);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "captureText...";
			return $"captureText (trimStart={TrimStart}, trimEnd={TrimEnd}): {GetTokenPattern(Child).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is CaptureTextTokenPattern pattern &&
				   Child == pattern.Child &&
				   TrimStart == pattern.TrimStart &&
				   TrimEnd == pattern.TrimEnd;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + TrimStart.GetHashCode();
			hashCode = hashCode * 397 + TrimEnd.GetHashCode();
			return hashCode;
		}
	}
}