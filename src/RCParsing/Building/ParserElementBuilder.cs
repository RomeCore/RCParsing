using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.Building.TokenPatterns;
using RCParsing.TokenPatterns;

namespace RCParsing.Building
{
	/// <summary>
	/// The base class for all parser element builders.
	/// </summary>
	public abstract class ParserElementBuilder<T>
	{
		/// <summary>
		/// Gets a value indicating whether the parser element can be built.
		/// </summary>
		public abstract bool CanBeBuilt { get; }

		/// <summary>
		/// Gets this instance. Used for fluent interface.
		/// </summary>
		protected abstract T GetThis();

		/// <summary>
		/// Adds a named token to the current sequence.
		/// </summary>
		/// <param name="tokenName">The name of the token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public abstract T Token(string tokenName);

		/// <summary>
		/// Add a child pattern to the current sequence.
		/// </summary>
		/// <param name="token">The child pattern to add.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public abstract T AddToken(TokenPattern token,
			Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null);

		/// <summary>
		/// Adds a custom token pattern to the current sequence.
		/// </summary>
		/// <param name="matchFunction">
		/// The function to use for matching the token pattern. <br/>
		/// Parameters: <br/>
		/// - <see cref="CustomTokenPattern"/>: The current custom token pattern. <br/>
		/// - <see cref="string"/>: The input text to match. <br/>
		/// - <see cref="int"/>: The position in the input text to start matching from. <br/>
		/// - <see cref="int"/>: The position in the input text to stop matching at. <br/>
		/// - <see cref="object"/>?: The optional context parameter to that have been passed from parser. <br/>
		/// Returns: <br/>
		/// - <see cref="ParsedElement"/>: The parsed element containing the result of the match.
		/// </param>
		/// <param name="stringRepresentation">The string representation of custom token pattern.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Custom(Func<CustomTokenPattern, string, int, int, object?, ParsedElement> matchFunction,
			string stringRepresentation = "custom",
			Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new CustomTokenPattern(matchFunction, stringRepresentation), factory, config);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal character.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(char literal, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new LiteralCharTokenPattern(literal), factory, config);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal character.</param>
		/// <param name="comparison">The string comparison to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(char literal, StringComparison comparison, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new LiteralCharTokenPattern(literal, comparison), factory, config);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal string.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(string literal, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			if (literal.Length == 1)
				return Literal(literal[0], factory, config);
			return AddToken(new LiteralTokenPattern(literal), factory, config);
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal string.</param>
		/// <param name="comparison">The string comparison to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(string literal, StringComparison comparison, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			if (literal.Length == 1)
				return Literal(literal[0], comparison, factory, config);
			return AddToken(new LiteralTokenPattern(literal, comparison), factory, config);
		}

		/// <summary>
		/// Adds a character predicate token to the current sequence.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Char(Func<char, bool> charPredicate, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new CharacterTokenPattern(charPredicate), factory, config);
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="minCount">The minimum inclusive number of characters to match.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Chars(Func<char, bool> charPredicate, int minCount, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RepeatCharactersTokenPattern(charPredicate, minCount, -1),
				factory, config);
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="minCount">The minimum inclusive number of characters to match.</param>
		/// <param name="maxCount">The maximum inclusive number of characters to match. -1 means no limit.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Chars(Func<char, bool> charPredicate, int minCount, int maxCount, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RepeatCharactersTokenPattern(charPredicate, minCount, maxCount),
				factory, config);
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence that matches zero or more occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T ZeroOrMoreChars(Func<char, bool> charPredicate, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RepeatCharactersTokenPattern(charPredicate, 0, -1),
				factory, config);
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence that matches one or more occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T OneOrMoreChars(Func<char, bool> charPredicate, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RepeatCharactersTokenPattern(charPredicate, 1, -1),
				factory, config);
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(params string[] literals)
		{
			return AddToken(new LiteralChoiceTokenPattern(literals));
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(IEnumerable<string> literals, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new LiteralChoiceTokenPattern(literals), factory, config);
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(IEnumerable<string> literals, StringComparer comparer, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new LiteralChoiceTokenPattern(literals, comparer), factory, config);
		}

		/// <summary>
		/// Adds an identifier token to the current sequence.
		/// </summary>
		/// <param name="startPredicate">The predicate to use for the first character of the identifier.</param>
		/// <param name="continuePredicate">The predicate to use for the remaining characters of the identifier.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(Func<char, bool> startPredicate, Func<char, bool> continuePredicate,
			Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new IdentifierTokenPattern(startPredicate, continuePredicate), factory, config);
		}

		/// <summary>
		/// Adds an identifier token to the current sequence.
		/// </summary>
		/// <param name="startPredicate">The predicate to use for the first character of the identifier.</param>
		/// <param name="continuePredicate">The predicate to use for the remaining characters of the identifier.</param>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(Func<char, bool> startPredicate, Func<char, bool> continuePredicate,
			int minLength, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new IdentifierTokenPattern(startPredicate, continuePredicate, minLength), factory, config);
		}

		/// <summary>
		/// Adds an identifier token to the current sequence.
		/// </summary>
		/// <param name="startPredicate">The predicate to use for the first character of the identifier.</param>
		/// <param name="continuePredicate">The predicate to use for the remaining characters of the identifier.</param>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="maxLength">The maximum length of the identifier. Default is -1 (no limit).</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(Func<char, bool> startPredicate, Func<char, bool> continuePredicate,
			int minLength, int maxLength, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new IdentifierTokenPattern(startPredicate, continuePredicate, minLength, maxLength), factory, config);
		}

		/// <summary>
		/// Adds an ASCII identifier token to the current sequence.
		/// </summary>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(IdentifierTokenPattern.AsciiIdentifier(), factory, config);
		}

		/// <summary>
		/// Adds an ASCII identifier token to the current sequence.
		/// </summary>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(int minLength, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(IdentifierTokenPattern.AsciiIdentifier(minLength), factory, config);
		}

		/// <summary>
		/// Adds an ASCII identifier token to the current sequence.
		/// </summary>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="maxLength">The maximum length of the identifier. Default is -1 (no limit).</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(int minLength, int maxLength, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(IdentifierTokenPattern.AsciiIdentifier(minLength, maxLength), factory, config);
		}

		/// <summary>
		/// Adds an Unicode identifier token to the current sequence.
		/// </summary>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T UnicodeIdentifier(Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(IdentifierTokenPattern.UnicodeIdentifier(), factory, config);
		}

		/// <summary>
		/// Adds an Unicode identifier token to the current sequence.
		/// </summary>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T UnicodeIdentifier(int minLength, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(IdentifierTokenPattern.UnicodeIdentifier(minLength), factory, config);
		}

		/// <summary>
		/// Adds an Unicode identifier token to the current sequence.
		/// </summary>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="maxLength">The maximum length of the identifier. Default is -1 (no limit).</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T UnicodeIdentifier(int minLength, int maxLength, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(IdentifierTokenPattern.UnicodeIdentifier(minLength, maxLength), factory, config);
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The regular expression.</param>
		/// <param name="options">The regular expression options. <see cref="RegexOptions.Compiled"/> by default.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(string regex, RegexOptions options = RegexOptions.Compiled, Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RegexTokenPattern(regex, options), factory, config);
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The regular expression.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(string regex, Func<ParsedRuleResult, object?>? factory,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new RegexTokenPattern(regex), factory, config);
		}

		/// <summary>
		/// Adds a whitespace token to the current sequence.
		/// </summary>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Whitespaces(Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new WhitespacesTokenPattern(), factory, config);
		}

		/// <summary>
		/// Adds a end of file (EOF) token to the current sequence.
		/// </summary>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EOF(Func<ParsedRuleResult, object?>? factory = null,
			Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new EOFTokenPattern(), factory, config);
		}

		#region EscapedText

		/// <summary>
		/// Adds an escaped text token to the current sequence with custom escape mappings, forbidden sequences, and string comparer.
		/// </summary>
		/// <param name="escapeMappings">The mappings for escape sequences to their replacements.</param>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedText(IEnumerable<KeyValuePair<string, string>> escapeMappings, IEnumerable<string> forbidden,
			bool allowsEmpty = true, StringComparer? comparer = null,
			Func<ParsedRuleResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(new EscapedTextTokenPattern(escapeMappings, forbidden, allowsEmpty, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double character escaping strategy.
		/// </summary>
		/// <param name="charSource">The source string of characters to be escaped.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleChars(IEnumerable<char> charSource, bool allowsEmpty = true, StringComparer? comparer = null,
			Func<ParsedRuleResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleCharacters(charSource, allowsEmpty, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleSequences(IEnumerable<string> sequences, bool allowsEmpty = true, StringComparer? comparer = null,
			Func<ParsedRuleResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleSequences(sequences, allowsEmpty, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double character escaping strategy.
		/// </summary>
		/// <param name="characters">The source collection of characters to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleChars(params char[] characters)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleCharacters(characters), null, null);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleSequences(params string[] sequences)
		{
			return AddToken(EscapedTextTokenPattern.CreateDoubleSequences(sequences), null, null);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix escaping strategy.
		/// </summary>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(IEnumerable<char> charSource, char prefix, bool allowsEmpty = true, StringComparer? comparer = null,
			Func<ParsedRuleResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(charSource, prefix, allowsEmpty, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(IEnumerable<string> sequences, string prefix, bool allowsEmpty = true, StringComparer? comparer = null,
			Func<ParsedRuleResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(sequences, prefix, allowsEmpty, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix character escaping strategy.
		/// </summary>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="characters">The source collection of characters to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(char prefix, params char[] characters)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(characters, prefix), null, null);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix sequence escaping strategy.
		/// </summary>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(string prefix, params string[] sequences)
		{
			return AddToken(EscapedTextTokenPattern.CreatePrefix(sequences, prefix), null, null);
		}
		/// <summary>
		/// Adds an escaped text token to the current sequence that matches until any of the specified forbidden sequences is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T TextUntil(IEnumerable<string> forbidden, bool allowsEmpty = true, StringComparer? comparer = null,
			Func<ParsedRuleResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreateUntil(forbidden, allowsEmpty, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence that matches until any of the specified forbidden characters is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbiddenChars">The set of forbidden characters that terminate the match.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <param name="factory">The factory function to create a parsed value.</param>
		/// <param name="config">The action to configure the local settings for this token.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T TextUntil(IEnumerable<char> forbiddenChars, bool allowsEmpty = true, StringComparer? comparer = null,
			Func<ParsedRuleResult, object?>? factory = null, Action<ParserLocalSettingsBuilder>? config = null)
		{
			return AddToken(EscapedTextTokenPattern.CreateUntil(forbiddenChars, allowsEmpty, comparer), factory, config);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence that matches until any of the specified forbidden characters is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="characters">The source collection of characters to be forbidden.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T TextUntil(params char[] characters)
		{
			return AddToken(EscapedTextTokenPattern.CreateUntil(characters), null, null);
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence that matches until any of the specified forbidden sequences is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbidden">The array of forbidden sequences that terminate the match.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T TextUntil(params string[] forbidden)
		{
			return AddToken(EscapedTextTokenPattern.CreateUntil(forbidden), null, null);
		}

		#endregion
	}
}