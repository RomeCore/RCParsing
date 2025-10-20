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
		/// The mode that affects the sequence advance behavior.
		/// </summary>
		public AdvanceMode AdvanceMode { get; }

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
		/// <param name="advanceMode">The mode that affects the advance behavior.</param>
		/// <param name="first">The token pattern ID that should be parsed first.</param>
		/// <param name="middle">
		/// The middle token pattern ID that will be parsed second,
		/// intermediate value of this element will be passed up.
		/// </param>
		/// <param name="last">The token pattern ID that should be parsed last.</param>
		public BetweenTokenPattern(AdvanceMode advanceMode, int first, int middle, int last)
		{
			First = first;
			Middle = middle;
			Last = last;
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				if (GetTokenPattern(First).IsOptional)
				{
					if (GetTokenPattern(Middle).IsOptional)
					{
						return new(GetTokenPattern(First).FirstChars.Concat(GetTokenPattern(Middle).FirstChars).Concat(GetTokenPattern(Last).FirstChars));
					}
					return new(GetTokenPattern(First).FirstChars.Concat(GetTokenPattern(Middle).FirstChars));
				}
				return GetTokenPattern(First).FirstChars;
			}
		}
		protected override bool IsFirstCharDeterministicCore
		{
			get
			{
				if (GetTokenPattern(First).IsOptional)
				{
					if (GetTokenPattern(Middle).IsOptional)
					{
						return GetTokenPattern(First).IsFirstCharDeterministic && GetTokenPattern(Middle).IsFirstCharDeterministic && GetTokenPattern(Last).IsFirstCharDeterministic;
					}
					return GetTokenPattern(First).IsFirstCharDeterministic && GetTokenPattern(Middle).IsFirstCharDeterministic;
				}
				return GetTokenPattern(First).IsFirstCharDeterministic;
			}
		}
		protected override bool IsOptionalCore => GetTokenPattern(First).IsOptional && GetTokenPattern(Middle).IsOptional && GetTokenPattern(Last).IsOptional;



		private TokenPattern _first, _middle, _last;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);

			_first = GetTokenPattern(First);
			_middle = GetTokenPattern(Middle);
			_last = GetTokenPattern(Last);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			var initialPosition = position;

			var first = _first.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue: false, ref furthestError);
			if (!first.success)
				return ParsedElement.Fail;

			bool advanceEmpty = AdvanceMode == AdvanceMode.AdvanceEmpty;
			if (advanceEmpty || first.length > 0)
				position = first.startIndex + first.length;

			var middle = _middle.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue, ref furthestError);
			if (!middle.success)
				return ParsedElement.Fail;
			if (advanceEmpty || middle.length > 0)
				position = middle.startIndex + middle.length;

			var last = _last.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue: false, ref furthestError);
			if (!last.success)
				return ParsedElement.Fail;
			if (advanceEmpty || last.length > 0)
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