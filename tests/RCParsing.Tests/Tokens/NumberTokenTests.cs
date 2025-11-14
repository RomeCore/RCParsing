using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCParsing.Building;
using RCParsing.TokenPatterns;

namespace RCParsing.Tests.Tokens
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

			var res = parser.TryMatchToken("integer", "3218a");
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(3218, res.GetIntermediateValue<int>());

			res = parser.TryMatchToken("integer", "a3");
			Assert.False(res.Success);

			res = parser.TryMatchToken("sinteger", "-3218a");
			Assert.True(res.Success);
			Assert.Equal(5, res.Length);
			Assert.Equal(-3218, res.GetIntermediateValue<int>());

			res = parser.TryMatchToken("sinteger", "-a");
			Assert.False(res.Success);

			res = parser.TryMatchToken("sshort", "+103");
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
			var res = parser.TryMatchToken("float", "3.14abc");
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(3.14f, res.GetIntermediateValue<float>());

			// Float with implicit fractional part
			res = parser.TryMatchToken("float", "5.xyz");
			Assert.True(res.Success);
			Assert.Equal(2, res.Length); // "5."
			Assert.Equal(5.0f, res.GetIntermediateValue<float>());

			// Scientific notation
			res = parser.TryMatchToken("scientific", "1.23e-10end");
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
			var res = parser.TryMatchToken("strict_float", ".5abc");
			Assert.False(res.Success); // No implicit integer part allowed

			res = parser.TryMatchToken("strict_float", "5.xyz");
			Assert.True(res.Success);
			Assert.Equal(1, res.Length);
			Assert.Equal(5f, res.GetIntermediateValue<float>());

			// Lenient float accepts implicit parts
			res = parser.TryMatchToken("lenient_float", ".5abc");
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.Equal(0.5f, res.GetIntermediateValue<float>());

			res = parser.TryMatchToken("lenient_float", "5.xyz");
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
			var res = parser.TryMatchToken("smart_number", "42abc");
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.IsType<int>(res.IntermediateValue);
			Assert.Equal(42, res.GetIntermediateValue<int>());

			// Float -> float
			res = parser.TryMatchToken("smart_number", "3.14abc");
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.IsType<float>(res.IntermediateValue);
			Assert.Equal(3.14f, res.GetIntermediateValue<float>());

			// Scientific without fractional dot -> int
			res = parser.TryMatchToken("smart_number", "1e5abc");
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
			var res = parser.TryMatchToken("scientific", "1.5e-10abc");
			Assert.True(res.Success);
			Assert.Equal(7, res.Length); // "1.5e-10"
			Assert.Equal(1.5e-10, res.GetIntermediateValue<double>());

			// Exponent without digits -> backtrack to float
			res = parser.TryMatchToken("scientific", "2.5e+abc");
			Assert.True(res.Success);
			Assert.Equal(3, res.Length); // "2.5" (backtracked from exponent)
			Assert.Equal(2.5, res.GetIntermediateValue<double>());

			// Just 'e' without sign -> backtrack to float
			res = parser.TryMatchToken("scientific", "3.0etest");
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
				.Number<float>(NumberFlags.UnsignedScientific);

			var parser = builder.Build();

			// Unsigned integer rejects sign
			var res = parser.TryMatchToken("uint", "-123abc");
			Assert.False(res.Success);

			res = parser.TryMatchToken("uint", "+123abc");
			Assert.False(res.Success);

			res = parser.TryMatchToken("uint", "456abc");
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal(456, res.GetIntermediateValue<int>());

			// Unsigned float
			res = parser.TryMatchToken("ufloat", "-1.5abc");
			Assert.False(res.Success);

			res = parser.TryMatchToken("ufloat", "2.5abc");
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal(2.5f, res.GetIntermediateValue<float>());
		}
		
		[Fact(DisplayName = "Number token with unsigned variants and group separators")]
		public void Unsigned_Groups()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("uint")
				.Number<int>(NumberFlags.UnsignedInteger | NumberFlags.GroupSeparators, groupSeparator: '\'');
			builder.CreateToken("ufloat")
				.Number<float>(NumberFlags.UnsignedScientific | NumberFlags.GroupSeparators, decimalPoint: ',', groupSeparator: '\'');

			var parser = builder.Build();

			// Unsigned integer rejects sign
			var res = parser.TryMatchToken("uint", "-123abc");
			Assert.False(res.Success);

			res = parser.TryMatchToken("uint", "+123abc");
			Assert.False(res.Success);

			res = parser.TryMatchToken("uint", "45'67abc");
			Assert.True(res.Success);
			Assert.Equal(5, res.Length);
			Assert.Equal(4567, res.GetIntermediateValue<int>());

			// Unsigned float
			res = parser.TryMatchToken("ufloat", "-1'0,5'4abc");
			Assert.False(res.Success);

			res = parser.TryMatchToken("ufloat", "2'0,5''7abc");
			Assert.True(res.Success);
			Assert.Equal(8, res.Length);
			Assert.Equal(20.57f, res.GetIntermediateValue<float>());
		}

		[Fact(DisplayName = "Number token boundary cases")]
		public void BoundaryCases()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("number")
				.Number<double>(NumberFlags.Float | NumberFlags.Exponent);

			var parser = builder.Build();

			// Empty string
			var res = parser.TryMatchToken("number", "");
			Assert.False(res.Success);

			// Only sign
			res = parser.TryMatchToken("number", "-");
			Assert.False(res.Success);

			// Only decimal point
			res = parser.TryMatchToken("number", ".");
			Assert.False(res.Success);

			// Only exponent marker
			res = parser.TryMatchToken("number", "e");
			Assert.False(res.Success);

			// Valid edge cases
			res = parser.TryMatchToken("number", ".5abc");
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.Equal(0.5, res.GetIntermediateValue<double>());

			res = parser.TryMatchToken("number", "5.abc");
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.Equal(5.0, res.GetIntermediateValue<double>());
		}

		[Fact(DisplayName = "IntegerNumber token matches numeric strings with different bases")]
		public void IntegerNumber_WithDifferentBases()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("binary")
				.IntegerNumber<int>(2);
			builder.CreateToken("hex")
				.IntegerNumber<int>(16);
			builder.CreateToken("octal")
				.IntegerNumber<int>(signed: false, 8);

			var parser = builder.Build();

			// Binary
			var res = parser.TryMatchToken("binary", "10101abc");
			Assert.True(res.Success);
			Assert.Equal(5, res.Length);
			Assert.Equal(21, res.GetIntermediateValue<int>());

			// Hexadecimal
			res = parser.TryMatchToken("hex", "FF|abc");
			Assert.True(res.Success);
			Assert.Equal(2, res.Length);
			Assert.Equal(255, res.GetIntermediateValue<int>());

			// Octal
			res = parser.TryMatchToken("octal", "755abc");
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal(493, res.GetIntermediateValue<int>());
		}

		[Fact(DisplayName = "IntegerNumber token with base mappings")]
		public void IntegerNumber_WithBaseMappings()
		{
			var builder = new ParserBuilder();

			var baseMappings = new Dictionary<char, int>
			{
				['b'] = 2,
				['o'] = 8,
				['x'] = 16
			};

			builder.CreateToken("mapped")
				.IntegerNumber<int>(10, baseMappings);

			var parser = builder.Build();

			// Default base (decimal)
			var res = parser.TryMatchToken("mapped", "123abc");
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal(123, res.GetIntermediateValue<int>());

			// Binary mapping
			res = parser.TryMatchToken("mapped", "0b101abc");
			Assert.True(res.Success);
			Assert.Equal(5, res.Length);
			Assert.Equal(5, res.GetIntermediateValue<int>());

			// Octal mapping
			res = parser.TryMatchToken("mapped", "0o77abc");
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(63, res.GetIntermediateValue<int>());

			// Hexadecimal mapping
			res = parser.TryMatchToken("mapped", "0x1F|abc");
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(31, res.GetIntermediateValue<int>());
		}

		[Fact(DisplayName = "IntegerNumber token handles signed and unsigned correctly")]
		public void IntegerNumber_SignedUnsigned()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("signed_int")
				.IntegerNumber<int>(signed: true);
			builder.CreateToken("unsigned_int")
				.IntegerNumber<uint>(signed: false);

			var parser = builder.Build();

			// Signed accepts negative numbers
			var res = parser.TryMatchToken("signed_int", "-123abc");
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(-123, res.GetIntermediateValue<int>());

			// Unsigned rejects negative numbers
			res = parser.TryMatchToken("unsigned_int", "-123abc");
			Assert.False(res.Success);

			// Unsigned accepts positive numbers
			res = parser.TryMatchToken("unsigned_int", "456abc");
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal(456u, res.GetIntermediateValue<uint>());
		}

		[Fact(DisplayName = "IntegerNumber token with group separators")]
		public void IntegerNumber_WithGroupSeparators()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("grouped")
				.IntegerNumber<long>(IntegerNumberFlags.GroupSeparators, groupSeparator: '_');
			builder.CreateToken("grouped_hex")
				.IntegerNumber<long>(IntegerNumberFlags.GroupSeparators, 16, groupSeparator: '_');

			var parser = builder.Build();

			// Decimal with groups
			var res = parser.TryMatchToken("grouped", "1_000_000abc");
			Assert.True(res.Success);
			Assert.Equal(9, res.Length);
			Assert.Equal(1000000L, res.GetIntermediateValue<long>());

			// Hexadecimal with groups
			res = parser.TryMatchToken("grouped_hex", "FF_FF_FFzdb");
			Assert.True(res.Success);
			Assert.Equal(8, res.Length);
			Assert.Equal(0xFFFFFFL, res.GetIntermediateValue<long>());
		}

		[Fact(DisplayName = "IntegerNumber token boundary cases")]
		public void IntegerNumber_BoundaryCases()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("int")
				.IntegerNumber<int>();
			builder.CreateToken("uint")
				.IntegerNumber<uint>();

			var parser = builder.Build();

			// Empty string
			var res = parser.TryMatchToken("int", "");
			Assert.False(res.Success);

			// Only sign
			res = parser.TryMatchToken("int", "-");
			Assert.False(res.Success);

			// Only base prefix
			res = parser.TryMatchToken("int", "0x");
			Assert.True(res.Success);
			Assert.Equal(1, res.Length);

			// Valid edge case - single digit
			res = parser.TryMatchToken("int", "0abc");
			Assert.True(res.Success);
			Assert.Equal(1, res.Length);
			Assert.Equal(0, res.GetIntermediateValue<int>());

			// Maximum value
			res = parser.TryMatchToken("int", "2147483647abc");
			Assert.True(res.Success);
			Assert.Equal(10, res.Length);
			Assert.Equal(int.MaxValue, res.GetIntermediateValue<int>());

			// Over maximum value
			res = parser.TryMatchToken("int", "2147483648abc");
			Assert.False(res.Success);
		}

		[Fact(DisplayName = "IntegerNumber token with different numeric types")]
		public void IntegerNumber_DifferentTypes()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("byte_num")
				.IntegerNumber<byte>();
			builder.CreateToken("short_num")
				.IntegerNumber<short>(signed: true);
			builder.CreateToken("long_num")
				.IntegerNumber<long>();

			var parser = builder.Build();

			// Byte
			var res = parser.TryMatchToken("byte_num", "255abc");
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal((byte)255, res.GetIntermediateValue<byte>());

			// Short
			res = parser.TryMatchToken("short_num", "-32768abc");
			Assert.True(res.Success);
			Assert.Equal(6, res.Length);
			Assert.Equal(-32768, res.GetIntermediateValue<short>());

			// Long
			res = parser.TryMatchToken("long_num", "9223372036854775807abc");
			Assert.True(res.Success);
			Assert.Equal(19, res.Length);
			Assert.Equal(long.MaxValue, res.GetIntermediateValue<long>());
		}

		[Fact(DisplayName = "IntegerNumber token handles invalid base characters")]
		public void IntegerNumber_InvalidBaseCharacters()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("binary")
				.IntegerNumber<int>(2);
			builder.CreateToken("decimal")
				.IntegerNumber<int>(10);

			var parser = builder.Build();

			// Binary with invalid digit
			var res = parser.TryMatchToken("binary", "102abc");
			Assert.True(res.Success);
			Assert.Equal(2, res.Length); // Only "10" is valid binary
			Assert.Equal(2, res.GetIntermediateValue<int>());

			// Decimal with all valid digits
			res = parser.TryMatchToken("decimal", "123abc");
			Assert.True(res.Success);
			Assert.Equal(3, res.Length);
			Assert.Equal(123, res.GetIntermediateValue<int>());
		}

		[Fact(DisplayName = "IntegerNumber token with custom base mappings edge cases")]
		public void IntegerNumber_CustomBaseMappingsEdgeCases()
		{
			var builder = new ParserBuilder();

			var customMappings = new Dictionary<char, int>
			{
				['a'] = 11,
				['b'] = 12,
				['z'] = 36
			};

			builder.CreateToken("custom_base")
				.IntegerNumber<long>(IntegerNumberFlags.GroupSeparators | IntegerNumberFlags.Signed,
					10, customMappings, groupSeparator: '_');

			var parser = builder.Build();

			// Base 11 with custom mapping
			var res = parser.TryMatchToken("custom_base", "0a10abc");
			Assert.True(res.Success);
			Assert.Equal(5, res.Length);
			Assert.Equal(131L, res.GetIntermediateValue<long>()); // 1*11^2 + 0*11^1 + 10*11^0

			// Base 36 with maximum digit
			res = parser.TryMatchToken("custom_base", "0zzz bc");
			Assert.True(res.Success);
			Assert.Equal(4, res.Length);
			Assert.Equal(1295L, res.GetIntermediateValue<long>()); // 35*36 + 35

			// Base 36 with maximum digit and groups
			res = parser.TryMatchToken("custom_base", "0z_z_z bc");
			Assert.True(res.Success);
			Assert.Equal(6, res.Length);
			Assert.Equal(1295L, res.GetIntermediateValue<long>());

			// Base 36 with maximum digit, sign and groups
			res = parser.TryMatchToken("custom_base", "-0z0_z_z bc");
			Assert.True(res.Success);
			Assert.Equal(8, res.Length);
			Assert.Equal(-1295L, res.GetIntermediateValue<long>());
		}

		[Fact]
		public void Number_Integer_OverflowCases()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("byte")
				.Number<byte>();
			builder.CreateToken("sbyte")
				.Number<sbyte>();
			builder.CreateToken("short")
				.Number<short>();
			builder.CreateToken("ushort")
				.Number<ushort>();
			builder.CreateToken("int")
				.Number<int>();
			builder.CreateToken("uint")
				.Number<uint>();
			builder.CreateToken("long")
				.Number<long>();
			builder.CreateToken("ulong")
				.Number<ulong>();

			var parser = builder.Build();

			// byte: 0 to 255
			var res = parser.TryMatchToken("byte", "-1");
			Assert.False(res.Success);
			res = parser.TryMatchToken("byte", "0");
			Assert.True(res.Success);
			res = parser.TryMatchToken("byte", "255");
			Assert.True(res.Success);
			res = parser.TryMatchToken("byte", "256");
			Assert.False(res.Success);

			// sbyte: -128 to 127
			res = parser.TryMatchToken("sbyte", "-129");
			Assert.False(res.Success);
			res = parser.TryMatchToken("sbyte", "-128");
			Assert.True(res.Success);
			res = parser.TryMatchToken("sbyte", "127");
			Assert.True(res.Success);
			res = parser.TryMatchToken("sbyte", "128");
			Assert.False(res.Success);

			// short: -32768 to 32767
			res = parser.TryMatchToken("short", "-32769");
			Assert.False(res.Success);
			res = parser.TryMatchToken("short", "-32768");
			Assert.True(res.Success);
			res = parser.TryMatchToken("short", "32767");
			Assert.True(res.Success);
			res = parser.TryMatchToken("short", "32768");
			Assert.False(res.Success);

			// ushort: 0 to 65535
			res = parser.TryMatchToken("ushort", "-1");
			Assert.False(res.Success);
			res = parser.TryMatchToken("ushort", "0");
			Assert.True(res.Success);
			res = parser.TryMatchToken("ushort", "65535");
			Assert.True(res.Success);
			res = parser.TryMatchToken("ushort", "65536");
			Assert.False(res.Success);

			// int: -2147483648 to 2147483647
			res = parser.TryMatchToken("int", "-2147483649");
			Assert.False(res.Success);
			res = parser.TryMatchToken("int", "-2147483648");
			Assert.True(res.Success);
			res = parser.TryMatchToken("int", "2147483647");
			Assert.True(res.Success);
			res = parser.TryMatchToken("int", "2147483648");
			Assert.False(res.Success);

			// uint: 0 to 4294967295
			res = parser.TryMatchToken("uint", "-1");
			Assert.False(res.Success);
			res = parser.TryMatchToken("uint", "0");
			Assert.True(res.Success);
			res = parser.TryMatchToken("uint", "4294967295");
			Assert.True(res.Success);
			res = parser.TryMatchToken("uint", "4294967296");
			Assert.False(res.Success);

			// long: -9223372036854775808 to 9223372036854775807
			res = parser.TryMatchToken("long", "-9223372036854775809");
			Assert.False(res.Success);
			res = parser.TryMatchToken("long", "-9223372036854775808");
			Assert.True(res.Success);
			res = parser.TryMatchToken("long", "9223372036854775807");
			Assert.True(res.Success);
			res = parser.TryMatchToken("long", "9223372036854775808");
			Assert.False(res.Success);

			// ulong: 0 to 18446744073709551615
			res = parser.TryMatchToken("ulong", "-1");
			Assert.False(res.Success);
			res = parser.TryMatchToken("ulong", "0");
			Assert.True(res.Success);
			res = parser.TryMatchToken("ulong", "18446744073709551615");
			Assert.True(res.Success);
			res = parser.TryMatchToken("ulong", "18446744073709551616");
			Assert.False(res.Success);
		}

		[Fact]
		public void IntegerNumber_OverflowCases()
		{
			var builder = new ParserBuilder();

			builder.CreateToken("byte")
				.IntegerNumber<byte>();
			builder.CreateToken("sbyte")
				.IntegerNumber<sbyte>();
			builder.CreateToken("short")
				.IntegerNumber<short>();
			builder.CreateToken("ushort")
				.IntegerNumber<ushort>();
			builder.CreateToken("int")
				.IntegerNumber<int>();
			builder.CreateToken("uint")
				.IntegerNumber<uint>();
			builder.CreateToken("long")
				.IntegerNumber<long>();
			builder.CreateToken("ulong")
				.IntegerNumber<ulong>();

			var parser = builder.Build();

			// byte: 0 to 255
			var res = parser.TryMatchToken("byte", "-1");
			Assert.False(res.Success);
			res = parser.TryMatchToken("byte", "0");
			Assert.True(res.Success);
			res = parser.TryMatchToken("byte", "255");
			Assert.True(res.Success);
			res = parser.TryMatchToken("byte", "256");
			Assert.False(res.Success);

			// sbyte: -128 to 127
			res = parser.TryMatchToken("sbyte", "-129");
			Assert.False(res.Success);
			res = parser.TryMatchToken("sbyte", "-128");
			Assert.True(res.Success);
			res = parser.TryMatchToken("sbyte", "127");
			Assert.True(res.Success);
			res = parser.TryMatchToken("sbyte", "128");
			Assert.False(res.Success);

			// short: -32768 to 32767
			res = parser.TryMatchToken("short", "-32769");
			Assert.False(res.Success);
			res = parser.TryMatchToken("short", "-32768");
			Assert.True(res.Success);
			res = parser.TryMatchToken("short", "32767");
			Assert.True(res.Success);
			res = parser.TryMatchToken("short", "32768");
			Assert.False(res.Success);

			// ushort: 0 to 65535
			res = parser.TryMatchToken("ushort", "-1");
			Assert.False(res.Success);
			res = parser.TryMatchToken("ushort", "0");
			Assert.True(res.Success);
			res = parser.TryMatchToken("ushort", "65535");
			Assert.True(res.Success);
			res = parser.TryMatchToken("ushort", "65536");
			Assert.False(res.Success);

			// int: -2147483648 to 2147483647
			res = parser.TryMatchToken("int", "-2147483649");
			Assert.False(res.Success);
			res = parser.TryMatchToken("int", "-2147483648");
			Assert.True(res.Success);
			res = parser.TryMatchToken("int", "2147483647");
			Assert.True(res.Success);
			res = parser.TryMatchToken("int", "2147483648");
			Assert.False(res.Success);

			// uint: 0 to 4294967295
			res = parser.TryMatchToken("uint", "-1");
			Assert.False(res.Success);
			res = parser.TryMatchToken("uint", "0");
			Assert.True(res.Success);
			res = parser.TryMatchToken("uint", "4294967295");
			Assert.True(res.Success);
			res = parser.TryMatchToken("uint", "4294967296");
			Assert.False(res.Success);

			// long: -9223372036854775808 to 9223372036854775807
			res = parser.TryMatchToken("long", "-9223372036854775809");
			Assert.False(res.Success);
			res = parser.TryMatchToken("long", "-9223372036854775808");
			Assert.True(res.Success);
			res = parser.TryMatchToken("long", "9223372036854775807");
			Assert.True(res.Success);
			res = parser.TryMatchToken("long", "9223372036854775808");
			Assert.False(res.Success);

			// ulong: 0 to 18446744073709551615
			res = parser.TryMatchToken("ulong", "-1");
			Assert.False(res.Success);
			res = parser.TryMatchToken("ulong", "0");
			Assert.True(res.Success);
			res = parser.TryMatchToken("ulong", "18446744073709551615");
			Assert.True(res.Success);
			res = parser.TryMatchToken("ulong", "18446744073709551616");
			Assert.False(res.Success);
		}
	}
}