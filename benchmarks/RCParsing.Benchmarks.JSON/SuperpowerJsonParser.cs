using System;
using System.Collections.Generic;
using System.Globalization;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;

namespace BenchmarkJSON
{
	public static class SuperpowerJsonParser
	{
		private static readonly TokenListParser<JsonToken, object> JsonNull =
			Token.EqualTo(JsonToken.Null).Value((object)null);

		private static readonly TokenListParser<JsonToken, object> JsonBoolean =
			Token.EqualTo(JsonToken.True).Value((object)true)
				.Or(Token.EqualTo(JsonToken.False).Value((object)false));

		private static readonly TokenListParser<JsonToken, object> JsonNumber =
			Token.EqualTo(JsonToken.Number)
				.Apply(Numerics.DecimalDouble)
				.Select(x => (object)x);

		private static readonly TokenListParser<JsonToken, object> JsonString =
			Token.EqualTo(JsonToken.String)
				.Select(x => (object)x.ToStringValue());

		private static readonly TokenListParser<JsonToken, object> JsonArray =
			from open in Token.EqualTo(JsonToken.LBracket)
			from elements in Parse.Ref(() => JsonValue!)
				.ManyDelimitedBy(Token.EqualTo(JsonToken.Comma))
			from close in Token.EqualTo(JsonToken.RBracket)
			select (object)elements;

		private static readonly TokenListParser<JsonToken, KeyValuePair<string, object>> JsonProperty =
			from name in Token.EqualTo(JsonToken.String).Select(x => x.ToStringValue())
			from colon in Token.EqualTo(JsonToken.Colon)
			from value in Parse.Ref(() => JsonValue!)
			select new KeyValuePair<string, object>(name, value);

		private static readonly TokenListParser<JsonToken, object> JsonObject =
			from open in Token.EqualTo(JsonToken.LBrace)
			from properties in JsonProperty
				.ManyDelimitedBy(Token.EqualTo(JsonToken.Comma))
			from close in Token.EqualTo(JsonToken.RBrace)
			select (object)new Dictionary<string, object>(properties);

		private static readonly TokenListParser<JsonToken, object> JsonValue =
			JsonNull
				.Or(JsonBoolean)
				.Or(JsonNumber)
				.Or(JsonString)
				.Or(JsonArray)
				.Or(JsonObject);

		public enum JsonToken
		{
			LBrace,
			RBrace,
			LBracket,
			RBracket,
			Colon,
			Comma,
			String,
			Number,
			True,
			False,
			Null,
			WhiteSpace
		}

		public static Tokenizer<JsonToken> Tokenizer { get; } = new TokenizerBuilder<JsonToken>()
			.Match(Span.EqualTo("{"), JsonToken.LBrace)
			.Match(Span.EqualTo("}"), JsonToken.RBrace)
			.Match(Span.EqualTo("["), JsonToken.LBracket)
			.Match(Span.EqualTo("]"), JsonToken.RBracket)
			.Match(Span.EqualTo(":"), JsonToken.Colon)
			.Match(Span.EqualTo(","), JsonToken.Comma)
			.Match(Span.EqualTo("true"), JsonToken.True)
			.Match(Span.EqualTo("false"), JsonToken.False)
			.Match(Span.EqualTo("null"), JsonToken.Null)
			.Match(Numerics.Decimal, JsonToken.Number)
			.Match(QuotedString(), JsonToken.String)
			.Ignore(Span.WhiteSpace)
			.Build();

		private static TextParser<TextSpan> QuotedString()
		{
			return Span.EqualTo("\"")
				.IgnoreThen(Character.ExceptIn('"', '\\')
					.Or(Character.EqualTo('\\')
						.IgnoreThen(Character.AnyChar))
					.Many())
				.IgnoreThen(Span.EqualTo("\""));
		}

		public static object ParseJson(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return null;

			try
			{
				var tokens = Tokenizer.Tokenize(input);
				return JsonValue.Parse(tokens);
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Invalid JSON input", nameof(input), ex);
			}
		}
	}
}