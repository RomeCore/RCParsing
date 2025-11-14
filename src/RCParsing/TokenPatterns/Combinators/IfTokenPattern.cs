using System;
using System.Collections.Generic;
using System.Linq;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that conditionally matches one of two patterns based on parser parameter.
	/// </summary>
	public class IfTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the parser parameter condition function that determines which branch to take.
		/// </summary>
		public Func<object?, bool> Condition { get; }

		/// <summary>
		/// Gets the token pattern ID for the true branch.
		/// </summary>
		public int TrueBranch { get; }

		/// <summary>
		/// Gets the token pattern ID for the false branch. Can be -1 if not specified.
		/// </summary>
		public int FalseBranch { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="IfTokenPattern"/> class.
		/// </summary>
		/// <param name="condition">The parser parameter condition function that determines which branch to take.</param>
		/// <param name="trueBranch">The token pattern ID for the true branch.</param>
		/// <param name="falseBranch">The token pattern ID for the false branch.</param>
		public IfTokenPattern(Func<object?, bool> condition, int trueBranch, int falseBranch = -1)
		{
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
			TrueBranch = trueBranch;
			FalseBranch = falseBranch;
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				var set = new HashSet<char>(GetTokenPattern(TrueBranch).FirstChars);
				if (FalseBranch >= 0)
					set.UnionWith(GetTokenPattern(FalseBranch).FirstChars);
				return set;
			}
		}

		protected override bool IsFirstCharDeterministicCore => GetTokenPattern(TrueBranch).IsFirstCharDeterministic && (TryGetTokenPattern(FalseBranch)?.IsFirstCharDeterministic ?? true);
		protected override bool IsOptionalCore => GetTokenPattern(TrueBranch).IsOptional || (TryGetTokenPattern(FalseBranch)?.IsOptional ?? false);

		private TokenPattern _trueBranch;
		private TokenPattern? _falseBranch;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);
			_trueBranch = GetTokenPattern(TrueBranch);
			_falseBranch = TryGetTokenPattern(FalseBranch);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			var branch = Condition(parserParameter) ? _trueBranch : _falseBranch;
			if (branch != null)
				return branch.Match(input, position, barrierPosition, parserParameter, calculateIntermediateValue, ref furthestError);

			if (position >= furthestError.position)
				furthestError = new ParsingError(position, 0, "Condition not met and else branch is not evailable.", Id, true);
			return ParsedElement.Fail;
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "if...";
			return $"if: {GetTokenPattern(TrueBranch).ToString(remainingDepth - 1)} | {TryGetTokenPattern(FalseBranch)?.ToString(remainingDepth - 1) ?? "no else branch"}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is IfTokenPattern pattern &&
				   TrueBranch == pattern.TrueBranch &&
				   FalseBranch == pattern.FalseBranch &&
				   Equals(Condition, pattern.Condition);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + TrueBranch.GetHashCode();
			hashCode = hashCode * 397 + FalseBranch.GetHashCode();
			hashCode = hashCode * 397 + Condition.GetHashCode();
			return hashCode;
		}
	}
}