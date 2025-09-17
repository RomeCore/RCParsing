using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using RCParsing.TokenPatterns;

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
		public int TokenPatternId { get; }

		/// <summary>
		/// The token pattern associated with this rule.
		/// </summary>
		public TokenPattern TokenPattern => _pattern ?? Parser.TokenPatterns[TokenPatternId];

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenParserRule"/> class.
		/// </summary>
		/// <param name="tokenPattern">The token pattern ID to match for this rule.</param>
		public TokenParserRule(int tokenPattern)
		{
			TokenPatternId = tokenPattern;
		}

		protected override HashSet<char>? FirstCharsCore => TokenPattern.FirstChars;



		delegate ParsedRule ParseDelegate(ref ParserContext ctx, ref ParserSettings settings, ref ParserSettings childSettings);

		private bool parseIgnoringBarriers = false;
		private TokenPattern _pattern;
		private ParseDelegate parseFunction;

		protected override void PreInitialize(ParserInitFlags initFlags)
		{
			base.PreInitialize(initFlags);

			_pattern = Parser.TokenPatterns[TokenPatternId];
			ParsedValueFactory ??= _pattern.DefaultParsedValueFactory;
		}

		protected override void Initialize(ParserInitFlags initFlags)
		{
			base.Initialize(initFlags);

			ParsedRule ParseIgnoringBarriers(ref ParserContext ctx, ref ParserSettings stng, ref ParserSettings chStng)
			{
				var match = _pattern.Match(ctx.input, ctx.position, ctx.maxPosition,
					ctx.parserParameter, true);
				if (!match.success)
				{
					RecordError(ref ctx, ref stng, "Failed to parse token");
					return ParsedRule.Fail;
				}

				return ParsedRule.Token(Id, match.startIndex, match.length, ctx.passedBarriers, match.intermediateValue);
			}

			ParsedRule ParseUsingBarriers(ref ParserContext ctx, ref ParserSettings stng, ref ParserSettings chStng)
			{
				if (stng.ignoreBarriers)
				{
					return ParseIgnoringBarriers(ref ctx, ref stng, ref chStng);
				}

				if (ctx.barrierTokens.TryGetBarrierToken(ctx.position, ctx.passedBarriers, out var barrierToken))
				{
					if (barrierToken.tokenId == TokenPatternId)
					{
						return ParsedRule.Token(Id, ctx.position, barrierToken.length,
							barrierToken.index + 1, null);
					}
					else
					{
						RecordError(ref ctx, ref stng, "Failed to match virtual token.");
						return ParsedRule.Fail;
					}
				}

				if (_pattern is BarrierTokenPattern)
				{
					RecordError(ref ctx, ref stng, "Failed to match virtual token.");
					return ParsedRule.Fail;
				}

				int maxPos = ctx.barrierTokens.GetNextBarrierPosition(ctx.position, ctx.passedBarriers);
				if (maxPos == -1) maxPos = ctx.maxPosition;

				var match = _pattern.Match(ctx.input, ctx.position, maxPos, ctx.parserParameter, true);
				if (!match.success)
				{
					RecordError(ref ctx, ref stng, "Failed to parse token.");
					return ParsedRule.Fail;
				}

				return ParsedRule.Token(Id, match.startIndex, match.length,
					ctx.passedBarriers, match.intermediateValue);
			}

			parseFunction = Parser.Tokenizers.Length == 0 ? ParseIgnoringBarriers : ParseUsingBarriers;

			if (Parser.Tokenizers.Length == 0 && _pattern is BarrierTokenPattern)
				throw new InvalidOperationException($"Cannot use barrier token pattern '{_pattern}' without tokenizers.");

			if (initFlags.HasFlag(ParserInitFlags.EnableMemoization))
			{
				var prev = parseFunction;

				ParsedRule ParseMemoized(ref ParserContext ctx, ref ParserSettings stng, ref ParserSettings chStng)
				{
					if (ctx.cache.TryGetRule(Id, ctx.position, out var cachedResult))
						return cachedResult;

					cachedResult = prev(ref ctx, ref stng, ref chStng);
					ctx.cache.AddRule(Id, ctx.position, cachedResult);
					return cachedResult;
				}

				parseFunction = ParseMemoized;
			}

			parseIgnoringBarriers = parseFunction == ParseIgnoringBarriers;
		}

		public override ParsedRule Parse(ParserContext context, ParserSettings settings, ParserSettings childSettings)
		{
			return parseFunction(ref context, ref settings, ref childSettings);
		}



		public override string ToStringOverride(int remainingDepth)
		{
			string alias = Aliases.Count > 0 ? $" '{Aliases.Last()}'" : string.Empty;
			if (!string.IsNullOrEmpty(alias))
				return $"{alias} {TokenPattern.ToString(remainingDepth)}";

			return TokenPattern.ToString(remainingDepth);
		}

		public override string ToStackTraceString(int remainingDepth, int childIndex)
		{
			string alias = Aliases.Count > 0 ? $"'{Aliases.Last()}'" : string.Empty;

			if (!string.IsNullOrEmpty(alias))
				return $"{alias} {GetTokenPattern(TokenPatternId).ToString(remainingDepth)}";

			return GetTokenPattern(TokenPatternId).ToString(remainingDepth);
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is TokenParserRule rule &&
				   TokenPatternId == rule.TokenPatternId;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * -1521134295 + TokenPatternId.GetHashCode();
			return hashCode;
		}
	}
}