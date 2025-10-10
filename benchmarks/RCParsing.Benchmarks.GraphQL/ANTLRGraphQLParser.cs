using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace RCParsing.Benchmarks.GraphQL
{
	public static class ANTLRGraphQLParser
	{
		public static void Parse(string input)
		{
			var charStream = CharStreams.fromString(input);
			var lexer = new GraphQLLexer(charStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new GraphQLParser(tokens);
			parser.document();
		}
	}
}