using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCParsing.Utils
{
	/// <summary>
	/// A lazy-loaded array that initializes its elements on demand.
	/// </summary>
	public sealed class LazyArray<T> : IEnumerable<T>, IReadOnlyList<T>
	{
		private readonly Func<int, T> _factory;
		private BitArray? _initialized;
		private T[]? _array;

		int IReadOnlyCollection<T>.Count => Length;
		T IReadOnlyList<T>.this[int index] => this[index];

		/// <summary>
		/// Gets the length of the array.
		/// </summary>
		public int Length { get; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void TryCreateArrays()
		{
			if (_initialized == null)
			{
				_initialized = new BitArray(Length);
				_array = new T[Length];
			}
		}

		/// <summary>
		/// Gets the element at the specified index.
		/// </summary>
		/// <param name="index">The zero-based index of the element to get.</param>
		/// <returns>The element at the specified index.</returns>
		public T this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				TryCreateArrays();

				if (_initialized[index])
					return _array[index];

				_initialized[index] = true;
				var value = _array[index] = _factory(index);
				return value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LazyArray{T}"/> class.
		/// </summary>
		/// <param name="length">The length of the array.</param>
		/// <param name="factory">The function to create elements on demand.</param>
		public LazyArray(int length, Func<int, T> factory)
		{
			Length = length;
			_factory = factory;
		}

		/// <summary>
		/// Copies the elements of the array to a new array with new factory.
		/// </summary>
		/// <param name="factory">The function to create elements on demand for the new array.</param>
		/// <returns>A new instance of the <see cref="LazyArray{T}"/> class with the same length, copied elements and new factory.</returns>
		public LazyArray<T> WithFactory(Func<int, T> factory)
		{
			var result = new LazyArray<T>(Length, factory);
			if (_initialized != null)
			{
				result.TryCreateArrays();
				for (int i = 0; i < Length; i++)
				{
					if (_initialized[i])
					{
						result._initialized[i] = true;
						result._array[i] = _array[i];
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Copies the elements of the array to a new array with some elements invalidated.
		/// </summary>
		/// <param name="factory">The function to create elements on demand for the new array.</param>
		/// <param name="predicate">The function to determine which elements to invalidate.</param>
		/// <returns>A new instance of the <see cref="LazyArray{T}"/> class with the same length, copied valid elements and new factory.</returns>
		public LazyArray<T> InvalidatedWhere(Func<int, T> factory, Func<int, T, bool> predicate)
		{
			var result = new LazyArray<T>(Length, factory);
			if (_initialized != null)
			{
				result.TryCreateArrays();
				for (int i = 0; i < Length; i++)
				{
					if (_initialized[i] && predicate(i, _array[i]))
					{
						result._initialized[i] = true;
						result._array[i] = _array[i];
					}
				}
			}
			return result;
		}

		/// <summary>
		/// Copies the elements of the array to a new array with all initialized elements converted.
		/// </summary>
		/// <param name="factory">The function to create elements on demand for the new array.</param>
		/// <param name="converter">The function to convert elements.</param>
		/// <returns>A new instance of the <see cref="LazyArray{T}"/> class with the same length, converted elements and new factory.</returns>
		public LazyArray<T> Converted(Func<int, T> factory, Func<int, T, T> converter)
		{
			var result = new LazyArray<T>(Length, factory);
			if (_initialized != null)
			{
				result.TryCreateArrays();
				for (int i = 0; i < Length; i++)
				{
					if (_initialized[i])
					{
						result._initialized[i] = true;
						result._array[i] = converter(i, _array[i]);
					}
				}
			}
			return result;
		}

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < Length; i++)
				yield return this[i];
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}