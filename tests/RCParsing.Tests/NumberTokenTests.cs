using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests
{
	/// <summary>
	/// Oh yeah, most complex token in the library must have their own tests. Let's see if we can make it work!
	/// </summary>
	public class NumberTokenTests
	{
		[Fact(DisplayName = "Number token matches numeric strings and exposes parsed value as intermediate value")]
		public void Integer()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("integer")
				.Number<int>();
			builder.CreateToken("sinteger")
				.Number<int>(signed: true);
			builder.CreateToken("sshort")
				.Number<short>(signed: true);

			var parser = builder.Build();

			parser.TryMatchToken("integer", "3218a", out var res);
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(3218, res.GetIntermediateValue<int>());

			parser.TryMatchToken("integer", "a3", out res);
			Assert.False(res.Success);

			parser.TryMatchToken("sinteger", "-3218a", out res);
			Assert.True(res.Success);
			Assert.Equal(5, res.Length);
			Assert.Equal(-3218, res.GetIntermediateValue<int>());

			parser.TryMatchToken("sinteger", "-a", out res);
			Assert.False(res.Success);

			parser.TryMatchToken("sshort", "+103", out res);
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(103, res.GetIntermediateValue<short>());
		}

		[Fact(DisplayName = "Number token matches floating-point numbers")]
		public void Float()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("float")
				.Number<float>(NumberFlags.Float);
			builder.CreateToken("scientific")
				.Number<double>(NumberFlags.Scientific);

			var parser = builder.Build();

			// Basic float
			parser.TryMatchToken("float", "3.14abc", out var res);
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(3.14f, res.GetIntermediateValue<float>());

			// Float with implicit fractional part
			parser.TryMatchToken("float", "5.xyz", out res);
			Assert.True(res.Success);
			Assert.Equal(2, res.Length); // "5."
			Assert.Equal(5.0f, res.GetIntermediateValue<float>());

			// Scientific notation
			parser.TryMatchToken("scientific", "1.23e-10end", out res);
			Assert.True(res.Success);
			Assert.Equal(8, res.Length);
			Assert.Equal(1.23e-10, res.GetIntermediateValue<double>(), 1e-8);
		}

		[Fact(DisplayName = "Number token handles implicit parts correctly")]
		public void ImplicitParts()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("strict_float")
				.Number<float>(NumberFlags.StrictFloat);
			builder.CreateToken("lenient_float")
				.Number<float>(NumberFlags.Float);

			var parser = builder.Build();

			// Strict float rejects implicit parts
			parser.TryMatchToken("strict_float", ".5abc", out var res);
			Assert.False(res.Success); // No implicit integer part allowed

			parser.TryMatchToken("strict_float", "5.xyz", out res);
			Assert.True(res.Success);
			Assert.Equal(1, res.Length);
			Assert.Equal(5f, res.GetIntermediateValue<float>());

			// Lenient float accepts implicit parts
			parser.TryMatchToken("lenient_float", ".5abc", out res);
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.Equal(0.5f, res.GetIntermediateValue<float>());

			parser.TryMatchToken("lenient_float", "5.xyz", out res);
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.Equal(5.0f, res.GetIntermediateValue<float>());
		}

		[Fact(DisplayName = "Number token with PreferSimpler type selection")]
		public void PreferSimpler()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("smart_number")
				.Number(NumberType.PreferSimpler, NumberFlags.Float | NumberFlags.Exponent);

			var parser = builder.Build();

			// Integer -> int
			parser.TryMatchToken("smart_number", "42abc", out var res);
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.IsType<int>(res.IntermediateValue);
			Assert.Equal(42, res.GetIntermediateValue<int>());

			// Float -> float
			parser.TryMatchToken("smart_number", "3.14abc", out res);
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.IsType<float>(res.IntermediateValue);
			Assert.Equal(3.14f, res.GetIntermediateValue<float>());

			// Scientific without fractional dot -> int
			parser.TryMatchToken("smart_number", "1e5abc", out res);
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.IsType<int>(res.IntermediateValue);
			Assert.Equal(100000, res.GetIntermediateValue<int>());
		}

		[Fact(DisplayName = "Number token handles exponent backtracking correctly")]
		public void ExponentBacktracking()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("scientific")
				.Number<double>(NumberFlags.Scientific);
			builder.CreateToken("identifier")
				.Identifier();

			var parser = builder.Build();

			// Valid exponent
			parser.TryMatchToken("scientific", "1.5e-10abc", out var res);
			Assert.True(res.Success);
			Assert.Equal(7, res.Length); // "1.5e-10"
			Assert.Equal(1.5e-10, res.GetIntermediateValue<double>());

			// Exponent without digits -> backtrack to float
			parser.TryMatchToken("scientific", "2.5e+abc", out res);
			Assert.True(res.Success);
			Assert.Equal(3, res.Length); // "2.5" (backtracked from exponent)
			Assert.Equal(2.5, res.GetIntermediateValue<double>());

			// Just 'e' without sign -> backtrack to float
			parser.TryMatchToken("scientific", "3.0etest", out res);
			Assert.True(res.Success);
			Assert.Equal(3, res.Length); // "3.0"
			Assert.Equal(3.0, res.GetIntermediateValue<double>());
		}

		[Fact(DisplayName = "Number token with unsigned variants")]
		public void Unsigned()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("uint")
				.Number<int>(NumberFlags.UnsignedInteger);
			builder.CreateToken("ufloat")
				.Number<float>(NumberFlags.UnsignedFloat);

			var parser = builder.Build();

			// Unsigned integer rejects sign
			parser.TryMatchToken("uint", "-123abc", out var res);
			Assert.False(res.Success);

			parser.TryMatchToken("uint", "+123abc", out res);
			Assert.False(res.Success);

			parser.TryMatchToken("uint", "456abc", out res);
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal(456, res.GetIntermediateValue<int>());

			// Unsigned float
			parser.TryMatchToken("ufloat", "-1.5abc", out res);
			Assert.False(res.Success);

			parser.TryMatchToken("ufloat", "2.5abc", out res);
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal(2.5f, res.GetIntermediateValue<float>());
		}

		[Fact(DisplayName = "Number token boundary cases")]
		public void BoundaryCases()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Float | NumberFlags.Exponent);

			var parser = builder.Build();

			// Empty string
			parser.TryMatchToken("number", "", out var res);
			Assert.False(res.Success);

			// Only sign
			parser.TryMatchToken("number", "-", out res);
			Assert.False(res.Success);

			// Only decimal point
			parser.TryMatchToken("number", ".", out res);
			Assert.False(res.Success);

			// Only exponent marker
			parser.TryMatchToken("number", "e", out res);
			Assert.False(res.Success);

			// Valid edge cases
			parser.TryMatchToken("number", ".5abc", out res);
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.Equal(0.5, res.GetIntermediateValue<double>());

			parser.TryMatchToken("number", "5.abc", out res);
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.Equal(5.0, res.GetIntermediateValue<double>());
		}
	}
}