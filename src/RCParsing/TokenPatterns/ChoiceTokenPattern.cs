using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using RCParsing.Utils;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches one of several token patterns.
	/// </summary>
	public class ChoiceTokenPattern : TokenPattern
	{
		/// <summary>
		/// The IDs of the token patterns to try.
		/// </summary>
		public ImmutableArray<int> Choices { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChoiceTokenPattern"/> class.
		/// </summary>
		/// <param name="tokenPatternIds">The token patterns ids to try.</param>
		public ChoiceTokenPattern(IEnumerable<int> tokenPatternIds)
		{
			Choices = tokenPatternIds?.ToImmutableArray()
				?? throw new ArgumentNullException(nameof(tokenPatternIds));
			if (Choices.IsEmpty)
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



		public override ParsedElement Match(string input, int position)
		{
			foreach (var tokenId in Choices)
			{
				var token = TryMatchToken(tokenId, input, position);
				if (token.success)
				{
					token.elementId = Id;
					return token;
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