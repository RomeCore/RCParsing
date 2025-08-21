using System;

namespace RCParsing.Utils
{
	/// <summary>
	/// Represents a type that can hold either a value of type <typeparamref name="T1"/> or <typeparamref name="T2"/>,
	/// but not both at the same time. This is a discriminated union for two types.
	/// </summary>
	/// <typeparam name="T1">The first possible type.</typeparam>
	/// <typeparam name="T2">The second possible type.</typeparam>
	public readonly struct Or<T1, T2> : IEquatable<Or<T1, T2>>
	{
		private readonly byte _variantIndex;
		private readonly T1 _value1;
		private readonly T2 _value2;

		/// <summary>
		/// Gets the index of the currently active variant (0 for <typeparamref name="T1"/>, 1 for <typeparamref name="T2"/>).
		/// </summary>
		public int VariantIndex => _variantIndex;

		/// <summary>
		/// Gets the value of the currently active variant as <typeparamref name="T1"/>. Throws an exception if the wrong type is accessed.
		/// </summary>
		public T1 Value1 =>
			_variantIndex == 0
				? _value1
				: throw new InvalidOperationException($"Active variant is not {typeof(T1)}.");

		/// <summary>
		/// Gets the value of the currently active variant as <typeparamref name="T2"/>. Throws an exception if the wrong type is accessed.
		/// </summary>
		public T2 Value2 =>
			_variantIndex == 1
				? _value2
				: throw new InvalidOperationException($"Active variant is not {typeof(T2)}.");

		static Or()
		{
			if (typeof(T1) == typeof(T2))
				throw new InvalidOperationException($"Types in {nameof(Or<T1, T2>)} cannot be same!");
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Or{T1, T2}"/> struct with a value of type <typeparamref name="T1"/>.
		/// </summary>
		/// <param name="value">The value to store.</param>
		/// <exception cref="Exception">Thrown when <typeparamref name="T1"/> and <typeparamref name="T2"/> are the same type.</exception>
		public Or(T1 value)
		{
			_variantIndex = 0;
			_value1 = value;
			_value2 = default!;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Or{T1, T2}"/> struct with a value of type <typeparamref name="T2"/>.
		/// </summary>
		/// <param name="value">The value to store.</param>
		/// <exception cref="Exception">Thrown when <typeparamref name="T1"/> and <typeparamref name="T2"/> are the same type.</exception>
		public Or(T2 value)
		{
			_variantIndex = 1;
			_value1 = default!;
			_value2 = value;
		}

		/// <summary>
		/// Explicitly creates a new <see cref="Or{T1, T2}"/> struct with a value of type <typeparamref name="T1"/>.
		/// </summary>
		/// <param name="value">The value to store.</param>
		/// <returns>A new created <see cref="Or{T1, T2}"/> with used <typeparamref name="T1"/> type as active.</returns>
		public static Or<T1, T2> CreateT1(T1 value)
		{
			return new Or<T1, T2>(value);
		}

		/// <summary>
		/// Explicitly creates a new <see cref="Or{T1, T2}"/> struct with a value of type <typeparamref name="T2"/>.
		/// </summary>
		/// <param name="value">The value to store.</param>
		/// <returns>A new created <see cref="Or{T1, T2}"/> with used <typeparamref name="T2"/> type as active.</returns>
		public static Or<T1, T2> CreateT2(T2 value)
		{
			return new Or<T1, T2>(value);
		}

		/// <summary>
		/// Gets the value as <typeparamref name="T1"/> or <see langword="default"/> if the active variant is not <typeparamref name="T1"/>.
		/// </summary>
		/// <returns>The value as <typeparamref name="T1"/> or <see langword="default"/>.</returns>
		public T1 AsT1() =>
			_variantIndex == 0
				? _value1
				: default;

		/// <summary>
		/// Gets the value as <typeparamref name="T2"/> or <see langword="default"/> if the active variant is not <typeparamref name="T2"/>.
		/// </summary>
		/// <returns>The value as <typeparamref name="T2"/> or <see langword="default"/>.</returns>
		public T2 AsT2() =>
			_variantIndex == 1
				? _value2
				: default;

		/// <summary>
		/// Gets the value as <typeparamref name="T2"/> or <see langword="default"/> if the active variant is not <typeparamref name="T2"/>.
		/// </summary>
		/// <param name="value">When this method returns, contains the value if the active variant is <typeparamref name="T1"/>; otherwise, the default value.</param>
		/// <returns>true if the active variant is <typeparamref name="T1"/>; otherwise, false.</returns>
		public bool TryGetT1(out T1 value)
		{
			if (_variantIndex == 0)
			{
				value = _value1;
				return true;
			}
			value = default!;
			return false;
		}

		/// <summary>
		/// Attempts to get the value as <typeparamref name="T2"/>.
		/// </summary>
		/// <param name="value">When this method returns, contains the value if the active variant is <typeparamref name="T2"/>; otherwise, the default value.</param>
		/// <returns>true if the active variant is <typeparamref name="T2"/>; otherwise, false.</returns>
		public bool TryGetT2(out T2 value)
		{
			if (_variantIndex == 1)
			{
				value = _value2;
				return true;
			}
			value = default!;
			return false;
		}

		/// <summary>
		/// Determines whether the active variant is of the specified type.
		/// </summary>
		/// <typeparam name="T">The type to check against.</typeparam>
		/// <returns>true if the active variant is of type <typeparamref name="T"/>; otherwise, false.</returns>
		public bool Is<T>()
		{
			if (typeof(T) == typeof(T1) && VariantIndex == 0)
				return true;
			if (typeof(T) == typeof(T2) && VariantIndex == 1)
				return true;

			return false;
		}

		/// <summary>
		/// Matches the active variant and executes the corresponding handler function.
		/// </summary>
		/// <typeparam name="T">The return type of the handler functions.</typeparam>
		/// <param name="t1Handler">The function to execute if the active variant is <typeparamref name="T1"/>.</param>
		/// <param name="t2Handler">The function to execute if the active variant is <typeparamref name="T2"/>.</param>
		/// <returns>The result of the executed handler function.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the variant index is invalid.</exception>
		public T Match<T>(Func<T1, T> t1Handler, Func<T2, T> t2Handler)
		{
			switch (VariantIndex)
			{
				case 0:
					return t1Handler(_value1);
				case 1:
					return t2Handler(_value2);
				default:
					throw new InvalidOperationException("Invalid variant index.");
			}
		}

		/// <summary>
		/// Executes an action based on the active variant.
		/// </summary>
		/// <param name="t1Handler">The action to execute if the active variant is <typeparamref name="T1"/>.</param>
		/// <param name="t2Handler">The action to execute if the active variant is <typeparamref name="T2"/>.</param>
		/// <exception cref="InvalidOperationException">Thrown when the variant index is invalid.</exception>
		public void Switch(Action<T1> t1Handler, Action<T2> t2Handler)
		{
			switch (VariantIndex)
			{
				case 0:
					t1Handler(_value1);
					return;
				case 1:
					t2Handler(_value2);
					return;
				default:
					throw new InvalidOperationException("Invalid variant index.");
			}
		}

		/// <summary>
		/// Deconstructs the active variant into output parameter; other parameters will be set to <see langword="default"/>.
		/// </summary>
		/// <param name="value1">The value to set if active type is <typeparamref name="T1"/>.</param>
		/// <param name="value2">The value to set if active type is <typeparamref name="T2"/>.</param>
		public void Deconstruct(out T1 value1, out T2 value2)
		{
			// Non-active variants is default (see constructor)
			value1 = _value1;
			value2 = _value2;
		}

		public override bool Equals(object other)
		{
			if (other is null)
				return false;

			if (other is Or<T1, T2> otherOr)
				return Equals(otherOr);

			return false;
		}

		public bool Equals(Or<T1, T2> other)
		{
			if (VariantIndex != other.VariantIndex)
				return false;

			switch (VariantIndex)
			{
				case 0:
					return Equals(_value1, other._value1);
				case 1:
					return Equals(_value2, other._value2);
				default:
					return false;
			}
		}

		public override int GetHashCode()
		{
			int hc = VariantIndex.GetHashCode();
			switch (VariantIndex)
			{
				case 0:
					return (hc * 377) ^ _value1?.GetHashCode() ?? 0;
				case 1:
					return (hc * 377) ^ _value2?.GetHashCode() ?? 0;
				default:
					return hc;
			}
		}

		public static implicit operator Or<T1, T2>(T1 value)
		{
			return new Or<T1, T2>(value);
		}
		public static implicit operator Or<T1, T2>(T2 value)
		{
			return new Or<T1, T2>(value);
		}

		public static bool operator == (Or<T1, T2> left, Or<T1, T2> right)
		{
			return Equals(left, right);
		}
		public static bool operator != (Or<T1, T2> left, Or<T1, T2> right)
		{
			return !Equals(left, right);
		}

		public static bool operator == (Or<T1, T2> left, T1 right)
		{
			if (left.VariantIndex == 0)
				return Equals(left._value1, right);
			return false;
		}
		public static bool operator != (Or<T1, T2> left, T1 right)
		{
			return !(left == right);
		}
		public static bool operator == (Or<T1, T2> left, T2 right)
		{
			if (left.VariantIndex == 1)
				return Equals(left._value2, right);
			return false;
		}
		public static bool operator != (Or<T1, T2> left, T2 right)
		{
			return !(left == right);
		}
		public static bool operator ==(T1 left, Or<T1, T2> right)
		{
			return right == left;
		}
		public static bool operator !=(T1 left, Or<T1, T2> right)
		{
			return right != left;
		}
		public static bool operator ==(T2 left, Or<T1, T2> right)
		{
			return right == left;
		}
		public static bool operator !=(T2 left, Or<T1, T2> right)
		{
			return right != left;
		}
	}
}