using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents the type of number to convert.
	/// </summary>
	public enum NumberType
	{
		/// <summary>
		/// Converts the parsed value to an <see cref="int"/> if it has no decimal point, otherwise to a <see cref="float"/>.
		/// </summary>
		PreferSimpler,

		/// <summary>
		/// Converts the parsed value to a <see cref="byte"/>.
		/// </summary>
		Byte,

		/// <summary>
		/// Converts the parsed value to a <see cref="sbyte"/>.
		/// </summary>
		SignedByte,

		/// <summary>
		/// Converts the parsed value to a <see cref="ushort"/>.
		/// </summary>
		UnsignedShort,

		/// <summary>
		/// Converts the parsed value to a <see cref="short"/>.
		/// </summary>
		Short,

		/// <summary>
		/// Converts the parsed value to a <see cref="uint"/>.
		/// </summary>
		UnsignedInteger,

		/// <summary>
		/// Converts the parsed value to a <see cref="int"/>.
		/// </summary>
		Integer,

		/// <summary>
		/// Converts the parsed value to a <see cref="ulong"/>.
		/// </summary>
		UnsignedLong,

		/// <summary>
		/// Converts the parsed value to a <see cref="long"/>.
		/// </summary>
		Long,

		/// <summary>
		/// Converts the parsed value to a <see cref="float"/>.
		/// </summary>
		Float,

		/// <summary>
		/// Converts the parsed value to a <see cref="double"/>.
		/// </summary>
		Double,

		/// <summary>
		/// Converts the parsed value to a <see cref="decimal"/>.
		/// </summary>
		Decimal
	}

	/// <summary>
	/// Represents the flags that can be used to modify the behavior of the number token pattern.
	/// </summary>
	[Flags]
	public enum NumberFlags
	{
		/// <summary>
		/// Standard integer (signed, no fractional part, no exponent).
		/// </summary>
		Integer = Signed,

		/// <summary>
		/// Unsigned integer (no sign, no fractional part, no exponent).
		/// </summary>
		UnsignedInteger = None,

		/// <summary>
		/// Standard floating-point number with decimal part.
		/// Example: "3.14", "-5.", "+.5"
		/// </summary>
		Float = Signed | DecimalPoint | ImplicitIntegerPart | ImplicitFractionalPart,

		/// <summary>
		/// Standard unsigned floating-point number with decimal part.
		/// Example: "3.14", "-5.", "+.5"
		/// </summary>
		UnsignedFloat = DecimalPoint | ImplicitIntegerPart | ImplicitFractionalPart,

		/// <summary>
		/// Strict (no implicit integer or fractional parts) floating-point number with decimal part.
		/// Example: "3.14", "-5"
		/// </summary>
		StrictFloat = Signed | DecimalPoint,

		/// <summary>
		/// Strict (no implicit integer or fractional parts) unsigned floating-point number with decimal part.
		/// Example: "3.14", "5"
		/// </summary>
		StrictUnsignedFloat = DecimalPoint,

		/// <summary>
		/// Scientific notation floating-point.
		/// Example: "1.23e-10", ".14e+9", "-10e5", "123"
		/// </summary>
		Scientific = Float | Exponent,

		/// <summary>
		/// Unsigned scientific notation floating-point.
		/// </summary>
		UnsignedScientific = UnsignedFloat | Exponent,

		/// <summary>
		/// Strict scientific notation floating-point.
		/// Example: "1.23e-10"
		/// </summary>
		StrictScientific = StrictFloat | Exponent,

		/// <summary>
		/// Strict unsigned scientific notation floating-point.
		/// </summary>
		StrictUnsignedScientific = StrictUnsignedFloat | Exponent,

		// === Flags ===

		/// <summary>
		/// No specific flags.
		/// </summary>
		None = 0,

		/// <summary>
		/// Allows leading '+' or '-' sign.
		/// </summary>
		Signed = 1 << 0,

		/// <summary>
		/// Allows decimal point in the number.
		/// </summary>
		DecimalPoint = 1 << 1,

		/// <summary>
		/// Allows exponent part for a scientific notation.
		/// </summary>
		Exponent = 1 << 2,

		/// <summary>
		/// Allows implicit integer part before the decimal point. For example, ".5" is treated as "0.5".
		/// </summary>
		ImplicitIntegerPart = 1 << 3,

		/// <summary>
		/// Allows implicit fractional part after the decimal point. For example, "5." is treated as "5.0".
		/// </summary>
		ImplicitFractionalPart = 1 << 4,
	}

	/// <summary>
	/// Represents a token pattern that can be used to match numbers.
	/// </summary>
	public class NumberTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the type of number that this pattern matches.
		/// </summary>
		public NumberType NumberType { get; }

		/// <summary>
		/// Gets the number flags that apply to this pattern.
		/// </summary>
		public NumberFlags Flags { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NumberTokenPattern"/> class.
		/// </summary>
		/// <param name="numberType">The type of number that this pattern matches.</param>
		/// <param name="flags">The number flags that apply to this pattern.</param>
		public NumberTokenPattern(NumberType numberType, NumberFlags flags)
		{
			NumberType = numberType;
			Flags = flags;
		}



		protected override HashSet<char>? FirstCharsCore
		{
			get
			{
				var set = new HashSet<char>();
				for (char c = '0'; c <= '9'; c++)
					set.Add(c);
				if ((Flags & NumberFlags.Signed) != 0)
				{
					set.Add('+');
					set.Add('-');
				}
				if ((Flags & NumberFlags.DecimalPoint) != 0)
					set.Add('.');
				return set;
			}
		}

		public override ParsedElement Match(string input, int position, int barrierPosition, object? parserParameter)
		{
			if (position >= barrierPosition)
				return ParsedElement.Fail;
			int startPos = position;

			bool positiveSign = true;
			if ((Flags & NumberFlags.Signed) != 0)
			{
				if (input[position] == '-')
				{
					positiveSign = false;
					position++;
				}
				else if (input[position] == '+')
				{
					positiveSign = true;
					position++;
				}
			}

			int integerPart = 0; int digit; int integerDigitCount = 0;
			double fractionalPart = 0; int fractionalDigitCount = 0; double fractionalPower = 10;
			int exponentPart = 0; int exponentDigitCount = 0; bool positiveExponent = true;

			// Parse the integer part of the number.
			while (position < barrierPosition && (digit = input[position] - '0') >= 0 && digit <= 9)
			{
				integerPart = integerPart * 10 + digit;
				integerDigitCount++;
				position++;
			}

			// Integer position = position after optional sign and digits.
			int integerPosition = position, fractionalPosition = position, exponentPosition = position;
			if (position == barrierPosition) goto check;

			// Parse the fractional part of the number.
			if ((Flags & NumberFlags.DecimalPoint) != 0 &&
				position < barrierPosition && input[position] == '.')
			{
				position++; // Consume the '.'
				while (position < barrierPosition && (digit = input[position] - '0') >= 0 && digit <= 9)
				{
					fractionalPart += digit / fractionalPower;
					fractionalPower *= 10;
					fractionalDigitCount++;
					position++;
				}
			}

			// Fractional position = position after optional sign, digits, '.' and optional digits.
			fractionalPosition = exponentPosition = position;
			if (position == barrierPosition) goto check;

			// Parse the exponent part of the number.
			if ((Flags & NumberFlags.Exponent) != 0 &&
				position < barrierPosition && (input[position] == 'e' || input[position] == 'E'))
			{
				// Consume the 'e' or 'E'
				position++;
				if (position == barrierPosition) goto check;

				if (input[position] == '-')
				{
					positiveExponent = false;
					position++;
				}
				else if (input[position] == '+')
				{
					positiveExponent = true;
					position++;
				}

				if (position == barrierPosition) goto check;

				while (position < barrierPosition && (digit = input[position] - '0') >= 0 && digit <= 9)
				{
					exponentPart = exponentPart * 10 + digit;
					exponentDigitCount++;
					position++;
				}
			}

			// Exponent position = position after optional sign, digits, '.', optional digits, 'e' or 'E', optional sign, and optional digits.
			exponentPosition = position;

		check:

			// Check if the number is valid and at least one digit was parsed.
			int length = position - startPos;
			if (length == 0 || (integerDigitCount == 0 && fractionalDigitCount == 0))
				return ParsedElement.Fail;

			// ".x" is allowed only if ImplicitIntegerPart is enabled.
			if (integerDigitCount == 0 && (Flags & NumberFlags.ImplicitIntegerPart) == 0)
				return ParsedElement.Fail;

			// "x." is allowed only if ImplicitFractionalPart is enabled.
			if (fractionalDigitCount == 0 && (Flags & NumberFlags.ImplicitFractionalPart) == 0)
			{
				length = integerPosition - startPos;
				goto calculation;
			}

			// Backtrack
			if (exponentDigitCount == 0 && (Flags & NumberFlags.Exponent) != 0)
			{
				length = fractionalPosition - startPos;
				goto calculation;
			}

		calculation:

			// Calculate the result based on the parsed number.
			double result = integerPart + fractionalPart;
			if (!positiveSign)
				result = -result;

			// Apply the exponent part if it exists.
			if (exponentDigitCount > 0)
			{
				int exp = positiveExponent ? exponentPart : -exponentPart;
				result *= Math.Pow(10, exp);
			}

			// Convert the result to the appropriate type based on the number type.
			object? value = null;
			switch (NumberType)
			{
				case NumberType.PreferSimpler:
					if (fractionalPosition > integerPosition)
						value = (float)result;
					else
						value = (int)result;
					break;

				case NumberType.Byte:
					value = (byte)result;
					break;
				case NumberType.SignedByte:
					value = (sbyte)result;
					break;

				case NumberType.UnsignedShort:
					value = (ushort)result;
					break;
				case NumberType.Short:
					value = (short)result;
					break;

				case NumberType.UnsignedInteger:
					value = (uint)result;
					break;
				case NumberType.Integer:
					value = (int)result;
					break;

				case NumberType.UnsignedLong:
					value = (ulong)result;
					break;
				case NumberType.Long:
					value = (long)result;
					break;

				case NumberType.Float:
					value = (float)result;
					break;

				case NumberType.Double:
					value = result;
					break;

				case NumberType.Decimal:
					value = (decimal)result;
					break;
			}

			return new ParsedElement(startPos, length, value);
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "number";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is NumberTokenPattern other &&
				   NumberType == other.NumberType &&
				   Flags == other.Flags;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = base.GetHashCode();
				hash = (hash * 397) ^ NumberType.GetHashCode();
				hash = (hash * 397) ^ Flags.GetHashCode();
				return hash;
			}
		}
	}
}