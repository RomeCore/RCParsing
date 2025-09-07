using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RCParsing.Benchmarks.JSON
{
	public static class ParlotJsonParser
	{
		private static readonly Parser<object> Json;

		static ParlotJsonParser()
		{
			var jsonValue = Deferred<object>();

			var jsonString = Between(Terms.Char('"'), AnyCharBefore(Terms.Char('"')), Terms.Char('"')).Then<object>(s => s.Span.ToString());
			var jsonNumber = Terms.Number<int>(NumberOptions.Integer).Then<object>();
			var jsonTrue = Terms.Text("true").Then<object>(_ => true);
			var jsonFalse = Terms.Text("false").Then<object>(_ => false);
			var jsonNull = Terms.Text("null").Then<object>(_ => null);

			var jsonArray = Between(SkipWhiteSpace(Terms.Char('[')),
									Separated(SkipWhiteSpace(Terms.Char(',')), jsonValue),
									SkipWhiteSpace(Terms.Char(']')))
							.Then<object>(list => list.ToArray());

			var jsonPair = SkipWhiteSpace(jsonString).And(SkipWhiteSpace(Terms.Char(':'))).And(jsonValue)
									 .Then(t => new KeyValuePair<string, object>((string)t.Item1, t.Item3));

			var jsonObject = Between(SkipWhiteSpace(Terms.Char('{')),
									 Separated(SkipWhiteSpace(Terms.Char(',')), jsonPair),
									 SkipWhiteSpace(Terms.Char('}')))
							.Then<object>(pairs =>
							{
								var dict = new Dictionary<string, object>(pairs.Count);
								foreach (var kv in pairs)
								{
									dict[kv.Key] = kv.Value;
								}
								return dict;
							});

			jsonValue.Parser = SkipWhiteSpace(OneOf(jsonString, jsonNumber, jsonObject, jsonArray, jsonTrue, jsonFalse, jsonNull));

			Json = jsonValue.Compile();
		}

		public static object Parse(string input)
		{
			Json.TryParse(input, out var obj, out var err);
			return obj;
		}
	}
}