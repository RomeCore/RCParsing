using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace RCParsing.TokenPatterns
{
	/// <summary>
	/// Represents the type of number to convert for <see cref="IntegerNumberType"/>.
	/// </summary>
	public enum IntegerNumberType
	{
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
		Long
	}

	/// <summary>
	/// Represents the flags that can be used to modify the behavior of the <see cref="IntegerNumberTokenPattern"/>.
	/// </summary>
	[Flags]
	public enum IntegerNumberFlags
	{
		/// <summary>
		/// No specific flags.
		/// </summary>
		None = 0,

		/// <summary>
		/// Allows leading '+' or '-' sign.
		/// </summary>
		Signed = 1 << 0,

		/// <summary>
		/// Allows group separators in the number, like "1'099'321".
		/// </summary>
		GroupSeparators = 1 << 1
	}

	/// <summary>
	/// The token pattern that matches an integer number with support of multiple number systems and base mappings.
	/// </summary>
	public class IntegerNumberTokenPattern : TokenPattern
	{
		private static int[] numbersByChars;

		private static bool CheckBaseOutOfRange(int @base)
		{
			return @base < 2 || @base > 36;
		}

		static IntegerNumberTokenPattern()
		{
			numbersByChars = new int[char.MaxValue + 1];
			for (int i = 0; i < char.MaxValue; i++)
				numbersByChars[i] = -1;

			for (char c = '0'; c <= '9'; c++)
				numbersByChars[c] = c - '0';
			for (char c = 'a'; c <= 'z'; c++)
				numbersByChars[c] = c - 'a' + 10;
			for (char c = 'A'; c <= 'Z'; c++)
				numbersByChars[c] = c - 'A' + 10;
		}

		private Dictionary<char, int> _baseMappings;

		/// <summary>
		/// Gets the target type of number that intermediate value will be converted to.
		/// </summary>
		public IntegerNumberType NumberType { get; }

		/// <summary>
		/// Gets the number flags that affects parsing behaviour.
		/// </summary>
		public IntegerNumberFlags Flags { get; }

		/// <summary>
		/// Gets the default base number (2 is binary, 8 is octal, 10 is decimal, 16 is hexadecimal). 10 by default.
		/// </summary>
		public int DefaultBase { get; }

		/// <summary>
		/// Gets the base mappings for each character.
		/// </summary>
		public IDictionary<char, int> BaseMappings { get; }

		/// <summary>
		/// Gets the group separator character.
		/// </summary>
		public char GroupSeparator { get; }

		public IntegerNumberTokenPattern(IntegerNumberType numberType, IntegerNumberFlags flags, char groupSeparator = '_')
		{
			NumberType = numberType;
			Flags = flags;
			GroupSeparator = groupSeparator;
			DefaultBase = 10;
			_baseMappings = new();
			BaseMappings = new ReadOnlyDictionary<char, int>(_baseMappings);
		}
		
		public IntegerNumberTokenPattern(IntegerNumberType numberType, IntegerNumberFlags flags, int defaultBase = 10, char groupSeparator = '_')
		{
			if (CheckBaseOutOfRange(defaultBase))
				throw new ArgumentOutOfRangeException(nameof(defaultBase), "Default base mapping is out of range, allowed range is [2, 36].");

			NumberType = numberType;
			Flags = flags;
			GroupSeparator = groupSeparator;
			DefaultBase = defaultBase;
			_baseMappings = new();
			BaseMappings = new ReadOnlyDictionary<char, int>(_baseMappings);
		}
		
		public IntegerNumberTokenPattern(IntegerNumberType numberType, IntegerNumberFlags flags, int defaultBase = 10, IDictionary<char, int>? baseMappings = null, char groupSeparator = '_')
		{
			if (CheckBaseOutOfRange(defaultBase))
				throw new ArgumentOutOfRangeException(nameof(defaultBase), "Default base mapping is out of range, allowed range is [2, 36].");

			NumberType = numberType;
			Flags = flags;
			GroupSeparator = groupSeparator;
			DefaultBase = defaultBase;

			if (baseMappings != null)
			{
				if (baseMappings.Values.Any(CheckBaseOutOfRange))
					throw new ArgumentOutOfRangeException(nameof(baseMappings), "One of base mappings is out of range, allowed range is [2, 36].");
				_baseMappings = new(baseMappings);
			}
			else
				_baseMappings = new();
			BaseMappings = new ReadOnlyDictionary<char, int>(_baseMappings);
		}

		protected override HashSet<char> FirstCharsCore
		{
			get
			{
				var set = new HashSet<char>();

				int baseUnder10 = Math.Min(10, DefaultBase);
				for (char c = '0'; c <= '0' + baseUnder10; c++)
					set.Add(c);
				int baseOver10 = DefaultBase - 10;
				for (int o = 0; o < baseOver10; o++)
				{
					set.Add((char)('a' + o));
					set.Add((char)('A' + o));
				}
				if (Flags.HasFlag(IntegerNumberFlags.Signed))
				{
					set.Add('+');
					set.Add('-');
				}
				return set;
			}
		}

		protected override bool IsFirstCharDeterministicCore => true;
		protected override bool IsOptionalCore => false;

		public override ParsedElement Match(string input, int position, int barrierPosition,
			object? parserParameter, bool calculateIntermediateValue, ref ParsingError furthestError)
		{
			int startPosition = position;
			int lastDigitPos = position;
			if (position >= barrierPosition)
				return ParsedElement.Fail;

			bool isNegative = false;
			if (Flags.HasFlag(IntegerNumberFlags.Signed) && position < barrierPosition)
			{
				char sign = input[position];
				if (sign == '+' || sign == '-')
				{
					isNegative = sign == '-';
					position++;
				}
			}

			bool allowGroup = Flags.HasFlag(IntegerNumberFlags.GroupSeparators);
			char groupSeparator = GroupSeparator;

			ulong unsignedAccumulator = 0;
			bool had0atFirstChar = false;

			int digitCounter = 0;
			int numberBase = DefaultBase;

			while (position < barrierPosition)
			{
				char c = input[position];

				if (allowGroup && c == groupSeparator)
				{
					position++;
					continue;
				}

				int digit;
				if (had0atFirstChar && _baseMappings.TryGetValue(c, out var mappedBase))
				{
					had0atFirstChar = false;
					numberBase = mappedBase;
					position++;
					continue;
				}
				else
				{
					digit = c <= char.MaxValue ? numbersByChars[c] : -1;
				}

				if (digit < 0 || digit >= numberBase)
					break;

				had0atFirstChar = digitCounter == 0 && digit == 0;
				digitCounter++;

				try
				{
					checked
					{
						unsignedAccumulator = unsignedAccumulator * (uint)numberBase + (uint)digit;
					}
				}
				catch (OverflowException)
				{
					if (startPosition >= furthestError.position)
						furthestError = new ParsingError(startPosition, 0, "Integer literal is too large.", Id, true);
					return ParsedElement.Fail;
				}

				position++;
				lastDigitPos = position;
			}

			if (lastDigitPos == startPosition)
			{
				if (startPosition >= furthestError.position)
					furthestError = new ParsingError(startPosition, 0, "Expected integer number.", Id, true);
				return ParsedElement.Fail;
			}

			if (!calculateIntermediateValue)
				return new ParsedElement(startPosition, position - startPosition);

			object? value;
			try
			{
				value = ConvertToTargetType(unsignedAccumulator, isNegative);
			}
			catch (OverflowException)
			{
				if (startPosition >= furthestError.position)
					furthestError = new ParsingError(startPosition, 0, "Integer literal is out of range.", Id, true);
				return ParsedElement.Fail;
			}
			catch (Exception ex)
			{
				if (startPosition >= furthestError.position)
					furthestError = new ParsingError(startPosition, 0, ex.Message, Id, true);
				return ParsedElement.Fail;
			}

			if (!calculateIntermediateValue)
				value = null;

			return new ParsedElement(startPosition, position - startPosition, value);
		}

		private object ConvertToTargetType(ulong unsignedAccumulator, bool isNegative)
		{
			switch (NumberType)
			{
				case IntegerNumberType.Byte:
					{
						if (isNegative)
							throw new OverflowException();
						checked
						{
							return (byte)unsignedAccumulator;
						}
					}
				case IntegerNumberType.SignedByte:
					{
						checked
						{
							return isNegative ? (sbyte)-(byte)unsignedAccumulator : (sbyte)unsignedAccumulator;
						}
					}
				case IntegerNumberType.UnsignedShort:
					{
						if (isNegative)
							throw new OverflowException();
						checked
						{
							return (ushort)unsignedAccumulator;
						}
					}
				case IntegerNumberType.Short:
					{
						checked
						{
							return isNegative ? (short)-(ushort)unsignedAccumulator : (short)unsignedAccumulator;
						}
					}
				case IntegerNumberType.UnsignedInteger:
					{
						if (isNegative)
							throw new OverflowException();
						checked
						{
							return (uint)unsignedAccumulator;
						}
					}
				case IntegerNumberType.Integer:
					{
						checked
						{
							return isNegative ? (int)-(uint)unsignedAccumulator : (int)unsignedAccumulator;
						}
					}
				case IntegerNumberType.UnsignedLong:
					{
						if (isNegative)
							throw new OverflowException();
						checked
						{
							return unsignedAccumulator;
						}
					}
				case IntegerNumberType.Long:
					{
						checked
						{
							if (isNegative)
							{
								if (unsignedAccumulator == 9223372036854775808)
									return long.MinValue;
								return -(long)unsignedAccumulator;
							}
							return (long)unsignedAccumulator;
						}
					}
				default:
					throw new InvalidOperationException($"Unsupported IntegerNumberType: {NumberType}.");
			}
		}

		public override string ToStringOverride(int remainingDepth)
		{
			return "integer";
		}

		public override bool Equals(object? obj)
		{
			return base.Equals(obj) &&
				   obj is IntegerNumberTokenPattern other &&
				   NumberType == other.NumberType &&
				   Flags == other.Flags &&
				   DefaultBase == other.DefaultBase &&
				   _baseMappings.Count == other._baseMappings.Count &&
				   !_baseMappings.Except(other._baseMappings).Any() &&
				   GroupSeparator == other.GroupSeparator;
		}

		public override int GetHashCode()
		{
			int hashCode = base.GetHashCode();
			hashCode = hashCode * 397 + NumberType.GetHashCode();
			hashCode = hashCode * 397 + Flags.GetHashCode();
			hashCode = hashCode * 397 + DefaultBase.GetHashCode();
			foreach (var kv in _baseMappings)
			{
				hashCode = hashCode * 397 + kv.Key.GetHashCode();
				hashCode = hashCode * 397 + kv.Value.GetHashCode();
			}
			hashCode = hashCode * 397 + GroupSeparator.GetHashCode();
			return hashCode;
		}
	}
}