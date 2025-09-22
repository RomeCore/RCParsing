using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RCParsing.Utils
{
	/// <summary>
	/// A simple implementation of a lazy initialization pattern. This class is used to defer the creation of an object until it is actually needed.
	/// </summary>
	/// <remarks>
	/// Much faster than System.Lazy, but less feature-rich.
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	public sealed class LazyValue<T>
	{
		private readonly Func<T> _factory;
		private bool _isValueCreated;
		private T _value;

		/// <summary>
		/// Initializes a new instance of the <see cref="LazyValue{T}"/> class. The provided factory function will be called to create the value when it is first accessed.
		/// </summary>
		/// <param name="factory">The function to call when the value is first accessed. This function should return an instance of type <typeparamref name="T"/>.</param>
		public LazyValue(Func<T> factory)
		{
			_factory = factory ?? throw new ArgumentNullException(nameof(factory));
			_isValueCreated = false;
			_value = default;
		}

		/// <summary>
		/// Gets the value of the lazy-initialized object. If the value has not yet been created, it will be created by calling the factory function provided during initialization.
		/// </summary>
		public T Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				if (_isValueCreated)
					return _value;
				_isValueCreated = true;
				_value = _factory();
				return _value;
			}
		}
	}
}