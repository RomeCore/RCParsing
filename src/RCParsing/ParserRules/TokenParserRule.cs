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
		private Func<ParserContext, ParserContext, ParsedRule> parseFunction;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			_pattern = Parser.TokenPatterns[TokenPattern];
		}

		protected override void Initialize(ParserInitFlags initFlags)
		{
			parseFunction = (ctx, chCtx) =>
			{
				var match = _pattern.Match(ctx.str, ctx.position, ctx.str.Length, ctx.parserParameter);
				if (!match.success)
				{
					RecordError(ref ctx, "Failed to parse token");
					return ParsedRule.Fail;
				}

				return ParsedRule.Token(Id, TokenPattern, match.startIndex, match.length, match.intermediateValue);
			};

			if (initFlags.HasFlag(ParserInitFlags.EnableMemoization))
			{
				var previous = parseFunction;
				parseFunction = (ctx, chCtx) =>
				{
					if (ctx.cache.TryGetRule(Id, ctx.position, out var cachedResult))
						return cachedResult;
					cachedResult = previous(ctx, chCtx);
					ctx.cache.AddRule(Id, ctx.position, cachedResult);
					return cachedResult;
				};
			}
		}

		public override ParsedRule Parse(ParserContext context, ParserContext childContext)
		{
			return parseFunction(context, childContext);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;
			if (!string.IsNullOrEmpty(alias))
				return $"{alias} {GetTokenPattern(TokenPattern).ToString(remainingDepth)}";

			return GetTokenPattern(TokenPattern).ToString(remainingDepth);
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $"'{Aliases.Last()}'" : string.Empty;

			if (!string.IsNullOrEmpty(alias))
				return $"{alias} {GetTokenPattern(TokenPattern).ToString(remainingDepth)}";

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