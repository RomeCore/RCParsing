using System;
using System.Collections.Generic;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The mapper delegate type for <see cref="MapSpanTokenPattern"/>.
	/// </summary>
	/// <param name="span">The span to map.</param>
	/// <returns>The output intermediate value.</returns>
	public delegate object? SpanMapper(ReadOnlySpan<char> span);

	/// <summary>
	/// The token pattern that matches a single element,
	/// transforming its matched text span with a provided function.
	/// </summary>
	public class MapSpanTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID of the child that should be parsed.
		/// </summary>
		public int Child { get; }

		/// <summary>
		/// Gets the mapping function that transforms the matched text span.
		/// </summary>
		public SpanMapper Mapper { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="MapSpanTokenPattern"/> class.
		/// </summary>
		/// <param name="child">The token pattern ID of the child element that must be matched.</param>
		/// <param name="mapper">The transformation function applied to the matched text span.</param>
		public MapSpanTokenPattern(int child, SpanMapper mapper)
		{
			Child = child;
			Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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

			var child = _child.Match(input, position, barrierPosition, parserParameter,
				false, ref furthestError);

			if (!child.success)
				return ParsedElement.Fail;

			var span = input.AsSpan(child.startIndex, child.length);
			child.intermediateValue = Mapper(span);
			return child;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "mapSpan...";
			return $"mapSpan: {GetTokenPattern(Child).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is MapSpanTokenPattern pattern &&
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