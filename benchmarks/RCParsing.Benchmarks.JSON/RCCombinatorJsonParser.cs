using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Benchmarks.JSON
{
	public class RCCombinatorJsonParser
	{
		static Parser parser;
		static TokenPattern valueTokenPattern;

		public static void FillWithRules(ParserBuilder builder)
		{
			builder.CreateToken("string_inner")
				.Custom((self, input, start, end, parameter, calc) =>
				{
					int pos = start;
					while (pos < end && input[pos] != '"')
						pos++;
					return new ParsedElement(start, pos - start, input.Substring(start, pos - start));
				});

			builder.CreateToken("string")
				.Between(
					b => b.Literal('"'),
					b => b.Token("string_inner"),
					b => b.Literal('"'));

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Integer);

			builder.CreateToken("true")
				.Return(b => b.Literal("true"), true);

			builder.CreateToken("false")
				.Return(b => b.Literal("false"), false);

			builder.CreateToken("null")
				.Return(b => b.Literal("null"), null);

			builder.CreateToken("value")
				.SkipWhitespaces(b =>
					b.Choice(
						c => c.Token("string"),
						c => c.Token("number"),
						c => c.Token("true"),
						c => c.Token("false"),
						c => c.Token("null"),
						c => c.Token("array"),
						c => c.Token("object")
				));

			builder.CreateToken("value_list")
				.ZeroOrMoreSeparated(
					b => b.Token("value"),
					b => b.SkipWhitespaces(b => b.Literal(',')))
				.Pass(v =>
				{
					return v.ToArray();
				});

			builder.CreateToken("array")
				.Between(
					b => b.Literal('['),
					b => b.Token("value_list"),
					b => b.SkipWhitespaces(b => b.Literal(']')));

			builder.CreateToken("pair")
				.SkipWhitespaces(b => b.Token("string"))
				.SkipWhitespaces(b => b.Literal(':'))
				.Token("value")
				.Pass(v =>
				{
					return KeyValuePair.Create((string)v[0], v[2]);
				});

			builder.CreateToken("pair_list")
				.ZeroOrMoreSeparated(
					b => b.Token("pair"),
					b => b.SkipWhitespaces(b => b.Literal(',')))
				.Pass(v =>
				{
					var dict = new Dictionary<string, object>(v.Count);
					foreach (var child in v)
					{
						var kvp = (KeyValuePair<string, object>)child;
						dict.Add(kvp.Key, kvp.Value);
					}
					return dict;
				});

			builder.CreateToken("object")
				.Between(
					b => b.Literal('{'),
					b => b.Token("pair_list"),
					b => b.SkipWhitespaces(b => b.Literal('}')));

			builder.CreateMainRule("content")
				.Token("value");
		}

		static RCCombinatorJsonParser()
		{
			var builder = new ParserBuilder();
			builder.Settings.UseFirstCharacterMatch();
			FillWithRules(builder);
			parser = builder.Build();
			valueTokenPattern = parser.GetTokenPattern("value");
		}

		public static object Parse(string text)
		{
			ParsingError error = new ParsingError(-1, 0);
			return valueTokenPattern.Match(text, 0, text.Length, null, true, ref error).intermediateValue;
		}
	}
}