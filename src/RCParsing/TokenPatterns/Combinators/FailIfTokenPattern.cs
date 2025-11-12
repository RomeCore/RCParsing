using System;
using System.Collections.Generic;
using System.Linq;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches a single element,
	/// but fails if the condition function returns true for the intermediate value.
	/// </summary>
	public class FailIfTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the token pattern ID of the child that should be parsed.
		/// </summary>
		public int Child { get; }

		/// <summary>
		/// Gets the condition function that determines if the match should fail.
		/// </summary>
		public Func<object?, bool> Condition { get; }

		/// <summary>
		/// Gets the error message to use when the condition fails.
		/// </summary>
		public string ErrorMessage { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="FailIfTokenPattern"/> class.
		/// </summary>
		/// <param name="child">The token pattern ID of the child element that must be matched.</param>
		/// <param name="condition">The condition function that determines if the match should fail.</param>
		/// <param name="errorMessage">The error message to use when the condition fails.</param>
		public FailIfTokenPattern(int child, Func<object?, bool> condition, string errorMessage = "Condition failed.")
		{
			Child = child;
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
			ErrorMessage = errorMessage;
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
			var child = _child.Match(input, position, barrierPosition, parserParameter,
				calculateIntermediateValue: true, ref furthestError);

			if (!child.success)
				return ParsedElement.Fail;

			if (Condition(child.intermediateValue))
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, ErrorMessage, Id, true);
				return ParsedElement.Fail;
			}

			return child;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "fail if...";
			return $"fail if: {GetTokenPattern(Child).ToString(remainingDepth - 1)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is FailIfTokenPattern pattern &&
				   Child == pattern.Child &&
				   Equals(Condition, pattern.Condition) &&
				   ErrorMessage == pattern.ErrorMessage;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Child.GetHashCode();
			hashCode = hashCode * 397 + Condition.GetHashCode();
			hashCode = hashCode * 397 + ErrorMessage.GetHashCode();
			return hashCode;
		}
	}
}