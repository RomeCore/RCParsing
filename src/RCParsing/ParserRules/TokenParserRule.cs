using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace RCParsing.ParserRules
{
	/// <summary>
	/// Represents a parser rule that matches a specific token.
	/// </summary>
	public class TokenParserRule : ParserRule
	{
		/// <summary>
		/// The token pattern ID to match for this rule.
		/// </summary>
		public int TokenPattern { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenParserRule"/> class.
		/// </summary>
		/// <param name="tokenPattern">The token pattern ID to match for this rule.</param>
		public TokenParserRule(int tokenPattern)
		{
			TokenPattern = tokenPattern;
		}




		private TokenPattern _pattern;

		protected override void Initialize()
		{
			_pattern = Parser.TokenPatterns[TokenPattern];
		}

		public override ParsedRule Parse(ParserContext context, ParserContext childContext)
		{
			var match = _pattern.Match(context.str, context.position);
			if (!match.success)
			{
				RecordError(context, "Failed to parse token.");
				return ParsedRule.Fail;
			}

			return ParsedRule.Token(Id, TokenPattern, match.startIndex, match.length, match.intermediateValue);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			return GetTokenPattern(TokenPattern).ToString(remainingDepth);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is TokenParserRule rule &&
				   TokenPattern == rule.TokenPattern;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + TokenPattern.GetHashCode();
			return hashCode;
		}
	}
}