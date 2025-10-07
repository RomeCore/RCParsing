using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCParsing.Utils
{
	internal static class Extensions
	{
		/// <summary>
		/// Splits the string into lines using '\r\n', '\r' and '\n' as delimiters. It is a wrapper for <see cref="string.Split(string[], StringSplitOptions)"/> with predefined parameters.
		/// </summary>
		/// <param name="str">The string to split.</param>
		/// <param name="options">String splitting options. Default is <see cref="StringSplitOptions.RemoveEmptyEntries"/>.</param>
		/// <returns>Array of lines.</returns>
		public static string[] SplitLines(this string str, StringSplitOptions options = StringSplitOptions.None)
		{
			return str.Split(new[] { "\r\n", "\r", "\n" }, options);
		}

		/// <summary>
		/// Splits the string into lines using '\r\n', '\r' and '\n' as delimiters. It is a wrapper for <see cref="string.Split(string[], StringSplitOptions)"/> with predefined parameters.
		/// </summary>
		/// <param name="str">The string to split.</param>
		/// <param name="maxCount">Maximum number of substrings to return.</param>
		/// <param name="options">String splitting options. Default is <see cref="StringSplitOptions.RemoveEmptyEntries"/>.</param>
		/// <returns>Array of lines.</returns>
		public static string[] SplitLines(this string str, int maxCount, StringSplitOptions options = StringSplitOptions.None)
		{
			return str.Split(new[] { "\r\n", "\r", "\n" }, maxCount, options);
		}

		/// <summary>
		/// Adds an indentation to each line of the provided string.
		/// </summary>
		/// <param name="str">The string to indent.</param>
		/// <param name="indentString">The string to use as the indentation.</param>
		/// <param name="addIndentToFirstLine">Whether to add indentation to the first line. Default is <see langword="true"/>.</param>
		/// <returns></returns>
		public static string Indent(this string str, string indentString = "\t", bool addIndentToFirstLine = true)
		{
			if (string.IsNullOrEmpty(str))
				return str;

			var lines = str.SplitLines();
			var sb = new StringBuilder();

			for (int i = 0; i < lines.Length; i++)
			{
				if (i > 0 || addIndentToFirstLine)
					sb.Append(indentString);
				sb.Append(lines[i]);
				if (i < lines.Length - 1)
					sb.AppendLine();
			}

			return sb.ToString();
		}

		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="comparison"/> means ignore case string comparison.
		/// </summary>
		/// <param name="comparison">The string comparison to check.</param>
		public static bool IsIgnoreCase(this StringComparison comparison)
		{
			return comparison == StringComparison.OrdinalIgnoreCase ||
				comparison == StringComparison.CurrentCultureIgnoreCase ||
				comparison == StringComparison.InvariantCultureIgnoreCase;
		}
		
		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="comparison"/> means case-sensitive string comparison.
		/// </summary>
		/// <param name="comparison">The string comparison to check.</param>
		public static bool IsCaseSensitive(this StringComparison comparison)
		{
			return comparison == StringComparison.Ordinal ||
				comparison == StringComparison.CurrentCulture ||
				comparison == StringComparison.InvariantCulture;
		}

		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="comparer"/> is one of default set from <see cref="StringComparer"/> that means ignore case string comparison.
		/// </summary>
		/// <param name="comparer">The string comparer to check.</param>
		public static bool IsDefaultIgnoreCase(this StringComparer comparer)
		{
			return comparer == StringComparer.OrdinalIgnoreCase ||
				comparer == StringComparer.CurrentCultureIgnoreCase ||
				comparer == StringComparer.InvariantCultureIgnoreCase;
		}

		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="comparer"/> is one of default set from <see cref="StringComparer"/> that means case-sensitive string comparison.
		/// </summary>
		/// <param name="comparer">The string comparer to check.</param>
		public static bool IsDefaultCaseSensitive(this StringComparer? comparer)
		{
			return comparer == StringComparer.Ordinal ||
				comparer == StringComparer.CurrentCulture ||
				comparer == StringComparer.InvariantCulture;
		}

		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="comparer"/> is one of default set from <see cref="StringComparer"/> or <see langword="null"/>.
		/// </summary>
		/// <param name="comparer">The string comparer to check.</param>
		public static bool IsNullOrDefault(this StringComparer? comparer)
		{
			return comparer == null ||
				comparer == StringComparer.Ordinal ||
				comparer == StringComparer.CurrentCulture ||
				comparer == StringComparer.InvariantCulture ||
				comparer == StringComparer.OrdinalIgnoreCase ||
				comparer == StringComparer.CurrentCultureIgnoreCase ||
				comparer == StringComparer.InvariantCultureIgnoreCase;
		}
	}
}