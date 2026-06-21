using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents a token pattern that uses an entire <see cref="Parser"/> for matching.
	/// </summary>
	public class ParserTokenPattern : TokenPattern
	{
		/// <summary>
		/// The parser to use for matching.
		/// </summary>
		public Parser MatchParser { get; }

		/// <summary>
		/// The alias for the rule to parse.
		/// </summary>
		public string? RuleAlias { get; }

		/// <summary>
		/// The alias for the token to parse.
		/// </summary>
		public string? TokenAlias { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserTokenPattern"/> class.
		/// </summary>
		/// <param name="matchParser">The parser to use for matching.</param>
		/// <param name="ruleAlias">The alias for the rule to parse.</param>
		/// <param name="tokenAlias">The alias for the token to parse.</param>
		public ParserTokenPattern(Parser matchParser, string? ruleAlias = null, string? tokenAlias = null)
		{
			MatchParser = matchParser ?? throw new ArgumentNullException(nameof(matchParser));
			RuleAlias = ruleAlias;
			TokenAlias = tokenAlias;
		}

		protected override HashSet<char> FirstCharsCore => new HashSet<char>();
		protected override bool IsFirstCharDeterministicCore => false;
		protected override bool IsOptionalCore => true;

		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter,
			bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (position > barrierPosition)
			{
				return ParsedElement.Fail;
			}

			try
			{
				if (RuleAlias != null || TokenAlias == null)
				{
					var context = new ParserContext(MatchParser, input, parserParameter)
					{
						position = position,
						maxPosition = barrierPosition
					};
					var result = MatchParser.ParseRule(RuleAlias, context);
					if (calculateIntermediateValue)
						return new ParsedElement(result.StartIndex, result.Length, result.Value);
					else
						return new ParsedElement(result.StartIndex, result.Length, null);
				}
				else
				{
					if (calculateIntermediateValue)
					{
						var result = MatchParser.TryMatchToken(TokenAlias, input, position, barrierPosition - position);
						if (!result.Success)
							return ParsedElement.Fail;
						if (calculateIntermediateValue)
							return new ParsedElement(result.StartIndex, result.Length, result.IntermediateValue);
						else
							return new ParsedElement(result.StartIndex, result.Length, null);
					}
					else
					{
						if (MatchParser.MatchesToken(TokenAlias, input, position, barrierPosition - position, out ParsedElement matchedToken, parserParameter))
							return matchedToken;
						return ParsedElement.Fail;
					}
				}
			}
			catch (Exception ex)
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, ex.Message, Id, true);
				return ParsedElement.Fail;
			}
		}

		public override string ToStringOverride(int remainingDepth)
		{
			if (RuleAlias != null)
				return $"parser(rule:{RuleAlias})";
			if (TokenAlias != null)
				return $"parser(token:{TokenAlias})";
			return "parser(main)";
		}
	}
}
