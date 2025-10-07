using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using RCParsing.Utils;
using System.Collections.ObjectModel;

namespace RCParsing.TokenPatterns.Combinators
{
	/// <summary>
	/// Matches one of several token patterns.
	/// </summary>
	public class ChoiceTokenPattern : TokenPattern
	{
		private readonly int[] _choicesIds;

		/// <summary>
		/// The child element selection behaviour of this choice token pattern.
		/// </summary>
		public ChoiceMode Mode { get; }

		/// <summary>
		/// The IDs of the token patterns to try.
		/// </summary>
		public IReadOnlyList<int> Choices { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="mode">The child element selection behaviour of this choice token pattern.</param>
		/// <param name="tokenPatternIds">The token patterns ids to try.</param>
		public ChoiceTokenPattern(ChoiceMode mode, IEnumerable<int> tokenPatternIds)
		{
			if (!Enum.IsDefined(typeof(ChoiceMode), mode))
				throw new ArgumentOutOfRangeException(nameof(mode));
			Mode = mode;

			_choicesIds = tokenPatternIds?.ToArray() ?? throw new ArgumentNullException(nameof(tokenPatternIds));
			Choices = _choicesIds.AsReadOnlyList();
			if (_choicesIds.Length == 0)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				var childFirstChars = Choices.Select(id => GetTokenPattern(id).FirstChars).ToArray();
				return new(childFirstChars.SelectMany(fcs => fcs).Distinct());
			}
		}
		protected override bool IsFirstCharDeterministicCore
		{
			get
			{
				var childDeterministic = Choices.Select(id => GetTokenPattern(id).IsFirstCharDeterministic).ToArray();
				return childDeterministic.All(o => o);
			}
		}
		protected override bool IsOptionalCore
		{
			get
			{
				var childOptionals = Choices.Select(id => GetTokenPattern(id).IsOptional).ToArray();
				return childOptionals.Any(o => o);
			}
		}



		private TokenPattern[] _choices = null!;
		private TokenPattern[][] _optimizedCandidates;
		private TokenPattern[] _nonDeterministic;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);
			_choices = Choices.Select(i => GetTokenPattern(i)).ToArray();
		}

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			if (initFlags.HasFlag(ParserInitFlags.FirstCharacterMatch))
			{
				_optimizedCandidates = new TokenPattern[char.MaxValue + 1][];
				var nonDeterministic = new List<TokenPattern>();

				foreach (var ch in FirstChars)
				{
					var choicesByChar = new List<TokenPattern>();
					foreach (var pattern in _choices)
						if (!pattern.IsFirstCharDeterministic || pattern.FirstChars.Contains(ch))
							choicesByChar.Add(pattern);
					_optimizedCandidates[ch] = choicesByChar.ToArray();
				}

				foreach (var pattern in _choices)
					if (!pattern.IsFirstCharDeterministic)
						nonDeterministic.Add(pattern);

				_nonDeterministic = nonDeterministic.ToArray();

				for (int c = 0; c < 0xffff + 1; c++)
				{
					if (_optimizedCandidates[c] == null)
						_optimizedCandidates[c] = _nonDeterministic;
				}
			}
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (_optimizedCandidates != null && position < barrierPosition)
			{
				var firstChar = input[position];
				var candidates = _optimizedCandidates[firstChar];

				switch (Mode)
				{
					default:
					case ChoiceMode.First:

						for (int i = 0; i < candidates.Length; i++)
						{
							var result = candidates[i].Match(input, position, barrierPosition,
								parserParameter, calculateIntermediateValue, ref furthestError);
							if (result.success)
								return result;
						}

						break;

					case ChoiceMode.Shortest:

						int shortestLen = int.MaxValue;
						ParsedElement shortest = ParsedElement.Fail;

						for (int i = 0; i < candidates.Length; i++)
						{
							var result = candidates[i].Match(input, position, barrierPosition,
								parserParameter, calculateIntermediateValue, ref furthestError);
							if (result.success && shortestLen > result.length)
							{
								shortest = result;
								shortestLen = shortest.length;
							}
						}

						if (shortest.success)
							return shortest;

						break;

					case ChoiceMode.Longest:

						int longestLen = int.MinValue;
						ParsedElement longest = ParsedElement.Fail;

						for (int i = 0; i < candidates.Length; i++)
						{
							var result = candidates[i].Match(input, position, barrierPosition,
								parserParameter, calculateIntermediateValue, ref furthestError);
							if (result.success && longestLen < result.length)
							{
								longest = result;
								longestLen = longest.length;
							}
						}

						if (longest.success)
							return longest;

						break;
				}
			}
			else
			{
				switch (Mode)
				{
					default:
					case ChoiceMode.First:

						for (int i = 0; i < _choices.Length; i++)
						{
							var result = _choices[i].Match(input, position, barrierPosition,
								parserParameter, calculateIntermediateValue, ref furthestError);
							if (result.success)
								return result;
						}

						break;

					case ChoiceMode.Shortest:

						int shortestLen = int.MaxValue;
						ParsedElement shortest = ParsedElement.Fail;

						for (int i = 0; i < _choices.Length; i++)
						{
							var result = _choices[i].Match(input, position, barrierPosition,
								parserParameter, calculateIntermediateValue, ref furthestError);
							if (result.success && shortestLen > result.length)
							{
								shortest = result;
								shortestLen = shortest.length;
							}
						}

						if (shortest.success)
							return shortest;

						break;

					case ChoiceMode.Longest:

						int longestLen = int.MinValue;
						ParsedElement longest = ParsedElement.Fail;

						for (int i = 0; i < _choices.Length; i++)
						{
							var result = _choices[i].Match(input, position, barrierPosition,
								parserParameter, calculateIntermediateValue, ref furthestError);
							if (result.success && longestLen < result.length)
							{
								longest = result;
								longestLen = longest.length;
							}
						}

						if (longest.success)
							return longest;

						break;
				}
			}

			return ParsedElement.Fail;
		}



		public override string ToStringOverride(int remainingDepth)
		{
			if (remainingDepth <= 0)
				return "choice...";
			return $"choice:\n" +
				string.Join("\n", Choices.Select(c => GetTokenPattern(c).ToString(remainingDepth - 1)))
				.Indent("  ");
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is ChoiceTokenPattern other &&
				   Choices.SequenceEqual(other.Choices);
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + Choices.GetSequenceHashCode();
			return hashCode;
		}
	}
}