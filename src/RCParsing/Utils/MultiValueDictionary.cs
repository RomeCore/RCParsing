using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RCParsing.Utils
{
	/// <summary>
	/// A thread-safe dictionary that associates a single key with multiple values.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
	public class MultiValueDictionary<TKey, TValue> : IDictionary<TKey, ICollection<TValue>>, IReadOnlyDictionary<TKey, ICollection<TValue>>, IEnumerable<KeyValuePair<TKey, ICollection<TValue>>>
	{
		private class Entry
		{
			public List<TValue> Values { get; }
			public ReadOnlyCollection<TValue> ReadOnlyValues { get; }

			public Entry()
			{
				Values = new List<TValue>();
				ReadOnlyValues = new ReadOnlyCollection<TValue>(Values);
			}

			public Entry(TValue value)
			{
				Values = new List<TValue> { value };
				ReadOnlyValues = new ReadOnlyCollection<TValue>(Values);
			}

			public Entry(IEnumerable<TValue> values)
			{
				Values = new List<TValue>(values);
				ReadOnlyValues = new ReadOnlyCollection<TValue>(Values);
			}
		}

		private Entry _nullEntry;
		private readonly ConcurrentDictionary<TKey, Entry> _dictionary;
		private readonly IEqualityComparer<TKey> _comparer;

		/// <summary>
		/// Initializes a new instance of the <see cref="MultiValueDictionary{TKey, TValue}"/> class.
		/// </summary>
		/// <param name="comparer">The equality comparer to use for keys. If null, the default comparer is used.</param>
		public MultiValueDictionary(IEqualityComparer<TKey> comparer = null)
		{
			_comparer = comparer ?? EqualityComparer<TKey>.Default;
			_dictionary = new ConcurrentDictionary<TKey, Entry>(_comparer);
		}

		/// <summary>
		/// Gets or sets the collection of values associated with the specified key.
		/// </summary>
		/// <param name="key">The key to get or set.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
		/// <exception cref="KeyNotFoundException">Thrown if the key does not exist when getting.</exception>
		public ICollection<TValue> this[TKey key]
		{
			get
			{
				if (key == null)
				{
					if (_nullEntry == null)
						throw new KeyNotFoundException($"The key '{null}' was not found.");

					lock (_nullEntry)
						return _nullEntry.ReadOnlyValues;
				}

				if (!_dictionary.TryGetValue(key, out var list))
					throw new KeyNotFoundException($"The key '{key}' was not found.");

				lock (list)
					return list.ReadOnlyValues;
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (key == null)
					_nullEntry = new Entry(value);
				else
					_dictionary[key] = new Entry(value);
			}
		}

		/// <summary>
		/// Gets the collection of keys in the dictionary.
		/// </summary>
		public ICollection<TKey> Keys { get
			{
				if (_nullEntry != null)
					return new List<TKey> { default }.Concat(_dictionary.Keys).ToList();
				return _dictionary.Keys;
			}
		}

		/// <summary>
		/// Gets the collection of value collections in the dictionary.
		/// </summary>
		public ICollection<ICollection<TValue>> Values { get
			{
				var collections = _dictionary.Values.Select(list =>
				{
					lock (list)
						return (ICollection<TValue>)list.ReadOnlyValues;
				});

				if (_nullEntry != null)
					lock (_nullEntry)
						collections = collections.Prepend(_nullEntry.Values);

				return collections.ToList();
			}
		}

		/// <summary>
		/// Adds a key with an associated collection of values to the dictionary.
		/// </summary>
		/// <param name="key">The key to add.</param>
		/// <param name="value">The collection of values to associate with the key.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if the key already exists.</exception>
		public void Add(TKey key, ICollection<TValue> value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (key == null)
			{
				if (_nullEntry != null)
					throw new ArgumentException($"An item with the key '{null}' already exists.", nameof(key));
				_nullEntry = new Entry(value);
			}

			if (!_dictionary.TryAdd(key, new Entry(value)))
				throw new ArgumentException($"An item with the key '{key}' already exists.", nameof(key));
		}

		/// <summary>
		/// Adds a key-value pair to the dictionary.
		/// </summary>
		/// <param name="item">The key-value pair to add.</param>
		public void Add(KeyValuePair<TKey, ICollection<TValue>> item)
		{
			Add(item.Key, item.Value);
		}

		/// <summary>
		/// Determines whether the dictionary contains a specific key-value pair.
		/// </summary>
		/// <param name="item">The key-value pair to check.</param>
		/// <returns>True if the key-value pair exists; otherwise, false.</returns>
		public bool Contains(KeyValuePair<TKey, ICollection<TValue>> item)
		{
			if (item.Key == null)
			{
				if (_nullEntry == null)
					return false;
				lock (_nullEntry)
					return item.Value.SequenceEqual(_nullEntry.ReadOnlyValues);
			}

			if (_dictionary.TryGetValue(item.Key, out var list))
				lock (list)
					return list.Values.SequenceEqual(item.Value);

			return false;
		}

		/// <summary>
		/// Copies the dictionary's key-value pairs to an array.
		/// </summary>
		/// <param name="array">The array to copy to.</param>
		/// <param name="arrayIndex">The starting index in the array.</param>
		public void CopyTo(KeyValuePair<TKey, ICollection<TValue>>[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			if (arrayIndex < 0 || arrayIndex > array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			if (array.Length - arrayIndex < _dictionary.Count)
				throw new ArgumentException("The array is too small to accommodate the dictionary contents.");

			int i = arrayIndex;

			if (_nullEntry != null)
				lock (_nullEntry)
					array[i++] = new KeyValuePair<TKey, ICollection<TValue>>(default, _nullEntry.ReadOnlyValues);

			foreach (var kvp in _dictionary)
				lock (kvp.Value)
					array[i++] = new KeyValuePair<TKey, ICollection<TValue>>(kvp.Key, kvp.Value.ReadOnlyValues);
		}

		/// <summary>
		/// Gets the number of keys in the dictionary.
		/// </summary>
		public int Count => _nullEntry != null ? _dictionary.Count + 1 : _dictionary.Count;

		/// <summary>
		/// Gets a value indicating whether the dictionary is read-only (always false).
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>
		/// Removes a key and its associated values from the dictionary.
		/// </summary>
		/// <param name="key">The key to remove.</param>
		/// <returns>True if the key was removed; false if it was not found.</returns>
		public bool Remove(TKey key)
		{
			return RemoveKey(key);
		}

		/// <summary>
		/// Removes a specific key-value pair from the dictionary.
		/// </summary>
		/// <param name="item">The key-value pair to remove.</param>
		/// <returns>True if the key-value pair was removed; false if not found.</returns>
		public bool Remove(KeyValuePair<TKey, ICollection<TValue>> item)
		{
			if (item.Key == null)
			{
				if (_nullEntry == null)
					return false;

				lock (_nullEntry)
				{
					if (_nullEntry.Values.SequenceEqual(item.Value))
					{
						_nullEntry = null;
						return true;
					}
					return false;
				}
			}

			if (_dictionary.TryGetValue(item.Key, out var list))
				lock (list)
					if (list.Values.SequenceEqual(item.Value))
						return _dictionary.TryRemove(item.Key, out _);

			return false;
		}

		/// <summary>
		/// Tries to get the collection of values for a specified key.
		/// </summary>
		/// <param name="key">The key to look up.</param>
		/// <param name="value">When this method returns, contains the collection of values if the key exists; otherwise, null.</param>
		/// <returns>True if the key exists; false otherwise.</returns>
		public bool TryGetValue(TKey key, out ICollection<TValue> value)
		{
			if (key == null)
			{
				if (_nullEntry != null)
				{
					value = _nullEntry.ReadOnlyValues;
					return true;
				}
				value = null;
				return false;
			}

			if (_dictionary.TryGetValue(key, out var list))
			{
				lock (list)
				{
					value = list.ReadOnlyValues;
					return true;
				}
			}
			value = null;
			return false;
		}

		/// <summary>
		/// Adds a single value to the collection associated with the specified key.
		/// </summary>
		public void Add(TKey key, TValue value)
		{
			if (key == null)
			{
				if (_nullEntry == null)
					_nullEntry = new Entry(value);
				else 
					lock (_nullEntry)
						_nullEntry.Values.Add(value);
			}

			else
				_dictionary.AddOrUpdate(
					key,
					_ => new Entry(value),
					(_, list) =>
					{
						lock (list)
						{
							list.Values.Add(value);
							return list;
						}
					});
		}

		/// <summary>
		/// Adds multiple values to the collection associated with the specified key.
		/// </summary>
		public void AddRange(TKey key, IEnumerable<TValue> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			if (key == null)
			{
				if (_nullEntry == null)
					_nullEntry = new Entry(values);
				else
					lock (_nullEntry)
						_nullEntry.Values.AddRange(values);
			}

			else
				_dictionary.AddOrUpdate(
				key,
				_ => new Entry(values),
				(_, list) =>
				{
					lock (list)
					{
						list.Values.AddRange(values);
						return list;
					}
				});
		}

		/// <summary>
		/// Removes a specific value from the collection associated with the specified key.
		/// </summary>
		public bool Remove(TKey key, TValue value)
		{
			if (key == null)
			{
				if (_nullEntry == null)
					return false;

				lock (_nullEntry)
					return _nullEntry.Values.Remove(value);
			}

			if (_dictionary.TryGetValue(key, out var list))
			{
				lock (list)
				{
					bool removed = list.Values.Remove(value);
					if (list.Values.Count == 0)
						_dictionary.TryRemove(key, out _);
					return removed;
				}
			}
			return false;
		}

		/// <summary>
		/// Removes all values associated with the specified key.
		/// </summary>
		public bool RemoveKey(TKey key)
		{
			if (key == null)
			{
				if (_nullEntry == null)
					return false;
				_nullEntry = null;
				return true;
			}

			return _dictionary.TryRemove(key, out _);
		}

		/// <summary>
		/// Clears all keys and their associated values from the dictionary.
		/// </summary>
		public void Clear()
		{
			_nullEntry = null;
			_dictionary.Clear();
		}

		/// <summary>
		/// Determines whether the dictionary contains the specified key.
		/// </summary>
		public bool ContainsKey(TKey key)
		{
			if (key == null)
				return _nullEntry != null;

			return _dictionary.ContainsKey(key);
		}

		/// <summary>
		/// Determines whether the dictionary contains the specified value for the given key.
		/// </summary>
		public bool ContainsValue(TKey key, TValue value)
		{
			if (key == null)
			{
				if (_nullEntry == null)
					return false;
				lock (_nullEntry)
					return _nullEntry.Values.Contains(value);
			}

			if (_dictionary.TryGetValue(key, out var list))
				lock (list)
					return list.Values.Contains(value);

			return false;
		}

		/// <summary>
		/// Gets all values associated with the specified key.
		/// </summary>
		public IEnumerable<TValue> GetValues(TKey key)
		{
			if (key == null)
			{
				if (_nullEntry == null)
					return Enumerable.Empty<TValue>();
				lock (_nullEntry)
					return _nullEntry.Values;
			}

			if (_dictionary.TryGetValue(key, out var list))
				lock (list)
					return list.ReadOnlyValues;

			return Enumerable.Empty<TValue>();
		}

		/// <summary>
		/// Gets the total number of values across all keys in the dictionary.
		/// </summary>
		public int ValueCount
		{
			get
			{
				var sum = _dictionary.Values.Sum(list =>
				{
					lock (list)
						return list.Values.Count;
				});

				if (_nullEntry != null)
					lock (_nullEntry)
						sum += _nullEntry.Values.Count;

				return sum;
			}
		}

		IEnumerable<TKey> IReadOnlyDictionary<TKey, ICollection<TValue>>.Keys => Keys;
		IEnumerable<ICollection<TValue>> IReadOnlyDictionary<TKey, ICollection<TValue>>.Values => Values;

		/// <summary>
		/// Returns an enumerator that iterates through the dictionary's key-value pairs.
		/// </summary>
		public IEnumerator<KeyValuePair<TKey, ICollection<TValue>>> GetEnumerator()
		{
			if (_nullEntry != null)
				lock (_nullEntry)
					yield return new KeyValuePair<TKey, ICollection<TValue>>(default, _nullEntry.ReadOnlyValues);

			foreach (var kvp in _dictionary)
				lock (kvp.Value)
					yield return new KeyValuePair<TKey, ICollection<TValue>>(kvp.Key, kvp.Value.ReadOnlyValues);
		}

		/// <summary>
		/// Returns an enumerator that iterates through the dictionary's key-value pairs (non-generic).
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

	/// <summary>
	/// Extension methods for working with <see cref="MultiValueDictionary{TKey, TValue}"/>.
	/// </summary>
	public static class MultiValueDictionaryExtensions
	{
		/// <summary>
		/// Converts an enumerable of key-value pairs to a MultiValueDictionary.
		/// </summary>
		public static MultiValueDictionary<TKey, TValue> ToMultiValueDictionary<TKey, TValue>(
			this IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> source,
			IEqualityComparer<TKey> comparer = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var multiDict = new MultiValueDictionary<TKey, TValue>(comparer);
			foreach (var kvp in source)
			{
				if (kvp.Key != null)
				{
					multiDict.AddRange(kvp.Key, kvp.Value ?? Enumerable.Empty<TValue>());
				}
			}
			return multiDict;
		}

		/// <summary>
		/// Flattens all values in the MultiValueDictionary into a single enumerable.
		/// </summary>
		public static IEnumerable<TValue> FlattenValues<TKey, TValue>(this MultiValueDictionary<TKey, TValue> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			foreach (var list in source.Values)
			{
				foreach (var value in list)
				{
					yield return value;
				}
			}
		}

		/// <summary>
		/// Converts an enumerable by a key and value selectors into a MultiValueDictionary.
		/// </summary>
		public static MultiValueDictionary<TKey, TElement> ToMultiValueDictionary<TSource, TKey, TElement>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, TElement> elementSelector,
			IEqualityComparer<TKey> comparer = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (keySelector == null)
				throw new ArgumentNullException(nameof(keySelector));
			if (elementSelector == null)
				throw new ArgumentNullException(nameof(elementSelector));

			var multiDict = new MultiValueDictionary<TKey, TElement>(comparer);
			foreach (var item in source)
			{
				var key = keySelector(item);
				multiDict.Add(key, elementSelector(item));
			}
			return multiDict;
		}
		
		/// <summary>
		/// Converts an enumerable by a key selector into a MultiValueDictionary.
		/// </summary>
		public static MultiValueDictionary<TKey, TElement> ToMultiValueDictionary<TSource, TKey, TElement>(
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			Func<TSource, IEnumerable<TElement>> elementSelector,
			IEqualityComparer<TKey> comparer = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (keySelector == null)
				throw new ArgumentNullException(nameof(keySelector));
			if (elementSelector == null)
				throw new ArgumentNullException(nameof(elementSelector));

			var multiDict = new MultiValueDictionary<TKey, TElement>(comparer);
			foreach (var item in source)
			{
				var key = keySelector(item);
				multiDict.AddRange(key, elementSelector(item));
			}
			return multiDict;
		}

		/// <summary>
		/// Filters the MultiValueDictionary to include only keys that satisfy a predicate.
		/// </summary>
		public static MultiValueDictionary<TKey, TValue> WhereKeys<TKey, TValue>(
			this MultiValueDictionary<TKey, TValue> source,
			Func<TKey, bool> predicate,
			IEqualityComparer<TKey> comparer = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			var result = new MultiValueDictionary<TKey, TValue>(comparer);
			foreach (var kvp in source)
			{
				if (predicate(kvp.Key))
				{
					result.AddRange(kvp.Key, kvp.Value);
				}
			}
			return result;
		}

		/// <summary>
		/// Filters the MultiValueDictionary to include only key-value pairs where at least one value satisfies a predicate.
		/// </summary>
		public static MultiValueDictionary<TKey, TValue> WhereValues<TKey, TValue>(
			this MultiValueDictionary<TKey, TValue> source,
			Func<TValue, bool> predicate,
			IEqualityComparer<TKey> comparer = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			var result = new MultiValueDictionary<TKey, TValue>(comparer);
			foreach (var kvp in source)
			{
				var filteredValues = kvp.Value.Where(predicate).ToList();
				if (filteredValues.Any())
				{
					result.AddRange(kvp.Key, filteredValues);
				}
			}
			return result;
		}

		/// <summary>
		/// Merges another MultiValueDictionary into the source, combining values for duplicate keys.
		/// </summary>
		public static MultiValueDictionary<TKey, TValue> Merge<TKey, TValue>(
			this MultiValueDictionary<TKey, TValue> source,
			MultiValueDictionary<TKey, TValue> other,
			IEqualityComparer<TKey> comparer = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (other == null)
				throw new ArgumentNullException(nameof(other));

			var result = new MultiValueDictionary<TKey, TValue>(comparer);
			foreach (var kvp in source)
			{
				result.AddRange(kvp.Key, kvp.Value);
			}
			foreach (var kvp in other)
			{
				result.AddRange(kvp.Key, kvp.Value);
			}
			return result;
		}

		/// <summary>
		/// Converts the MultiValueDictionary to a standard Dictionary with a single value per key using a value selector.
		/// </summary>
		public static Dictionary<TKey, TResult> ToDictionary<TKey, TValue, TResult>(
			this MultiValueDictionary<TKey, TValue> source,
			Func<IEnumerable<TValue>, TResult> valueSelector,
			IEqualityComparer<TKey> comparer = null)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));
			if (valueSelector == null)
				throw new ArgumentNullException(nameof(valueSelector));

			var dict = new Dictionary<TKey, TResult>(comparer);
			foreach (var kvp in source)
			{
				if (kvp.Key != null)
					dict[kvp.Key] = valueSelector(kvp.Value);
			}
			return dict;
		}

		public static ICollection<TValue> GetValuesOrEmpty<TKey, TValue>(this MultiValueDictionary<TKey, TValue> source, TKey key)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (source.TryGetValue(key, out var values))
				return values;

			return new List<TValue>();
		}

		public static TValue GetFirstValueOrDefault<TKey, TValue>(this MultiValueDictionary<TKey, TValue> source, TKey key)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (source.TryGetValue(key, out var values))
			{
				return values.FirstOrDefault();
			}

			return default;
		}

		public static MultiValueDictionary<TKey, TValue> Flip<TKey, TValue>(this IEnumerable<KeyValuePair<TValue, TKey>> source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var multiDict = new MultiValueDictionary<TKey, TValue>();
			foreach (var kvp in source)
			{
				multiDict.Add(kvp.Value, kvp.Key);
			}
			return multiDict;
		}
	}
}