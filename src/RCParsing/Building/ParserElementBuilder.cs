using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RCParsing.Building.TokenPatterns;
using RCParsing.TokenPatterns;

namespace RCParsing.Building
{
	/// <summary>
	/// The base class for all parser element builders.
	/// </summary>
	public abstract partial class ParserElementBuilder<T>
	{
		protected static object? DefaultFactory_Token(ParsedRuleResultBase r) => r.IntermediateValue;

		/// <summary>
		/// Gets the master parser builder associated with this rule builder, if any.
		/// </summary>
		public ParserBuilder? ParserBuilder { get; }

		/// <summary>
		/// Initializes a new instance of <see cref="ParserElementBuilder{T}"/> class.
		/// </summary>
		public ParserElementBuilder()
		{
			ParserBuilder = null;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="ParserElementBuilder{T}"/> class.
		/// </summary>
		/// <param name="parserBuilder">The master parser builder associated with this builder.</param>
		public ParserElementBuilder(ParserBuilder? parserBuilder)
		{
			ParserBuilder = parserBuilder;
		}

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
		/// <returns>Current instance for method chaining.</returns>
		public abstract T Token(TokenPattern token);

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
		/// Returns: <br/>
		/// - <see cref="ParsedElement"/>: The parsed element containing the result of the match.
		/// </param>
		/// <param name="stringRepresentation">The string representation of custom token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Custom(Func<CustomTokenPattern, string, int, int, ParsedElement> matchFunction,
			string stringRepresentation = "custom")
		{
			return Token(new CustomTokenPattern((self, i, s, e, p, c) => matchFunction(self, i, s, e),
				stringRepresentation));
		}

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
		/// <returns>Current instance for method chaining.</returns>
		public T Custom(Func<CustomTokenPattern, string, int, int, object?, ParsedElement> matchFunction,
			string stringRepresentation = "custom")
		{
			return Token(new CustomTokenPattern((self, i, s, e, p, c) => matchFunction(self, i, s, e, p),
				stringRepresentation));
		}

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
		/// - <see cref="bool"/>: Whether to calculate intermediate value. <br/>
		/// Returns: <br/>
		/// - <see cref="ParsedElement"/>: The parsed element containing the result of the match.
		/// </param>
		/// <param name="stringRepresentation">The string representation of custom token pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Custom(Func<CustomTokenPattern, string, int, int, object?, bool, ParsedElement> matchFunction,
			string stringRepresentation = "custom")
		{
			return Token(new CustomTokenPattern(matchFunction, stringRepresentation));
		}

