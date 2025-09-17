using System.Linq;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches a sequence of two elements,
	/// passing the second element's intermediate value up.
	/// </summary>
	public class SecondTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID that should be parsed first.
		/// </summary>
		public int First { get; }

		/// <summary>
		/// Gets the token pattern ID that should be parsed second,
		/// intermediate value of this element will be passed up.
		/// </summary>
		public int Second { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="SecondTokenPattern"/> class.
		/// </summary>
		/// <param name="first">The token pattern ID that should be parsed first.</param>
		/// <param name="second">
		/// The token pattern ID that should be parsed second,
		/// intermediate value of this element will be passed up.
		/// </param>
		public SecondTokenPattern(int first, int second)
		{
			First = first;
			Second = second;
		}

		private TokenPattern _first = null!;
		private TokenPattern _second = null!;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);

			_first = GetTokenPattern(First);
			_second = GetTokenPattern(Second);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			var initialPosition = position;

			var first = _first.Match(input, position, barrierPosition, parserParameter, false);
			if (!first.success)
				return ParsedElement.Fail;
			position = first.startIndex + first.length;

			var second = _second.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue);
			if (!second.success)
				return ParsedElement.Fail;
			position = second.startIndex + second.length;

			return new ParsedElement(initialPosition, position - initialPosition, second.intermediateValue);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "second...";

			return $"second:\n" +
				   string.Join("\n", new int[] { First, Second }
					   .Select(c => GetTokenPattern(c).ToString(remainingDepth - 1)))
					   .Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SecondTokenPattern pattern &&
				   First == pattern.First &&
				   Second == pattern.Second;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + First.GetHashCode();
			hashCode = hashCode * 397 + Second.GetHashCode();
			return hashCode;
		}
	}
}