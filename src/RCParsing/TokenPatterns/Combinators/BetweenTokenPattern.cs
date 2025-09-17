using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches a sequence of three elements, passing middle element's intermediate value up.
	/// </summary>
	public class BetweenTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID that should be parsed first.
		/// </summary>
		public int First { get; }

		/// <summary>
		/// Gets the middle token pattern ID that will be parsed second,
		/// intermediate value of this element will be passed up.
		/// </summary>
		public int Middle { get; }

		/// <summary>
		/// Gets the token pattern ID that should be parsed last.
		/// </summary>
		public int Last { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="BetweenTokenPattern"/> class.
		/// </summary>
		/// <param name="first">The token pattern ID that should be parsed first.</param>
		/// <param name="middle">
		/// The middle token pattern ID that will be parsed second,
		/// intermediate value of this element will be passed up.
		/// </param>
		/// <param name="last">The token pattern ID that should be parsed last.</param>
		public BetweenTokenPattern(int first, int middle, int last)
		{
			First = first;
			Middle = middle;
			Last = last;
		}



		private TokenPattern _first, _middle, _last;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);

			_first = GetTokenPattern(First);
			_middle = GetTokenPattern(Middle);
			_last = GetTokenPattern(Last);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			var initialPosition = position;

			var first = _first.Match(input, position, barrierPosition, parserParameter, false);
			if (!first.success)
				return ParsedElement.Fail;
			position = first.startIndex + first.length;

			var middle = _middle.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue);
			if (!middle.success)
				return ParsedElement.Fail;
			position = middle.startIndex + middle.length;

			var last = _last.Match(input, position, barrierPosition, parserParameter, false);
			if (!last.success)
				return ParsedElement.Fail;
			position = last.startIndex + last.length;

			return new ParsedElement(initialPosition, position - initialPosition, middle.intermediateValue);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "between...";

			return $"between:\n" +
				string.Join("\n", new int[] { First, Middle, Last }
				.Select(c => GetTokenPattern(c).ToString(remainingDepth - 1)))
				.Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is BetweenTokenPattern pattern &&
				   First == pattern.First &&
				   Middle == pattern.Middle &&
				   Last == pattern.Last;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + First.GetHashCode();
			hashCode = hashCode * 397 + Middle.GetHashCode();
			hashCode = hashCode * 397 + Last.GetHashCode();
			return hashCode;
		}
	}
}