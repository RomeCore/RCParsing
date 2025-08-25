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
		private BitArray _initialized;
		private T[] _array;

		int IReadOnlyCollection<T>.Count => Length;
		T IReadOnlyList<T>.this[int index] => this[index];

		/// <summary>
		/// Gets the length of the array.
		/// </summary>
		public int Length { get; }

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
				if (_initialized == null)
				{
					_initialized = new BitArray(Length);
					_array = new T[Length];
				}

				if (_initialized[index])
					return _array[index];

				var value = _array[index] = _factory(index);
				_initialized[index] = true;
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