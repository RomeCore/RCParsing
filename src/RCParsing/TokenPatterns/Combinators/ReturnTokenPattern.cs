using System.Collections.Generic;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches a single element, always returning a fixed intermediate value.
	/// </summary>
	public class ReturnTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID of the child that should be parsed.
		/// </summary>
		public int Child { get; }

		/// <summary>
		/// Gets the fixed intermediate value that will always be passed up upon successful match.
		/// </summary>
		public object? Value { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="ReturnTokenPattern"/> class.
		/// </summary>
		/// <param name="child">The token pattern ID of the child element that must be matched.</param>
		/// <param name="value">The fixed intermediate value to return upon successful match.</param>
		public ReturnTokenPattern(int child, object? value)
		{
			Child = child;
			Value = value;
		}

		protected override HashSet<char>? FirstCharsCore => GetTokenPattern(Child).FirstChars;



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
			return new ParsedElement(initialPosition, position - initialPosition, Value);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "return...";
			return $"return (value={Value}): {GetTokenPattern(Child).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is ReturnTokenPattern pattern &&
				   Child == pattern.Child &&
				   Equals(Value, pattern.Value);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + Value?.GetHashCode() ?? 0;
			return hashCode;
		}
	}
}