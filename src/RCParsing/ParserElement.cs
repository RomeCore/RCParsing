using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RCParsing.Utils;

namespace RCParsing
{
	/// <summary>
	/// Represents a parser element. This is an abstract base class for both token patterns and rules.
	/// </summary>
	public abstract class ParserElement
	{
		/// <summary>
		/// Gets the unique identifier for this parser element.
		/// </summary>
		public int Id { get; internal set; } = -1;

		/// <summary>
		/// Gets the aliases for this parser element.
		/// </summary>
		public IReadOnlyList<string> Aliases { get; internal set; } = Array.Empty<string>();
		
		/// <summary>
		/// Gets the last alias for this parser element.
		/// </summary>
		public string? Alias => Aliases.Count != 0 ? Aliases[Aliases.Count - 1] : null;

		/// <summary>
		/// Gets the parser that contains this parser element.
		/// </summary>
		public Parser Parser { get; internal set; } = null!;



		/// <summary>
		/// Gets the core implementation of <see cref="FirstChars"/>, which
		/// returns a set of characters that are allowed as the first character of this parser element.
		/// </summary>
		protected virtual HashSet<char>? FirstCharsCore => null;

		/// <summary>
		/// Gets a value indicating whether the first character of this parser element is deterministic.
		/// </summary>
		/// <remarks>
		/// Deterministic means that the character at the current position can be used to strongly
		/// determine which rule or token will be matched next. <br/>
		/// </remarks>
		public bool IsFirstCharDeterministic => FirstChars != null;

		private readonly Lazy<HashSet<char>?> _firstCharsLazy;
		/// <summary>
		/// Gets a set of characters that can appear at the beginning of this parser element. If null,
		/// it means that the first character is not deterministic.
		/// </summary>
		public HashSet<char>? FirstChars => _firstCharsLazy.Value;

		/// <summary>
		/// Initializes a new instance of the <see cref="ParserElement"/> class.
		/// </summary>
		public ParserElement()
		{
			_firstCharsLazy = new(() => FirstCharsCore);
		}

		/// <summary>
		/// Pre-initializes this parser element. This method is called by the parser when it adds all elements (rules and tokens)
		/// to itself.
		/// </summary>
		/// <remarks>
		/// May be used to perform initialization tasks.
		/// </remarks>
		protected virtual void PreInitialize(ParserInitFlags initFlags)
		{
		}

		/// <summary>
		/// Initializes this parser element. This method is called after the 'PreInitialize' method.
		/// </summary>
		/// <remarks>
		/// May be used to perform some optimizations.
		/// </remarks>
		protected virtual void Initialize(ParserInitFlags initFlags)
		{
		}

		/// <summary>
		/// Post-initializes this parser element. This method is called after the 'Initialize' method.
		/// </summary>
		/// <remarks>
		/// May be used to perform initialization tasks.
		/// </remarks>
		protected virtual void PostInitialize(ParserInitFlags initFlags)
		{
		}

		/// <summary>
		/// Pre-initializes this parser element internally.
		/// </summary>
		internal void PreInitializeInternal(ParserInitFlags initFlags) => PreInitialize(initFlags);

		/// <summary>
		/// Initializes this parser element internally.
		/// </summary>
		internal void InitializeInternal(ParserInitFlags initFlags) => Initialize(initFlags);

		/// <summary>
		/// Post-initializes this parser element internally.
		/// </summary>
		internal void PostInitializeInternal(ParserInitFlags initFlags) => PostInitialize(initFlags);



		/// <summary>
		/// Gets the rule by index within the current parser.
		/// </summary>
		/// <param name="index">The index of the rule to retrieve.</param>
		/// <returns>The rule at the specified index.</returns>
		protected ParserRule GetRule(int index)
		{
			return Parser.Rules[index];
		}

		/// <summary>
		/// Gets the token pattern by index within the current parser.
		/// </summary>
		/// <param name="index">The index of the token pattern to retrieve.</param>
		/// <returns>The token pattern at the specified index.</returns>
		protected TokenPattern GetTokenPattern(int index)
		{
			return Parser.TokenPatterns[index];
		}

		/// <summary>
		/// Records an error associated with this element in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		protected void RecordError(ref ParserContext context, ref ParserSettings settings)
		{
			context.RecordError(settings, null, Id, this is TokenPattern);
		}

		/// <summary>
		/// Records an error associated with this element in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		/// <param name="error">The parsing error to record.</param>
		protected void RecordError(ref ParserContext context, ref ParserSettings settings, ParsingError error)
		{
			context.RecordError(settings, error);
		}

		/// <summary>
		/// Records an error associated with this element and the specific message in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		/// <param name="position">The position in the input string where the error occurred.</param>
		protected void RecordError(ref ParserContext context, ref ParserSettings settings, int position)
		{
			context.RecordError(settings, position, null, Id, this is TokenPattern);
		}

		/// <summary>
		/// Records an error associated with this element and the specific message in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		/// <param name="message">The error message to record.</param>
		protected void RecordError(ref ParserContext context, ref ParserSettings settings, string? message)
		{
			context.RecordError(settings, message, Id, this is TokenPattern);
		}

