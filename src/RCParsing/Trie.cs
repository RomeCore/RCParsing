using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// A highly optimized and memory-efficient trie data structure for string lookup and data retrieval.
	/// </summary>
	/// <remarks>
	/// Uses 3 node types to optimize memory usage and performance: <br/>
	/// - Direct character nodes with up to 3 children <br/>
	/// - Array nodes with up to 256 ASCII children <br/>
	/// - Dictionary nodes with support of custom comparers and Unicode characters
	/// </remarks>
	public partial class Trie
	{
		private readonly TrieNode root;
		private readonly IEqualityComparer<char> comparer;

		/// <summary>
		/// Initializes a new empty instance of the <see cref="Trie"/> class.
		/// </summary>
		public Trie()
		{
			this.comparer = EqualityComparer<char>.Default;
			this.root = new TrieNode { type = TrieNodeType.Terminal }; // empty root
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Trie"/> class.
		/// </summary>
		/// <remarks>
		/// Builds an immutable trie from the provided sequence of (key, value) pairs.
		/// The trie structure is built once during construction and then becomes immutable.
		/// </remarks>
		/// <param name="entries">Sequence of key/value pairs to insert into the trie. Keys must not be null.</param>
		/// <param name="charComparer">Optional character comparer which will be passed into nodes; when provided
		/// dictionaries will be used to represent branches and character equality follows this comparer.</param>
		public Trie(IEnumerable<KeyValuePair<string, object?>> entries, IEqualityComparer<char>? charComparer = null)
		{
			if (entries == null)
				throw new ArgumentNullException(nameof(entries));

			this.comparer = charComparer ?? EqualityComparer<char>.Default;
			this.root = new TrieNode { type = TrieNodeType.Terminal };

			// Build the tree once. TrieNode.Add expects a comparer of type IEqualityComparer<char>.
			// (The node implementation will decide to use dictionary immediately if comparer != null.)
			foreach (var e in entries)
			{
				if (e.Key == null) throw new ArgumentException("Key cannot be null", nameof(entries));
				// Node.Add will mutate node fields while building — it's allowed during constructor.
				this.root.Add(e.Key, e.Value, this.comparer);
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Trie"/> class.
		/// </summary>
		/// <remarks>
		/// Convenience constructor for building a trie from keys only (values will be null).
		/// </remarks>
		/// <param name="keys">Sequence of keys to insert.</param>
		/// <param name="charComparer">Optional character comparer.</param>
		public Trie(IEnumerable<string> keys, IEqualityComparer<char>? charComparer = null)
			: this(BuildFromKeys(keys), charComparer)
		{
		}

		private static IEnumerable<KeyValuePair<string, object?>> BuildFromKeys(IEnumerable<string> keys)
		{
			if (keys == null) throw new ArgumentNullException(nameof(keys));
			foreach (var k in keys)
			{
				if (k == null) throw new ArgumentException("Key cannot be null", nameof(keys));
				yield return new KeyValuePair<string, object?>(k, null);
			}
		}

		/// <summary>
		/// Attempts to find the longest stored key that matches the input <paramref name="text"/>
		/// starting at <paramref name="startIndex"/>. If a match is found, returns true and outputs
		/// the matched value and length in characters consumed.
		/// </summary>
		/// <param name="text">Input text to search in.</param>
		/// <param name="startIndex">Start position in <paramref name="text"/>.</param>
		/// <param name="value">Value associated with the matched key (or null if stored value is null).</param>
		/// <param name="matchedLength">Length in characters of the matched key (0 if no match).</param>
		/// <returns>True if at least one stored key matches a prefix of <paramref name="text"/> starting at <paramref name="startIndex"/>.</returns>
		public bool TryGetLongestMatch(string text, int startIndex, out object? value, out int matchedLength)
		{
			return TryGetLongestMatch(text, startIndex, text.Length, out value, out matchedLength);
		}

		/// <summary>
		/// Attempts to find the longest stored key that matches the input <paramref name="text"/>
		/// starting at <paramref name="startIndex"/>. If a match is found, returns true and outputs
		/// the matched value and length in characters consumed.
		/// </summary>
		/// <param name="text">Input text to search in.</param>
		/// <param name="startIndex">Start position in <paramref name="text"/>.</param>
		/// <param name="endIndex">End position in <paramref name="text"/> (exclusive).</param>
		/// <param name="value">Value associated with the matched key (or null if stored value is null).</param>
		/// <param name="matchedLength">Length in characters of the matched key (0 if no match).</param>
		/// <returns>True if at least one stored key matches a prefix of <paramref name="text"/> starting at <paramref name="startIndex"/>.</returns>
		public bool TryGetLongestMatch(string text, int startIndex, int endIndex, out object? value, out int matchedLength)
		{
			value = null;
			matchedLength = 0;

			var node = root;
			int lastTerminalPos = -1;
			object? lastTerminalValue = null;

			// If root itself is terminal => empty key exists
			if (node.isTerminal)
			{
				lastTerminalPos = startIndex;
				lastTerminalValue = node.value;
			}

			// Traverse characters one by one; record the last terminal node encountered.
			for (int i = startIndex; i < endIndex; i++)
			{
				char c = text[i];
				if (!TryGetChild(node, c, out var next))
					break;

				node = next;

				if (node.isTerminal)
				{
					lastTerminalPos = i + 1;
					lastTerminalValue = node.value;
				}
			}

			if (lastTerminalPos >= 0)
			{
				matchedLength = lastTerminalPos - startIndex;
				value = lastTerminalValue;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Attempts to find the longest stored key that matches the input <paramref name="text"/>
		/// starting at <paramref name="startIndex"/>. If a match is found, returns true and outputs
		/// the matched value and length in characters consumed.
		/// </summary>
		/// <param name="text">Input text to search in.</param>
		/// <param name="startIndex">Start position in <paramref name="text"/>.</param>
		/// <param name="value">Value associated with the matched key (or null if stored value is null).</param>
		/// <param name="matchedLength">Length in characters of the matched key (0 if no match).</param>
		/// <returns>True if at least one stored key matches a prefix of <paramref name="text"/> starting at <paramref name="startIndex"/>.</returns>
		public bool TryGetLongestMatch(ReadOnlySpan<char> text, int startIndex, out object? value, out int matchedLength)
		{
			value = null;
			matchedLength = 0;

			var node = root;
			int lastTerminalPos = -1;
			object? lastTerminalValue = null;

			// If root itself is terminal => empty key exists
			if (node.isTerminal)
			{
				lastTerminalPos = startIndex;
				lastTerminalValue = node.value;
			}

			// Traverse characters one by one; record the last terminal node encountered.
			for (int i = startIndex; i < text.Length; i++)
			{
				char c = text[i];
				if (!TryGetChild(node, c, out var next))
					break;

				node = next;

				if (node.isTerminal)
				{
					lastTerminalPos = i + 1;
					lastTerminalValue = node.value;
				}
			}

			if (lastTerminalPos >= 0)
			{
				matchedLength = lastTerminalPos - startIndex;
				value = lastTerminalValue;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Returns all stored keys that match starting at <paramref name="startIndex"/>.
		/// Each result is returned as a pair: (lengthConsumed, value).
		/// Results are yielded in order of increasing length (shorter matches first).
		/// </summary>
		/// <param name="text">Input text.</param>
		/// <param name="startIndex">Start index.</param>
		/// <returns>IEnumerable of (length, value) tuples for every stored key that matches at the position.</returns>
		public IEnumerable<(int length, object? value)> GetAllMatches(string text, int startIndex)
		{
			return GetAllMatches(text, startIndex, text.Length);
		}

		/// <summary>
		/// Returns all stored keys that match starting at <paramref name="startIndex"/>.
		/// Each result is returned as a pair: (lengthConsumed, value).
		/// Results are yielded in order of increasing length (shorter matches first).
		/// </summary>
		/// <param name="text">Input text.</param>
		/// <param name="startIndex">Start index.</param>
		/// <param name="endIndex">Maximum index to consider.</param>
		/// <returns>IEnumerable of (length, value) tuples for every stored key that matches at the position.</returns>
		public IEnumerable<(int length, object? value)> GetAllMatches(string text, int startIndex, int endIndex)
		{
			var node = root;

			if (node.isTerminal)
				yield return (0, node.value);

			for (int i = startIndex; i < endIndex; i++)
			{
				char c = text[i];
				if (!TryGetChild(node, c, out var next))
					yield break;

				node = next;

				if (node.isTerminal)
					yield return (i + 1 - startIndex, node.value);
			}
		}

		/// <summary>
		/// Returns all stored keys that match starting at <paramref name="startIndex"/>.
		/// Each result is returned as a pair: (lengthConsumed, value).
		/// Results are yielded in order of increasing length (shorter matches first).
		/// </summary>
		/// <param name="text">Input text.</param>
		/// <param name="startIndex">Start index.</param>
		/// <returns>IEnumerable of (length, value) tuples for every stored key that matches at the position.</returns>
		public IEnumerable<(int length, object? value)> GetAllMatches(ReadOnlySpan<char> text, int startIndex)
		{
			var node = root;
			var result = new List<(int length, object? value)>();

			if (node.isTerminal)
				result.Add((0, node.value));

			for (int i = startIndex; i < text.Length; i++)
			{
				char c = text[i];
				if (!TryGetChild(node, c, out var next))
					break;

				node = next;

				if (node.isTerminal)
					result.Add((i + 1 - startIndex, node.value));
			}

			return result;
		}

		/// <summary>
		/// Returns whether the substring text[startIndex..endOfText] is a prefix of at least one stored key.
		/// Equivalent to <see cref="IsPrefix(string,int,int)"/> with the remaining length to the end of the string.
		/// </summary>
		/// <param name="text">Input text.</param>
		/// <param name="startIndex">Start index.</param>
		/// <returns>True if the suffix text[startIndex..] is a prefix of some stored key.</returns>
		public bool IsPrefix(string text, int startIndex)
		{
			return IsPrefix(text, startIndex, text.Length);
		}

		/// <summary>
		/// Returns whether the substring text[startIndex..startIndex+length] is a prefix of at least one stored key.
		/// That is, whether walking the trie for the given substring succeeds (regardless of terminal flags).
		/// </summary>
		/// <param name="text">Input text.</param>
		/// <param name="startIndex">Start index in text.</param>
		/// <param name="endIndex">End index in text (exclusive).</param>
		/// <returns>True if the provided substring is a prefix of some stored key.</returns>
		public bool IsPrefix(string text, int startIndex, int endIndex)
		{
			var node = root;

			for (int i = startIndex; i < endIndex; i++)
			{
				char c = text[i];
				if (!TryGetChild(node, c, out var next))
					return false;
				node = next;
			}

			// We succeeded walking all characters; it is a prefix if node exists (we don't require terminal).
			return true;
		}

		/// <summary>
		/// Determines whether the substring of the specified text starting at the given index
		/// is a strict prefix of any key stored in the trie.
		/// </summary>
		/// <remarks>
		/// A strict prefix means that the substring exactly matches the path to a node in the trie,
		/// the node is not terminal, and it has at least one child node. In other words,
		/// the substring can be extended to form at least one complete key in the trie.
		/// </remarks>
		/// <param name="text">Input text to check.</param>
		/// <param name="startIndex">Start index in <paramref name="text"/>.</param>
		/// <returns><see langword="true"/> when the substring is a strict prefix of some stored key; otherwise <see langword="false"/>.</returns>
		public bool IsStrictPrefixOfAny(string text, int startIndex)
		{
			return IsStrictPrefixOfAny(text, startIndex, text.Length);
		}

		/// <summary>
		/// Determines whether the substring of the specified text starting at the given index
		/// is a strict prefix of any key stored in the trie.
		/// </summary>
		/// <remarks>
		/// A strict prefix means that the substring exactly matches the path to a node in the trie,
		/// the node is not terminal, and it has at least one child node. In other words,
		/// the substring can be extended to form at least one complete key in the trie.
		/// </remarks>
		/// <param name="text">Input text to check.</param>
		/// <param name="startIndex">Start index in <paramref name="text"/>.</param>
		/// <param name="endIndex">End index in <paramref name="text"/> (exclusive).</param>
		/// <returns><see langword="true"/> when the substring is a strict prefix of some stored key; otherwise <see langword="false"/>.</returns>
		public bool IsStrictPrefixOfAny(string text, int startIndex, int endIndex)
		{
			var node = root;

			// Walk as far as input goes.
			for (int i = startIndex; i < endIndex; i++)
			{
				char ch = text[i];
				if (!TryGetChild(node, ch, out var child))
					return false; // mismatch => not a prefix at all
				node = child;
			}

			// We consumed the whole remaining input along a trie path.
			// It's a strict prefix if current node is not terminal and it has at least one child.
			return !node.isTerminal && NodeHasAnyChild(node);
		}

		/// <summary>
		/// Determines whether the substring of the specified text starting at the given index
		/// is a strict prefix of any key stored in the trie.
		/// </summary>
		/// <remarks>
		/// A strict prefix means that the substring exactly matches the path to a node in the trie,
		/// the node is not terminal, and it has at least one child node. In other words,
		/// the substring can be extended to form at least one complete key in the trie.
		/// </remarks>
		/// <param name="text">Input text to check.</param>
		/// <param name="startIndex">Start index in <paramref name="text"/>.</param>
		/// <returns><see langword="true"/> when the substring is a strict prefix of some stored key; otherwise <see langword="false"/>.</returns>
		public bool IsStrictPrefixOfAny(ReadOnlySpan<char> text, int startIndex)
		{
			var node = root;

			for (int i = startIndex; i < text.Length; i++)
			{
				char ch = text[i];
				if (!TryGetChild(node, ch, out var child))
					return false;
				node = child;
			}

			return !node.isTerminal && NodeHasAnyChild(node);
		}

		/// <summary>
		/// Checks whether the provided node has any child (regardless of node.type).
		/// This inspects the internal node representation (single/two/three/array/dictionary).
		/// </summary>
		private bool NodeHasAnyChild(TrieNode node)
		{
			if (node == null) return false;

			switch (node.type)
			{
				case TrieNodeType.Terminal:
					return false; // Terminal type stores no children in our design

				case TrieNodeType.SingleChild:
					return node.child1 != null;

				case TrieNodeType.TwoChildren:
					return node.child1 != null || node.child2 != null;

				case TrieNodeType.ThreeChildren:
					return node.child1 != null || node.child2 != null || node.child3 != null;

				case TrieNodeType.Array:
					{
						var arr = node.array;
						if (arr == null) return false;
						// fast scan for any non-null entry
						for (int i = 0; i < arr.Length; i++)
							if (arr[i] != null) return true;
						return false;
					}

				case TrieNodeType.Dictionary:
					return node.dictionary != null && node.dictionary.Count > 0;

				default:
					return false;
			}
		}

		/// <summary>
		/// Returns true if there exists at least one stored key that exactly matches a prefix of the input
		/// starting at <paramref name="startIndex"/>. If found, the method returns the matched length via out parameter.
		/// If multiple matches exist, the longest is returned.
		/// </summary>
		/// <param name="text">Input text.</param>
		/// <param name="startIndex">Start index.</param>
		/// <param name="matchedLength">Matched length (0 when no match).</param>
		/// <returns>True if a stored key matches a prefix of the input.</returns>
		public bool ContainsMatch(string text, int startIndex, out int matchedLength)
		{
			bool ok = TryGetLongestMatch(text, startIndex, out _, out matchedLength);
			return ok;
		}

		/// <summary>
		/// Returns true if there exists at least one stored key that exactly matches a prefix of the input
		/// starting at <paramref name="startIndex"/>. If found, the method returns the matched length via out parameter.
		/// If multiple matches exist, the longest is returned.
		/// </summary>
		/// <param name="text">Input text.</param>
		/// <param name="startIndex">Start index.</param>
		/// <param name="endIndex">End index in text (exclusive).</param>
		/// <param name="matchedLength">Matched length (0 when no match).</param>
		/// <returns>True if a stored key matches a prefix of the input.</returns>
		public bool ContainsMatch(string text, int startIndex, int endIndex, out int matchedLength)
		{
			bool ok = TryGetLongestMatch(text, startIndex, endIndex, out _, out matchedLength);
			return ok;
		}

		/// <summary>
		/// Convenience: returns the matched value for the longest match starting at <paramref name="startIndex"/>,
		/// or <paramref name="defaultValue"/> if none found. Also returns the matched length in <paramref name="matchedLength"/>.
		/// </summary>
		public object? GetValueOrDefault(string text, int startIndex, object? defaultValue, out int matchedLength)
		{
			if (TryGetLongestMatch(text, startIndex, out var value, out matchedLength))
				return value;
			return defaultValue;
		}

		/// <summary>
		/// Convenience: returns the matched value for the longest match starting at <paramref name="startIndex"/>,
		/// or <paramref name="defaultValue"/> if none found. Also returns the matched length in <paramref name="matchedLength"/>.
		/// </summary>
		public object? GetValueOrDefault(string text, int startIndex, int endIndex, object? defaultValue, out int matchedLength)
		{
			if (TryGetLongestMatch(text, startIndex, endIndex, out var value, out matchedLength))
				return value;
			return defaultValue;
		}

		/// <summary>
		/// Traverses the trie node to get the child associated with character <paramref name="c"/>.
		/// This method understands the internal representation of a node (single / two / three / array / dictionary).
		/// </summary>
		/// <param name="node">Current node.</param>
		/// <param name="c">Character to descend by.</param>
		/// <param name="child">Output child node (null if not present).</param>
		/// <returns>True when a child exists for the given character.</returns>
		private bool TryGetChild(TrieNode node, char c, out TrieNode? child)
		{
			child = null;
			if (node == null)
				return false;

			switch (node.type)
			{
				case TrieNodeType.Terminal:
					// No children stored in Terminal mode
					return false;

				case TrieNodeType.SingleChild:
					if (node.char1 == c)
					{
						child = node.child1;
						return child != null;
					}
					return false;

				case TrieNodeType.TwoChildren:
					if (node.char1 == c)
					{
						child = node.child1;
						return child != null;
					}
					if (node.char2 == c)
					{
						child = node.child2;
						return child != null;
					}
					return false;

				case TrieNodeType.ThreeChildren:
					if (node.char1 == c)
					{
						child = node.child1;
						return child != null;
					}
					if (node.char2 == c)
					{
						child = node.child2;
						return child != null;
					}
					if (node.char3 == c)
					{
						child = node.child3;
						return child != null;
					}
					return false;

				case TrieNodeType.Array:
					// array is used only for ASCII (0..255) indices in node implementation
					if (c <= 255)
					{
						var idx = (byte)c;
						child = node.array[idx];
						return child != null;
					}
					return false;

				case TrieNodeType.Dictionary:
					return node.dictionary.TryGetValue(c, out child);

				default:
					return false;
			}
		}
	}
}