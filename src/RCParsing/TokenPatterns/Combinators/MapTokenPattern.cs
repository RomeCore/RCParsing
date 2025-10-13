using System;
using System.Collections.Generic;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches a single element,
	/// transforming its intermediate value with a provided function.
	/// </summary>
	public class MapTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID of the child that should be parsed.
		/// </summary>
		public int Child { get; }

		/// <summary>
		/// Gets the mapping function that transforms the intermediate value of the child.
		/// </summary>
		public Func<object?, object?> Mapper { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="MapTokenPattern"/> class.
		/// </summary>
		/// <param name="child">The token pattern ID of the child element that must be matched.</param>
		/// <param name="mapper">The transformation function applied to the child's intermediate value.</param>
		public MapTokenPattern(int child, Func<object?, object?> mapper)
		{
			Child = child;
			Mapper = mapper;
		}

		protected override HashSet<char> FirstCharsCore => GetTokenPattern(Child).FirstChars;
		protected override bool IsFirstCharDeterministicCore => GetTokenPattern(Child).IsFirstCharDeterministic;
		protected override bool IsOptionalCore => GetTokenPattern(Child).IsOptional;



		private TokenPattern _child;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);
			_child = GetTokenPattern(Child);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (!calculateIntermediateValue)
				return _child.Match(input, position, barrierPosition, parserParameter,
					false, ref furthestError);

			var initialPosition = position;
			var child = _child.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue, ref furthestError);

			if (!child.success)
				return ParsedElement.Fail;

			position = child.startIndex + child.length;
			var mappedValue = Mapper(child.intermediateValue);
			return new ParsedElement(initialPosition, position - initialPosition, mappedValue);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "map...";
			return $"map: {GetTokenPattern(Child).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is MapTokenPattern pattern &&
				   Child == pattern.Child &&
				   Equals(Mapper, pattern.Mapper);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			if (Mapper != null)
				hashCode = hashCode * 397 + Mapper.GetHashCode();
			return hashCode;
		}
	}
}