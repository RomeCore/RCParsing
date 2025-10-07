﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// Represents a token pattern that is optional. It can match either the wrapped token pattern or no tokens at all.
	/// </summary>
	public class OptionalTokenPattern : TokenPattern
	{
		/// <summary>
		/// The token pattern ID that this optional pattern wraps.
		/// </summary>
		public int Child { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OptionalTokenPattern"/> class.
		/// </summary>
		/// <param name="child">The token pattern ID that this optional pattern wraps.</param>
		public OptionalTokenPattern(int child)
		{
			Child = child;
		}

		protected override HashSet<char> FirstCharsCore => GetTokenPattern(Child).FirstChars;
		protected override bool IsFirstCharDeterministicCore => GetTokenPattern(Child).IsFirstCharDeterministic;
		protected override bool IsOptionalCore => true;



		private TokenPattern _pattern;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			_pattern = GetTokenPattern(Child);
		}



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			var token = _pattern.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue, ref furthestError);
			if (token.success)
				return token;
			else
				return new ParsedElement(position, 0);
		}



		public override string ToStringOverride(int remainingDepth = 2)
		{
			if (remainingDepth <= 0)
				return "optional...";
			return $"optional: {GetTokenPattern(Child).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is OptionalTokenPattern pattern &&
				   Child == pattern.Child;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Child.GetHashCode();
			return hashCode;
		}
	}
}