using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sprache;

namespace RCParsing.Benchmarks.JSON
{
	public static class SpracheJsonParser
	{
		private static readonly Parser<object> Null = Parse.String("null").Return((object)null).Token();
		private static readonly Parser<object> Bool = Parse.String("true").Return((object)true).Or(Parse.String("false").Return((object)false)).Token();
		private static readonly Parser<object> Number = Parse.Number.Select(s => (object)int.Parse(s)).Token();

		private static readonly Parser<object> String =
			(from first in Parse.Char('"')
			from content in Parse.CharExcept('"').Many().Text()
			from second in Parse.Char('"')
			select content).Token();

		private static readonly Parser<object> Array =
			from first in Parse.Char('[').Token()
			from content in Parse.Ref(() => Json).DelimitedBy(Parse.Char(',').Token())
			from second in Parse.Char(']').Token()
			select content;

		private static readonly Parser<KeyValuePair<string, object>> Pair =
			from key in String
			from delimiter in Parse.Char(':').Token()
			from value in Parse.Ref(() => Json)
			select KeyValuePair.Create((string)key, value);

		private static readonly Parser<object> Object =
			from first in Parse.Char('{').Token()
			from content in Pair.DelimitedBy(Parse.Char(',').Token())
			from second in Parse.Char('}').Token()
			select content.ToDictionary();

		private static readonly Parser<object> Json = String.Or(Number).Or(Bool).Or(Null).Or(Array).Or(Object);

		public static object ParseJson(string input)
		{
			var result = Json.Parse(input);
			return result;
		}
	}
}