		/// <summary>
		/// Adds a literal char token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal character.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(char literal)
		{
			return Token(new LiteralCharTokenPattern(literal));
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal character.</param>
		/// <param name="comparison">The string comparison to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(char literal, StringComparison comparison)
		{
			return Token(new LiteralCharTokenPattern(literal, comparison));
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal string.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(string literal)
		{
			return Token(new LiteralTokenPattern(literal));
		}

		/// <summary>
		/// Adds a literal token to the current sequence.
		/// </summary>
		/// <param name="literal">The literal string.</param>
		/// <param name="comparison">The string comparison to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Literal(string literal, StringComparison comparison)
		{
			return Token(new LiteralTokenPattern(literal, comparison));
		}

		/// <summary>
		/// Adds a character predicate token to the current sequence.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Char(Func<char, bool> charPredicate)
		{
			return Token(new CharacterTokenPattern(charPredicate));
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence with specified minimum occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="min">The minimum inclusive number of characters to match.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Chars(Func<char, bool> charPredicate, int min)
		{
			return Token(new RepeatCharactersTokenPattern(charPredicate, min, -1));
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence with specified minimum and maximum occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <param name="min">The minimum inclusive number of characters to match.</param>
		/// <param name="max">The maximum inclusive number of characters to match. -1 means no limit.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Chars(Func<char, bool> charPredicate, int min, int max)
		{
			return Token(new RepeatCharactersTokenPattern(charPredicate, min, max));
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence that matches zero or more occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T ZeroOrMoreChars(Func<char, bool> charPredicate)
		{
			return Token(new RepeatCharactersTokenPattern(charPredicate, 0, -1));
		}

		/// <summary>
		/// Adds a repeatable character predicate token to the current sequence that matches one or more occurrences.
		/// </summary>
		/// <param name="charPredicate">The character predicate to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T OneOrMoreChars(Func<char, bool> charPredicate)
		{
			return Token(new RepeatCharactersTokenPattern(charPredicate, 1, -1));
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(params string[] literals)
		{
			return Token(new LiteralChoiceTokenPattern(literals));
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(IEnumerable<string> literals)
		{
			return Token(new LiteralChoiceTokenPattern(literals));
		}

		/// <summary>
		/// Adds a literal choice token to the current sequence.
		/// </summary>
		/// <param name="literals">The literal strings set.</param>
		/// <param name="comparer">The optional string comparer to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T LiteralChoice(IEnumerable<string> literals, StringComparer? comparer)
		{
			return Token(new LiteralChoiceTokenPattern(literals, comparer));
		}

		/// <summary>
		/// Adds an identifier token to the current sequence.
		/// </summary>
		/// <param name="startPredicate">The predicate to use for the first character of the identifier.</param>
		/// <param name="continuePredicate">The predicate to use for the remaining characters of the identifier.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(Func<char, bool> startPredicate, Func<char, bool> continuePredicate)
		{
			return Token(new IdentifierTokenPattern(startPredicate, continuePredicate));
		}

		/// <summary>
		/// Adds an identifier token to the current sequence.
		/// </summary>
		/// <param name="startPredicate">The predicate to use for the first character of the identifier.</param>
		/// <param name="continuePredicate">The predicate to use for the remaining characters of the identifier.</param>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(Func<char, bool> startPredicate, Func<char, bool> continuePredicate, int minLength)
		{
			return Token(new IdentifierTokenPattern(startPredicate, continuePredicate, minLength));
		}

		/// <summary>
		/// Adds an identifier token to the current sequence.
		/// </summary>
		/// <param name="startPredicate">The predicate to use for the first character of the identifier.</param>
		/// <param name="continuePredicate">The predicate to use for the remaining characters of the identifier.</param>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="maxLength">The maximum length of the identifier. Default is -1 (no limit).</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(Func<char, bool> startPredicate, Func<char, bool> continuePredicate,
			int minLength, int maxLength)
		{
			return Token(new IdentifierTokenPattern(startPredicate, continuePredicate, minLength, maxLength));
		}

		/// <summary>
		/// Adds an ASCII identifier token to the current sequence.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier()
		{
			return Token(IdentifierTokenPattern.AsciiIdentifier());
		}

		/// <summary>
		/// Adds an ASCII identifier token to the current sequence.
		/// </summary>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(int minLength)
		{
			return Token(IdentifierTokenPattern.AsciiIdentifier(minLength));
		}

		/// <summary>
		/// Adds an ASCII identifier token to the current sequence.
		/// </summary>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="maxLength">The maximum length of the identifier. Default is -1 (no limit).</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Identifier(int minLength, int maxLength)
		{
			return Token(IdentifierTokenPattern.AsciiIdentifier(minLength, maxLength));
		}

		/// <summary>
		/// Adds an Unicode identifier token to the current sequence.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public T UnicodeIdentifier()
		{
			return Token(IdentifierTokenPattern.UnicodeIdentifier());
		}

		/// <summary>
		/// Adds an Unicode identifier token to the current sequence.
		/// </summary>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T UnicodeIdentifier(int minLength)
		{
			return Token(IdentifierTokenPattern.UnicodeIdentifier(minLength));
		}

		/// <summary>
		/// Adds an Unicode identifier token to the current sequence.
		/// </summary>
		/// <param name="minLength">The minimum length of the identifier. Default is 1.</param>
		/// <param name="maxLength">The maximum length of the identifier. Default is -1 (no limit).</param>
		/// <returns>Current instance for method chaining.</returns>
		public T UnicodeIdentifier(int minLength, int maxLength)
		{
			return Token(IdentifierTokenPattern.UnicodeIdentifier(minLength, maxLength));
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The constructed regular expression, it's recommended to prepend the '\G' into a pattern.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(Regex regex)
		{
			return Token(new RegexTokenPattern(regex));
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The regular expression.</param>
		/// <param name="options">The regular expression options. <see cref="RegexOptions.Compiled"/> by default.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(string regex, RegexOptions options = RegexOptions.Compiled)
		{
			return Token(new RegexTokenPattern(regex, options));
		}

		/// <summary>
		/// Adds a regular expression token to the current sequence.
		/// </summary>
		/// <param name="regex">The regular expression.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Regex(string regex)
		{
			return Token(new RegexTokenPattern(regex));
		}

		/// <summary>
		/// Adds a spaces token to the current sequence.
		/// Matches one or more of ' ' or '\t' characters.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public T Spaces()
		{
			return Token(new SpacesTokenPattern());
		}

		/// <summary>
		/// Adds a whitespaces token to the current sequence.
		/// Matches one or more of ' ', '\t', '\n' and '\n' characters.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public T Whitespaces()
		{
			return Token(new WhitespacesTokenPattern());
		}

		/// <summary>
		/// Adds a newline token to the current sequence.
		/// Matches a newline, the '\r\n', '\r' or '\n' sequence.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public T Newline()
		{
			return Token(new NewlineTokenPattern());
		}

		/// <summary>
		/// Adds a end of file (EOF) token to the current sequence.
		/// </summary>
		/// <returns>Current instance for method chaining.</returns>
		public T EOF()
		{
			return Token(new EOFTokenPattern());
		}

		private static NumberType NumberTypeFromCLR(Type type)
		{
			if (type == null)
				return NumberType.PreferSimpler;

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
					return NumberType.Byte;
				case TypeCode.SByte:
					return NumberType.SignedByte;
				case TypeCode.UInt16:
					return NumberType.UnsignedShort;
				case TypeCode.Int16:
					return NumberType.Short;
				case TypeCode.UInt32:
					return NumberType.UnsignedInteger;
				case TypeCode.Int32:
					return NumberType.Integer;
				case TypeCode.UInt64:
					return NumberType.UnsignedLong;
				case TypeCode.Int64:
					return NumberType.Long;
				case TypeCode.Single:
					return NumberType.Float;
				case TypeCode.Double:
					return NumberType.Double;
				case TypeCode.Decimal:
					return NumberType.Decimal;
				default:
					throw new ArgumentException("Unsupported number type: " + type);
			}
		}

		private static NumberFlags NumberFlagsFromCLR(Type type)
		{
			if (type == null)
				return NumberFlags.None;

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return NumberFlags.UnsignedInteger;
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					return NumberFlags.Integer;
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return NumberFlags.StrictFloat;
				default:
					throw new ArgumentException("Unsupported number type: " + type);
			}
		}

		private static NumberFlags NumberFlagsFromCLR(Type type, bool signed)
		{
			if (type == null)
				return NumberFlags.None;

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					return signed ? NumberFlags.Integer : NumberFlags.UnsignedInteger;
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return signed ? NumberFlags.StrictFloat : NumberFlags.StrictUnsignedFloat;
				default:
					throw new ArgumentException("Unsupported number type: " + type);
			}
		}

		/// <summary>
		/// Adds a number token to the current sequence.
		/// </summary>
		/// <remarks>
		/// <see cref="NumberFlags.Integer"/> or
		/// <see cref="NumberFlags.UnsignedInteger"/>, based on <paramref name="signed"/>
		/// will be used here, intermediate value will be converted to <see cref="int"/>.
		/// </remarks>
		/// <param name="signed">Whether the number can have a sign.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Number(bool signed = false)
		{
			return Token(new NumberTokenPattern(NumberType.Integer,
				signed ? NumberFlags.Integer : NumberFlags.UnsignedInteger));
		}

		/// <summary>
		/// Adds a number token to the current sequence.
		/// </summary>
		/// <remarks>
		/// Intemediate value will be converted to <see cref="float"/> if original string has decimal point, otherwise to <see cref="int"/>.
		/// </remarks>
		/// <param name="flags">The number flags to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Number(NumberFlags flags)
		{
			return Token(new NumberTokenPattern(NumberType.PreferSimpler, flags));
		}

		/// <summary>
		/// Adds a number token to the current sequence.
		/// </summary>
		/// <param name="flags">The number flags to use.</param>
		/// <param name="type">The number type to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Number(NumberType type, NumberFlags flags)
		{
			return Token(new NumberTokenPattern(type, flags));
		}

		/// <summary>
		/// Adds a number token to the current sequence.
		/// </summary>
		/// <remarks>
		/// Flags and intermediate value conversion type will be inferred from <typeparamref name="TNum"/> type.
		/// </remarks>
		/// <returns>Current instance for method chaining.</returns>
		public T Number<TNum>()
		{
			var type = NumberTypeFromCLR(typeof(TNum));
			var flags = NumberFlagsFromCLR(typeof(TNum));
			return Token(new NumberTokenPattern(type, flags));
		}

		/// <summary>
		/// Adds a number token to the current sequence.
		/// </summary>
		/// <remarks>
		/// Flags and intermediate value conversion type will be inferred from <typeparamref name="TNum"/> type.
		/// </remarks>
		/// <param name="signed">Whether the number can have a sign.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Number<TNum>(bool signed)
		{
			var type = NumberTypeFromCLR(typeof(TNum));
			var flags = NumberFlagsFromCLR(typeof(TNum), signed);
			return Token(new NumberTokenPattern(type, flags));
		}

		/// <summary>
		/// Adds a number token to the current sequence.
		/// </summary>
		/// <remarks>
		/// Intermediate value conversion type will be inferred from <typeparamref name="TNum"/> type.
		/// </remarks>
		/// <param name="flags">The number flags to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T Number<TNum>(NumberFlags flags)
		{
			var type = NumberTypeFromCLR(typeof(TNum));
			return Token(new NumberTokenPattern(type, flags));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with custom escape mappings, forbidden sequences, and string comparer.
		/// </summary>
		/// <param name="escapeMappings">The mappings for escape sequences to their replacements.</param>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedText(IEnumerable<KeyValuePair<string, string>> escapeMappings, IEnumerable<string> forbidden,
			bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return Token(new EscapedTextTokenPattern(escapeMappings, forbidden, allowsEmpty, comparer));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double character escaping strategy.
		/// </summary>
		/// <param name="charSource">The source string of characters to be escaped.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleChars(IEnumerable<char> charSource, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return Token(EscapedTextTokenPattern.CreateDoubleCharacters(charSource, allowsEmpty, comparer));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleSequences(IEnumerable<string> sequences, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return Token(EscapedTextTokenPattern.CreateDoubleSequences(sequences, allowsEmpty, comparer));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double character escaping strategy.
		/// </summary>
		/// <param name="characters">The source collection of characters to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleChars(params char[] characters)
		{
			return Token(EscapedTextTokenPattern.CreateDoubleCharacters(characters));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with double sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextDoubleSequences(params string[] sequences)
		{
			return Token(EscapedTextTokenPattern.CreateDoubleSequences(sequences));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix escaping strategy.
		/// </summary>
		/// <param name="charSource">The source collection (or <see cref="string"/>) of characters to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(IEnumerable<char> charSource, char prefix, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return Token(EscapedTextTokenPattern.CreatePrefix(charSource, prefix, allowsEmpty, comparer));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix sequence escaping strategy.
		/// </summary>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(IEnumerable<string> sequences, string prefix, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return Token(EscapedTextTokenPattern.CreatePrefix(sequences, prefix, allowsEmpty, comparer));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix character escaping strategy.
		/// </summary>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="characters">The source collection of characters to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(char prefix, params char[] characters)
		{
			return Token(EscapedTextTokenPattern.CreatePrefix(characters, prefix));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence with prefix sequence escaping strategy.
		/// </summary>
		/// <param name="prefix">The prefix used for escaping.</param>
		/// <param name="sequences">The source collection of sequences to be escaped.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T EscapedTextPrefix(string prefix, params string[] sequences)
		{
			return Token(EscapedTextTokenPattern.CreatePrefix(sequences, prefix));
		}
		/// <summary>
		/// Adds an escaped text token to the current sequence that matches until any of the specified forbidden sequences is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbidden">The set of forbidden sequences that terminate the match.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T TextUntil(IEnumerable<string> forbidden, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return Token(EscapedTextTokenPattern.CreateUntil(forbidden, allowsEmpty, comparer));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence that matches until any of the specified forbidden characters is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbiddenChars">The set of forbidden characters that terminate the match.</param>
		/// <param name="allowsEmpty">Indicates whether an empty string is allowed as a match.</param>
		/// <param name="comparer">The string comparer to use.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T TextUntil(IEnumerable<char> forbiddenChars, bool allowsEmpty = true, StringComparer? comparer = null)
		{
			return Token(EscapedTextTokenPattern.CreateUntil(forbiddenChars, allowsEmpty, comparer));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence that matches until any of the specified forbidden characters is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="characters">The source collection of characters to be forbidden.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T TextUntil(params char[] characters)
		{
			return Token(EscapedTextTokenPattern.CreateUntil(characters));
		}

		/// <summary>
		/// Adds an escaped text token to the current sequence that matches until any of the specified forbidden sequences is encountered,
		/// with no escape sequences defined.
		/// </summary>
		/// <param name="forbidden">The array of forbidden sequences that terminate the match.</param>
		/// <returns>Current instance for method chaining.</returns>
		public T TextUntil(params string[] forbidden)
		{
			return Token(EscapedTextTokenPattern.CreateUntil(forbidden));
		}
	}
}