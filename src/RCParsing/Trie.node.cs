using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	public partial class Trie
	{
		private enum TrieNodeType
		{
			Terminal,
			SingleChild,
			TwoChildren,
			ThreeChildren,
			Array,
			Dictionary
		}

		private class TrieNode
		{
			public TrieNodeType type;

			public bool isTerminal;
			public object? value;

			public char char1;
			public TrieNode? child1;
			public char char2;
			public TrieNode? child2;
			public char char3;
			public TrieNode? child3;

			public TrieNode?[]? array;
			public Dictionary<char, TrieNode>? dictionary;

			public void Add(string key, object? value, IEqualityComparer<char>? comparer)
			{
				AddInternal(key, 0, value, comparer);
			}

			private void AddInternal(string key, int pos, object? value, IEqualityComparer<char>? comparer)
			{
				if (pos >= key.Length)
				{
					this.isTerminal = true;
					this.value = value;
					return;
				}

				char c = key[pos];

				if (comparer != null)
				{
					EnsureDictionary(comparer);

					if (!dictionary!.TryGetValue(c, out var child))
					{
						child = new TrieNode { type = TrieNodeType.Terminal };
						dictionary.Add(c, child);
					}

					child.AddInternal(key, pos + 1, value, comparer);
					return;
				}

				switch (type)
				{
					case TrieNodeType.Terminal:
						MakeSingleChild(c, key, pos, value, comparer);
						break;

					case TrieNodeType.SingleChild:
						if (c == char1)
						{
							child1!.AddInternal(key, pos + 1, value, comparer);
						}
						else
						{
							char2 = c;
							child2 = new TrieNode { type = TrieNodeType.Terminal };
							type = TrieNodeType.TwoChildren;
							child2.AddInternal(key, pos + 1, value, comparer);
						}
						break;

					case TrieNodeType.TwoChildren:
						if (c == char1)
						{
							child1!.AddInternal(key, pos + 1, value, comparer);
						}
						else if (c == char2)
						{
							child2!.AddInternal(key, pos + 1, value, comparer);
						}
						else
						{
							char3 = c;
							child3 = new TrieNode { type = TrieNodeType.Terminal };
							type = TrieNodeType.ThreeChildren;
							child3.AddInternal(key, pos + 1, value, comparer);
						}
						break;

					case TrieNodeType.ThreeChildren:
						if (c == char1)
						{
							child1!.AddInternal(key, pos + 1, value, comparer);
						}
						else if (c == char2)
						{
							child2!.AddInternal(key, pos + 1, value, comparer);
						}
						else if (c == char3)
						{
							child3!.AddInternal(key, pos + 1, value, comparer);
						}
						else
						{
							bool canUseArray = IsAscii(char1) && IsAscii(char2) && IsAscii(char3) && IsAscii(c);
							if (canUseArray)
							{
								UpgradeThreeToArray();
								AddToArray(c, key, pos, value, comparer);
							}
							else
							{
								UpgradeThreeToDictionary(comparer);
								AddToDictionary(c, key, pos, value, comparer);
							}
						}
						break;

					case TrieNodeType.Array:
						if (!IsAscii(c))
						{
							UpgradeArrayToDictionary(comparer);
							AddToDictionary(c, key, pos, value, comparer);
						}
						else
						{
							AddToArray(c, key, pos, value, comparer);
						}
						break;

					case TrieNodeType.Dictionary:
						if (!dictionary!.TryGetValue(c, out var childNode))
						{
							childNode = new TrieNode { type = TrieNodeType.Terminal };
							dictionary.Add(c, childNode);
						}
						childNode.AddInternal(key, pos + 1, value, comparer);
						break;

					default:
						throw new InvalidOperationException($"Unknown node type {type}");
				}
			}

			private static bool IsAscii(char c) => c <= 255;

			private void MakeSingleChild(char c, string key, int pos, object? value, IEqualityComparer<char>? comparer)
			{
				char1 = c;
				child1 = new TrieNode { type = TrieNodeType.Terminal };
				type = TrieNodeType.SingleChild;
				child1.AddInternal(key, pos + 1, value, comparer);
			}

			private void EnsureDictionary(IEqualityComparer<char>? comparer)
			{
				if (dictionary != null)
					return;

				if (comparer != null)
					dictionary = new Dictionary<char, TrieNode>(comparer);
				else
					dictionary = new Dictionary<char, TrieNode>();

				switch (type)
				{
					case TrieNodeType.SingleChild:
						dictionary[char1] = child1 ?? new TrieNode { type = TrieNodeType.Terminal };
						child1 = null;
						break;
					case TrieNodeType.TwoChildren:
						dictionary[char1] = child1 ?? new TrieNode { type = TrieNodeType.Terminal };
						dictionary[char2] = child2 ?? new TrieNode { type = TrieNodeType.Terminal };
						child1 = child2 = null;
						break;
					case TrieNodeType.ThreeChildren:
						dictionary[char1] = child1 ?? new TrieNode { type = TrieNodeType.Terminal };
						dictionary[char2] = child2 ?? new TrieNode { type = TrieNodeType.Terminal };
						dictionary[char3] = child3 ?? new TrieNode { type = TrieNodeType.Terminal };
						child1 = child2 = child3 = null;
						break;
					case TrieNodeType.Array:
						if (array != null)
						{
							for (int i = 0; i < array.Length; i++)
							{
								var n = array[i];
								if (n != null) dictionary[(char)i] = n;
							}
							array = null;
						}
						break;
				}

				type = TrieNodeType.Dictionary;
			}

			private void UpgradeThreeToArray()
			{
				var a = new TrieNode?[256];
				a[(byte)char1] = child1;
				a[(byte)char2] = child2;
				a[(byte)char3] = child3;
				array = a;

				child1 = child2 = child3 = null;
				type = TrieNodeType.Array;
			}

			private void UpgradeThreeToDictionary(IEqualityComparer<char>? comparer)
			{
				if (comparer != null)
					dictionary = new Dictionary<char, TrieNode>(comparer);
				else
					dictionary = new Dictionary<char, TrieNode>();

				dictionary[char1] = child1 ?? new TrieNode { type = TrieNodeType.Terminal };
				dictionary[char2] = child2 ?? new TrieNode { type = TrieNodeType.Terminal };
				dictionary[char3] = child3 ?? new TrieNode { type = TrieNodeType.Terminal };
				child1 = child2 = child3 = null;

				type = TrieNodeType.Dictionary;
			}

			private void UpgradeArrayToDictionary(IEqualityComparer<char>? comparer)
			{
				if (comparer != null)
					dictionary = new Dictionary<char, TrieNode>(comparer);
				else
					dictionary = new Dictionary<char, TrieNode>();

				if (array != null)
				{
					for (int i = 0; i < array.Length; i++)
					{
						var n = array[i];
						if (n != null)
							dictionary[(char)i] = n;
					}
					array = null;
				}

				type = TrieNodeType.Dictionary;
			}

			private void AddToArray(char c, string key, int pos, object? value, IEqualityComparer<char>? comparer)
			{
				int idx = (byte)c;
				if (array == null)
				{
					array = new TrieNode?[256];
					type = TrieNodeType.Array;
				}

				var child = array[idx];
				if (child == null)
				{
					child = new TrieNode { type = TrieNodeType.Terminal };
					array[idx] = child;
				}
				child.AddInternal(key, pos + 1, value, comparer);
			}

			private void AddToDictionary(char c, string key, int pos, object? value, IEqualityComparer<char>? comparer)
			{
				if (dictionary == null)
				{
					dictionary = new Dictionary<char, TrieNode>();
					type = TrieNodeType.Dictionary;
				}

				if (!dictionary.TryGetValue(c, out var child))
				{
					child = new TrieNode { type = TrieNodeType.Terminal };
					dictionary.Add(c, child);
				}
				child.AddInternal(key, pos + 1, value, comparer);
			}
		}
	}
}