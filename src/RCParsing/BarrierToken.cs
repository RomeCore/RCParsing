using System;
using System.Collections.Generic;
using System.Text;

namespace RCParsing
{
	/// <summary>
	/// Represents a barrier token
	/// </summary>
	public readonly struct BarrierToken
	{
		/// <summary>
		/// Gets the starting index of the barrier token in the input text. <br/>
		/// OR <br/>
		/// Gets the index of the barrier token in the barrier tokens collection.
		/// </summary>
		public readonly int startIndex;

		/// <summary>
		/// Gets the length of the token in the input text.
		/// </summary>
		public readonly int length;

		/// <summary>
		/// Gets the alias of the token pattern for this barrier token.
		/// </summary>
		public readonly string tokenAlias;

		/// <summary>
		/// Initializes a new instance of the <see cref="BarrierToken"/> struct.
		/// </summary>
		/// <param name="startIndex">The starting index of the token in the input text.</param>
		/// <param name="length">The length of the token in the input text.</param>
		/// <param name="tokenAlias">The alias of the token pattern for this barrier token.</param>
		public BarrierToken(int startIndex, int length, string tokenAlias)
		{
			this.startIndex = startIndex;
			this.length = length;
			this.tokenAlias = tokenAlias;
		}
	}

	/// <summary>
	/// Represents a barrier token that was converted for use in parsing processes.
	/// </summary>
	public readonly struct IntermediateBarrierToken
	{
		/// <summary>
		/// Gets the ID of the barrier token.
		/// </summary>
		public readonly int tokenId;

		/// <summary>
		/// Gets the index of the barrier token in the barrier tokens collection.
		/// </summary>
		public readonly int index;

		/// <summary>
		/// Gets the length of the token in the input text.
		/// </summary>
		public readonly int length;

		/// <summary>
		/// Gets the alias of the token pattern for this barrier token.
		/// </summary>
		public readonly string tokenAlias;

		/// <summary>
		/// Initializes a new instance of the <see cref="BarrierToken"/> struct.
		/// </summary>
		/// <param name="tokenId">The ID of the barrier token.</param>
		/// <param name="index">The index of the barrier token in the barrier tokens collection.</param>
		/// <param name="length">The length of the token in the input text.</param>
		/// <param name="tokenAlias">The alias of the token pattern for this barrier token.</param>
		public IntermediateBarrierToken(int tokenId, int index, int length, string tokenAlias)
		{
			this.tokenId = tokenId;
			this.index = index;
			this.length = length;
			this.tokenAlias = tokenAlias;
		}
	}
}