		/// <summary>
		/// Records an error associated with this element and the specific message in the provided parser context.
		/// </summary>
		/// <param name="context">The parser context to record the error in.</param>
		/// <param name="settings">The settings that affects the recording behavior.</param>
		/// <param name="position">The position in the input string where the error occurred.</param>
		/// <param name="message">The error message to record.</param>
		protected void RecordError(ref ParserContext context, ref ParserSettings settings, int position, string? message)
		{
			context.RecordError(settings, position, message, Id, this is TokenPattern);
		}

		/// <summary>
		/// Tries to match a token with the given ID using the specified parsing context.
		/// </summary>
		/// <param name="tokenId">The ID of the token to match.</param>
		/// <param name="input">The input string to match against.</param>
		/// <param name="position">The starting position in the input string to match against.</param>
		/// <param name="barrierPosition">The position in the input string to stop matching at.</param>
		/// <param name="parserParameter">The optional parameter to pass to the token pattern. Can be used to pass additional information to the custom token patterns.</param>
		/// <param name="calculateIntermediateValue">
		/// Whether to calculate intermediate value.
		/// Will be <see langword="false"/> when it will be ignored and should not be calculated.
		/// </param>
		/// <returns>The parsed token containing the result of the match operation or <see cref="ParsedElement.Fail"/> if the match failed.</returns>
		protected ParsedElement TryMatchToken(int tokenId, string input, int position,
			int barrierPosition, object? parserParameter, bool calculateIntermediateValue)
		{
			return Parser.MatchToken(tokenId, input, position, barrierPosition,
				parserParameter, calculateIntermediateValue);
		}

		/// <summary>
		/// Tries to match a token with the given ID using the specified parsing context.
		/// </summary>
		/// <param name="tokenId">The ID of the token to match.</param>
		/// <param name="input">The input string to match against.</param>
		/// <param name="position">The starting position in the input string to match against.</param>
		/// <param name="barrierPosition">The position in the input string to stop matching at.</param>
		/// <param name="parserParameter">The optional parameter to pass to the token pattern. Can be used to pass additional information to the custom token patterns.</param>
		/// <param name="calculateIntermediateValue">
		/// Whether to calculate intermediate value.
		/// Will be <see langword="false"/> when it will be ignored and should not be calculated.
		/// </param>
		/// <param name="furthestError">The furthest error encountered during the match operation. If no error was encountered, this will be <see cref="ParsingError.Empty"/>.</param>
		/// <returns>The parsed token containing the result of the match operation or <see cref="ParsedElement.Fail"/> if the match failed.</returns>
		protected ParsedElement TryMatchToken(int tokenId, string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, out ParsingError furthestError)
		{
			return Parser.MatchToken(tokenId, input, position, barrierPosition,
				parserParameter, calculateIntermediateValue, out furthestError);
		}

		/// <summary>
		/// Tries to parse a rule with the given ID using the specified parsing context.
		/// </summary>
		/// <param name="ruleId">The ID of the rule to parse.</param>
		/// <param name="context">The parsing context to use for the parse operation.</param>
		/// <param name="settings">The settings to use for parsing.</param>
		/// <returns>The results of rule parsing operation if parsing was successful; otherwise, <see cref="ParsedRule.Fail"/> if parsing failed.</returns>
		protected ParsedRule TryParseRule(int ruleId, ParserContext context, ParserSettings settings)
		{
			return Parser.TryParseRule(ruleId, context, settings);
		}

		/// <summary>
		/// Tries to find all matches in the given input using the specified rule identifier and parser context.
		/// </summary>
		/// <param name="ruleId">The ID of the rule to parse.</param>
		/// <param name="context">The parser context to use for parsing.</param>
		/// <param name="settings">The settings to use for parsing.</param>
		/// <returns>The all matches found in the input.</returns>
		protected IEnumerable<ParsedRule> FindAllMatches(int ruleId, ParserContext context, ParserSettings settings)
		{
			return Parser.FindAllMatches(ruleId, context, settings);
		}

		/// <summary>
		/// Returns a string representation of the parser element using a specified depth for expansion.
		/// </summary>
		/// <param name="remainingDepth">The maximum depth to which the element should be expanded in the string representation. Defaults to 2.</param>
		/// <returns>A string representation of the rule.</returns>
		public abstract string ToStringOverride(int remainingDepth);

		public string ToString(int remainingDepth)
		{
			if (Aliases.Count > 0)
				return $"'{Aliases.Last()}'";
			return ToStringOverride(remainingDepth);
		}

		public override string ToString()
		{
			if (Aliases.Count > 0)
				return $"'{Aliases.Last()}'";
			return ToStringOverride(2); // Default depth is 2.
		}

		public override bool Equals(object? obj)
		{
			return obj is ParserElement other &&
				Id == other.Id &&
				ReferenceEquals(Parser, other.Parser) &&
				Aliases.SequenceEqual(other.Aliases);
		}

		public override int GetHashCode()
		{
			int hashCode = 17;
			hashCode ^= Id.GetHashCode() * 23;
			hashCode ^= (Parser?.GetHashCode() ?? 0) * 27;
			hashCode ^= Aliases.GetSequenceHashCode() * 29;
			return hashCode;
		}
	}
}