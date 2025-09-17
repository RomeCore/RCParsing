using RCParsing.Utils;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches a child pattern while skipping any whitespace characters
	/// before it. The intermediate value of the child pattern is passed up.
	/// </summary>
	public class SkipWhitespacesTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID that should be parsed after skipping whitespaces.
		/// </summary>
		public int Pattern { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="SkipWhitespacesTokenPattern"/> class.
		/// </summary>
		/// <param name="pattern">The token pattern ID that should be parsed after skipping whitespaces.</param>
		public SkipWhitespacesTokenPattern(int pattern)
		{
			Pattern = pattern;
		}

		private TokenPattern _pattern = null!;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);
			_pattern = GetTokenPattern(Pattern);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue)
		{
			var initialPosition = position;

			// Skip any whitespace characters
			while (position < barrierPosition && char.IsWhiteSpace(input[position]))
			{
				position++;
			}

			// Match the child pattern at the new position
			var result = _pattern.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue);

			if (!result.success)
				return ParsedElement.Fail;

			// Calculate the total length including skipped whitespace
			var totalLength = (result.startIndex + result.length) - initialPosition;

			return new ParsedElement(initialPosition, totalLength, result.intermediateValue);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "skipWhitespaces...";

			return $"skipWhitespaces:\n" +
				   GetTokenPattern(Pattern).ToString(remainingDepth - 1).Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SkipWhitespacesTokenPattern pattern &&
				   Pattern == pattern.Pattern;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Pattern.GetHashCode();
			return hashCode;
		}
	}
}