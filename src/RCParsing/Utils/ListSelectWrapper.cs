using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCParsing.Utils
{
	/// <summary>
	/// A read-only wrapper around a list that applies a transformation to each element when accessed.
	/// </summary>
	/// <typeparam name="TIn">The type of the elements in the original list.</typeparam>
	/// <typeparam name="TOut">The type of the elements after transformation.</typeparam>
	public class ListSelectWrapper<TIn, TOut> : IReadOnlyList<TOut>
	{
		private readonly IReadOnlyList<TIn> _list;
		private readonly Func<TIn, TOut> _selector;

		/// <summary>
		/// Initializes a new instance of the <see cref="ListSelectWrapper{TIn, TOut}" /> class.
		/// </summary>
		/// <param name="list">The original list to wrap.</param>
		/// <param name="selector">The transformation function to apply to each element.</param>
		public ListSelectWrapper(IReadOnlyList<TIn> list, Func<TIn, TOut> selector)
		{
			_list = list;
			_selector = selector;
		}

		public TOut this[int index] => _selector(_list[index]);
		public int Count => _list.Count;

		public IEnumerator<TOut> GetEnumerator()
		{
			return _list.Select(_selector).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}