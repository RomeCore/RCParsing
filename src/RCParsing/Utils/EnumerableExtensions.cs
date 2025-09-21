using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCParsing.Utils
{
	internal static class EnumerableExtensions
	{
		/// <summary>
		/// Converts to new <see cref="ReadOnlyCollection{T}"/>.
		/// </summary>
		public static IReadOnlyList<T> AsReadOnlyList<T>(this IList<T> collection)
		{
			return new ReadOnlyCollection<T>(collection);
		}
		
		/// <summary>
		/// Casts to <see cref="ReadOnlyCollection{T}"/> or creates new.
		/// </summary>
		public static IReadOnlyList<T> AsReadOnlyCollection<T>(this IEnumerable<T> collection)
		{
			return collection as ReadOnlyCollection<T> ?? collection.ToList().AsReadOnly();
		}

		/// <summary>
		/// Casts to <see cref="IReadOnlyList{T}"/> or creates new <see cref="ReadOnlyCollection{T}"/>.
		/// </summary>
		public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> collection)
		{
			return collection as IReadOnlyList<T> ?? collection.ToList().AsReadOnly();
		}

		/// <summary>
		/// Casts to <see cref="ReadOnlyDictionary{TKey, TValue}"/> or creates new.
		/// </summary>
		public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
		{
			return dictionary as ReadOnlyDictionary<TKey, TValue> ?? new ReadOnlyDictionary<TKey, TValue>(dictionary);
		}

		/// <summary>
		/// Checks if two collections are equal using the specified comparer. If no comparer is provided, uses default equality comparer for type T.
		/// </summary>
		public static bool SetEqual<T>(this IEnumerable<T> first, IEnumerable<T> second, IEqualityComparer<T>? comparer = null)
		{
			if (first == null)
				throw new ArgumentNullException(nameof(first));
			if (second == null)
				throw new ArgumentNullException(nameof(second));

			comparer ??= EqualityComparer<T>.Default;

			var firstSet = new HashSet<T>(first, comparer);
			var secondSet = new HashSet<T>(second, comparer);

			return firstSet.SetEquals(secondSet);
		}

		/// <summary>
		/// Gets the combined hash code for <see cref="IEnumerable"/> depending on the order of items.
		/// </summary>
		public static int GetSequenceHashCode(this IEnumerable collection, IEqualityComparer? comparer = null)
		{
			int result = 17;

			unchecked
			{
				if (comparer == null)
					foreach (var item in collection)
						result *= (item?.GetHashCode() ?? 0) * 397;
				else
					foreach (var item in collection)
						result *= (item != null ? comparer.GetHashCode(item) : 0) * 397 + 1597851631;
			}

			return result;
		}

		/// <summary>
		/// Gets the combined hash code for <see cref="IEnumerable"/> regardless of order.
		/// </summary>
		public static int GetSetHashCode(this IEnumerable collection, IEqualityComparer? comparer = null)
		{
			int result = 17;

			unchecked
			{
				if (comparer == null)
					foreach (var item in collection)
						result ^= (item?.GetHashCode() ?? 0) * 397;
				else
					foreach (var item in collection)
						result ^= (item != null ? comparer.GetHashCode(item) : 0) * 397 + 1597851631;
			}

			return result;
		}

		/// <summary>
		/// Gets the combined hash code for <see cref="IEnumerable"/> depending on the order of items.
		/// </summary>
		public static int GetSequenceHashCode<T>(this IEnumerable<T> collection, IEqualityComparer<T>? comparer = null)
		{
			int result = 17;

			unchecked
			{
				if (comparer == null)
					foreach (var item in collection)
						result *= (item?.GetHashCode() ?? 0) * 397;
				else
					foreach (var item in collection)
						result *= (item != null ? comparer.GetHashCode(item) : 0) * 397 + 1597851631;
			}

			return result;
		}

		/// <summary>
		/// Gets the combined hash code for <see cref="IEnumerable"/> regardless of order.
		/// </summary>
		public static int GetSetHashCode<T>(this IEnumerable<T> collection, IEqualityComparer<T>? comparer = null)
		{
			int result = 17;

			unchecked
			{
				if (comparer == null)
					foreach (var item in collection)
						result ^= (item?.GetHashCode() ?? 0) * 397;
				else
					foreach (var item in collection)
						result ^= (item != null ? comparer.GetHashCode(item) : 0) * 397 + 1597851631;
			}

			return result;
		}

		/// <summary>
		/// Checks if <see cref="IEnumerable"/> has elements.
		/// </summary>
		public static bool Any(this IEnumerable enumerable)
		{
			foreach (var e in enumerable)
				return true;
			return false;
		}

		/// <summary>
		/// Checks if <see cref="IEnumerable"/> is empty.
		/// </summary>
		public static bool Empty(this IEnumerable enumerable)
		{
			foreach (var e in enumerable)
				return false;
			return true;
		}

		/// <summary>
		/// Returns null if <see cref="IEnumerable"/> is null or empty.
		/// </summary>
		public static TEnum? ToNullIfEmpty<TEnum>(this TEnum? enumerable)
			where TEnum : class, IEnumerable
		{
			if (enumerable?.Any() ?? false)
				return enumerable;
			return default;
		}
		
		/// <summary>
		/// Returns <see langword="true"/> if <see cref="IEnumerable"/> is null or empty.
		/// </summary>
		public static bool IsNullOrEmpty(this IEnumerable? enumerable)
		{
			if (enumerable?.Any() ?? false)
				return false;
			return true;
		}

		/// <summary>
		/// Wraps <see cref="IEnumerator"/> into <see cref="IEnumerable"/>.
		/// </summary>
		public static IEnumerable Wrap(this IEnumerator enumerator)
		{
			return new EnumerableWrapper(() => enumerator);
		}

		/// <summary>
		/// Wraps <see cref="IEnumerator{T}"/> into <see cref="IEnumerable{T}"/>.
		/// </summary>
		public static IEnumerable<T> Wrap<T>(this IEnumerator<T> enumerator)
		{
			return new EnumerableWrapper<T>(() => enumerator);
		}

		/// <summary>
		/// Wraps <see cref="IEnumerable"/> into <see cref="IEnumerable"/> to prevent modifications via downcasting.
		/// </summary>
		public static IEnumerable Wrap(this IEnumerable enumerable)
		{
			return new EnumerableWrapper(enumerable.GetEnumerator);
		}

		/// <summary>
		/// Wraps <see cref="IEnumerable{T}"/> into <see cref="IEnumerable{T}"/> to prevent modifications via downcasting.
		/// </summary>
		public static IEnumerable<T> Wrap<T>(this IEnumerable<T> enumerable)
		{
			return new EnumerableWrapper<T>(enumerable.GetEnumerator);
		}

		private class EnumerableWrapper : IEnumerable
		{
			private readonly Func<IEnumerator> _factory;

			public EnumerableWrapper(Func<IEnumerator> factory)
			{
				_factory = factory;
			}

			public IEnumerator GetEnumerator()
			{
				return _factory.Invoke();
			}
		}

		private class EnumerableWrapper<T> : IEnumerable<T>
		{
			private readonly Func<IEnumerator<T>> _factory;

			public EnumerableWrapper(Func<IEnumerator<T>> factory)
			{
				_factory = factory;
			}

			public IEnumerator<T> GetEnumerator()
			{
				return _factory.Invoke();
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		/// <summary>
		/// Creates an enumerable that contains this value.
		/// </summary>
		public static IEnumerable<T> WrapIntoEnumerable<T>(this T value)
		{
			yield return value;
		}

		/// <summary>
		/// Creates an array that contains this value.
		/// </summary>
		public static T[] WrapIntoArray<T>(this T value)
		{
			return new T[] { value };
		}
	}
}