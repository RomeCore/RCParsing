using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace RCParsing.Benchmarks.Python
{
	public static class ANTLRPythonParser
	{
		public static void Parse(string input)
		{
			var charStream = CharStreams.fromString(input);
			var lexer = new PythonLexer(charStream);
			var tokens = new CommonTokenStream(lexer);
			var parser = new PythonParser(tokens);
			parser.file_input();
		}
	}
}