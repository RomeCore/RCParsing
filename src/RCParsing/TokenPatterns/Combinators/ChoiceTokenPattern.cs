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
		/// The IDs of the token patterns to try.
		/// </summary>
		public IReadOnlyList<int> Choices { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to try.</param>
		public ChoiceTokenPattern(IEnumerable<int> tokenPatternIds)
		{
			_choicesIds = tokenPatternIds?.ToArray() ?? throw new ArgumentNullException(nameof(tokenPatternIds));
			Choices = _choicesIds.AsReadOnlyList();
			if (_choicesIds.Length == 0)
				throw new ArgumentException("At least one token pattern must be provided.", nameof(tokenPatternIds));
		}

		protected override HashSet<char>? FirstCharsCore { get
			{
				var childFirstChars = Choices.Select(id => GetTokenPattern(id).FirstChars).ToArray();
				if (childFirstChars.Any(fcs => fcs == null))
					return null;
				return new (childFirstChars.SelectMany(fcs => fcs).Distinct());
			}
		}



		private TokenPattern[] _choices = null!;
		private TokenPattern[][]? _optimizedCandidates;
		private TokenPattern[] _nonDeterministicCandidates;

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
				var candidatesByChar = new List<TokenPattern>[char.MaxValue + 1];
				var nonDeterministic = new List<TokenPattern>();

				foreach (var pattern in _choices)
				{
					var firstChars = pattern.FirstChars;
					if (firstChars != null)
					{
						foreach (var ch in firstChars)
						{
							if (candidatesByChar[ch] == null)
								candidatesByChar[ch] = new List<TokenPattern>();
							candidatesByChar[ch]!.Add(pattern);
						}
					}
					else
					{
						nonDeterministic.Add(pattern);
					}
				}

				_optimizedCandidates = new TokenPattern[char.MaxValue + 1][];
				for (int i = 0; i < candidatesByChar.Length; i++)
				{
					_optimizedCandidates[i] = candidatesByChar[i]?.ToArray() ?? Array.Empty<TokenPattern>();
				}

				_nonDeterministicCandidates = nonDeterministic.ToArray();
			}
		}

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (_optimizedCandidates != null && position < barrierPosition)
			{
				var firstChar = input[position];
				var candidates = _optimizedCandidates[firstChar];

				for (int i = 0; i < candidates.Length; i++)
				{
					var result = candidates[i].Match(input, position, barrierPosition,
						parserParameter, calculateIntermediateValue, ref furthestError);
					if (result.success)
						return result;
				}

				for (int i = 0; i < _nonDeterministicCandidates.Length; i++)
				{
					var result = _nonDeterministicCandidates[i].Match(input, position, barrierPosition,
						parserParameter, calculateIntermediateValue, ref furthestError);
					if (result.success)
						return result;
				}
			}
			else
			{
				for (int i = 0; i < _choices.Length; i++)
				{
					var result = _choices[i].Match(input, position, barrierPosition,
						parserParameter, calculateIntermediateValue, ref furthestError);
					if (result.success)
						return result;
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