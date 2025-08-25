using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace RCParsing.Benchmarks.JSON
{
	public static class PidginJsonParser
	{
		private static readonly Parser<char, double> Number =
			DecimalNum.Select(x => (double)x)
			.Or(LongNum.Select(x => (double)x))
			.Before(SkipWhitespaces);

		private static readonly Parser<char, string> String =
			Token(c => c != '"')
			.ManyString()
			.Between(Char('"'))
			.Before(SkipWhitespaces);

		private static readonly Parser<char, object> JsonString =
			String.Select(x => (object)x);

		private static readonly Parser<char, object> JsonNumber =
			Number.Select(x => (object)x);

		private static readonly Parser<char, object> JsonBool =
			String("true").Then(Return<object>(true))
			.Or(String("false").Then(Return<object>(false)))
			.Before(SkipWhitespaces);

		private static readonly Parser<char, object> JsonNull =
			String("null").Then(Return<object>(null))
			.Before(SkipWhitespaces);

		private static readonly Parser<char, object> JsonArray =
			Rec(() => Json)
			.Separated(Char(',').Before(SkipWhitespaces))
			.Between(Char('[').Before(SkipWhitespaces), Char(']').Before(SkipWhitespaces))
			.Select(x => (object)x.ToArray());

		private static readonly Parser<char, KeyValuePair<string, object>> JsonProperty =
			from name in String
			from colon in Char(':').Before(SkipWhitespaces)
			from value in Rec(() => Json)
			select new KeyValuePair<string, object>(name, value);

		private static readonly Parser<char, object> JsonObject =
			JsonProperty
			.Separated(Char(',').Before(SkipWhitespaces))
			.Between(Char('{').Before(SkipWhitespaces), Char('}').Before(SkipWhitespaces))
			.Select(x =>
			{
				var dict = new Dictionary<string, object>();
				foreach (var kvp in x)
				{
					dict.Add(kvp.Key, kvp.Value);
				}
				return (object)dict;
			});

		private static readonly Parser<char, object> Json =
			JsonString
			.Or(JsonNumber)
			.Or(JsonBool)
			.Or(JsonNull)
			.Or(JsonArray)
			.Or(JsonObject);

		public static object Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return null;
			}

			try
			{
				var result = Json.Before(End).ParseOrThrow(input);
				return result;
			}
			catch (Exception ex)
			{
				throw new ArgumentException("Invalid JSON input", nameof(input), ex);
			}
		}
	}
}