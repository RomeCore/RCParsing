using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Matches a regular expression pattern in the input text.
	/// </summary>
	/// <remarks>
	/// Passes a <see cref="Match"/> object from the regex match as an intermediate value.
	/// </remarks>
	public class RegexTokenPattern : TokenPattern
	{
		/// <summary>
		/// The regular expression pattern string to match. If this token is constructed directly (without providing a pattern), returns <see langword="null"/>.
		/// </summary>
		public string? RegexPattern { get; }

		/// <summary>
		/// The value indicating whether 
		/// </summary>
		public bool? UsesStartAnchor { get; }
		
		/// <summary>
		/// The regular expression to match.
		/// </summary>
		public Regex Regex { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RegexTokenPattern"/> class.
		/// </summary>
		/// <param name="pattern">The regular expression pattern.</param>
		/// <param name="useStartAnchor">If true, the pattern will only match from the current position using \G anchor.</param>
		/// <param name="options">The regex options (default is None).</param>
		public RegexTokenPattern(string pattern, bool useStartAnchor = true, RegexOptions options = RegexOptions.Compiled)
		{
			if (string.IsNullOrEmpty(pattern))
				throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));

			RegexPattern = pattern;
			UsesStartAnchor = useStartAnchor;
			if (useStartAnchor)
				Regex = new Regex($"\\G{RegexPattern}", options);
			else
				Regex = new Regex(RegexPattern, options);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RegexTokenPattern"/> class.
		/// </summary>
		/// <param name="regex">The constructed regular expression, it's recommended to prepend the '\G' into a pattern.</param>
		public RegexTokenPattern(Regex regex)
		{
			if (regex == null)
				throw new ArgumentNullException(nameof(regex));
			RegexPattern = null;
			UsesStartAnchor = null;
			Regex = regex;
		}

		protected override HashSet<char> FirstCharsCore => new();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			var match = Regex.Match(input, position, barrierPosition - position);

			if (!match.Success)
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, "Cannot match regular expression.", Id, true);
				return ParsedElement.Fail;
			}
			return new ParsedElement(match.Index, match.Length, match);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return $"regex '{RegexPattern}'";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is RegexTokenPattern pattern &&
				   RegexPattern == pattern.RegexPattern;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			if (UsesStartAnchor == null)
				hashCode = hashCode * 397 ^ Regex.GetHashCode();
			else
			{
				hashCode = hashCode * 397 ^ RegexPattern.GetHashCode();
				hashCode = hashCode * 397 ^ UsesStartAnchor.GetHashCode();
			}
			return hashCode;
		}
	}
}