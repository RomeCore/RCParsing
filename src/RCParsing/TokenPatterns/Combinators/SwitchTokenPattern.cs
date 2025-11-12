using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// The token pattern that matches one of multiple patterns based on parser parameter.
	/// </summary>
	public class SwitchTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the selector function that determines which branch to take.
		/// </summary>
		public Func<object?, int> Selector { get; }

		/// <summary>
		/// Gets the token pattern IDs for the branches.
		/// </summary>
		public IReadOnlyList<int> Branches { get; }

		/// <summary>
		/// Gets the token pattern ID for the default branch. Can be -1 if not specified.
		/// </summary>
		public int DefaultBranch { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="SwitchTokenPattern"/> class.
		/// </summary>
		/// <param name="selector">The selector function that determines which branch to take.</param>
		/// <param name="branches">The token pattern IDs for the branches.</param>
		/// <param name="defaultBranch">The token pattern ID for the default branch.</param>
		public SwitchTokenPattern(Func<object?, int> selector, IEnumerable<int> branches, int defaultBranch = -1)
		{
			Selector = selector ?? throw new ArgumentNullException(nameof(selector));
			Branches = branches?.ToArray() ?? throw new ArgumentNullException(nameof(branches));
			DefaultBranch = defaultBranch;
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				var set = new HashSet<char>();
				foreach (var branchId in Branches)
				{
					var branchChars = GetTokenPattern(branchId).FirstChars;
					set.UnionWith(branchChars);
				}
				if (DefaultBranch >= 0)
					set.UnionWith(GetTokenPattern(DefaultBranch).FirstChars);
				return set;
			}
		}

		protected override bool IsFirstCharDeterministicCore => Branches.Append(DefaultBranch).All(branchId => TryGetTokenPattern(branchId)?.IsFirstCharDeterministic ?? true);
		protected override bool IsOptionalCore => Branches.Append(DefaultBranch).Any(branchId => TryGetTokenPattern(branchId)?.IsOptional ?? false);

		private TokenPattern[] _branches;
		private TokenPattern? _defaultBranch;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);
			_branches = Branches.Select(GetTokenPattern).ToArray();
			_defaultBranch = TryGetTokenPattern(DefaultBranch);
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			var index = Selector(parserParameter);
			if (index < 0 || index >= _branches.Length)
			{
				if (_defaultBranch != null)
					return _defaultBranch.Match(input, position, barrierPosition, parserParameter, calculateIntermediateValue, ref furthestError);

				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, $"Switch index {index} is out of range [0, {_branches.Length - 1}] and default branch is not available.", Id, true);
				return ParsedElement.Fail;
			}

			return _branches[index].Match(input, position, barrierPosition, parserParameter, calculateIntermediateValue, ref furthestError);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "switch...";

			var branchStrings = Branches.Select((b, i) => $"| {i}" + GetTokenPattern(b).ToString(remainingDepth - 1));
			if (DefaultBranch >= 0)
				branchStrings = branchStrings.Append($"| default: {GetTokenPattern(DefaultBranch).ToString(remainingDepth - 1)}");
			return $"switch: ({string.Join(Environment.NewLine, branchStrings.Prepend(""))})".Indent("  ", addIndentToFirstLine: false);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SwitchTokenPattern pattern &&
				   Branches.SequenceEqual(pattern.Branches) &&
				   Equals(Selector, pattern.Selector);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + Selector.GetHashCode();
			foreach (var branch in Branches)
			{
				hashCode = hashCode * 397 + branch;
			}
			hashCode = hashCode * 397 + DefaultBranch;
			return hashCode;
		}
	}
}