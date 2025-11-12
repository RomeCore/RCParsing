using System;
using System.Collections.Generic;
using System.Linq;
using RCParsing.Utils;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that matches one of multiple rules based on parser parameter.
	/// </summary>
	public class SwitchParserRule : ParserRule
	{
		/// <summary>
		/// Gets the selector function that determines which branch to take.
		/// </summary>
		public Func<object?, int> Selector { get; }

		/// <summary>
		/// Gets the rule IDs for the branches.
		/// </summary>
		public IReadOnlyList<int> Branches { get; }

		/// <summary>
		/// Gets the rule ID for the default branch. Can be -1 if not specified.
		/// </summary>
		public int DefaultBranch { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SwitchParserRule"/> class.
		/// </summary>
		/// <param name="selector">The selector function that determines which branch to take.</param>
		/// <param name="branches">The rule IDs for the branches.</param>
		/// <param name="defaultBranch">The rule ID for the default branch.</param>
		public SwitchParserRule(Func<object?, int> selector, IEnumerable<int> branches, int defaultBranch = -1)
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
					var branchChars = GetRule(branchId).FirstChars;
					set.UnionWith(branchChars);
				}
				if (DefaultBranch >= 0)
					set.UnionWith(GetRule(DefaultBranch).FirstChars);
				return set;
			}
		}

		protected override bool IsFirstCharDeterministicCore => Branches.Append(DefaultBranch).All(branchId => TryGetRule(branchId)?.IsFirstCharDeterministic ?? true);
		protected override bool IsOptionalCore => Branches.Append(DefaultBranch).Any(branchId => TryGetRule(branchId)?.IsOptional ?? false);



		private ParseDelegate parseFunction;
		private ParserRule[] _branches;
		private ParserRule? _defaultBranch;
		private bool[] _canBranchesBeInlined;
		private bool _canDefaultBranchBeInlined;

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			_branches = Branches.Select(GetRule).ToArray();
			_defaultBranch = TryGetRule(DefaultBranch);

			var inlineFlags = (initFlags & ParserInitFlags.InlineRules) != 0;
			_canBranchesBeInlined = _branches.Select(rule => rule.CanBeInlined && inlineFlags).ToArray();
			_canDefaultBranchBeInlined = _defaultBranch?.CanBeInlined == true && inlineFlags;

			ParsedRule ParseSwitch(ref ParserContext context, ref ParserSettings settings, ref ParserSettings childSettings)
			{
				var index = Selector(context.parserParameter);
				ParserRule? selectedRule = null;
				bool canBeInlined = false;

				if (index >= 0 && index < _branches.Length)
				{
					selectedRule = _branches[index];
					canBeInlined = _canBranchesBeInlined[index];
				}
				else if (_defaultBranch != null)
				{
					selectedRule = _defaultBranch;
					canBeInlined = _canDefaultBranchBeInlined;
				}

				if (selectedRule != null)
				{
					ParsedRule result;
					if (canBeInlined)
						result = selectedRule.Parse(context, childSettings, childSettings);
					else
						result = TryParseRule(selectedRule.Id, context, childSettings);

					if (result.success)
					{
						result.occurency = index;
						return new ParsedRule(Id,
							result.startIndex,
							result.length,
							result.passedBarriers,
							result.intermediateValue,
							result);
					}
				}

				return ParsedRule.Fail;
			}

			parseFunction = WrapParseFunction(ParseSwitch, initFlags);
		}

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction(ref context, ref settings, ref childSettings);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "Switch...";

			var branchStrings = Branches.Select((b, i) => $"| {i}: {GetRule(b).ToString(remainingDepth - 1)}");
			if (DefaultBranch >= 0)
				branchStrings = branchStrings.Append($"| default: {GetRule(DefaultBranch).ToString(remainingDepth - 1)}");
			return $"Switch:{Environment.NewLine}{string.Join(Environment.NewLine, branchStrings.Prepend("")).Indent("  ", addIndentToFirstLine: false)}";
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			if (remainingDepth <= 0)
				return "Switch...";

			var branchStrings = Branches.Select((b, i) =>
			{
				var branchStr = $"| {i}: {GetRule(b).ToString(remainingDepth - 1)}";
				return b == childIndex ? branchStr + " <-- here" : branchStr;
			});

			if (DefaultBranch >= 0)
			{
				var defaultStr = $"| default: {GetRule(DefaultBranch).ToString(remainingDepth - 1)}";
				branchStrings = branchStrings.Append(DefaultBranch == childIndex ? defaultStr + " <-- here" : defaultStr);
			}

			return $"Switch:{Environment.NewLine}{string.Join(Environment.NewLine, branchStrings.Prepend("")).Indent("  ", addIndentToFirstLine: false)}";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is SwitchParserRule rule &&
				   Branches.SequenceEqual(rule.Branches) &&
				   DefaultBranch == rule.DefaultBranch &&
				   Equals(Selector, rule.Selector);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + (Selector?.GetHashCode() ?? 0);
			foreach (var branch in Branches)
			{
				hashCode = hashCode * 397 + branch;
			}
			hashCode = hashCode * 397 + DefaultBranch;
			return hashCode;
		}
	}
}