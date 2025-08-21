using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace RCParsing.Utils
{
	/// <summary>
	/// Compares and compares equality of characters using a specified string comparer.
	/// </summary>
	public class CharComparer : IComparer<char>, IEqualityComparer<char>
	{
		private readonly StringComparer _strComp;

		public static CharComparer Ordinal { get; } = new CharComparer(StringComparer.Ordinal);
		public static CharComparer OrdinalIgnoreCase { get; } = new CharComparer(StringComparer.OrdinalIgnoreCase);
		public static CharComparer CurrentCulture { get; } = new CharComparer(StringComparer.CurrentCulture);
		public static CharComparer CurrentCultureIgnoreCase { get; } = new CharComparer(StringComparer.CurrentCultureIgnoreCase);
		public static CharComparer InvariantCulture { get; } = new CharComparer(StringComparer.InvariantCulture);
		public static CharComparer InvariantCultureIgnoreCase { get; } = new CharComparer(StringComparer.InvariantCultureIgnoreCase);

#if NET6_0_OR_GREATER
		/// <summary>
		/// Gets a <see cref="CharComparer"/> from the specified <see cref="StringComparison"/>.
		/// </summary>
		/// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
		/// <returns>A <see cref="CharComparer"/> that uses the specified <see cref="StringComparison"/>.</returns>
		public static CharComparer FromComparison(StringComparison comparison)
		{
			return new CharComparer(StringComparer.FromComparison(comparison));
		}
#endif

		/// <summary>
		/// Initializes a new instance of the <see cref="CharComparer" /> class.
		/// </summary>
		/// <param name="strComp">The string comparer to use for comparison.</param>
		public CharComparer(StringComparer strComp)
		{
			_strComp = strComp;
		}

		public int Compare(char x, char y)
		{
			return _strComp.Compare(x.ToString(), y.ToString());
		}

		public bool Equals(char x, char y)
		{
			return _strComp.Equals(x.ToString(), y.ToString());
		}

		public int GetHashCode(char obj)
		{
			return _strComp.GetHashCode(obj.ToString());
		}
	}
}