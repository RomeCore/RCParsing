using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RCParsing.Utils
{
	/// <summary>
	/// A read-only list wrapper that presents its elements in reverse order.
	/// </summary>
	/// <typeparam name="T">The type of elements in the list.</typeparam>
	public class ReversedList<T> : IReadOnlyList<T>
	{
		private readonly IReadOnlyList<T> _list;

		/// <summary>
		/// Creates a new instance of the <see cref="ReversedList{T}"/> class.
		/// </summary>
		/// <param name="list">The list to reverse.</param>
		public ReversedList(IReadOnlyList<T> list)
		{
			_list = list;
		}

		private class ReversedListEnumerator : IEnumerator<T>
		{
			private readonly IReadOnlyList<T> _list;
			private int _index;

			public ReversedListEnumerator(IReadOnlyList<T> list)
			{
				_list = list;
				_index = _list.Count;
			}

			public T Current => _list[_index];
			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				_index--;
				return _index >= 0;
			}

			public void Reset()
			{
				_index = _list.Count;
			}

			public void Dispose()
			{
			}
		}

		public T this[int index] => _list[_list.Count - 1 - index];
		public int Count => _list.Count;
		public IEnumerator<T> GetEnumerator() => new ReversedListEnumerator(_list);
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}