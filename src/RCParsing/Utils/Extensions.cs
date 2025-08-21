using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
	}
}