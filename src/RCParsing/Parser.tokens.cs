using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	public partial class Parser
	{
		/// <summary>
		/// Matches the given input using the specified token identifier and parser context.
		/// </summary>
		/// <returns>A parsed token object containing the result of the match.</returns>
		internal ParsedElement MatchToken(int tokenPatternId, string input, int position,
			int barrierPosition, object? parameter, bool calculateIntermediateValue)
		{
			var tokenPattern = _tokenPatterns[tokenPatternId];
			var error = new ParsingError(-1, 0);
			return tokenPattern.Match(input, position, barrierPosition, parameter,
				calculateIntermediateValue, ref error);
		}

		/// <summary>
		/// Matches the given input using the specified token identifier and parser context.
		/// </summary>
		/// <returns>A parsed token object containing the result of the match.</returns>
		internal ParsedElement MatchToken(int tokenPatternId, string input, int position,
			int barrierPosition, object? parameter, bool calculateIntermediateValue, out ParsingError furthestError)
		{
			var tokenPattern = _tokenPatterns[tokenPatternId];
			furthestError = new ParsingError(-1, 0);
			return tokenPattern.Match(input, position, barrierPosition, parameter,
				calculateIntermediateValue, ref furthestError);
		}



		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="context">The parser context to use for matching.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		/// <exception cref="ParsingException">Thrown if parsing fails.</exception>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, ParserContext context)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, context.parserParameter, true, out var error);

			if (!parsedToken.success && error.position >= 0)
				context.errors.Add(error);
			if (parsedToken.success)
				return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);

			if (error.position >= 0)
				context.errors.Add(error);
			throw new ParsingException(context, error);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Does not throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="context">The parser context to use for matching.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult TryMatchToken(string tokenPatternAlias, ParserContext context)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, context.parserParameter, true, out var error);
			if (!parsedToken.success && error.position >= 0)
				context.errors.Add(error);
			return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Converts intermediate value of the token to <typeparamref name="T"/>.
		/// Throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="context">The parser context to use for matching.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		/// <exception cref="ParsingException">Thrown if parsing fails.</exception>
		public T MatchToken<T>(string tokenPatternAlias, ParserContext context)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, context.parserParameter, true, out var error);
			if (parsedToken.success)
				return (T)parsedToken.intermediateValue;

			if (error.position >= 0)
				context.errors.Add(error);
			throw new ParsingException(context, error);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// if parsing was successful, otherwise returns <see langword="default"/>.
		/// Does not throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="context">The parser context to use for matching.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T? TryMatchToken<T>(string tokenPatternAlias, ParserContext context)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, context.parserParameter, true, out var error);
			if (parsedToken.success)
				return (T)parsedToken.intermediateValue;

			if (error.position >= 0)
				context.errors.Add(error);
			return default;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, string input, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true, out var error);

			if (!parsedToken.success && error.position >= 0)
				context.errors.Add(error);
			if (parsedToken.success)
				return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);

			if (error.position >= 0)
				context.errors.Add(error);
			throw new ParsingException(context, error);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Does not throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult TryMatchToken(string tokenPatternAlias, string input, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true, out var error);
			if (!parsedToken.success && error.position >= 0)
				context.errors.Add(error);
			return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Converts intermediate value of the token to <typeparamref name="T"/>.
		/// Throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T MatchToken<T>(string tokenPatternAlias, string input, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, 0,
				input.Length, parameter, true, out var error);
			if (!parsedToken.success)
				throw new ParsingException(CreateContext(input, parameter), error);
			return (T)parsedToken.intermediateValue;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// if parsing was successful, otherwise returns <see langword="default"/>.
		/// Does not throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T? TryMatchToken<T>(string tokenPatternAlias, string input, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, 0,
				input.Length, parameter, true);
			if (parsedToken.success && parsedToken.intermediateValue is T result)
				return result;
			return default;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, string input, int startIndex, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, startIndex, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true, out var error);

			if (!parsedToken.success && error.position >= 0)
				context.errors.Add(error);
			if (parsedToken.success)
				return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);

			if (error.position >= 0)
				context.errors.Add(error);
			throw new ParsingException(context, error);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Does not throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult TryMatchToken(string tokenPatternAlias, string input, int startIndex, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, startIndex, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true, out var error);
			if (!parsedToken.success && error.position >= 0)
				context.errors.Add(error);
			return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Converts intermediate value of the token to <typeparamref name="T"/>.
		/// Throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T MatchToken<T>(string tokenPatternAlias, string input, int startIndex, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				input.Length, parameter, true, out var error);
			if (!parsedToken.success)
				throw new ParsingException(CreateContext(input, parameter), error);
			return (T)parsedToken.intermediateValue;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// if parsing was successful, otherwise returns <see langword="default"/>.
		/// Does not throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T? TryMatchToken<T>(string tokenPatternAlias, string input, int startIndex, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				input.Length, parameter, true);
			if (parsedToken.success && parsedToken.intermediateValue is T result)
				return result;
			return default;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="length">Number of characters to parse from the input text.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult MatchToken(string tokenPatternAlias, string input, int startIndex, int length, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, startIndex, length, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true, out var error);

			if (!parsedToken.success && error.position >= 0)
				context.errors.Add(error);
			if (parsedToken.success)
				return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);

			if (error.position >= 0)
				context.errors.Add(error);
			throw new ParsingException(context, error);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// </summary>
		/// <remarks>
		/// Does not throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="length">Number of characters to parse from the input text.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public ParsedTokenResult TryMatchToken(string tokenPatternAlias, string input, int startIndex, int length, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var context = CreateContext(input, startIndex, length, parameter);
			var parsedToken = MatchToken(tokenPatternId, context.input, context.position,
				context.maxPosition, parameter, true, out var error);
			if (!parsedToken.success && error.position >= 0)
				context.errors.Add(error);
			return new ParsedTokenResult(null, context, parsedToken, tokenPatternId);
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// </summary>
		/// <remarks>
		/// Converts intermediate value of the token to <typeparamref name="T"/>.
		/// Throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="length">Number of characters to parse from the input text.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T MatchToken<T>(string tokenPatternAlias, string input, int startIndex, int length, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				startIndex + length, parameter, true, out var error);
			if (!parsedToken.success)
				throw new ParsingException(CreateContext(input, parameter), error);
			return (T)parsedToken.intermediateValue;
		}

		/// <summary>
		/// Parses the given input using the specified token pattern alias and input text.
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// </summary>
		/// <remarks>
		/// Converts intermediate value of the token to <typeparamref name="T"/>
		/// if parsing was successful, otherwise returns <see langword="default"/>.
		/// Does not throws an exception if parsing fails.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="length">Number of characters to parse from the input text.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns>A parsed token pattern containing the result of the parse.</returns>
		public T? TryMatchToken<T>(string tokenPatternAlias, string input, int startIndex, int length, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				startIndex + length, parameter, true, out _);
			if (parsedToken.success && parsedToken.intermediateValue is T result)
				return result;
			return default;
		}



		/// <summary>
		/// Checks if the given input matches the specified token pattern by its alias.
		/// </summary>
		/// <remarks>
		/// Does not calculates intermediate value.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns><see langword="true"/> if token matches the input string; otherwise, <see langword="false"/>.</returns>
		public bool MatchesToken(string tokenPatternAlias, string input, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, 0,
				input.Length, parameter, false, out _);
			return parsedToken.success;
		}

		/// <summary>
		/// Checks if the given input matches the specified token pattern by its alias.
		/// </summary>
		/// <remarks>
		/// Does not calculates intermediate value.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="matchedLength">The length of matched token counting from starting index, here it's 0.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns><see langword="true"/> if token matches the input string; otherwise, <see langword="false"/>.</returns>
		public bool MatchesToken(string tokenPatternAlias, string input, out int matchedLength, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, 0,
				input.Length, parameter, false, out _);
			matchedLength = parsedToken.startIndex + parsedToken.length;
			return parsedToken.success;
		}

		/// <summary>
		/// Checks if the given input matches the specified token pattern by its alias.
		/// </summary>
		/// <remarks>
		/// Does not calculates intermediate value.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns><see langword="true"/> if token matches the input string; otherwise, <see langword="false"/>.</returns>
		public bool MatchesToken(string tokenPatternAlias, string input, int startIndex, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				input.Length, parameter, false, out _);
			return parsedToken.success;
		}

		/// <summary>
		/// Checks if the given input matches the specified token pattern by its alias.
		/// </summary>
		/// <remarks>
		/// Does not calculates intermediate value.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="matchedLength">The length of matched token counting from <paramref name="startIndex"/>.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns><see langword="true"/> if token matches the input string; otherwise, <see langword="false"/>.</returns>
		public bool MatchesToken(string tokenPatternAlias, string input, int startIndex, out int matchedLength, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				input.Length, parameter, false, out _);
			matchedLength = parsedToken.startIndex + parsedToken.length - startIndex;
			return parsedToken.success;
		}

		/// <summary>
		/// Checks if the given input matches the specified token pattern by its alias.
		/// </summary>
		/// <remarks>
		/// Does not calculates intermediate value.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="length">Number of characters to parse from the input text.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns><see langword="true"/> if token matches the input string; otherwise, <see langword="false"/>.</returns>
		public bool MatchesToken(string tokenPatternAlias, string input, int startIndex, int length, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				startIndex + length, parameter, false, out _);
			return parsedToken.success;
		}

		/// <summary>
		/// Checks if the given input matches the specified token pattern by its alias.
		/// </summary>
		/// <remarks>
		/// Does not calculates intermediate value.
		/// </remarks>
		/// <param name="tokenPatternAlias">The alias for the token pattern to use.</param>
		/// <param name="input">The input text to parse.</param>
		/// <param name="startIndex">Starting index in the input text to parse.</param>
		/// <param name="length">Number of characters to parse from the input text.</param>
		/// <param name="matchedLength">The length of matched token counting from <paramref name="startIndex"/>.</param>
		/// <param name="parameter">Optional parameter to pass to the parser. Can be used to pass additional information to the custom token patterns.</param>
		/// <returns><see langword="true"/> if token matches the input string; otherwise, <see langword="false"/>.</returns>
		public bool MatchesToken(string tokenPatternAlias, string input, int startIndex, int length, out int matchedLength, object? parameter = null)
		{
			if (!_tokenPatternsAliases.TryGetValue(tokenPatternAlias, out var tokenPatternId))
				throw new ArgumentException("Invalid token pattern alias", nameof(tokenPatternAlias));

			var parsedToken = MatchToken(tokenPatternId, input, startIndex,
				startIndex + length, parameter, false, out _);
			matchedLength = parsedToken.startIndex + parsedToken.length - startIndex;
			return parsedToken.success;
		}
	}
}