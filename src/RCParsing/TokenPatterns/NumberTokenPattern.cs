using System;
using System.Collections.Generic;

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
	/// Represents the flags that can be used to modify the behavior of the <see cref="NumberTokenPattern"/>.
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
		/// Allows group separators in the number, like "1'099'321".
		/// </summary>
		GroupSeparators = 1 << 2,

		/// <summary>
		/// Allows exponent part for a scientific notation.
		/// </summary>
		Exponent = 1 << 3,

		/// <summary>
		/// Allows implicit integer part before the decimal point. For example, ".5" is treated as "0.5".
		/// </summary>
		ImplicitIntegerPart = 1 << 4,

		/// <summary>
		/// Allows implicit fractional part after the decimal point. For example, "5." is treated as "5.0".
		/// </summary>
		ImplicitFractionalPart = 1 << 5,
	}

	/// <summary>
	/// Represents a range of allowed count of group separators in a row while parsing a <see cref="NumberTokenPattern"/>.
	/// </summary>
	public struct NumberGroupOptions
	{
		/// <summary>
		/// The separator character.
		/// </summary>
		public char Separator { get; set; }

		/// <summary>
		/// Allow leading separators?
		/// </summary>
		public bool AllowLeadingSeparator { get => LeadingMinSize == 0; set => LeadingMinSize = value ? 0 : LeadingMinSize > 0 ? LeadingMinSize : 1; }

		/// <summary>
		/// Allow trailing separators?
		/// </summary>
		public bool AllowTrailingSeparator { get; set; }

		/// <summary>
		/// Minimum count of separators in one group.
		/// </summary>
		public int MinSeparators { get; set; }

		/// <summary>
		/// Maximum count of separators in one group.
		/// </summary>
		public int MaxSeparators { get; set; }

		/// <summary>
		/// Minimum size of leading group, can be 0 to allow leading separators.
		/// </summary>
		public int LeadingMinSize { get; set; }

		/// <summary>
		/// Maximum size of leading group.
		/// </summary>
		public int LeadingMaxSize { get; set; }

		/// <summary>
		/// Minimum size of groups.
		/// </summary>
		public int MinSize { get; set; }

		/// <summary>
		/// Maximum size of groups.
		/// </summary>
		public int MaxSize { get; set; }
	}

	/// <summary>
	/// Represents a token pattern that can be used to match numbers.
	/// </summary>
	public class NumberTokenPattern : TokenPattern
	{
		/// <summary>
		/// Gets the target type of number that intermediate value will be converted to.
		/// </summary>
		public NumberType NumberType { get; }

		/// <summary>
		/// Gets the number flags that affects parsing behaviour.
		/// </summary>
		public NumberFlags Flags { get; }

		/// <summary>
		/// Gets the decimal point character.
		/// </summary>
		public char DecimalPoint { get; }
		
		/// <summary>
		/// Gets the group separator character.
		/// </summary>
		public char GroupSeparator { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="NumberTokenPattern"/> class.
		/// </summary>
		/// <param name="numberType">The target type of number that intermediate value will be converted to.</param>
		/// <param name="flags">The number flags that affects parsing behaviour.</param>
		/// <param name="decimalPoint">The decimal point character.</param>
		/// <param name="groupSeparator">The group separator character.</param>
		public NumberTokenPattern(NumberType numberType, NumberFlags flags,
			char decimalPoint = '.', char groupSeparator = '_')
		{
			NumberType = numberType;
			Flags = flags;
			DecimalPoint = decimalPoint;
			GroupSeparator = groupSeparator;
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
				if ((Flags & NumberFlags.DecimalPoint) != 0 && (Flags & NumberFlags.ImplicitIntegerPart) != 0)
					set.Add(DecimalPoint);
				return set;
			}
		}
		protected override bool IsFirstCharDeterministicCore => true;
		protected override bool IsOptionalCore => false;



		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			if (position >= barrierPosition)
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(position, 0, "End of input reached.", Id, true);
				return ParsedElement.Fail;
			}
			int startPos = position;
			var flags = Flags;

			bool isNegative = false;
			if ((flags & NumberFlags.Signed) != 0)
			{
				if (input[position] == '-')
				{
					isNegative = true;
					position++;
				}
				else if (input[position] == '+')
				{
					isNegative = false;
					position++;
				}
			}

			ulong integerPart = 0; int digit; int integerDigitCount = 0;
			double fractionalPart = 0;  int fractionalDigitCount = 0; double fractionalPower = 10;
			int exponentPart = 0; int exponentDigitCount = 0; bool negativeExponent = false;

			// Parse the integer part of the number.
			try
			{
				checked
				{
					if ((flags & NumberFlags.GroupSeparators) != 0)
					{
						var groupSeparator = GroupSeparator;
						int lastDigitPosition = position;
						while (position < barrierPosition)
						{
							if ((digit = input[position] - '0') >= 0 && digit <= 9)
							{
								integerPart = integerPart * 10 + (uint)digit;
								integerDigitCount++;
								position++;
								lastDigitPosition = position;
							}
							else if (integerDigitCount > 0 && input[position] == groupSeparator)
							{
								position++;
							}
							else
							{
								break;
							}
						}
						position = lastDigitPosition;
					}
					else
					{
						while (position < barrierPosition && (digit = input[position] - '0') >= 0 && digit <= 9)
						{
							integerPart = integerPart * 10 + (uint)digit;
							integerDigitCount++;
							position++;
						}
					}
				}
			}
			catch (OverflowException)
			{
				if (startPos >= furthestError.position)
					furthestError = new ParsingError(startPos, 0, "Integer literal is too large.", Id, true);
				return ParsedElement.Fail;
			}

			// Integer position = position after optional sign and digits.
			int integerPosition = position, fractionalPosition = position, exponentPosition = position;
			if (position == barrierPosition) goto check;

			// Parse the fractional part of the number.
			if ((flags & NumberFlags.DecimalPoint) != 0 &&
				position < barrierPosition && input[position] == DecimalPoint)
			{
				position++; // Consume the '.'

				if ((flags & NumberFlags.GroupSeparators) != 0)
				{
					var groupSeparator = GroupSeparator;
					int lastDigitPosition = position;
					while (position < barrierPosition)
					{
						if ((digit = input[position] - '0') >= 0 && digit <= 9)
						{
							fractionalPart += digit / fractionalPower;
							fractionalPower *= 10;
							fractionalDigitCount++;
							position++;
							lastDigitPosition = position;
						}
						else if (fractionalDigitCount > 0 && input[position] == groupSeparator)
						{
							position++;
						}
						else
						{
							break;
						}
					}
					position = lastDigitPosition;
				}
				else
				{
					while (position < barrierPosition && (digit = input[position] - '0') >= 0 && digit <= 9)
					{
						fractionalPart += digit / fractionalPower;
						fractionalPower *= 10;
						fractionalDigitCount++;
						position++;
					}
				}
			}

			// Fractional position = position after optional sign, digits, '.' and optional digits.
			fractionalPosition = exponentPosition = position;
			if (position == barrierPosition) goto check;

			// Parse the exponent part of the number.
			if ((flags & NumberFlags.Exponent) != 0 &&
				position < barrierPosition && (input[position] == 'e' || input[position] == 'E'))
			{
				// Consume the 'e' or 'E'
				position++;
				if (position == barrierPosition) goto check;

				if (input[position] == '-')
				{
					negativeExponent = true;
					position++;
				}
				else if (input[position] == '+')
				{
					negativeExponent = false;
					position++;
				}

				if ((flags & NumberFlags.GroupSeparators) != 0)
				{
					var groupSeparator = GroupSeparator;
					int lastDigitPosition = position;
					while (position < barrierPosition)
					{
						if ((digit = input[position] - '0') >= 0 && digit <= 9)
						{
							exponentPart = exponentPart * 10 + digit;
							exponentDigitCount++;
							position++;
							lastDigitPosition = position;
						}
						else if (fractionalDigitCount > 0 && input[position] == groupSeparator)
						{
							position++;
						}
						else
						{
							break;
						}
					}
					position = lastDigitPosition;
				}
				else
				{
					while (position < barrierPosition && (digit = input[position] - '0') >= 0 && digit <= 9)
					{
						exponentPart = exponentPart * 10 + digit;
						exponentDigitCount++;
						position++;
					}
				}
			}

			// Exponent position = position after optional sign, digits, '.', optional digits, 'e' or 'E', optional sign, and optional digits.
			exponentPosition = position;

		check:

			// Check if the number is valid and at least one digit was parsed.
			int length = position - startPos;
			if (length == 0 || (integerDigitCount == 0 && fractionalDigitCount == 0))
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(startPos, 0, "Any digit expected.", Id, true);
				return ParsedElement.Fail;
			}

			// ".x" is allowed only if ImplicitIntegerPart is enabled.
			if (integerDigitCount == 0 && (flags & NumberFlags.ImplicitIntegerPart) == 0)
			{
				if (position >= furthestError.position)
					furthestError = new ParsingError(startPos, 0, "Implicit integer part is not allowed.", Id, true);
				return ParsedElement.Fail;
			}

			// "x." is allowed only if ImplicitFractionalPart is enabled.
			if (fractionalDigitCount == 0 && (flags & NumberFlags.ImplicitFractionalPart) == 0)
			{
				length = integerPosition - startPos;
				goto calculation;
			}

			// Backtrack
			if (exponentDigitCount == 0 && (flags & NumberFlags.Exponent) != 0)
			{
				length = fractionalPosition - startPos;
				goto calculation;
			}

		calculation:

			if (negativeExponent)
				exponentPart = -exponentPart;

			// Convert the result to the appropriate type based on the number type.
			object? value = null;
			try
			{
				value = ConvertToTargetType(fractionalPosition > integerPosition,
					integerPart, fractionalPart, isNegative, exponentPart);
			}
			catch (OverflowException)
			{
				if (startPos >= furthestError.position)
					furthestError = new ParsingError(startPos, 0, "Numeric literal is out of range for the target type.", Id, true);
				return ParsedElement.Fail;
			}

			return new ParsedElement(startPos, length, value);
		}

		private object ConvertToTargetType(bool hasFractionalPart, ulong integerPart, double fractionalPart, bool isNegative, int exponentPart)
		{
			switch (NumberType)
			{
				case NumberType.PreferSimpler:
					if (hasFractionalPart)
					{
						checked
						{
							float _value_F = (float)(integerPart + fractionalPart);
							if (isNegative)
								_value_F = -_value_F;
							if (exponentPart != 0)
								_value_F *= (float)Math.Pow(10, exponentPart);
							return _value_F;
						}
					}
					else
					{
						checked
						{
							int _value_I = isNegative ? (int)-(long)integerPart : (int)integerPart;
							if (isNegative)
								_value_I = -_value_I;
							if (exponentPart != 0)
								_value_I *= (int)Math.Pow(10, exponentPart);
							return _value_I;
						}
					}

				case NumberType.Byte:
					checked
					{
						if (isNegative)
							throw new OverflowException();
						byte _valueB = (byte)integerPart;
						if (exponentPart != 0)
							_valueB *= (byte)Math.Pow(10, exponentPart);
						return _valueB;
					}

				case NumberType.SignedByte:
					checked
					{
						sbyte _valueSB = isNegative ? (sbyte)-(long)integerPart : (sbyte)integerPart;
						if (exponentPart != 0)
							_valueSB *= (sbyte)Math.Pow(10, exponentPart);
						return _valueSB;
					}

				case NumberType.UnsignedShort:
					checked
					{
						if (isNegative)
							throw new OverflowException();
						ushort _valueUS = (ushort)integerPart;
						if (exponentPart != 0)
							_valueUS *= (ushort)Math.Pow(10, exponentPart);
						return _valueUS;
					}

				case NumberType.Short:
					checked
					{
						short _valueS = isNegative ? (short)-(long)integerPart : (short)integerPart;
						if (exponentPart != 0)
							_valueS *= (short)Math.Pow(10, exponentPart);
						return _valueS;
					}

				case NumberType.UnsignedInteger:
					checked
					{
						if (isNegative)
							throw new OverflowException();
						uint _valueUI = (uint)integerPart;
						if (exponentPart != 0)
							_valueUI *= (uint)Math.Pow(10, exponentPart);
						return _valueUI;
					}

				case NumberType.Integer:
					checked
					{
						int _valueI = isNegative ? (int)-(long)integerPart : (int)integerPart;
						if (exponentPart != 0)
							_valueI *= (int)Math.Pow(10, exponentPart);
						return _valueI;
					}

				case NumberType.UnsignedLong:
					checked
					{
						if (isNegative)
							throw new OverflowException();
						ulong _valueUL = integerPart;
						if (exponentPart != 0)
							_valueUL *= (ulong)Math.Pow(10, exponentPart);
						return _valueUL;
					}

				case NumberType.Long:
					checked
					{
						if (exponentPart != 0)
							integerPart *= (ulong)Math.Pow(10, exponentPart);
						if (isNegative)
						{
							if (integerPart == 9223372036854775808)
								return long.MinValue;
							else
								return -(long)integerPart;
						}
						else
							return (long)integerPart;
					}

				case NumberType.Float:
					float _valueF = (float)(integerPart + fractionalPart);
					if (isNegative)
						_valueF = -_valueF;
					if (exponentPart != 0)
						_valueF *= (float)Math.Pow(10, exponentPart);
					return _valueF;

				case NumberType.Double:
					double _valueD = integerPart + fractionalPart;
					if (isNegative)
						_valueD = -_valueD;
					if (exponentPart != 0)
						_valueD *= Math.Pow(10, exponentPart);
					return _valueD;

				case NumberType.Decimal:
					checked
					{
						decimal _valueDc = (decimal)(integerPart + fractionalPart);
						if (isNegative)
							_valueDc = -_valueDc;
						if (exponentPart != 0)
							_valueDc *= (decimal)Math.Pow(10, exponentPart);
						return _valueDc;
					}

				default:
					throw new InvalidOperationException($"Unsupported NumberType: {NumberType}.");
			}
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
				   Flags == other.Flags &&
				   DecimalPoint == other.DecimalPoint &&
				   GroupSeparator == other.GroupSeparator;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = base.GetHashCode();
				hash = (hash * 397) + NumberType.GetHashCode();
				hash = (hash * 397) + Flags.GetHashCode();
				hash = (hash * 397) + DecimalPoint.GetHashCode();
				hash = (hash * 397) + GroupSeparator.GetHashCode();
				return hash;
			}
		}
	}
}