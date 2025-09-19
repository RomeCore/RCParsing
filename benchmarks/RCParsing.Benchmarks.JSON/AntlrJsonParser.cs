using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Dia2Lib;

namespace RCParsing.Benchmarks.JSON
{
	public static class AntlrJsonParser
	{
		private static class Visitor
		{
			public static string String(ITerminalNode node)
			{
				// I currently don't know how to avoid second string allocation.
				return node.GetText()[1..^1];
			}

			public static object Value(jsonParser.ValueContext ast)
			{
				if (ast.STRING() is ITerminalNode strNode)
					return String(strNode);

				if (ast.NUMBER() is ITerminalNode numNode)
					return int.Parse(numNode.GetText());

				if (ast.TRUE() != null)
					return true;

				if (ast.FALSE() != null)
					return false;

				if (ast.NULL() != null)
					return null;

				if (ast.array() is jsonParser.ArrayContext arrayNode)
					return Array(arrayNode);

				if (ast.@object() is jsonParser.ObjectContext objectNode)
					return Object(objectNode);

				return null;
			}

			public static object[] Array(jsonParser.ArrayContext ast)
			{
				var values = ast.value();
				var result = new object[values.Length];
				for (int i = 0; i < values.Length; i++)
					result[i] = Value(values[i]);
				return result;
			}

			public static KeyValuePair<string, object> Pair(jsonParser.PairContext ast)
			{
				var key = String(ast.STRING());
				var value = Value(ast.value());
				return new KeyValuePair<string, object>(key, value);
			}

			public static Dictionary<string, object> Object(jsonParser.ObjectContext ast)
			{
				var pairs = ast.pair();
				var result = new Dictionary<string, object>(pairs.Length);
				for (int i = 0; i < pairs.Length; i++)
				{
					var pair = Pair(pairs[i]);
					result.Add(pair.Key, pair.Value);
				}
				return result;
			}
		}

		static AntlrJsonParser()
		{
		}

		public static object Parse(string input)
		{
			var charStream = CharStreams.fromString(input);
			var lexer = new jsonLexer(charStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new jsonParser(tokens);
			var value = parser.value();
			return Visitor.Value(value);
		}
	}
}