using System.Drawing;
using RCParsing;
using RCParsing.Building;
using SyntaxColorizer;
using static Colorful.Console;

var parser = ParserProvider.CreateParser();
var examples = CodeProvider.GetCodeExamples();

int current = 0;
while (true)
{
	var code = examples[current];

	try
	{
		var ast = parser.Parse(code);
		var tokens = ast.GetJoinedChildren().ToList();
		int lastIndex = 0;

		foreach (var token in tokens)
		{
			int startIndex = token.StartIndex;
			int length = token.Length;

			if (lastIndex < startIndex)
				Write(code[lastIndex..startIndex], ColorProvider.GetColorForComment());
			lastIndex = startIndex + length;

			var color = ColorProvider.GetColorForToken(token);
			Write(token.Text, color);
		}

		if (lastIndex < code.Length)
			Write(code[lastIndex..], ColorProvider.GetColorForComment());
	}
	catch (ParsingException ex)
	{
		Write(code);
		WriteLine();
		WriteLine();
		WriteLine(ex.Message);
	}

	WriteLine();
	WriteLine();
	WriteLine("Press any key to see the next example...");
	ReadKey();
	Clear();

	current++;
	if (current >= examples.Length)
		current = 0;
}