using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RCParsing
{
	/// <summary>
	/// Represents a collection of barrier tokens that can be used to limit the parsing process for unexpected tokens.
	/// </summary>
	public class BarrierTokenCollection
	{
		/// <summary>
		/// The array of barrier tokens.
		/// </summary>
		private IntermediateBarrierToken[][] _barrierTokens;

		/// <summary>
		/// The mapping from current postion to exact barrier token group index. -1 means no one barrier tokens at current position.
		/// </summary>
		private (int index, int maxBarrierIndex)[] _barrierPositionMap;

		/// <summary>
		/// The mapping from current position to next barrier token position. -1 means no more barrier tokens after current position.
		/// </summary>
		private (int position, int maxBarrierIndex)[] _nextPositionMap;

		/// <summary>
		/// The count of barrier token groups.
		/// </summary>
		private int _count;

		/// <summary>
		/// The length of the input string.
		/// </summary>
		private int _inputLength;

		/// <summary>
		/// Fills the barrier token collection with the given barrier tokens and input string.
		/// </summary>
		/// <param name="tokens">The collection of barrier tokens.</param>
		/// <param name="input">The input string.</param>
		/// <param name="parser">The parser used to get the token patterns.</param>
		public void FillWith(IEnumerable<BarrierToken> tokens, string input, Parser parser)
		{
			if (_count > 0)
				throw new InvalidOperationException("Barrier token collection is already filled.");
			if (tokens == null)
				throw new ArgumentNullException(nameof(tokens));

			var sorted = tokens
				.OrderBy(t => t.startIndex)
				.GroupBy(t => t.startIndex)
				.ToList();

			_inputLength = input.Length;

			if (sorted.Count == 0)
			{
				_barrierTokens = null;
				_barrierPositionMap = null;
				_nextPositionMap = null;
				_count = 0;
				return;
			}

			List<IntermediateBarrierToken[]> groups = new();
			List<(int index, int maxBarrierIndex)> startIndices = new();
			int index = 0;
			foreach (var group in sorted)
			{
				var tokensInGroup = new List<IntermediateBarrierToken>();

				foreach (var token in group)
				{
					var id = parser.GetTokenPattern(token.tokenAlias).Id;
					tokensInGroup.Add(new IntermediateBarrierToken(id, index++, token.length, token.tokenAlias));
				}

				startIndices.Add((group.Key, index));
				groups.Add(tokensInGroup.ToArray());
			}

			_barrierTokens = groups.ToArray();
			_count = _barrierTokens.Length;

			int maxPos = input.Length + 1;
			_barrierPositionMap = Enumerable.Repeat((-1, 0), maxPos).ToArray();
			_nextPositionMap = Enumerable.Repeat((-1, 0), maxPos).ToArray();

			for (int i = 0; i < _count; i++)
			{
				var startIndex = startIndices[i];
				if (startIndex.index < maxPos)
					_barrierPositionMap[startIndex.index] = (i, startIndex.maxBarrierIndex);
			}

			int nextBarrierPos = -1;
			int nextMaxIndex = 0;
			for (int pos = maxPos - 1; pos >= 0; pos--)
			{
				var posIndex = _barrierPositionMap[pos];
				if (posIndex.index != -1)
				{
					nextBarrierPos = pos;
					nextMaxIndex = posIndex.maxBarrierIndex;
				}
				_nextPositionMap[pos] = (nextBarrierPos, nextMaxIndex);
			}
		}

		/// <summary>
		/// Tries to get the barrier token at the specified position.
		/// </summary>
		/// <param name="position">The position to get the barrier token from.</param>
		/// <param name="passedBarriers">The starting index of the barrier token to get from.</param>
		/// <param name="token">The barrier token at the specified position.</param>
		/// <returns><see langword="true"/> if a barrier token was found at the specified position, <see langword="false"/> otherwise.</returns>
		public bool TryGetBarrierToken(int position, int passedBarriers, out IntermediateBarrierToken token)
		{
			if (_count == 0)
			{
				token = default;
				return false;
			}

			var index = _barrierPositionMap[position];
			if (index.index == -1 || index.maxBarrierIndex < passedBarriers)
			{
				token = default;
				return false;
			}

			var group = _barrierTokens[index.index];
			for (int i = 0; i < group.Length; i++)
			{
				var t = group[i];
				if (t.index >= passedBarriers)
				{
					token = t;
					return true;
				}
			}

			token = default;
			return false;
		}

		/// <summary>
		/// Gets the next barrier token group index after the specified position. If no more barrier tokens, returns -1.
		/// </summary>
		/// <param name="position">The position to get the next barrier token group index from.</param>
		/// <param name="passedBarriers">The pa</param>
		/// <returns>The index of the next barrier token group, or -1 if no more barrier tokens.</returns>
		public int GetNextBarrierPosition(int position, int passedBarriers)
		{
			if (_count == 0)
				return -1;

			if (position < 0 || position >= _nextPositionMap.Length)
				return -1;

			var next = _nextPositionMap[position];
			if (next.maxBarrierIndex > passedBarriers)
				return next.position;

			return GetNextBarrierPosition(position + 1, passedBarriers);
		}
	}
}