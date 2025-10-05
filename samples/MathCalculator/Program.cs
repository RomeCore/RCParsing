using MathCalculator;

while (true)
{
	Console.WriteLine("Write your math expression:");
	var expr = Console.ReadLine();

	try
	{
		var value = MathParser.ParseExpression(expr ?? string.Empty);
		Console.WriteLine($"Result: " + value);
	}
	catch (Exception ex)
	{
		Console.WriteLine(ex.ToString());
	}

	Console.WriteLine();
	Console.WriteLine("Press key to parse another expression.");
	Console.ReadKey();
	Console.Clear();
}